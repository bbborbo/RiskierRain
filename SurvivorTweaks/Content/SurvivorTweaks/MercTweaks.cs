using SurvivorTweaks.Modules;
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

namespace SurvivorTweaks.SurvivorTweaks
{
    class MercTweaks : SurvivorTweakBase<MercTweaks>
    {
        private static string exactingKeyword => attackSpeedDamageAdditive ? SharedUtilsPlugin.noAttackSpeedAdditiveKeywordToken : SharedUtilsPlugin.noAttackSpeedMultiplicativeKeywordToken;

        [AutoConfig("Mercenary : Base Movement Speed Stat", "Vanilla is 7", 8f)]
        public float moveSpeed = 8f; //7f
        [AutoConfig("Mercenary : Base Health Regeneration Stat", "Scales 20% per level. Vanilla is 1", 1f)]
        public float baseRegen = 1f; //1f
        [AutoConfig("Mercenary : Base Armor Stat", "Vanilla is 20", 0)]
        public int baseArmor = 0; //20
        [AutoConfig("Mercenary : Base Maximum Health Stat", "Scales 30% per level. Vanilla is 110", 110f)]
        public float baseHealth = 110f; //20
        [AutoConfig("Keywords : Exacting : Additive Damage", "Attack Speed is additive to Mercenary's damage damage if true, multiplicative if false. Vanilla is N/A", true)]
        public static bool attackSpeedDamageAdditive = true;

        [AutoConfig("Ability Tweaks (Primary) : Laser Sword : Damage Coefficient", "Expressed as a percent (eg 1.3 is 130%). Vanilla is 1.3", 1.3f)]
        public static float primaryDamageCoefficient = 1.3f;//1.3f

        [AutoConfig("Ability Tweaks (Secondary) : Whirlwind (Spin) : Base Cooldown", "Expressed in seconds. Vanilla is 2.5", 2.5f)]
        public static float spinCooldown = 2.5f; //2.5f
        [AutoConfig("Ability Tweaks (Secondary) : Whirlwind (Spin) : Damage Coefficient Per Slice", "Expressed as a percent (eg 2.5 is 250%). Vanilla is 2", 2.5f)]
        public static float spinDamageCoefficient = 2.5f;//2f

        [AutoConfig("Ability Tweaks (Secondary) : Rising Thunder (Uppercut) : Base Cooldown", "Expressed in seconds. Vanilla is 2.5", 3.5f)]
        public static float uppercutCooldown = 3.5f; //2.5f
        [AutoConfig("Ability Tweaks (Secondary) : Rising Thunder (Uppercut) : Damage Coefficient", "Expressed as a percent (eg 4.5 is 450%). Vanilla is 5.5", 4.5f)]
        public static float uppercutDamageCoefficient = 4.5f;//5.5f

        [AutoConfig("Ability Tweaks (Utility) : Blinding Assault (Fast Dash) : Base Cooldown", "Expressed in seconds. Vanilla is 8", 8f)]
        public static float fastDashCooldown = 8f; //8f
        [AutoConfig("Ability Tweaks (Utility) : Blinding Assault (Fast Dash) : Damage Coefficient", "Expressed as a percent (eg 3.0 is 300%). Vanilla is 3", 3f)]
        public static float fastDashDamageCoefficient = 3f;//3f
        [AutoConfig("Ability Tweaks (Utility) : Focused Assault (Slow Dash) : Base Cooldown", "Expressed in seconds. Vanilla is 8", 11f)]
        public static float focusDashCooldown = 11f; //8f
        [AutoConfig("Ability Tweaks (Utility) : Focused Assault (Slow Dash) : Damage Coefficient", "Expressed as a percent (eg 6.0 is 600%). Vanilla is 7", 6f)]
        public static float focusDashDamageCoefficient = 6f;//7f

        [AutoConfig("Ability Tweaks (Special) : Eviscerate : Base Cooldown", "Expressed in seconds. Vanilla is 6", 10f)]
        public static float eviscCooldown = 10f; //6f
        [AutoConfig("Ability Tweaks (Special) : Eviscerate : Proc Coefficient Per Slice", "Vanilla is 1", 0.4f)]
        public static float eviscProcCoefficient = 0.4f; //1f
        [AutoConfig("Ability Tweaks (Special) : Eviscerate : Slice State Duration", "Vanilla is 2", 2f)]
        public static float eviscDuration = 2f; //2f

        [AutoConfig("Ability Tweaks (Special) : Slicing Winds : Base Cooldown", "Expressed in seconds. Vanilla is 6", 9f)]
        public static float windsCooldown = 9f; //6f
        [AutoConfig("Ability Tweaks (Special) : Slicing Winds : Proc Coefficient Per Slice", "Vanilla is 1", 0.7f)]
        public static float windsProcCoefficient = 0.7f; //1f
        public override string survivorName => "Mercenary";
        public override string bodyName => "MERCBODY";

        public override void Init()
        {
            base.Init();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Merc.MercBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMoveSpeed = moveSpeed;
                body.baseArmor = baseArmor;
                body.baseRegen = baseRegen;
                body.levelRegen = baseRegen * 0.2f;
                body.baseMaxHealth = baseHealth;
                body.levelMaxHealth = baseHealth * 0.3f;

                DoPrimary(primary);
                DoSecondary(secondary);
                DoUtility(utility);
                DoSpecial(special);
            });
        }

        #region primary
        private void DoPrimary(SkillFamily family)
        {
            On.EntityStates.Merc.Weapon.GroundLight2.OnEnter += RemovePrimaryAspdScaling;

            SkillDef laserSword = family.variants[0].skillDef;
            laserSword.keywordTokens = new string[] { "KEYWORD_AGILE", exactingKeyword, "KEYWORD_EXPOSE" };
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
            LanguageAPI.Add(spin.skillDescriptionToken,
                $"Quickly slice horizontally twice, dealing <style=cIsDamage>2x{Tools.ConvertDecimal(spinDamageCoefficient)} damage</style>. If airborne, slice vertically instead.");

            SkillDef uppercut = family.variants[1].skillDef;
            uppercut.baseRechargeInterval = uppercutCooldown;
            uppercut.cancelSprintingOnActivation = false;
            uppercut.keywordTokens = new string[] { exactingKeyword };
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
            focusedDash.keywordTokens = new string[] { "KEYWORD_STUNNING", exactingKeyword, "KEYWORD_EXPOSE" };
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
