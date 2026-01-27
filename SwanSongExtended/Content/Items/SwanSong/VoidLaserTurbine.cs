using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using SwanSongExtended.Skills;
using SwanSongExtended.States;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using SwanSongExtended.Modules;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class VoidLaserTurbine : ItemBase<VoidLaserTurbine>
    {
        public static bool GetSolenoidConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Items : Super Solenoid Engine");
        }

        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return VoidLaserTurbine.GetSolenoidConfig();
        }
        public static BuffDef turbineChargeBuff;
        public static BuffDef turbineReadyBuff;
        public static float secondsOfChargeRequired = 90;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Super Solenoid Engine"; //Super Solonoid Organ

        public override string ItemLangTokenName => "VOIDLASERTURBINE";

        public override string ItemPickupDesc => "Using skills charges a devastating laser primary attack. " +
            "<style=cIsVoid>Corrupts all Brilliant Behemoths and Resonance Discs.</style>";

        public override string ItemFullDescription => $"Using your skills builds charge. " +
            $"After {secondsOfChargeRequired} seconds worth of charge has accumulated, " +
            $"prime 1 use <style=cStack>(+1 per stack)</style> " +
            $"of <style=cIsVoid>{VoidLaserTurbineSkill._SkillName}</style>, replacing your Primary attack. " +
            $"Firing <style=cIsVoid>{VoidLaserTurbineSkill._SkillName}</style> " +
            $"deals <style=cIsDamage>{Tools.ConvertDecimal(VoidLaserBeam.damageCoefficient)} damage</style>, " +
            $"piercing ALL enemies and terrain. " +
            $"<style=cIsVoid>Corrupts all Brilliant Behemoths and Resonance Discs.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => LoadDropPrefab("mdlVoidLaserTurbine");

        public override Sprite ItemIcon => LoadItemIcon("texIconVoidLaserTurbine");


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }

        public override void Init()
        {
            turbineChargeBuff = Content.CreateAndAddBuff(
                "bdSuperSolenoidCharge",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                new Color(0.5f, 0.0f, 0.4f),
                true, false,
                BuffDef.StackingDisplayMethod.Percentage
                );
            turbineReadyBuff = Content.CreateAndAddBuff(
                "bdSuperSolenoidReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.2f, 0.8f),
                false, false
                );
            base.Init();
        }
        public override void PostInit()
        {
            base.PostInit();
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Behemoth.Behemoth_asset);
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LaserTurbine.LaserTurbine_asset);
        }
    }
    public class VoidTurbineBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => VoidLaserTurbine.instance.ItemsDef;
        GenericSkill primarySkill;
        GenericSkill overriddenSkill;
        SkillDef primaryOverride => VoidLaserTurbineSkill.instance.SkillDef;
        void Start()
        {
            SkillLocator skillLocator = body.skillLocator;
            primarySkill = skillLocator ? skillLocator.primary : null;

            if (primarySkill)
            {
                primarySkill.onSkillChanged += this.TryOverrideSkill;
            }
            body.onSkillActivatedServer += OnSkillActivated;
        }

        private void OnSkillActivated(GenericSkill skill)
        {
            if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
                return;

            if(skill.baseRechargeInterval > 0 && skill.rechargeStock > 0)
            {
                float effectiveCooldown = skill.baseRechargeInterval;
                if (skill.rechargeStock > 1)
                    effectiveCooldown /= skill.rechargeStock;

                int buffsToGrant = Mathf.CeilToInt(Mathf.Floor(effectiveCooldown) * (100 / VoidLaserTurbine.secondsOfChargeRequired));
                if(buffsToGrant > 0)
                {
                    for(int i = 0; i < buffsToGrant; i++)
                    {
                        body.AddBuff(VoidLaserTurbine.turbineChargeBuff);
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (primarySkill)
            {
                if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff) && NetworkServer.active)
                    body.RemoveBuff(VoidLaserTurbine.turbineReadyBuff);

                primarySkill.onSkillChanged -= this.TryOverrideSkill;
                TryOverrideSkill(primarySkill);
            }
            body.onSkillActivatedServer -= OnSkillActivated;
        }
        void FixedUpdate()
        {
            if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
            {
                if(overriddenSkill != null && overriddenSkill.stock > 0)
                {
                    return;
                }
                UnreadyTurbineSkill();
            }
            int chargeCount = body.GetBuffCount(VoidLaserTurbine.turbineChargeBuff);
            if(chargeCount >= 100)
            {
                if (NetworkServer.active)
                {
                    while (chargeCount > 0)
                    {
                        body.RemoveBuff(VoidLaserTurbine.turbineChargeBuff);
                        chargeCount--;
                    }
                }
                ReadyTurbineSkill();
            }
        }

        private void UnreadyTurbineSkill()
        {
            if (NetworkServer.active)
                body.RemoveBuff(VoidLaserTurbine.turbineReadyBuff);

            if (primarySkill)
            {
                this.TryOverrideSkill(primarySkill);
            }
        }

        private void ReadyTurbineSkill()
        {
            body.AddBuff(VoidLaserTurbine.turbineReadyBuff);

            if (primarySkill)
            {
                this.TryOverrideSkill(primarySkill);
            }
        }

        private void TryOverrideSkill(GenericSkill skill)
        {
            if (skill)
            {
                if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
                {
                    if (this.overriddenSkill == null && !skill.HasSkillOverrideOfPriority(GenericSkill.SkillOverridePriority.Contextual))
                    {
                        this.overriddenSkill = skill;
                        this.overriddenSkill.SetSkillOverride(this, this.primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
                        this.overriddenSkill.maxStock = stack;
                        this.overriddenSkill.stock = stack;
                    }
                }
                else
                {
                    if (this.overriddenSkill)
                    {
                        overriddenSkill.UnsetSkillOverride(this, this.primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
                        overriddenSkill = null;
                    }
                }
            }
        }
    }
}
