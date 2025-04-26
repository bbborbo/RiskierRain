using EntityStates;
using EntityStates.VoidSurvivor.Weapon;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.States.VoidFiend;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.SurvivorTweaks
{
    class ViendTweaks : SurvivorTweakModule
    {
        static float corruptModeArmor = 25;

        static float minimumCorruptionPerVoidItem = 2; //2
        static float corruptionForFullDamage = 50; //50
        static float corruptionForFullHeal = -100; //-100
        static float corruptionFractionPerSecondWhileCorrupted = -0.04f; //aka 25s; -0.06666667f aka 15s
        static float corruptionPerSecondInCombat = 1.5f; //aka 66.6s; 3 aka 33.3s
        static float corruptionPerSecondOutOfCombat = 1.5f; //3
        static float corruptionPerCrit = 2; //2
        static float maxCorruption = 100; //100

        public static float primaryUnchargedDamage = 0.9f;
        public static float primaryChargedDamage = 4.8f;

        public static float primaryCorruptDps = 20;
        public static float primaryCorruptTickRate = 8;

        public static float secondaryCooldown = 5f; //4f

        public override string survivorName => "Void Fiend";
        public override string bodyName => "VoidSurvivorBody";


        public override void Init()
        {
            GetBodyObject();
            //CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            //body.
            GetStatCoefficients += ViendStatCoefficients;

            #region passive
            On.RoR2.VoidSurvivorController.OnEnable += VoidSurvivorController_OnEnable;
            //On.RoR2.Skills.VoidSurvivorSkillDef.HasRequiredCorruption += VoidSurvivorSkillDef_HasRequiredCorruption;
            #endregion

            #region primary
            SkillDef viendPrimary = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/FireHandBeam.asset").WaitForCompletion();
            CoreModules.Assets.RegisterEntityState(typeof(FireHandBeamNew));
            SerializableEntityStateType newViendPrimaryCharge = new SerializableEntityStateType(typeof(FireHandBeamNew));
            viendPrimary.activationState = newViendPrimaryCharge;
            LanguageAPI.Add("VOIDSURVIVOR_PRIMARY_DESCRIPTION", 
                $"Charge a <style=cIsUtility>slowing</style> long-range beam for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(primaryUnchargedDamage)}-{Tools.ConvertDecimal(primaryChargedDamage)} damage</style>.");

            On.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.OnEnter += FireCorruptHandBeam_OnEnter;
            LanguageAPI.Add("VOIDSURVIVOR_PRIMARY_UPRADE_TOOLTIP",
                $"<style=cKeywordName>【Corruption Upgrade】</style><style=cSub>Transform into a " +
                $"{Tools.ConvertDecimal(primaryCorruptDps)} damage short-range beam.</style>");
            #endregion

            #region secondary
            SkillDef viendSecondary = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/ChargeMegaBlaster.asset").WaitForCompletion();
            viendSecondary.cancelSprintingOnActivation = false;
            viendSecondary.beginSkillCooldownOnSkillEnd = true;
            viendSecondary.baseRechargeInterval = secondaryCooldown;
            viendSecondary.keywordTokens = new string[]{ "VOIDSURVIVOR_SECONDARY_UPRADE_TOOLTIP", "KEYWORD_AGILE" };

            LanguageAPI.Add("VOIDSURVIVOR_SECONDARY_DESCRIPTION",
                "<style=cIsUtility>Agile.</style> " +
                "Fire a plasma bolt for <style=cIsDamage>600% damage</style>. " +
                "Fully charge it for an explosive plasma ball instead, " +
                "dealing <style=cIsDamage>1100% damage</style>.");

            SkillDef viendSecondaryCorrupt = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/FireCorruptDisk.asset").WaitForCompletion();
            viendSecondaryCorrupt.cancelSprintingOnActivation = false;
            viendSecondaryCorrupt.beginSkillCooldownOnSkillEnd = true;
            viendSecondaryCorrupt.baseRechargeInterval = secondaryCooldown;
            #endregion

            #region special
            SkillDef viendSpecialHeal = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/CrushCorruption.asset").WaitForCompletion();
            if (viendSpecialHeal)
            {
                viendSpecialHeal.baseMaxStock = 2;
                viendSpecialHeal.rechargeStock = 1;
                viendSpecialHeal.baseRechargeInterval = 45;
            }
            On.EntityStates.VoidSurvivor.Weapon.ChargeCrushBase.OnEnter += ChargeCrushBase_OnEnter;

            SkillDef viendSpecialHurt = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/CrushHealth.asset").WaitForCompletion();
            if (viendSpecialHurt)
            {
                viendSpecialHurt.baseMaxStock = 2;
                viendSpecialHurt.rechargeStock = 1;
                viendSpecialHurt.baseRechargeInterval = 15;
            }
            #endregion
        }

        private void ChargeCrushBase_OnEnter(On.EntityStates.VoidSurvivor.Weapon.ChargeCrushBase.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.ChargeCrushBase self)
        {
            Debug.Log(self.baseDuration);
            if(self is ChargeCrushCorruption)
            {
                self.baseDuration = 1.5f;
            }
            if(self is ChargeCrushHealth)
            {
                self.baseDuration = 0.6f;
            }
            orig(self);
        }

        private void ViendStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(DLC1Content.Buffs.VoidSurvivorCorruptMode))
            {
                args.armorAdd -= (100 - corruptModeArmor);
            }
        }

        private void FireCorruptHandBeam_OnEnter(On.EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.FireCorruptHandBeam self)
        {
            self.tickRate = primaryCorruptTickRate;
            self.damageCoefficientPerSecond = primaryCorruptDps / (1 / (1 + RiskierRainPlugin.kitSlowAspdReduction));
            orig(self);
        }

        private bool VoidSurvivorSkillDef_HasRequiredCorruption(On.RoR2.Skills.VoidSurvivorSkillDef.orig_HasRequiredCorruption orig, RoR2.Skills.VoidSurvivorSkillDef self, GenericSkill skillSlot)
        {
            VoidSurvivorSkillDef.InstanceData instanceData = (VoidSurvivorSkillDef.InstanceData)skillSlot.skillInstanceData;
            VoidSurvivorController vsc = instanceData.voidSurvivorController;
            if (vsc)
            {
                float guh = ViendTweaks.maxCorruption - Mathf.Min(self.maximumCorruption, ViendTweaks.maxCorruption);
                float a = vsc.maxCorruption - vsc.minimumCorruption;
                float b = self.minimumCorruption - guh;
                if (a > b)
                    return true;
                return vsc.corruption >= self.minimumCorruption && vsc.corruption < self.maximumCorruption;
            }
            return false;
            //return orig(self, skillSlot);
        }

        private void VoidSurvivorController_OnEnable(On.RoR2.VoidSurvivorController.orig_OnEnable orig, RoR2.VoidSurvivorController self)
        {
            self.minimumCorruptionPerVoidItem = ViendTweaks.minimumCorruptionPerVoidItem;
            self.corruptionForFullDamage = ViendTweaks.corruptionForFullDamage;
            self.corruptionForFullHeal = ViendTweaks.corruptionForFullHeal;
            self.corruptionFractionPerSecondWhileCorrupted = ViendTweaks.corruptionFractionPerSecondWhileCorrupted;
            self.corruptionPerSecondInCombat = ViendTweaks.corruptionPerSecondInCombat;
            self.corruptionPerSecondOutOfCombat = ViendTweaks.corruptionPerSecondOutOfCombat;
            self.corruptionPerCrit = ViendTweaks.corruptionPerCrit;
            self.maxCorruption = ViendTweaks.maxCorruption;
            orig(self);
        }
    }
}