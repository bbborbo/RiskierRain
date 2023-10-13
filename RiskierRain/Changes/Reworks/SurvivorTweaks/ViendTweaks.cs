using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.SurvivorTweaks
{
    class ViendTweaks : SurvivorTweakModule
    {
        static float minimumCorruptionPerVoidItem = 2; //2
        static float corruptionForFullDamage = 50; //50
        static float corruptionForFullHeal = -100; //-100
        static float corruptionFractionPerSecondWhileCorrupted = -0.04f; //aka 20s; -0.06666667f aka 15s
        static float corruptionPerSecondInCombat = 1.5f; //aka 50s; 3 aka 33.3s
        static float corruptionPerSecondOutOfCombat = 1.5f; //3
        static float corruptionPerCrit = 2; //2
        static float maxCorruption = 100; //100

        public override string survivorName => "Void Fiend";
        public override string bodyName => "VoidSurvivorBody";

        public override void Init()
        {
            GetBodyObject();

            SkillDef viendSpecialHeal = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/CrushCorruption.asset").WaitForCompletion();
            if (viendSpecialHeal)
            {
                viendSpecialHeal.baseMaxStock = 3;
                viendSpecialHeal.baseRechargeInterval = 45;
            }

            SkillDef viendSpecialHurt = Addressables.LoadAssetAsync<SkillDef>("RoR2/DLC1/VoidSurvivor/CrushHealth.asset").WaitForCompletion();
            if (viendSpecialHurt)
            {
                viendSpecialHurt.baseMaxStock = 2;
                viendSpecialHurt.baseRechargeInterval = 15;
                viendSpecialHurt.rechargeStock = 1;
            }

            On.RoR2.VoidSurvivorController.OnEnable += VoidSurvivorController_OnEnable;
            //On.RoR2.Skills.VoidSurvivorSkillDef.HasRequiredCorruption += VoidSurvivorSkillDef_HasRequiredCorruption;
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