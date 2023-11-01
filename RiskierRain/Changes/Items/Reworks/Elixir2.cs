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

namespace RiskierRain.Items
{
    class Elixir2 : ItemBase<Elixir2>
    {
        public static BuffDef brewActiveBuff;
        public float buffDurationBase = 15f;
        public float buffDurationStack = 7.5f;
        public float damageBuff = 0.8f;
        public float msBuff = 0.45f;
        public int armorBuff = 60;

        public float instantHeal = 0.35f; //0.75f
        public override string ItemName => "Berserker\u2019s Brew";

        public override string ItemLangTokenName => "LEGALLYDISTINCTELIXIR";

        public override string ItemPickupDesc => "Receive healing and a massive stat boost at low health. Usable once per stage.";

        public override string ItemFullDescription => 
            $"Taking damage to below " +
            $"<style=cIsHealth>{Tools.ConvertDecimal(0.25f)} health</style> " +
            $"<style=cIsUtility>consumes</style> this item, " +
            $"instantly restoring <style=cIsHealing>{Tools.ConvertDecimal(instantHeal)}</style> " +
            $"of <style=cIsHealing>maximum health</style>. " +
            $"Consumption also grants a boost " +
            $"for {buffDurationBase} <style=cStack>(+{buffDurationStack} per stack)</style> seconds, " +
            $"increasing <style=cIsDamage>damage</style> by <style=cIsDamage>+{Tools.ConvertDecimal(damageBuff)}</style>, " +
            $"<style=cIsUtility>movement speed</style> by <style=cIsUtility>+{Tools.ConvertDecimal(msBuff)}</style>, " +
            $"and <style=cIsDamage>armor</style> by <style=cIsDamage>+{armorBuff}</style>. " +
            $"Regenerates at the start of each stage.";

        public override string ItemLore => 
            $"Order: 16 oz. Flask, Healing Potion (Not my Strongest)" +
            $"\r\nTracking Number: 10******" +
            $"\r\nEstimated Delivery: 12/31/2058" +
            $"\r\nShipping Method: Priority" +
            $"\r\nShipping Address: Cargo Bay 10-C, Terminal 504-B, UES Port Trailing Comet" +
            $"\nShipping Details: " +
            $"\n- Re: Potion seller, I am going into battle. I require your strongest potions." +
            $"\n\nMy potions are too strong for you, buyer. I've instead downgraded you to a weaker brew. " +
            $"My strongest potions would kill a dragon, let alone a man!";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel"); 

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.Healing, ItemTag.LowHealth, ItemTag.OnStageBeginEffect };

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += ElixirHook;
            GetStatCoefficients += BerserkerBrewBuff;
        }

        private void BerserkerBrewBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(brewActiveBuff))
            {
                args.armorAdd += armorBuff;
                args.moveSpeedMultAdd += msBuff;
                args.damageMultAdd += damageBuff;
            }
        }

        private void ElixirHook(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, RoR2.HealthComponent self, 
            float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker)
        {
            CharacterBody body = self.body;
            if (NetworkServer.active && body && damageValue > 0f)
            {
                int count = GetCount(body);
                if(count > 0 && self.isHealthLow)
                {
                    float buffDuration = buffDurationBase + buffDurationStack * (count - 1);
                    body.AddTimedBuff(brewActiveBuff, buffDuration);
                    self.HealFraction(instantHeal, default(ProcChainMask));

                    TransformPotions(count, body);

                    EffectData effectData = new EffectData
                    {
                        origin = self.transform.position
                    };
                    effectData.SetNetworkedObjectReference(self.gameObject);
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HealingPotionEffect"), effectData, true);
                }
            }
            orig(self, damageValue, damagePosition, damageIsSilent, attacker);
        }

        private void TransformPotions(int count, CharacterBody body)
        {
            Inventory inv = body.inventory;
            inv.RemoveItem(instance.ItemsDef, count);
            inv.GiveItem(Elixir2Consumed.instance.ItemsDef, count);

            CharacterMasterNotificationQueue.SendTransformNotification(
                body.master, instance.ItemsDef.itemIndex,
                Elixir2Consumed.instance.ItemsDef.itemIndex, 
                CharacterMasterNotificationQueue.TransformationType.Default);
        }

        public override void Init(ConfigFile config)
        {
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.HealingPotion));
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            brewActiveBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                brewActiveBuff.name = "BerserkerBrewActive";
                brewActiveBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texmovespeedbufficon");
                brewActiveBuff.buffColor = new Color(1f, 0.3f, 0.1f);
                brewActiveBuff.canStack = false;
                brewActiveBuff.isDebuff = false;
            };
            Assets.buffDefs.Add(brewActiveBuff);
        }
    }
}
