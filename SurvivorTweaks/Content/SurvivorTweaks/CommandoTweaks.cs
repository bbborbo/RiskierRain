using EntityStates;
using EntityStates.Commando;
using EntityStates.Commando.CommandoWeapon;
using R2API;
using SurvivorTweaks.Modules;
using SurvivorTweaks.States.Commando;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace SurvivorTweaks.SurvivorTweaks
{
    class CommandoTweaks : SurvivorTweakBase<CommandoTweaks>
    {
        [AutoConfig("Ability Tweaks (Primary) : Double Tap : Damage Coefficient", "Expressed as a percentage (eg 1.4 is 140%). Vanilla is 1", 1.4f)]
        public static float primaryDamageCoeff = 1.4f; //1.0f
        [AutoConfig("Ability Tweaks (Primary) : Double Tap : Base Attack Duration (Left)", "Expressed in seconds. Vanilla is 0.167", 0.16f)]
        public static float primaryDurationLeft = 0.16f; //0.167f
        [AutoConfig("Ability Tweaks (Primary) : Double Tap : Base Attack Duration (Right)", "Expressed in seconds. Vanilla is 0.167", 0.24f)]
        public static float primaryDurationRight = 0.24f; //0.167f

        public static GameObject phaseRoundPrefab;
        [AutoConfig("Ability Tweaks (Secondary) : Phase Round : Damage Coefficient", "Expressed as a percentage (eg 5.0 is 500%). Vanilla is 1.0", 5f)]
        public static float phaseRoundDamageCoeff = 5f; //3
        [AutoConfig("Ability Tweaks (Secondary) : Phase Round : Base Cooldown", "Expressed in seconds. Vanilla is 3", 4.0f)]
        public static float phaseRoundCooldown = 4f; //3
        [AutoConfig("Ability Tweaks (Secondary) : Phase Round : Base Attack Duration", "Expressed in seconds. Vanilla is 0.5", 0.7f)]
        public static float phaseRoundDuration = 0.7f; //0.5f
        [AutoConfig("Ability Tweaks (Secondary) : Phase Round : Projectile Scale", "Vanilla is 1.0", 2.0f)]
        public static float phaseRoundScale = 2f; //1f

        [AutoConfig("Ability Tweaks (Secondary) : Phase Blast : Damage Coefficient Per Pellet", "Expressed as a percentage (eg 3.0 is 300%). Vanilla is 2.0", 3.0f)]
        public static float phaseBlastDamageCoeff = 3f; //2f
        [AutoConfig("Ability Tweaks (Secondary) : Phase Blast : Base Cooldown", "Expressed in seconds. Vanilla is 3", 5.0f)]
        public static float phaseBlastCooldown = 5; //3f

        [AutoConfig("Ability Tweaks (Utility) : Tactical Dive (Roll) : Base Max Stock", "Vanilla is 1", 2)]
        public static int rollStock = 2; //1
        [AutoConfig("Ability Tweaks (Utility) : Tactical Dive (Roll) : Base Cooldown", "Expressed in seconds. Vanilla is 4", 6.0f)]
        public static float rollCooldown = 6f; //4f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Dive (Roll) : Base Attack Duration", "Expressed in seconds. Vanilla is 0.4", 0.2f)]
        public static float rollDuration = 0.2f; //0.4f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Dive (Roll) : Attack Speed Bonus", "Expressed as a percentage (eg 0.6 is 60%). Vanilla is 0", 0.6f)]
        public static float rollAspdBuff = 0.6f;
        [AutoConfig("Ability Tweaks (Utility) : Tactical Dive (Roll) : Attack Speed Duration", "Expressed in seconds. Vanilla is 0", 1f)]
        public static float rollAspdDuration = 1f;

        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Base Max Stock", "Vanilla is 1", 1)]
        public static int slideStock = 1; //1
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Base Cooldown", "Expressed in seconds. Vanilla is 4", 8.0f)]
        public static float slideCooldown = 8f; //4f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Duration Max", "Maximum duration the slide can be held. Expressed in seconds. Vanilla is 1", 4.0f)]
        public static float slideMaxDuration = 4f; //1f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Total Speed Multiplier", "Expressed as a percentage of vanilla's speed", 0.6f)]
        public static float slideSpeedMultiplier = 0.6f; //1f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Strafe Speed Multiplier", "Expressed as a percentage of vanilla's speed", 0.02f)]
        public static float slideStrafeMultiplier = 0.02f; //1f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Jump Boost Duration", "Expressed in seconds. Vanilla is 0.6", 0.6f)]
        public static float slideJumpDuration = 0.6f; //0.6f
        [AutoConfig("Ability Tweaks (Utility) : Tactical Slide : Jump Height Multiplier", "Expressed as a percentage of vanilla's boost", 1.2f)]
        public static float slideJumpMultiplier = 1.2f; //1f

        [AutoConfig("Ability Tweaks (Special) : Suppressive Fire : Maximum Targets", 4)]
        public static int soupMaxTargets = 4;
        [AutoConfig("Ability Tweaks (Special) : Suppressive Fire : Total Bullets", "Scales with attack speed. Vanilla is 6", 8)]
        public static int soupBaseShots = 8; //6
        [AutoConfig("Ability Tweaks (Special) : Suppressive Fire : Damage Coefficient Per Bullet", "Expressed as a percentage (eg 1.8 is 180%). Vanilla is 1", 1.8f)]
        public static float soupDamageCoeff = 1.8f; //1f
        [AutoConfig("Ability Tweaks (Special) : Suppressive Fire : Proc Coefficient Per Bullet", "Vanilla is 1", 1.0f)]
        public static float soupProcCoeff = 1f; //1f
        [AutoConfig("Ability Tweaks (Special) : Suppressive Fire : Base Cooldown", "Vanilla is 9", 13f)]
        public static float soupCooldown = 13f; //9f

        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Ignition Damage Type", "If true, frag grenade ignites. Vanilla is false", true)]
        public static bool nadeIgnition = true; //false
        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Projectile Blast Radius", "Expressed in meters. Vanilla is 11", 16f)]
        public static float nadeBlastRadius = 16f; //11f
        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Damage Coefficient", "Expressed as a percentage (eg 7.0 is 700%). Vanilla is 7", 7f)]
        public static float nadeDamage = 7f; //7f
        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Base Cooldown", "Expressed in seconds. Vanilla is 5", 8f)]
        public static float nadeCooldown = 8f; //5f
        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Projectile Mass", "Vanilla is 1", 2.5f)]
        public static float nadeMass = 2.5f; //1f
        [AutoConfig("Ability Tweaks (Special) : Frag Grenade : Projectile Drag", "Vanilla is 0", 0.9f)]
        public static float nadeDrag = 0.9f; //0f

        public override string survivorName => "Commando";

        public override string bodyName => "CommandoBody";

        public override void Init()
        {
            base.Init();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Commando.CommandoBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                ChangeSecondaries(secondary);

                ChangeUtilities();

                ChangeSpecials();
            });

            On.EntityStates.Commando.CommandoWeapon.FirePistol2.OnEnter += FirePistol2_OnEnter;
            LanguageAPI.Add("COMMANDO_PRIMARY_DESCRIPTION", $"Rapidly shoot an enemy for <style=cIsDamage>{Tools.ConvertDecimal(primaryDamageCoeff)} damage</style>.");
        }

        private void FirePistol2_OnEnter(On.EntityStates.Commando.CommandoWeapon.FirePistol2.orig_OnEnter orig, FirePistol2 self)
        {
            FirePistol2.damageCoefficient = primaryDamageCoeff;
            orig(self);
            self.duration = (self.pistol % 2 == 0 ? primaryDurationLeft : primaryDurationRight) / self.attackSpeedStat;
        }

        private void ChangeSpecials()
        {
            //soup
            SkillDef soupFire = special.variants[0].skillDef;
            Content.AddEntityState(typeof(SoupTargeting));
            Content.AddEntityState(typeof(SoupFire));
            SerializableEntityStateType newSoupFireState = new SerializableEntityStateType(typeof(SoupTargeting));
            soupFire.activationState = newSoupFireState;
            soupFire.baseRechargeInterval = soupCooldown;
            soupFire.beginSkillCooldownOnSkillEnd = true;
            soupFire.activationStateMachineName = "Weapon";
            soupFire.suppressSkillActivation = true;
            LanguageAPI.Add("COMMANDO_SPECIAL_NAME", $"Suppressive Barrage");
            LanguageAPI.Add("COMMANDO_SPECIAL_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. " +
                $"Take aim at up to <style=cIsDamage>{soupMaxTargets}</style> enemies, " +
                $"then fire at each target for <style=cIsDamage>{SoupFire.baseDuration}</style> seconds, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(soupDamageCoeff)} damage per shot</style>.");

            //nade
            SkillDef nade = special.variants[1].skillDef;
            nade.baseRechargeInterval = nadeCooldown;
            if(nadeIgnition)
                nade.keywordTokens = new string[1] { "KEYWORD_IGNITE" };
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Commando.CommandoGrenadeProjectile_prefab, (commandoNade) =>
            {
                if (commandoNade.TryGetComponent(out ProjectileDamage projectileDamage))
                {
                    if(nadeIgnition)
                        projectileDamage.damageType |= DamageType.IgniteOnHit;
                }

                if (commandoNade.TryGetComponent(out Rigidbody rb))
                {
                    rb.mass = nadeMass;
                    rb.drag = nadeDrag;
                }

                if (commandoNade.TryGetComponent(out ProjectileImpactExplosion pie))
                {
                    pie.blastRadius = nadeBlastRadius;
                }
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Commando.OmniExplosionVFXCommandoGrenade_prefab, (commandoNadeExplosion) =>
            {
                commandoNadeExplosion.transform.localScale = Vector3.one * nadeBlastRadius * 4 / 11;
            });

            if(nadeIgnition == true)
            {
                LanguageAPI.Add("COMMANDO_SPECIAL_ALT1_NAME", $"Incendiary Grenade");
                LanguageAPI.Add("COMMANDO_SPECIAL_ALT1_DESCRIPTION", $"<style=cIsDamage>Ignite</style>. Throw a grenade that explodes for <style=cIsDamage>700% damage</style>. Can hold up to 2.");
            }
            On.EntityStates.Commando.CommandoWeapon.ThrowGrenade.ModifyProjectileInfo += GrenadeDamage;
        }

        private void GrenadeDamage(On.EntityStates.Commando.CommandoWeapon.ThrowGrenade.orig_ModifyProjectileInfo orig, ThrowGrenade self, ref FireProjectileInfo fireProjectileInfo)
        {
            orig(self, ref fireProjectileInfo);
            fireProjectileInfo.damage = self.damageStat * nadeDamage;
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
            Content.AddEntityState(typeof(UltraSlide));
            Content.AddEntityState(typeof(UltraDash));
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
            Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Commando.FMJRamping_prefab).Completed += (ctx) =>
            {
                phaseRoundPrefab = ctx.Result;
                phaseRoundPrefab.transform.localScale *= phaseRoundScale;
            };
            On.EntityStates.GenericProjectileBaseState.OnEnter += PhaseRoundBuff;
            secondary.variants[0].skillDef.baseRechargeInterval = phaseRoundCooldown;
            secondary.variants[0].skillDef.fullRestockOnAssign = false;
            LanguageAPI.Add("COMMANDO_SECONDARY_DESCRIPTION", 
                $"Fire a <style=cIsDamage>piercing</style> bullet for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(phaseRoundDamageCoeff)} damage</style>. " +
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
            self.characterBody.AddTimedBuffAuthority(CommonAssets.commandoRollBuff.buffIndex, rollAspdDuration);
            self.characterBody.SetSpreadBloom(0, false);
        }

        private void RollStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(CommonAssets.commandoRollBuff))
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
