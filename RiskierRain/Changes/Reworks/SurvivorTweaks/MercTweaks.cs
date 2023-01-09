using RiskierRain.CoreModules;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.SurvivorTweaks
{
    class MercTweaks : SurvivorTweakModule
    {
        public override string survivorName => "Mercenary";
        public override string bodyName => "MERCBODY";

        public float moveSpeed = 8f; //7f

        public float secondaryCooldown = 3f; //2.5f
        public float utilityCooldown = 11f; //8f
        public float specialCooldown = 9f; //6f

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.moveSpeed = moveSpeed;

            secondary.variants[0].skillDef.baseRechargeInterval = secondaryCooldown;
            secondary.variants[0].skillDef.cancelSprintingOnActivation = false;
            secondary.variants[1].skillDef.baseRechargeInterval = secondaryCooldown;
            secondary.variants[1].skillDef.cancelSprintingOnActivation = false;

            utility.variants[0].skillDef.baseRechargeInterval = utilityCooldown;
            utility.variants[1].skillDef.baseRechargeInterval = utilityCooldown;

            special.variants[0].skillDef.baseRechargeInterval = specialCooldown;
            special.variants[1].skillDef.baseRechargeInterval = specialCooldown;
        }
    }
}
