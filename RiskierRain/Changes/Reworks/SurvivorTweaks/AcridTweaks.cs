using EntityStates;
using EntityStates.Croco;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Skills;
using SwanSongExtended.Orbs;
using SwanSongExtended.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;
using RainrotSharedUtils.Components;

namespace RiskierRain.SurvivorTweaks
{
    class AcridTweaks : SurvivorTweakModule
    {
        public static bool isLoaded;

        public static float poisonDuration = 8; //10
        public static float blightDuration = 5; //5

        public static float slashDuration = 0.8f; //1.5f

        public static float spitCooldown = 5f; //2
        public static float spitDamageCoeff = 1.6f; //2.4f
        public static float spitDamageCoeffAfterDistance = 6.2f; //2.4f
        public static float spitDistanceForBoost = 21f;
        public static float spitDuration = 0.4f; //0.5
        public static float spitBlastRadius = 6f; //3
        public static int spitBaseStock = 3;

        public static float biteForceStrength = 8000f; //0
        public static float biteCooldown = 3f; //2
        public static float biteDamageCoeff = 4.8f; //3.1f

        public static float causticCooldown = 8f; //6
        public static float frenziedCooldown = 10; //10
        public static float leapMinY = -0.3f; //0

        public static float epidemicCooldown = 15f; //10
        public static float epidemicDamageCoefficient = 0.5f; //1
        public static float epidemicSpreadRange = 50;
        public static float epidemicProjectileBlastRadius = 3f;
        public static ModdedDamageType AcridSkillBasedDamage;

        public override string survivorName => "Acrid";

        public override string bodyName => "CrocoBody";

        public static string AcridBlightKeywordToken = "KEYWORD_BLIGHT";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseMoveSpeed = 8;//7
            body.baseDamage = 12; //15

            ChangePassive();

            ChangeVanillaPrimary(primary);
            ChangeVanillaSecondaries(secondary);
            ChangeVanillaUtilities(utility);
            ChangeVanillaSpecials(special);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += ChangePoisonDuration;
            LanguageAPI.Add("KEYWORD_POISON",
                $"<style=cKeywordName>Poisonous</style>" +
                $"<style=cSub>Deal damage equal to <style=cIsDamage>up to {poisonDuration}%</style> of their maximum health over {poisonDuration}s. " +
                $"<i>Poison cannot kill enemies.</i></style>");
            LanguageAPI.Add(AcridBlightKeywordToken,
                $"<style=cKeywordName>Blight</style>" +
                $"<style=cSub>Deal <style=cIsDamage>60% base damage</style> over <style=cIsUtility>{blightDuration}s</style>. " +
                $"<i>Blight can stack.</i></style>");
        }

        private void ChangePassive()
        {
            AcridSkillBasedDamage = DamageAPI.ReserveDamageType();
            GenericSkill[] allSkills = bodyObject.GetComponents<GenericSkill>();
            GenericSkill passiveSkillSlot = allSkills[0];
            //foreach (GenericSkill skillSlot in allSkills)
            //{
            //    if (skillSlot.skillFamily.name == "CrocoBodyPassiveFamily")
            //    {
            //        passiveSkillSlot = skillSlot;
            //        break;
            //    }
            //}
            if (passiveSkillSlot)
            {
                passiveSkillSlot.hideInCharacterSelect = true;
                UnityEngine.Object.Destroy(passiveSkillSlot);
            }
            else
            {
                Debug.LogError("No ACRID passive skill found");
            }
            //On.RoR2.CrocoDamageTypeController.GetDamageType += CrocoDamageTypeController_GetDamageType;
            IL.EntityStates.Croco.FireSpit.OnEnter += FixSpitDamageTypes;
            On.EntityStates.Croco.Bite.AuthorityModifyOverlapAttack += FixBiteDamageTypes;
        }

        private void FixBiteDamageTypes(On.EntityStates.Croco.Bite.orig_AuthorityModifyOverlapAttack orig, Bite self, OverlapAttack overlapAttack)
        {
            overlapAttack.damageType = (DamageType.BlightOnHit | DamageType.BonusToLowHealth);
            overlapAttack.damageType.damageSource = DamageSource.Secondary;
        }

        private void FixSpitDamageTypes(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            if(c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<FireProjectileInfo>(nameof(FireProjectileInfo.damageTypeOverride))))
            {
                c.Index--;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<DamageTypeCombo, EntityState, DamageTypeCombo>>((damageTypeIn, state) =>
                {
                    if (state is FireDiseaseProjectile)
                    {
                        damageTypeIn.damageType = DamageType.PoisonOnHit;
                        damageTypeIn.damageSource = DamageSource.Special;
                        return damageTypeIn;
                    }
                    if (state is FireSpit)
                    {
                        damageTypeIn.damageType = DamageType.BlightOnHit;
                        damageTypeIn.damageSource = DamageSource.Secondary;
                        return damageTypeIn;
                    }
                    return damageTypeIn;
                });
            }
            else
            {
                Debug.LogError("Acrid spit damage type hook failed!!");
            }
        }

        private DamageTypeCombo CrocoDamageTypeController_GetDamageType(On.RoR2.CrocoDamageTypeController.orig_GetDamageType orig, CrocoDamageTypeController self)
        {
            DamageTypeCombo combo = DamageTypeCombo.Generic;
            combo.AddModdedDamageType(AcridSkillBasedDamage);
            return combo;
        }

        private void ChangeVanillaPrimary(SkillFamily family)
        {
            SkillDef primary = family.variants[0].skillDef;
            primary.canceledFromSprinting = false;
            primary.keywordTokens = new string[] { "KEYWORD_RAPID_REGEN", SwanSongExtended.Modules.CommonAssets.AcridFesterKeywordToken };
            LanguageAPI.Add("CROCO_PRIMARY_DESCRIPTION", 
                $"Maul an enemy for <style=cIsDamage>200% damage</style>. Every 3rd hit is <style=cIsHealing>Regenerative</style> and <style=cIsVoid>Festering</style> for <style=cIsDamage>400% damage</style>.");
            On.EntityStates.Croco.Slash.OnEnter += ChangeCrocoSlashDuration;
            On.EntityStates.Croco.Slash.AuthorityModifyOverlapAttack += CrocoSlashDamageType;
        }

        private void CrocoSlashDamageType(On.EntityStates.Croco.Slash.orig_AuthorityModifyOverlapAttack orig, Slash self, OverlapAttack overlapAttack)
        {
            orig(self, overlapAttack);
            if (self.isComboFinisher)
            {
                overlapAttack.AddModdedDamageType(SwanSongExtended.Modules.CommonAssets.AcridFesterDamage);
            }
        }

        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            //spit
            SkillDef secondary = family.variants[0].skillDef;
            secondary.baseRechargeInterval = spitCooldown;
            secondary.baseMaxStock = spitBaseStock;
            secondary.keywordTokens = new string[] { AcridBlightKeywordToken };
            LanguageAPI.Add("CROCO_SECONDARY_DESCRIPTION",
                $"<style=cIsVoid>Blight</style>. " +
                $"Spit toxic bile for <style=cIsDamage>{Tools.ConvertDecimal(spitDamageCoeff)} damage</style>, " +
                $"or <style=cIsDamage>{Tools.ConvertDecimal(spitDamageCoeffAfterDistance)} damage</style> after " +
                $"<style=cIsUtility>{spitDistanceForBoost}m</style>. Hold up to {spitBaseStock}.");
            GameObject spitProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoSpit.prefab").WaitForCompletion();

            ProjectileIncreaseDamageAfterDistance component = spitProjectilePrefab.AddComponent<ProjectileIncreaseDamageAfterDistance>();
            component.requiredDistance = spitDistanceForBoost;
            component.damageMultiplierOnIncrease = spitDamageCoeffAfterDistance / spitDamageCoeff;
            component.effectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/FlyingVermin/VerminSpitImpactEffect.prefab").WaitForCompletion();

            ProjectileImpactExplosion pie = spitProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.blastRadius = spitBlastRadius;
            }

            //bite
            SkillDef secondaryAlt = family.variants[1].skillDef;
            secondaryAlt.baseRechargeInterval = biteCooldown;
            secondaryAlt.keywordTokens = new string[] { AcridBlightKeywordToken, "KEYWORD_SLAYER", "KEYWORD_RAPID_REGEN" };
            On.EntityStates.Croco.Bite.OnEnter += BuffBite;
            LanguageAPI.Add("CROCO_SECONDARY_ALT_DESCRIPTION",
                $"<style=cIsVoid>Blight</style>. <style=cIsDamage>Slayer</style>. <style=cIsHealing>Regenerative</style>. " +
                $"Bite an enemy for <style=cIsDamage>{Tools.ConvertDecimal(biteDamageCoeff)} damage</style>.");
        }

        private void BuffBite(On.EntityStates.Croco.Bite.orig_OnEnter orig, EntityStates.Croco.Bite self)
        {
            self.damageCoefficient = biteDamageCoeff;
            orig(self);
            if (!RiskierRainPlugin.acridLungeLoaded)
            {
                self.characterMotor.velocity = Vector3.zero;
                self.characterMotor.ApplyForce(self.inputBank.aimDirection * biteForceStrength, true, false);
            }
        }

        private void ChangeCrocoSlashDuration(On.EntityStates.Croco.Slash.orig_OnEnter orig, EntityStates.Croco.Slash self)
        {
            self.baseDuration = slashDuration;
            orig(self);
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        {
            //caustic leap
            SkillDef utility = family.variants[0].skillDef;
            utility.baseRechargeInterval = causticCooldown;
            utility.keywordTokens = new string[] {SwanSongExtended.Modules.CommonAssets.AcridCorrosionKeywordToken, "KEYWORD_RAPID_REGEN", SwanSongExtended.Modules.CommonAssets.AcridFesterKeywordToken };
            LanguageAPI.Add("CROCO_UTILITY_DESCRIPTION", "<style=cIsDamage>Caustic</style>. <style=cIsDamage>Stunning</style>. <style=cIsVoid>Festering</style>. " +
                "Leap in the air, dealing <style=cIsDamage>320% damage</style>. Leave acid that deals <style=cIsDamage>25% damage</style>.");

            //frenzied leap
            SkillDef utilityAlt = family.variants[1].skillDef;
            utilityAlt.baseRechargeInterval = frenziedCooldown;

            /*foreach(SkillFamily.Variant variant in family.variants)
            {
                SkillDef s = variant.skillDef;
                s.interruptPriority = InterruptPriority.Skill;
                s.mustKeyPress = true;
            }*/

            //BaseLeap.blastRadius = leapBlastRadius;
            BaseLeap.minimumY = leapMinY;
            On.EntityStates.Croco.BaseLeap.DoImpactAuthority += AddLeapBounce;
            On.EntityStates.Croco.Leap.GetBlastDamageType += LeapDamageType;
        }

        private DamageTypeCombo LeapDamageType(On.EntityStates.Croco.Leap.orig_GetBlastDamageType orig, Leap self)
        {
            DamageTypeCombo dtc = orig(self);
            dtc.AddModdedDamageType(SwanSongExtended.Modules.CommonAssets.AcridFesterDamage);
            dtc.AddModdedDamageType(SwanSongExtended.Modules.CommonAssets.AcridCorrosiveDamage);
            return dtc;
        }

        private void AddLeapBounce(On.EntityStates.Croco.BaseLeap.orig_DoImpactAuthority orig, BaseLeap self)
        {
            orig(self);
            self.SmallHop(self.characterMotor, 3f);
        }

        #region specials
        void ChangeVanillaSpecials(SkillFamily family)
        {
            //epidemic
            SkillDef special = family.variants[0].skillDef;
            special.baseRechargeInterval = epidemicCooldown;
            special.keywordTokens = new string[] { "KEYWORD_POISON", SwanSongExtended.Modules.CommonAssets.AcridContagiousKeywordToken };
            LanguageAPI.Add("CROCO_SPECIAL_DESCRIPTION", 
                $"<style=cIsHealing>Poisonous</style>. <style=cIsHealth>Contagious</style>. " +
                $"Release a deadly disease that deals <style=cIsDamage>{Tools.ConvertDecimal(epidemicDamageCoefficient)} damage</style>. " +
                $"The disease spreads to up to <style=cIsDamage>20</style> targets.");

            GameObject diseaseProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoDiseaseProjectile.prefab").WaitForCompletion();

            ProjectileProximityBeamController beamController = diseaseProjectilePrefab.GetComponent<ProjectileProximityBeamController>();
            if (beamController)
            {
                beamController.attackRange = epidemicSpreadRange;
                ProjectileDiseaseOrbController diseaseOrbController = diseaseProjectilePrefab.AddComponent<ProjectileDiseaseOrbController>();
                diseaseOrbController.procCoefficient = beamController.procCoefficient;
                diseaseOrbController.damageCoefficient = beamController.damageCoefficient;
                diseaseOrbController.bounces = beamController.bounces;
                diseaseOrbController.maxOrbRange = epidemicSpreadRange;
                diseaseOrbController.blastRadius = epidemicProjectileBlastRadius;
                UnityEngine.Object.Destroy(beamController);
            }
            On.EntityStates.Croco.FireSpit.OnEnter += FireSpit_OnEnter;
        }

        private void FireSpit_OnEnter(On.EntityStates.Croco.FireSpit.orig_OnEnter orig, FireSpit self)
        {
            if(self is FireDiseaseProjectile)
            {
                self.damageCoefficient = epidemicDamageCoefficient;
            }
            else
            {
                self.damageCoefficient = spitDamageCoeff;
                self.baseDuration = spitDuration;
            }
            orig(self);
        }
        #endregion

        private void ChangePoisonDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //poison duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.DamageInfo>("damageType"),
                x => x.MatchLdcI4((int)DamageType.PoisonOnHit)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RoR2.DamageInfo>("procCoefficient")
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, poisonDuration);
            return;
            //blight duration
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.DamageType>(nameof(DamageInfo.damageType)),
                x => x.MatchLdcI4((int)DamageType.BlightOnHit)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<RoR2.DamageInfo>(nameof(DamageInfo.procCoefficient))
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, blightDuration);
        }
    }
}
