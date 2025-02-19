using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.HitHooks;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class Watch2 : ItemBase<Watch2>
    {
        #region config
        public override string ConfigName => "Reworks : Delicate Watch";
        [AutoConfig("Base Attack Speed Bonus", 0.35f)]
        public static float aspdBonusBase = 0.35f;
        [AutoConfig("Stack Attack Speed Bonus", 0.25f)]
        public static float aspdBonusStack = 0.25f;
        [AutoConfig("Stationary Wait Time", 1f)]
        public static float watchWaitTime = 1;
        #endregion
        public override AssetBundle assetBundle => null;
        public static BuffDef watchAspdBuff;
        public override string ItemName => "Delicate Watch";

        public override string ItemLangTokenName => "WATCH2";

        public override string ItemPickupDesc => "Greatly increase attack speed after standing still for 1 second. Breaks on low health.";

        public override string ItemFullDescription => $"After standing still for {DamageColor(watchWaitTime.ToString())} second, " +
            $"increase {DamageColor("attack speed")} by {ConvertDecimal(aspdBonusBase)} {StackText(ConvertDecimal(aspdBonusStack) + " per stack")}. " +
            $"Taking damage to below <style=cIsHealth>25% health</style> <style=cIsUtility>breaks</style> this item.";

        public override string ItemLore => "The wind blows over the plains. Two soldiers trudge along on their routine patrol, filling their boredom with casual conversation. \"Hm. Hey, Shelly, have I ever showed you my new Patex?\" Quinton mused. Shelly shot him a quizzical look. \"Tell me you didn't bring a $75,000 watch with you on a dangerous expedition into unknown territories...\"\n\nWhirling around to face his partner, Quinton gave a hearty laugh. \"Why, yes I did!\" Rolling up his sleeve, a glint of gold revealed his collector's watch. The metal surface gleamed proudly, reflecting a ray of sunlight into the eyes of a hidden Lemurian. \"What's the point of going through trials and tribulations in the middle of nowhere if I can't STYLE all over my fellow soldiers!?\" Quinton laughed, pounding his fist to his chest. \"I'm going to rub it in your face SO HARD when that thing inevitably breaks,\" Shelly chuckled. Quinton scoffed. \"Oh, please. We've been along this route countless times, and nothing's happened. We're lucky to be stationed on a quiet sector of this hellhole, and I doubt our luck will run out any time soon.\"\n\nAs if on cue, the aggrivated Lemurian, annoyed by the glare, leapt from the bushes and shot a fireball. \"Woah!\" Shelly shouted, raising her gun and killing the beast. \"Hah... So much about a quiet, sector, huh?\" Shelly turned to her partner, who was doubled over on the ground. Shelly's face blanched.\n\n\"Oh no... Were you hit? We need to get you to a medic, fast...!\"\n\n\"No.\" Quinton's voice was small and full of grief. \"I'm perfectly fine, but...\" Quinton looked up, revealing his gleaming Patex, having taken the fireball dead-on, had been reduced to a mangled mess of twisted metal and smoking polish. \"L-Look what that BEAST did to my precious watch!\"\n\nFor a moment, all was quiet on the plains. Then, the silence was yet again broken by Shelly's laughter and Quinton's desperate pleading.";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[]{ ItemTag.Damage };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/FragileDamageBonus/PickupDelicateWatch.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/FragileDamageBonus/texDelicateWatchIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += WatchAspdBuff;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.HealthComponent.UpdateLastHitTime += WatchBreak;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<WatchItemBehavior>(GetCount(self));
            }
        }

        private void WatchAspdBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int watchCount = GetCount(sender);
            if(watchCount > 0)
            {
                float aspdIncrease = 0;
                if (sender.HasBuff(watchAspdBuff))
                {
                    aspdIncrease += aspdBonusBase + aspdBonusStack * (watchCount - 1);
                }

                args.attackSpeedMultAdd += aspdIncrease;
            }
        }

        private void WatchBreak(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, HealthComponent self, 
            float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker, 
            bool delayedDamage, bool firstHitOfDelayedDamage)
        {
            orig(self, damageValue, damagePosition, damageIsSilent, attacker, delayedDamage, firstHitOfDelayedDamage);

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
        public override void Init()
        {
            SwanSongPlugin.RetierItem(nameof(DLC1Content.Items.FragileDamageBonus));
            watchAspdBuff = Content.CreateAndAddBuff("bdWatchAspd",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.yellow,
                true, false);

            base.Init();
        }
    }
    public class WatchItemBehavior : CharacterBody.ItemBehavior
    {
        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                float notMovingStopwatch = this.body.notMovingStopwatch;

                if (stack > 0 && notMovingStopwatch >= Watch2.watchWaitTime)
                {
                    if (!body.HasBuff(Watch2.watchAspdBuff))
                    {
                        this.body.AddBuff(Watch2.watchAspdBuff);
                        return;
                    }
                }
                else if (body.HasBuff(Watch2.watchAspdBuff))
                {
                    body.RemoveBuff(Watch2.watchAspdBuff);
                }
            }
        }

        private void OnDisable()
        {
            this.body.RemoveBuff(Watch2.watchAspdBuff);
        }
    }
}
