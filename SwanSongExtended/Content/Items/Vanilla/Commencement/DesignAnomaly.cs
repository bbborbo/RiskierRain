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
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class DesignAnomaly : ItemBase<DesignAnomaly>
    {
        #region config
        public override string ConfigName => "Items : Commencement : Relic of Design";

        [AutoConfig("Bonus Armor", 20)]
        public static int bonusArmor = 20;
        [AutoConfig("Backstab Damage Reduction", 0.4f)]
        public static float backstabDamageReduction = 0.4f;
        #endregion
        public static BuffDef beetleArmor;
        public static int maxBeetleArmorStacks = 3;
        public static int durationPerBeetleArmor = 3;
        public static int armorPerBuffBase = 50;
        public static int armorPerBuffStack = 25;
        public static float retaliateCrippleDuration = 9f;
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        public override string ItemName => "Relic of Design";

        public override string ItemLangTokenName => "DESIGNANOMALY";

        public override string ItemPickupDesc => "Periodically gain protection from damage.";

        public override string ItemFullDescription => $"After not taking damage for {UtilityColor($"{maxBeetleArmorStacks}")} seconds, " +
            $"gain a layer of {DamageColor("Chimera Armor")}, up to {UtilityColor($"{maxBeetleArmorStacks}")} times. " +
            $"Each layer of {DamageColor("Chimera Armor")} " +
            $"increases {HealingColor("armor")} by {HealingColor($"{armorPerBuffBase}")} {StackText("+" + armorPerBuffStack)}. " +
            $"Taking damage while protected strips 1 layer of {DamageColor("Chimera Armor")}, " +
            $"{DamageColor("Crippling")} the enemy who attacked you for {retaliateCrippleDuration}s.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/designanomaly.png");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal };


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
            beetleArmor = Content.CreateAndAddBuff("bdDesignArmor",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(),
                Color.cyan, true, false);
        }

        public override void Hooks()
        {
            //On.RoR2.HealthComponent.TakeDamageProcess += BackstabDamageReduction;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetStatCoefficients += ArmorBoost;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<DesignAnomalyBehavior>(GetCount(self));
            }
        }

        private void ArmorBoost(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            int buffCount = sender.GetBuffCount(beetleArmor);
            if(itemCount > 0 && buffCount > 0)
            {
                int armorPerBuff = armorPerBuffBase + armorPerBuffStack * (itemCount - 1);
                args.armorAdd += armorPerBuff * buffCount;
            }
        }

        private void BackstabDamageReduction(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if(damageInfo.damage > 0 && damageInfo.attacker)
            {
                CharacterBody victimBody = self.body;
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (victimBody && attackerBody) 
                {
                    int itemCount = GetCount(self.body);
                    if (attackerBody)
                    {
                        Vector3 vector = attackerBody.corePosition - damageInfo.position;
                        if (itemCount > 0 && (damageInfo.procChainMask.HasProc(ProcType.Backstab) || BackstabManager.IsBackstab(-vector, victimBody)))
                        {
                            float dmr = Mathf.Pow(1 - backstabDamageReduction, itemCount);
                            damageInfo.damage *= dmr;

                            Debug.Log($"Design Anomaly reduced damage by {Tools.ConvertDecimal(dmr)}!");
                        }
                    }
                }
            }
            orig(self, damageInfo);
        }
    }

    public class DesignAnomalyBehavior : CharacterBody.ItemBehavior
    {
        float beetleArmorInterval = DesignAnomaly.durationPerBeetleArmor;
        float beetleArmorStopwatch;
        void OnBeetleArmorGained()
        {
            GlobalEventManager.onServerDamageDealt += OnServerDamageDealt;
        }
        void OnBeetleArmorRemoved()
        {
            GlobalEventManager.onServerDamageDealt -= OnServerDamageDealt;
        }

        private void OnServerDamageDealt(DamageReport damageReport)
        {
            if (damageReport.victimBody != this.body || damageReport.attackerBody == this.body)
                return;
            if (damageReport.damageInfo.procCoefficient == 0)
                return;
            if (damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.Silent))
                return;

            int buffCount = body.GetBuffCount(DesignAnomaly.beetleArmor);
            if (buffCount > 0)
            {
                body.RemoveBuff(DesignAnomaly.beetleArmor);
                if (damageReport.attackerBody)
                    damageReport.attackerBody.AddTimedBuff(RoR2Content.Buffs.Cripple, DesignAnomaly.retaliateCrippleDuration);
            }
            if(buffCount <= 1)
            {
                OnBeetleArmorRemoved();
            }
        }

        private void FixedUpdate()
        {
            if (!NetworkServer.active)
                return;
            int buffCount = body.GetBuffCount(DesignAnomaly.beetleArmor);
            if(buffCount >= DesignAnomaly.maxBeetleArmorStacks)
            {
                beetleArmorStopwatch = beetleArmorInterval;
                return;
            }
            beetleArmorStopwatch -= Time.fixedDeltaTime;
            if(beetleArmorStopwatch <= 0)
            {
                beetleArmorStopwatch += beetleArmorInterval;
                body.AddBuff(DesignAnomaly.beetleArmor);
                if (buffCount == 0)
                    OnBeetleArmorGained();
            }
        }

        private void OnDisable()
        {
            int buffCount = body.GetBuffCount(DesignAnomaly.beetleArmor);
            if(buffCount > 0)
            {
                while (buffCount > 0)
                {
                    buffCount--;
                    this.body.RemoveBuff(DesignAnomaly.beetleArmor);
                }
                OnBeetleArmorRemoved();
            }
        }
    }
}
