﻿using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RiskierRainContent.CoreModules.StatHooks;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Items
{
    class NewLopper : ItemBase
    {
        internal static float maxHealthThreshold = 0.3f;
        int freeCritChance = 15;
        float freeCritDamage = 0.1f;
        int dangerCritChance = 50;
        float bonusCritDamageLowHealthBase = 0;
        float bonusCritDamageLowHealthStack = 2.5f;
        public static float rampageExtendTime = 4;

        public static BuffDef dangerCritBuff;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "The New Lopper";

        public override string ItemLangTokenName => "DANGERCRIT";

        public override string ItemPickupDesc => "Massively increase 'Critical Strike' damage at low health.";

        public override string ItemFullDescription => $"Gain <style=cIsDamage>{freeCritChance}% critical chance.</style> " +
            $"Falling below <style=cIsHealth>{Tools.ConvertDecimal(maxHealthThreshold)} health</style> sends you into a rampage, increasing " +
            $"<style=cIsDamage>critical strike damage by {Tools.ConvertDecimal(bonusCritDamageLowHealthBase + bonusCritDamageLowHealthStack)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(bonusCritDamageLowHealthStack)} per stack)</style>, and " +
            $"<style=cIsDamage>critical strike chance by {dangerCritChance - freeCritChance}%</style>. " +
            $"Killing enemies <style=cIsDamage>extends</style> the rampage for <style=cIsDamage>{rampageExtendTime} seconds</style>.";

        public override string ItemLore =>
@"Name: [REDACTED]
Date of birth: June 21st, 1961
Occupation: Executioner

Time of death: August 23rd, 2058
Location of death: [REDACTED], Mercury
Cause of death: Severe blood loss due to bodily mutilation

Notes:
An axe in pristine condition was found next to the body. Examination determined the weapon to be highly resistant to both wear and stains, particularly of blood. The axe’s construction is consistent with a smith on Sues Drive, Jupiter, that [REDACTED] was in contact with shortly before his death. 

[REDACTED] committed 126 known homicides in the hour preceding his death. Every victim was killed by decapitation and had no other wounds. Witnesses report [REDACTED] accidentally struck himself and bisected his body at the waist, bleeding to death moments later. 

Autopsy reveals degradation of internal organs predating [REDACTED]’s death. Symptoms appear to be consistent with certain strains of [REDACTED] found on Jupiter. No degradation was found in the brain, however it is possible that the illness influenced his mental state.
";

        public override ItemTier Tier => ItemTier.Tier3;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.LowHealth };

        public override GameObject ItemModel => LoadDropPrefab("NewLopper");

        public override Sprite ItemIcon => LoadItemIcon("texIconNewLopper");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetStatCoefficients += this.GiveBonusCrit;
        }

        private void GiveBonusCrit(CharacterBody sender, StatHookEventArgs args)
        {
            int count = GetCount(sender);
            if (count > 0)
            {
                int critAdd = freeCritChance;
                float critDmgAdd = 0;

                if (sender.HasBuff(dangerCritBuff))
                {
                    critAdd = dangerCritChance;
                    critDmgAdd = bonusCritDamageLowHealthBase + bonusCritDamageLowHealthStack * count;
                }
                args.critAdd += critAdd;
                args.critDamageMultAdd += critDmgAdd;
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.healthComponent != null)
                {
                    self.AddItemBehavior<NewLopperBehavior>(GetCount(self));
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }


        private void CreateBuff()
        {
            dangerCritBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                dangerCritBuff.buffColor = Color.black;
                dangerCritBuff.canStack = false;
                dangerCritBuff.isDebuff = false;
                dangerCritBuff.name = "NewLopperCritBonus";
                dangerCritBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion();
            };
            CoreModules.Assets.buffDefs.Add(dangerCritBuff);
        }
    }

    public class NewLopperBehavior : RoR2.CharacterBody.ItemBehavior
    {
        BuffIndex dangerCrit => NewLopper.dangerCritBuff.buffIndex;
        bool isLowHealth = false;
        void Start()
        {
            GlobalEventManager.onCharacterDeathGlobal += ExtendRampage;
        }

        private void ExtendRampage(DamageReport damageReport)
        {
            CharacterBody attackerBody = damageReport.attackerBody;
            if(attackerBody == this.body)
            {
                if(this.body.HasBuff(dangerCrit) && !isLowHealth)
                {
                    this.body.RemoveOldestTimedBuff(dangerCrit);
                    this.body.AddTimedBuffAuthority(dangerCrit, NewLopper.rampageExtendTime);
                }
            }
        }

        void FixedUpdate()
        {
            if(stack > 0)
            {
                float combinedHealthFraction = this.body.healthComponent.combinedHealthFraction;
                //int buffCount = this.body.GetBuffCount(dangerCrit);


                if (combinedHealthFraction <= NewLopper.maxHealthThreshold && !isLowHealth)
                {
                    if (this.body.HasBuff(dangerCrit))
                    {
                        this.body.RemoveOldestTimedBuff(dangerCrit);
                    }

                    this.body.AddBuff(dangerCrit);
                    isLowHealth = true;
                }
                else if (isLowHealth)
                {
                    isLowHealth = false;
                    this.body.RemoveBuff(dangerCrit);
                    this.body.AddTimedBuffAuthority(dangerCrit, NewLopper.rampageExtendTime);
                }
            }
        }
        void OnDestroy()
        {
            GlobalEventManager.onCharacterDeathGlobal -= ExtendRampage;

            if (isLowHealth)
                this.body.RemoveBuff(dangerCrit);
        }
    }
}
