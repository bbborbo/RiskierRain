using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class Elixir2 : ItemBase<Elixir2>
    {
        #region config
        public override string ConfigName => "Reworks : Elixir";

        [AutoConfig("Barrier Fraction On Use", 1)]
        public static float barrierFraction = 1;
        [AutoConfig("Move Speed Bonus", 0.14f)]
        public static float moveSpeedBuff = 0.14f;
        [AutoConfig("Attack Speed Bonus", 0.15f)]
        public static float attackSpeedBuff = 0.15f;
        [AutoConfig("Cooldown Reduction Bonus", 0.06f)]
        public static float cooldownReduction = 0.06f;

        public float buffDurationBase = 0;
        public float buffDurationStack = 0;
        public float damageBuff = 0.8f;
        public float msBuff = 0.45f;
        public int armorBuff = 60;

        public float instantHeal = 0.35f; //0.75f
        #endregion
        public override AssetBundle assetBundle => null;
        public static BuffDef brewActiveBuff;
        public override string ItemName => "Berserker\u2019s Brew";

        public override string ItemLangTokenName => "LEGALLYDISTINCTELIXIR";

        public override string ItemPickupDesc => "At low health, gain barrier, cleanse debuffs, and reset all cooldowns. Usable once per stage.";

        public override string ItemFullDescription => 
            $"Taking damage to below " +
            $"<style=cIsHealth>{Tools.ConvertDecimal(0.25f)} health</style> " +
            $"<style=cIsUtility>consumes</style> this item, " +
            $"instantly granting <style=cIsHealing>{Tools.ConvertDecimal(barrierFraction)}</style> " +
            $"of maximum health in <style=cIsHealing>barrier</style>, " +
            $"<style=cIsUtility>cleansing</style> all debuffs, " +
            $"and <style=cIsUtility>resetting</style> all cooldowns. " +
            $"Each empty bottle increases attack speed by <style=cIsDamage>{Tools.ConvertDecimal(attackSpeedBuff)}</style>, " +
            $"movement speed by <style=cIsDamage>{Tools.ConvertDecimal(moveSpeedBuff)}</style>, " +
            $"and reduces cooldowns by <style=cIsDamage>{Tools.ConvertDecimal(cooldownReduction)}</style>. " +
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

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/HealingPotion/PickupHealingPotion.prefab").WaitForCompletion(); 
        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/HealingPotion/texHealingPotion.png").WaitForCompletion();
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.Healing, ItemTag.LowHealth, ItemTag.OnStageBeginEffect };
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            SwanSongPlugin.RetierItem(nameof(DLC1Content.Items.HealingPotion));
            brewActiveBuff = Content.CreateAndAddBuff(
                "bdBerserkerBrewActive",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion(),//replace me
                new Color(1f, 0.3f, 0.1f),
                false, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.HealingPotion);
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
            float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker, 
            bool delayedDamage, bool firstHitOfDelayedDamage)
        {
            CharacterBody body = self.body;
            if (NetworkServer.active && body && damageValue > 0f)
            {
                int count = GetCount(body);
                if(count > 0 && self.isHealthLow)
                {
                    float buffDuration = buffDurationBase + buffDurationStack * (count - 1);
                    if(buffDuration > 0)
                        body.AddTimedBuff(brewActiveBuff, buffDuration);

                    self.AddBarrier(body.maxHealth * barrierFraction);
                    body.skillLocator.ApplyAmmoPack();
                    Util.CleanseBody(body, true, false, true, true, true, true);

                    TransformPotions(count, body);

                    EffectData effectData = new EffectData
                    {
                        origin = self.transform.position
                    };
                    effectData.SetNetworkedObjectReference(self.gameObject);
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HealingPotionEffect"), effectData, true);
                }
            }
            orig(self, damageValue, damagePosition, damageIsSilent, attacker, delayedDamage, firstHitOfDelayedDamage);
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
    }
}
