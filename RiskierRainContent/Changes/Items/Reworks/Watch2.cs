using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RiskierRain.CoreModules.StatHooks;

namespace RiskierRain.Items
{
    class Watch2 : ItemBase<Watch2>
    {
        public static BuffDef watchCritBuff;
        public static float critChanceBonus = 24;
        public static float critChancePerBuff => critChanceBonus / buffTotal;
        public static float buffTotal = 6;
        public static float buffDurationBase = 6f;
        public static float buffDurationStack = 3f;
        public override string ItemName => "Delicate Watch";

        public override string ItemLangTokenName => "WATCH2";

        public override string ItemPickupDesc => "Increase critical strike chance for a short time after being hit. Breaks on low health.";

        public override string ItemFullDescription => $"After getting hit, gain a <style=cIsDamage>{critChanceBonus}%</style> chance " +
            $"to <style=cIsDamage>Critically Strike</style>, fading over " +
            $"<style=cIsDamage>{buffDurationBase} seconds</style> <style=cStack>(+{buffDurationStack} per stack)</style>. " +
            $"Taking damage to below <style=cIsHealth>25% health</style> <style=cIsUtility>breaks</style> this item.";

        public override string ItemLore => "The wind blows over the plains. Two soldiers trudge along on their routine patrol, filling their boredom with casual conversation. \"Hm. Hey, Shelly, have I ever showed you my new Patex?\" Quinton mused. Shelly shot him a quizzical look. \"Tell me you didn't bring a $75,000 watch with you on a dangerous expedition into unknown territories...\"\n\nWhirling around to face his partner, Quinton gave a hearty laugh. \"Why, yes I did!\" Rolling up his sleeve, a glint of gold revealed his collector's watch. The metal surface gleamed proudly, reflecting a ray of sunlight into the eyes of a hidden Lemurian. \"What's the point of going through trials and tribulations in the middle of nowhere if I can't STYLE all over my fellow soldiers!?\" Quinton laughed, pounding his fist to his chest. \"I'm going to rub it in your face SO HARD when that thing inevitably breaks,\" Shelly chuckled. Quinton scoffed. \"Oh, please. We've been along this route countless times, and nothing's happened. We're lucky to be stationed on a quiet sector of this hellhole, and I doubt our luck will run out any time soon.\"\n\nAs if on cue, the aggrivated Lemurian, annoyed by the glare, leapt from the bushes and shot a fireball. \"Woah!\" Shelly shouted, raising her gun and killing the beast. \"Hah... So much about a quiet, sector, huh?\" Shelly turned to her partner, who was doubled over on the ground. Shelly's face blanched.\n\n\"Oh no... Were you hit? We need to get you to a medic, fast...!\"\n\n\"No.\" Quinton's voice was small and full of grief. \"I'm perfectly fine, but...\" Quinton looked up, revealing his gleaming Patex, having taken the fireball dead-on, had been reduced to a mangled mess of twisted metal and smoking polish. \"L-Look what that BEAST did to my precious watch!\"\n\nFor a moment, all was quiet on the plains. Then, the silence was yet again broken by Shelly's laughter and Quinton's desperate pleading.";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[]{ ItemTag.Damage };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += WatchCritChance;
            GetHitBehavior += WatchGetHit;
            //On.RoR2.HealthComponent.UpdateLastHitTime += WatchBreak;
        }

        private void WatchBreak(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            orig(self, damageValue, damagePosition, damageIsSilent, attacker);

            CharacterBody body = self.body;
            if (NetworkServer.active && body && damageValue > 0f)
            {
                int watchCount = GetCount(body);
                if(watchCount > 0 && self.isHealthLow)
                {
                    body.inventory.GiveItem(DLC1Content.Items.FragileDamageBonusConsumed, watchCount);
                    body.inventory.RemoveItem(Watch2.instance.ItemsDef, watchCount);
                    CharacterMasterNotificationQueue.SendTransformNotification(body.master, 
                        Watch2.instance.ItemsDef.itemIndex, DLC1Content.Items.FragileDamageBonusConsumed.itemIndex, 
                        CharacterMasterNotificationQueue.TransformationType.Default);
                    EffectData effectData2 = new EffectData
                    {
                        origin = self.transform.position
                    };
                    effectData2.SetNetworkedObjectReference(self.gameObject);
                    EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData2, true);
                }
            }
        }

        private void WatchGetHit(CharacterBody body, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.procCoefficient > 0 && damageInfo.damage > 0 && !damageInfo.rejected)
            {
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();
                if (victimBody)
                {
                    Inventory inv = victimBody.inventory;
                    if (inv)
                    {
                        int itemCount = inv.GetItemCount(DLC1Content.Items.FragileDamageBonus);
                        if (itemCount > 0)
                        {
                            victimBody.ClearTimedBuffs(watchCritBuff);
                            float duration = buffDurationStack * (itemCount - 1) + buffDurationBase;
                            for (int i = 0; i < buffTotal; i++)
                            {
                                victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration * (float)(i + 1) / (float)buffTotal);
                            }
                            //victimBody.AddTimedBuffAuthority(watchCritBuff.buffIndex, duration);
                        }
                    }
                }
            }
        }

        private void WatchCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(watchCritBuff);
            args.critAdd += critChancePerBuff * buffCount;

            if (sender.inventory?.GetItemCount(DLC1Content.Items.FragileDamageBonusConsumed) > 0)
                args.critAdd += 2;
        }

        public override void Init(ConfigFile config)
        {
            //RiskierRainContent.RetierItem(DLC1Content.Items.FragileDamageBonus);
            CreateBuff();
            //CreateItem();
            //CreateLang();
            Hooks();
            LanguageAPI.Add("ITEM_FRAGILEDAMAGEBONUS_PICKUP", ItemPickupDesc);
            LanguageAPI.Add("ITEM_FRAGILEDAMAGEBONUS_DESC", ItemFullDescription);
        }
        private void CreateBuff()
        {
            watchCritBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                watchCritBuff.buffColor = Color.yellow;
                watchCritBuff.canStack = true;
                watchCritBuff.isDebuff = false;
                watchCritBuff.name = "DelicateWatchCritChance";
                watchCritBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffFullCritIcon");
            };
            Assets.buffDefs.Add(watchCritBuff);
        }
    }
}
