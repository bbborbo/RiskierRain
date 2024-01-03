using EntityStates;
using EntityStates.Commando.CommandoWeapon;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.EntityState.Commando;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.SurvivorTweaks
{
    class CommandoTweaks : SurvivorTweakModule
    {
        public static float primaryDamageCoeff = 1.4f; //1.0f
        public static float primaryDuration = 0.2f; //0.167f

        public static GameObject phaseRoundPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/FMJ");
        public static float phaseRoundDamageCoeff = 5f; //3
        public static float phaseRoundCooldown = 4f; //3
        public static float phaseRoundDuration = 0.7f; //0.5f

        public static float phaseBlastDamageCoeff = 3f; //2f
        public static float phaseBlastCooldown = 7; //3f

        public static int rollStock = 2; //1
        public static float rollCooldown = 4f; //4f
        public static float rollDuration = 0.2f; //0.4f
        public static float slideCooldown = 5f; //4f
        public static float rollAspdBuff = 1.0f; 
        public static float rollAspdDuration = 0.5f; 

        public static int soupMaxTargets = 6;
        public static int soupBaseShots = 9; //6
        public static float soupDamageCoeff = 1.8f; //1f
        public static float soupCooldown = 12f; //9f

        public override string survivorName => "Commando";

        public override string bodyName => "CommandoBody";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            FirePistol2.baseDuration = primaryDuration;
            FirePistol2.damageCoefficient = primaryDamageCoeff;
            LanguageAPI.Add("COMMANDO_PRIMARY_DESCRIPTION", $"Rapidly shoot an enemy for <style=cIsDamage>{Tools.ConvertDecimal(primaryDamageCoeff)} damage</style>.");

            ChangeSecondaries(secondary);

            //roll
            utility.variants[0].skillDef.baseMaxStock = rollStock;
            utility.variants[0].skillDef.baseRechargeInterval = rollCooldown;
            utility.variants[0].skillDef.forceSprintDuringState = true;
            utility.variants[0].skillDef.cancelSprintingOnActivation = false;
            On.EntityStates.Commando.DodgeState.OnEnter += DodgeBuff;
            On.EntityStates.Commando.DodgeState.OnExit += DodgeBuffExit;
            LanguageAPI.Add("COMMANDO_UTILITY_DESCRIPTION", $"<style=cIsUtility>Roll</style> a short distance, " +
                $"then briefly increase your <style=cIsDamage>attack speed</style> " +
                $"by <style=cIsDamage>{Tools.ConvertDecimal(rollAspdBuff)}</style>. " +
                $"Has <style=cIsUtility>{rollStock}</style> charges.");
            GetStatCoefficients += RollStatBuff;

            //slide
            utility.variants[1].skillDef.baseRechargeInterval = slideCooldown;

            //special
            SkillDef soupFire = special.variants[0].skillDef;
            Assets.RegisterEntityState(typeof(SoupTargeting));
            Assets.RegisterEntityState(typeof(SoupFire));
            SerializableEntityStateType newSoupFireState = new SerializableEntityStateType(typeof(SoupTargeting));
            soupFire.activationState = newSoupFireState;
            soupFire.baseRechargeInterval = soupCooldown;
            soupFire.beginSkillCooldownOnSkillEnd = true;
            soupFire.activationStateMachineName = "Weapon";
            LanguageAPI.Add("COMMANDO_SPECIAL_NAME", $"Suppressive Barrage");
            LanguageAPI.Add("COMMANDO_SPECIAL_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. " +
                $"Take aim at up to <style=cIsDamage>{soupMaxTargets}</style> enemies, " +
                $"then fire at each target for <style=cIsDamage>{SoupFire.baseDuration}</style> seconds, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(soupDamageCoeff)} damage per shot</style>.");
        }


        #region primary
        private void ChangeSecondaries(SkillFamily secondary)
        {
            //phase round
            phaseRoundPrefab.transform.localScale *= 2;
            On.EntityStates.GenericProjectileBaseState.OnEnter += PhaseRoundBuff;
            secondary.variants[0].skillDef.baseRechargeInterval = phaseRoundCooldown;
            LanguageAPI.Add("COMMANDO_SECONDARY_DESCRIPTION", 
                $"Fire a <style=cIsDamage>piercing</style> bullet for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(phaseRoundDamageCoeff)} damage </style>. " +
                $"Deals <style=cIsDamage>40%</style> more damage every time it passes through an enemy.");

            //phase blast
            On.EntityStates.GenericBulletBaseState.OnEnter += PhaseBlastBuff;
            secondary.variants[1].skillDef.baseRechargeInterval = phaseBlastCooldown;
            LanguageAPI.Add("COMMANDO_SECONDARY_ALT1_DESCRIPTION",
                $"Fire two close-range blasts that deal " +
                $"<style=cIsDamage>8x{Tools.ConvertDecimal(phaseBlastDamageCoeff)} damage</style> total.");
        }

        private void PhaseRoundBuff(On.EntityStates.GenericProjectileBaseState.orig_OnEnter orig, EntityStates.GenericProjectileBaseState self)
        {
            if(self is FireFMJ)
            {
                self.damageCoefficient = phaseRoundDamageCoeff;
                self.baseDuration = phaseRoundDuration;
            }
            orig(self);
        }
        private void PhaseBlastBuff(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if(self is FireShotgunBlast)
            {
                self.damageCoefficient = phaseBlastDamageCoeff;
            }
            orig(self);
        }
        #endregion

        private void DodgeBuff(On.EntityStates.Commando.DodgeState.orig_OnEnter orig, EntityStates.Commando.DodgeState self)
        {
            self.duration = rollDuration;
            self.initialSpeedCoefficient = 10f; //5
            self.finalSpeedCoefficient = 2.5f; //2.5
            orig(self);
        }

        private void DodgeBuffExit(On.EntityStates.Commando.DodgeState.orig_OnExit orig, EntityStates.Commando.DodgeState self)
        {
            orig(self);
            self.characterBody.AddTimedBuffAuthority(Assets.commandoRollBuff.buffIndex, rollAspdDuration);
            self.characterBody.SetSpreadBloom(0, false);
        }

        private void RollStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(Assets.commandoRollBuff))
            {
                args.attackSpeedMultAdd += rollAspdBuff;
            }
        }

        private void SoupBuff(On.EntityStates.Commando.CommandoWeapon.FireBarrage.orig_OnEnter orig, FireBarrage self)
        {
            FireBarrage.damageCoefficient = soupDamageCoeff;
            orig(self);
        }
    }
}
