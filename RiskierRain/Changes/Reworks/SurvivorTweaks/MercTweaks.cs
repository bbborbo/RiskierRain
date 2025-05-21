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
using EntityStates.Merc.Weapon;
using RainrotSharedUtils;
using EntityStates.Merc;
using UnityEngine.AddressableAssets;

namespace RiskierRain.SurvivorTweaks
{
    class MercTweaks : SurvivorTweakModule
    {
        public override string survivorName => "Mercenary";
        public override string bodyName => "MERCBODY";

        public bool attackSpeedDamageAdditive = false;
        public float moveSpeed = 8f; //7f

        public static float primaryDamageCoefficient = 1.3f;//1.3f

        public static float spinCooldown = 2.5f; //2.5f
        public static float spinDamageCoefficient = 2.5f;//2f
        public static float uppercutCooldown = 3.5f; //2.5f
        public static float uppercutDamageCoefficient = 4.5f;//5.5f

        public static float fastDashCooldown = 8f; //8f
        public static float fastDashDamageCoefficient = 3f;//3f
        public static float focusDashCooldown = 11f; //8f
        public static float focusDashDamageCoefficient = 6f;//7f

        public static float eviscCooldown = 12f; //6f
        public static float eviscProcCoefficient = 0.4f; //1f
        public static float eviscDuration = 3.5f; //2f
        public static float windsCooldown = 9f; //6f
        public static float windsProcCoefficient = 0.7f; //1f

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseMoveSpeed = moveSpeed;

            DoPrimary(primary);
            DoSecondary(secondary);
            DoUtility(utility);
            DoSpecial(special);
        }

        #region
        private void DoPrimary(SkillFamily family)
        {
            On.EntityStates.Merc.Weapon.GroundLight2.OnEnter += RemovePrimaryAspdScaling;

            SkillDef laserSword = family.variants[0].skillDef;
            laserSword.keywordTokens = new string[] { "KEYWORD_AGILE", SharedUtilsPlugin.noAttackSpeedKeywordToken, "KEYWORD_EXPOSE" };
            LanguageAPI.Add(laserSword.skillDescriptionToken,
                $"<style=cIsUtility>Agile</style>. <style=cIsUtility>Exacting</style>. " +
                $"Slice in front for <style=cIsDamage>{Tools.ConvertDecimal(primaryDamageCoefficient)}</style>. " +
                $"Every 3rd hit strikes in a greater area and <style=cIsUtility>Exposes</style> enemies.");
        }

        private void RemovePrimaryAspdScaling(On.EntityStates.Merc.Weapon.GroundLight2.orig_OnEnter orig, EntityStates.Merc.Weapon.GroundLight2 self)
        {
            self.damageCoefficient = primaryDamageCoefficient;
            orig(self);
            self.duration = self.baseDuration;
            self.durationBeforeInterruptable = (self.isComboFinisher ? GroundLight2.comboFinisherBaseDurationBeforeInterruptable : GroundLight2.baseDurationBeforeInterruptable);
            self.ignoreAttackSpeed = true;
            self.scaleHitPauseDurationAndVelocityWithAttackSpeed = false;

            //float finalDamageCoefficient = self.overlapAttack.damage + self.overlapAttack.damage * ((self.attackSpeedStat - 1f) * (self.overlapAttack.damage / 100f));
            //self.overlapAttack.damage = finalDamageCoefficient;
            if (attackSpeedDamageAdditive)
            {
                self.overlapAttack.damage += self.characterBody.baseDamage * self.attackSpeedStat;
            }
            else
            {
                self.overlapAttack.damage *= self.attackSpeedStat;
            }
        }
        #endregion
        #region secondary
        private void DoSecondary(SkillFamily family)
        {
            SkillDef spin = family.variants[0].skillDef;
            spin.baseRechargeInterval = spinCooldown;
            spin.cancelSprintingOnActivation = false;
            On.EntityStates.Merc.WhirlwindBase.OnEnter += SpinChanges;

            SkillDef uppercut = family.variants[1].skillDef;
            uppercut.baseRechargeInterval = uppercutCooldown;
            uppercut.cancelSprintingOnActivation = false;
            uppercut.keywordTokens = new string[] { "KEYWORD_STUNNING", SharedUtilsPlugin.noAttackSpeedKeywordToken, "KEYWORD_EXPOSE" };
            LanguageAPI.Add(uppercut.skillDescriptionToken,
                $"<style=cIsUtility>Exacting</style>. " +
                $"Unleash a slicing uppercut, dealing <style=cIsDamage>{Tools.ConvertDecimal(uppercutDamageCoefficient)} damage</style> and sending you airborne.");
            On.EntityStates.Merc.Uppercut.OnEnter += UppercutChanges;
        }

        private void SpinChanges(On.EntityStates.Merc.WhirlwindBase.orig_OnEnter orig, WhirlwindBase self)
        {
            self.baseDamageCoefficient = spinDamageCoefficient;
            orig(self);
        }

        private void UppercutChanges(On.EntityStates.Merc.Uppercut.orig_OnEnter orig, EntityStates.Merc.Uppercut self)
        {
            Uppercut.baseDamageCoefficient = uppercutDamageCoefficient;
            orig(self);
            self.duration = Uppercut.baseDuration;
            if (attackSpeedDamageAdditive)
            {
                self.overlapAttack.damage += self.characterBody.baseDamage * self.attackSpeedStat;
            }
            else
            {
                self.overlapAttack.damage *= self.attackSpeedStat;
            }
        }
        #endregion
        #region utility
        private void DoUtility(SkillFamily family)
        {
            SkillDef fastDash = family.variants[0].skillDef;
            fastDash.baseRechargeInterval = fastDashCooldown;

            SkillDef focusedDash = family.variants[1].skillDef;
            focusedDash.baseRechargeInterval = focusDashCooldown;
            focusedDash.keywordTokens = new string[] { "KEYWORD_STUNNING", SharedUtilsPlugin.noAttackSpeedKeywordToken, "KEYWORD_EXPOSE" };
            LanguageAPI.Add(focusedDash.skillDescriptionToken,
                $"<style=cIsUtility>Stunning</style>. <style=cIsUtility>Exacting</style>. " +
                $"Dash forward, dealing <style=cIsDamage>{Tools.ConvertDecimal(focusDashDamageCoefficient)} damage</style> " +
                $"and <style=cIsUtility>Exposing</style> enemies after <style=cIsUtility>1 second</style>.");
            On.EntityStates.Merc.FocusedAssaultDash.OnEnter += RemoveFocusedDashAspdScaling;
        }

        private void RemoveFocusedDashAspdScaling(On.EntityStates.Merc.FocusedAssaultDash.orig_OnEnter orig, EntityStates.Merc.FocusedAssaultDash self)
        {
            self.damageCoefficient = 0.4f;
            self.delayedDamageCoefficient = focusDashDamageCoefficient;
            if (attackSpeedDamageAdditive)
            {
                //self.damageCoefficient += self.attackSpeedStat - 1;
                self.delayedDamageCoefficient += self.attackSpeedStat - 1;
            }
            else
            {
                //self.damageCoefficient *= self.attackSpeedStat;
                self.delayedDamageCoefficient *= self.attackSpeedStat;
            }
            orig(self);
            self.duration = self.baseDuration;
        }
        #endregion
        #region special
        private void DoSpecial(SkillFamily family)
        {
            SkillDef evisc = family.variants[0].skillDef;
            evisc.baseRechargeInterval = eviscCooldown;

            On.EntityStates.Merc.Evis.OnEnter += EvisOnEnter;
            On.EntityStates.Merc.Evis.OnExit += EvisOnExit;

            SkillDef winds = family.variants[1].skillDef;
            winds.baseRechargeInterval = windsCooldown;

            GameObject windsSlicingProjectile = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Merc/EvisOverlapProjectile.prefab").WaitForCompletion();
            if (windsSlicingProjectile)
            {
                ProjectileOverlapAttack poa = windsSlicingProjectile.GetComponent<ProjectileOverlapAttack>();
                if (poa)
                {
                    poa.overlapProcCoefficient = windsProcCoefficient;
                }
            }
        }

        private void EvisOnEnter(On.EntityStates.Merc.Evis.orig_OnEnter orig, EntityStates.Merc.Evis self)
        {
            EntityStates.Merc.Evis.duration = eviscDuration;
            EntityStates.Merc.Evis.procCoefficient = eviscProcCoefficient;
            orig(self);
        }

        private void EvisOnExit(On.EntityStates.Merc.Evis.orig_OnExit orig, EntityStates.Merc.Evis self)
        {
            orig(self);
        }
        #endregion
    }
}
