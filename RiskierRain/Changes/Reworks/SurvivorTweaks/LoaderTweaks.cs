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
        //float chargeFistVelocityCoefficient = .3f; //.3f

        float pylonDamage = 2; //1 

        public override string survivorName => "Loader";

        public override string bodyName => "LoaderBody";


        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);



            ChangeVanillaUtilities(utility);
            //EntityStates.Loader.BaseSwingChargedFist.velocityDamageCoefficient = chargeFistVelocityCoefficient;
            EntityStates.Loader.ThrowPylon.damageCoefficient = pylonDamage;
            
        }


        private void ChangeVanillaUtilities(SkillFamily family)
        {
            family.variants[0].skillDef.baseRechargeInterval = chargeFistCooldown;
            family.variants[1].skillDef.baseRechargeInterval = chargeZapFistCooldown;

        }
    }
}
