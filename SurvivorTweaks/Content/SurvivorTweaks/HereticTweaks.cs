using BepInEx;
using EntityStates;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RainrotSharedUtils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SurvivorTweaks.Modules;
using System;
using UnityEngine;
using UnityEngine.Networking;
using static MoreStats.StatHooks;

namespace SurvivorTweaks.SurvivorTweaks
{
    class HereticTweaks : SurvivorTweakBase<HereticTweaks>
    {
        [AutoConfig("Ability Tweaks (Secondary) : Charge Duration Max", "Expressed in seconds. Vanilla is 2", 3f)]
        public static float secondaryMaxCharge = 3; //2f
        [AutoConfig("Ability Tweaks (Secondary) : Tick Damage Coefficient", "Expressed as a percentage (eg 1.0 is 100%). Vanilla is 1.75", 1f)]
        public static float secondaryBladesDamage = 1f; //1.75f
        [AutoConfig("Ability Tweaks (Secondary) : Tick Frequency", "Expressed in ticks per second. Vanilla is 5", 6f)]
        public static float secondaryBladesFrequency = 6f; //5f
        [AutoConfig("Ability Tweaks (Secondary) : Tick Proc Coefficient", "Vanilla is 0.2", 0.5f)]
        public static float secondaryBladesProc = 0.5f; //0.2f
        [AutoConfig("Ability Tweaks (Secondary) : Blast Damage Coefficient", "Expressed as a percentage (eg 9.0 is 900%). Vanilla is 7", 9f)]
        public static float secondaryExplosionDamage = 9f; //7
        [AutoConfig("Ability Tweaks (Secondary) : Blast Proc Coefficient", "Vanilla is 1", 1f)]
        public static float secondaryExplosionProc = 1; //1f

        [AutoConfig("Ability Tweaks (Utility) : Total Heal Fraction", "Expressed as a percentage (eg 0.25 is 25%). Vanilla is 0.182", 0.25f)]
        public static float shadowfadeBaseHealFraction = 0.25f;
        public override string bodyName => "HereticBody";

        public override string survivorName => "Heretic";
        public override void Init()
        {
            base.Init();
            //GetBodyObject();
            //GetSkillsFromBodyObject(bodyObject);
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Heretic.HereticBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                CharacterBody vanillaHereticBody = bodyObject.GetComponent<CharacterBody>();
                vanillaHereticBody.baseMaxHealth = 260;
                vanillaHereticBody.baseRegen = -4;
                vanillaHereticBody.baseDamage = 16;
                vanillaHereticBody.baseArmor = 30;

                vanillaHereticBody.levelMaxHealth = vanillaHereticBody.baseMaxHealth * 0.3f;
                vanillaHereticBody.levelRegen = vanillaHereticBody.baseRegen * 0.2f;
                vanillaHereticBody.levelDamage = vanillaHereticBody.baseDamage * 0.2f;
            });

            #region secondary
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_LunarSkillReplacements.LunarSecondaryProjectile_prefab, (lunarSecondaryProjectile) =>
            {
                ProjectileDotZone blades = lunarSecondaryProjectile.GetComponent<ProjectileDotZone>();
                blades.damageCoefficient = secondaryBladesDamage / secondaryExplosionDamage;
                blades.resetFrequency = secondaryBladesFrequency;
                blades.fireFrequency = secondaryBladesFrequency * 3;
                blades.overlapProcCoefficient = secondaryBladesProc / secondaryExplosionProc;

                ProjectileExplosion explosion = lunarSecondaryProjectile.GetComponent<ProjectileExplosion>();
                explosion.blastDamageCoefficient = 1;
                explosion.blastProcCoefficient = secondaryExplosionProc;
                explosion.falloffModel = BlastAttack.FalloffModel.Linear;
                explosion.blastRadius = 17f;
            });

            On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += HooksDamageBuff;
            On.EntityStates.Mage.Weapon.BaseChargeBombState.OnEnter += HooksChargeTweak;

            LanguageAPI.Add("SKILL_LUNAR_SECONDARY_REPLACEMENT_DESCRIPTION",
                $"Charge up a ball of blades that " +
                $"deals <style=cIsDamage>{Tools.ConvertDecimal(secondaryBladesDamage * secondaryBladesFrequency)} damage per second</style>. " +
                $"After a delay, explode and " +
                $"<style=cIsDamage>root</style> all enemies " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(secondaryExplosionDamage)} damage</style>.");
            #endregion

            #region utility
            On.EntityStates.GhostUtilitySkillState.OnEnter += ShadowfadeEnter;
            On.EntityStates.GhostUtilitySkillState.OnEnter += ShadowfadeExit;

            LanguageAPI.Add("SKILL_LUNAR_UTILITY_REPLACEMENT_DESCRIPTION",
                $"Fade away, becoming <style=cIsUtility>intangible</style> " +
                $"and <style=cIsUtility>gaining movement speed</style>. " +
                $"<style=cIsHealing>Heal</style> for " +
                $"<style=cIsHealing>{Tools.ConvertDecimal(shadowfadeBaseHealFraction)} of your maximum health</style>.");
            #endregion
        }

        private static void HooksChargeTweak(On.EntityStates.Mage.Weapon.BaseChargeBombState.orig_OnEnter orig, EntityStates.Mage.Weapon.BaseChargeBombState self)
        {
            if (self is EntityStates.GlobalSkills.LunarNeedle.ChargeLunarSecondary)
            {
                self.baseDuration = secondaryMaxCharge;
            }
            orig(self);
        }

        private static void HooksDamageBuff(On.EntityStates.Mage.Weapon.BaseThrowBombState.orig_OnEnter orig, EntityStates.Mage.Weapon.BaseThrowBombState self)
        {
            if (self is EntityStates.GlobalSkills.LunarNeedle.ThrowLunarSecondary)
            {
                self.minDamageCoefficient = secondaryExplosionDamage;
                self.maxDamageCoefficient = secondaryExplosionDamage;
            }
            orig(self);
        }

        private static void ShadowfadeEnter(On.EntityStates.GhostUtilitySkillState.orig_OnEnter orig, GhostUtilitySkillState self)
        {
            orig(self);
            GhostUtilitySkillState.healFractionPerTick = shadowfadeBaseHealFraction / (GhostUtilitySkillState.baseDuration * GhostUtilitySkillState.healFrequency);
        }

        private static void ShadowfadeExit(On.EntityStates.GhostUtilitySkillState.orig_OnEnter orig, GhostUtilitySkillState self)
        {
            if (NetworkServer.active)
            {
                self.healthComponent.HealFraction(GhostUtilitySkillState.healFractionPerTick, default(ProcChainMask));
            }
            orig(self);
        }
    }
}
