using EntityStates;
using EntityStates.Commando;
using EntityStates.Commando.CommandoWeapon;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.States.Commando;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
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
        public static float rollCooldown = 6f; //4f
        public static float rollDuration = 0.2f; //0.4f
        public static float rollAspdBuff = 0.6f; 
        public static float rollAspdDuration = 1f;

        public static int slideStock = 1; //1
        public static float slideCooldown = 8f; //4f
        public static float slideMaxDuration = 4f; //1f
        public static float slideSpeedMultiplier = 0.6f; //1f
        public static float slideStrafeMultiplier = 0.02f; //1f
        public static float slideJumpDuration = 0.6f; //0.6f
        public static float slideJumpMultiplier = 1.2f; //1f

        public static int soupMaxTargets = 4;
        public static int soupBaseShots = 8; //6
        public static float soupDamageCoeff = 1.8f; //1f
        public static float soupProcCoeff = 0.7f; //1f
        public static float soupCooldown = 13f; //9f

        public static float nadeRadius = 16f; //11f
        public static float nadeCooldown = 8f; //5f
        public static float nadeMass = 2.5f; //1f
        public static float nadeDrag = 0.9f; //0f

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

            ChangeUtilities();

            ChangeSpecials();
        }

        private void ChangeSpecials()
        {
            //soup
            SkillDef soupFire = special.variants[0].skillDef;
            CoreModules.Assets.RegisterEntityState(typeof(SoupTargeting));
            CoreModules.Assets.RegisterEntityState(typeof(SoupFire));
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

            //nade
            SkillDef nade = special.variants[1].skillDef;
            nade.baseRechargeInterval = nadeCooldown;
            nade.keywordTokens = new string[1] { "KEYWORD_IGNITE" };
            GameObject commandoNade = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/CommandoGrenadeProjectile.prefab").WaitForCompletion();

            ProjectileDamage projectileDamage = commandoNade.GetComponent<ProjectileDamage>();
            if (projectileDamage)
            {
                projectileDamage.damageType |= DamageType.IgniteOnHit;
            }

            Rigidbody rb = commandoNade.GetComponent<Rigidbody>();
            if (rb)
            {
                rb.mass = nadeMass;
                rb.drag = nadeDrag;
            }

            ProjectileImpactExplosion pie = commandoNade.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastRadius = nadeRadius;

                GameObject commandoNadeExplosion = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Commando/OmniExplosionVFXCommandoGrenade.prefab").WaitForCompletion();
                commandoNadeExplosion.transform.localScale = Vector3.one * nadeRadius * 4 / 11;
            }

            LanguageAPI.Add("COMMANDO_SPECIAL_ALT1_NAME", $"Incendiary Grenade");
            LanguageAPI.Add("COMMANDO_SPECIAL_ALT1_DESCRIPTION", $"<style=cIsDamage>Ignite</style>. Throw a grenade that explodes for <style=cIsDamage>700% damage</style>. Can hold up to 2.");
        }

        private void ChangeUtilities()
        {
            //roll
            SkillDef roll = utility.variants[0].skillDef;
            roll.baseMaxStock = rollStock;
            roll.rechargeStock = rollStock;
            roll.baseRechargeInterval = rollCooldown;
            roll.forceSprintDuringState = true;
            roll.cancelSprintingOnActivation = false;
            roll.resetCooldownTimerOnUse = false;
            On.EntityStates.Commando.DodgeState.OnEnter += DodgeBuff;
            On.EntityStates.Commando.DodgeState.OnExit += DodgeBuffExit;
            LanguageAPI.Add("COMMANDO_UTILITY_DESCRIPTION", $"<style=cIsUtility>Roll</style> a short distance, " +
                $"then briefly increase your <style=cIsDamage>attack speed</style> " +
                $"by <style=cIsDamage>{Tools.ConvertDecimal(rollAspdBuff)}</style>. " +
                $"Has <style=cIsUtility>{rollStock}</style> charges.");
            GetStatCoefficients += RollStatBuff;

            //slide
            SkillDef slide = utility.variants[1].skillDef;
            CoreModules.Assets.RegisterEntityState(typeof(UltraSlide));
            CoreModules.Assets.RegisterEntityState(typeof(UltraDash));
            SerializableEntityStateType ultraSlideState = new SerializableEntityStateType(typeof(UltraSlide));
            slide.activationState = ultraSlideState;
            slide.baseRechargeInterval = slideCooldown;
            slide.baseMaxStock = slideStock;
            slide.rechargeStock = 1;
            slide.beginSkillCooldownOnSkillEnd = true;

            LanguageAPI.Add("COMMANDO_UTILITY_ALT_DESCRIPTION", 
                $"Hold to <style=cIsUtility>slide</style> on the ground. " +
                $"While sliding, jump to <style=cIsUtility>dash</style> in another direction. " +
                $"You can <style=cIsDamage>fire while sliding</style>.");
        }


        #region primary
        private void ChangeSecondaries(SkillFamily secondary)
        {
            //phase round
            phaseRoundPrefab.transform.localScale *= 2;
            On.EntityStates.GenericProjectileBaseState.OnEnter += PhaseRoundBuff;
            secondary.variants[0].skillDef.baseRechargeInterval = phaseRoundCooldown;
            secondary.variants[0].skillDef.fullRestockOnAssign = false;
            LanguageAPI.Add("COMMANDO_SECONDARY_DESCRIPTION", 
                $"Fire a <style=cIsDamage>piercing</style> bullet for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(phaseRoundDamageCoeff)} damage </style>. " +
                $"Deals <style=cIsDamage>40%</style> more damage every time it passes through an enemy.");

            //phase blast
            On.EntityStates.GenericBulletBaseState.OnEnter += PhaseBlastBuff;
            secondary.variants[1].skillDef.baseRechargeInterval = phaseBlastCooldown;
            secondary.variants[1].skillDef.fullRestockOnAssign = false;
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
            self.characterBody.AddTimedBuffAuthority(CoreModules.Assets.commandoRollBuff.buffIndex, rollAspdDuration);
            self.characterBody.SetSpreadBloom(0, false);
        }

        private void RollStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(CoreModules.Assets.commandoRollBuff))
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
