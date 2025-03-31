using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.SurvivorTweaks
{
    class LoaderTweaks : SurvivorTweakModule
    {
        float chargeFistCooldown = 10;
        float chargeZapFistCooldown = 10;
        float chargeFistVelocityCoefficient = .4f; //.3f

        float pylonDamage = 2; //1 

        public override string survivorName => "Loader";

        public override string bodyName => "LoaderBody";


        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);



            ChangeVanillaUtilities(utility);
            On.EntityStates.Loader.BaseSwingChargedFist.OnEnter += LoaderChargeFistOnEnterHook;

            EntityStates.Loader.BaseSwingChargedFist.velocityDamageCoefficient = chargeFistVelocityCoefficient;
            EntityStates.Loader.ThrowPylon.damageCoefficient = pylonDamage;
            
        }


        private void LoaderChargeFistOnEnterHook(On.EntityStates.Loader.BaseSwingChargedFist.orig_OnEnter orig, EntityStates.Loader.BaseSwingChargedFist self)
        {

            orig(self);
            Debug.Log("damage = " + self.damageCoefficient);
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        {
            family.variants[0].skillDef.baseRechargeInterval = chargeFistCooldown;
            family.variants[1].skillDef.baseRechargeInterval = chargeZapFistCooldown;

            Debug.Log("charged punch velocityDamageCoeffecient = " + EntityStates.Loader.BaseSwingChargedFist.velocityDamageCoefficient);           
        }
    }
}
