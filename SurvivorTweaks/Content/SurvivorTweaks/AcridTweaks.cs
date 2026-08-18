using EntityStates;
using EntityStates.Croco;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.Skills;
using SurvivorTweaks.Orbs;
using SurvivorTweaks.Modules;
using SurvivorTweaks.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;
using RainrotSharedUtils.Components;
using static RoR2.DotController;

namespace SurvivorTweaks.SurvivorTweaks
{
    class AcridTweaks : SurvivorTweakBase<AcridTweaks>
    {
        public static bool isLoaded;

        public static bool GetKitDOTFilter(DotIndex dotIndex)
        {
            return dotIndex == DotIndex.Blight || dotIndex == DotIndex.Poison || dotIndex == CommonAssets.corrosionDotIndex;
        }
        public static List<VineOrb.SplitDebuffInformation> GetContagiousDOTInfo(CharacterBody victimBody, CharacterBody attackerBody)
        {
            List<VineOrb.SplitDebuffInformation> list = new List<VineOrb.SplitDebuffInformation>();
            DotController dotController = DotController.FindDotController(victimBody.gameObject);
            foreach (BuffIndex buffIndex in BuffCatalog.debuffAndDotsIndicesExcludingNoxiousThorns)
            {
                BuffDef buffDef = BuffCatalog.GetBuffDef(buffIndex);
                if (!buffDef.isDOT || dotController == null)
                    continue;
                int buffCount = victimBody.GetBuffCount(buffDef);
                if (buffCount > 0)
                {
                    int count = Mathf.CeilToInt((float)buffCount * AcridTweaks.contagiousTransferRate);
                    DotController.DotIndex dotDefIndex = DotController.GetDotDefIndex(buffDef);
                    if (AcridTweaks.contagiousOnlyKitDots && AcridTweaks.GetKitDOTFilter(dotDefIndex) == false)
                        continue;

                    bool isTimed = false;
                    float duration = 0f;
                    isTimed = dotController.GetDotStackTotalDurationForIndex(dotDefIndex, out duration);

                    VineOrb.SplitDebuffInformation item = new VineOrb.SplitDebuffInformation
                    {
                        attacker = attackerBody.gameObject,
                        attackerMaster = attackerBody.master,
                        index = buffIndex,
                        isTimed = isTimed,
                        duration = duration,
                        count = count
                    };
                    list.Add(item);
                }
            }
            return list;
        }

        [AutoConfig("Acrid : Base Damage Stat", "Scales 20% per level. Vanilla is 15", 9f)]
        public static float acridBaseDamage = 9; //15
        [AutoConfig("Keywords : Poisonous : Status Duration", "Expressed in seconds", 10f)]
        public static float poisonDuration = 10; //10
        [AutoConfig("Keywords : Blighted : Status Duration", "Expressed in seconds", 5f)]
        public static float blightDuration = 5; //5
        [AutoConfig("Keywords : Caustic : Status Duration", "Expressed in seconds", 8f)]
        public static float corrosionDuration = 8f;
        [AutoConfig("Keywords : Caustic : Armor Reduction Per Stack", 15)]
        public static int corrosionArmorReduction = 15;
        [AutoConfig("Keywords : Caustic : Base Damage Per Second", "Expressed as a percentage (eg 1.0 is 100%)", 1f)]
        public static float corrosionDamagePerSecond = 1f;
        [AutoConfig("Keywords : Caustic : Tick Interval", "Expressed in seconds", 1f)]
        public static float corrosionTickInterval = 1f;

        [AutoConfig("Contagious Keyword : Transfer Rate", "Expressed as a percentage (eg 0.5 is 50%)", 0.5f)]
        public static float contagiousTransferRate = 0.5f;
        [AutoConfig("Contagious Keyword : Affect Kit DOTs Only", "Affects all DOTs if true, only kit DOTs if false", false)]
        public static bool contagiousOnlyKitDots = false;
        [AutoConfig("Festering Keyword : Affect Kit DOTs Only", "Affects all DOTs if true, only kit DOTs if false", true)]
        public static bool festerOnlyKitDots = true;

        [AutoConfig("Ability Tweaks (Primary) : Festering Wounds : Base Attack Duration", "Expressed in seconds. Vanilla is 1.5", 0.9f)]
        public static float slashDuration = 0.9f; //1.5f
        [AutoConfig("Ability Tweaks (Primary) : Festering Wounds : Canceled By Sprinting", false)]
        public static bool slashCanceledBySprinting = false; //true

        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Base Cooldown", "Expressed in seconds. Vanilla is 2", 5f)]
        public static float spitCooldown = 5f; //2
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Projectile Damage Coefficient", "Expressed as a percentage (eg 1.8 is 180%). Vanilla is 2.4", 1.8f)]
        public static float spitDamageCoeff = 1.8f; //2.4f
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Projectile Damage Coefficient (Boosted)", "Expressed as a percentage (eg 5.8 is 580%). Vanilla is 2.4", 5.8f)]
        public static float spitDamageCoeffAfterDistance = 5.8f; //2.4f
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Projectile Flight Distance For Damage Boost", "Expressed in meters. Vanilla is N/A", 21f)]
        public static float spitDistanceForBoost = 21f;
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Base Attack Duration", "Expressed in seconds. Vanilla is 0.5", 0.4f)]
        public static float spitDuration = 0.4f; //0.5
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Projectile Blast Radius", "Expressed in meters. Vanilla is 3", 6f)]
        public static float spitBlastRadius = 6f; //3
        [AutoConfig("Ability Tweaks (Secondary) : Neurotoxin : Base Max Stock", "Vanilla is 1", 3)]
        public static int spitBaseStock = 3;

        [AutoConfig("Ability Tweaks (Secondary) : Ravenous Bite : Lunge Force", "Vanilla is 0", 8000f)]
        public static float biteForceStrength = 8000f; //0
        [AutoConfig("Ability Tweaks (Secondary) : Ravenous Bite : Base Cooldown", "Expressed in seconds. Vanilla is 2", 3f)]
        public static float biteCooldown = 3f; //2
        [AutoConfig("Ability Tweaks (Secondary) : Ravenous Bite : Damage Coefficient", "Expressed as a percentage (eg 4.8 is 480%). Vanilla is 3.1", 4.8f)]
        public static float biteDamageCoeff = 4.8f; //3.1f

        [AutoConfig("Ability Tweaks (Utility) : Caustic Leap : Base Cooldown", "Expressed in seconds. Vanilla is 6", 7f)]
        public static float causticCooldown = 7f; //6
        public static float frenziedCooldown = 9; //10
        [AutoConfig("Ability Tweaks (Utility) : Caustic Leap : Minimum Horizontal Launch Angle", "Vanilla is 0", -0.5f)]
        public static float leapMinY = -0.5f; //0

        [AutoConfig("Ability Tweaks (Special) : Epidemic : Base Cooldown", "Expressed in seconds. Vanilla is 10", 15f)]
        public static float epidemicCooldown = 15f; //10
        [AutoConfig("Ability Tweaks (Special) : Epidemic : Damage Coefficient", "Expressed as a percentage (eg 0.5 is 50%). Vanilla is 1.0", 0.5f)]
        public static float epidemicDamageCoefficient = 0.5f; //1
        [AutoConfig("Ability Tweaks (Special) : Epidemic : Disease Initial Range", "Expressed in meters. Vanilla is 30", 80f)]
        public static float epidemicInitialRange = 80;
        [AutoConfig("Ability Tweaks (Special) : Epidemic : Disease Spread Range", "Expressed in meters. Vanilla is 30", 35f)]
        public static float epidemicSpreadRange = 35;
        [AutoConfig("Ability Tweaks (Special) : Epidemic : Projectile Blast Radius", "Expressed in meters.", 3f)]
        public static float epidemicProjectileBlastRadius = 3f;
        [AutoConfig("Ability Tweaks (Special) : Epidemic : Disease Max Bounces", "Vanilla is 20", 20)]
        public static int epidemicMaxTargets = 20;
        public static ModdedDamageType AcridSkillBasedDamage;

        public override string survivorName => "Acrid";

        public override string bodyName => "CrocoBody";

        public static string AcridBlightKeywordToken = "KEYWORD_BLIGHT";

        public override void Init()
        {
            base.Init();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Croco.CrocoBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMoveSpeed = 8;//7
                body.baseDamage = acridBaseDamage; //15
                body.levelDamage = acridBaseDamage * 0.2f;

                ChangePassive();

                ChangeVanillaPrimary(primary);
                ChangeVanillaSecondaries(secondary);
                ChangeVanillaUtilities(utility);
                ChangeVanillaSpecials(special);
            });

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += ChangePoisonDuration;
            LanguageAPI.Add("KEYWORD_POISON",
                $"<style=cKeywordName>Poisonous</style>" +
                $"<style=cSub>Deal damage equal to <style=cIsDamage>up to {poisonDuration}%</style> of their maximum health over {poisonDuration}s. " +
                $"<i>Poison cannot kill enemies.</i></style>");
            LanguageAPI.Add(AcridBlightKeywordToken,
                $"<style=cKeywordName>Blighted</style>" +
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
            primary.canceledFromSprinting = slashCanceledBySprinting;
            primary.keywordTokens = new string[] { /*"KEYWORD_AGILE",*/ "KEYWORD_RAPID_REGEN", CommonAssets.AcridFesterKeywordToken };
            LanguageAPI.Add("CROCO_PRIMARY_DESCRIPTION", 
                //$"<style=cIsUtility>Agile</style>. " +
                $"Maul an enemy for <style=cIsDamage>200% damage</style>. Every 3rd hit is <style=cIsHealing>Regenerative</style> and <style=cIsVoid>Festering</style> for <style=cIsDamage>400% damage</style>.");
            On.EntityStates.Croco.Slash.OnEnter += ChangeCrocoSlashDuration;
            On.EntityStates.Croco.Slash.AuthorityModifyOverlapAttack += CrocoSlashDamageType;
        }

        private void CrocoSlashDamageType(On.EntityStates.Croco.Slash.orig_AuthorityModifyOverlapAttack orig, Slash self, OverlapAttack overlapAttack)
        {
            orig(self, overlapAttack);
            if (self.isComboFinisher)
            {
                overlapAttack.AddModdedDamageType(CommonAssets.AcridFesterDamage);
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
                $"<style=cIsVoid>Blighted</style>. " +
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
                $"<style=cIsVoid>Blighted</style>. <style=cIsDamage>Slayer</style>. <style=cIsHealing>Regenerative</style>. " +
                $"Bite an enemy for <style=cIsDamage>{Tools.ConvertDecimal(biteDamageCoeff)} damage</style>.");
        }

        private void BuffBite(On.EntityStates.Croco.Bite.orig_OnEnter orig, EntityStates.Croco.Bite self)
        {
            self.damageCoefficient = biteDamageCoeff;
            orig(self);
            if (!SurvivorTweaksPlugin.acridLungeLoaded)
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
            utility.keywordTokens = new string[] {CommonAssets.AcridCorrosionKeywordToken, "KEYWORD_RAPID_REGEN", CommonAssets.AcridFesterKeywordToken };
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
            //On.EntityStates.Croco.BaseLeap
            On.EntityStates.Croco.BaseLeap.DoImpactAuthority += AddLeapBounce;
            On.EntityStates.Croco.Leap.GetBlastDamageType += LeapDamageType;
        }

        private DamageTypeCombo LeapDamageType(On.EntityStates.Croco.Leap.orig_GetBlastDamageType orig, Leap self)
        {
            DamageTypeCombo dtc = orig(self);
            dtc.AddModdedDamageType(CommonAssets.AcridFesterDamage);
            dtc.AddModdedDamageType(CommonAssets.AcridCorrosiveDamage);
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
            special.keywordTokens = new string[] { "KEYWORD_POISON", CommonAssets.AcridContagiousKeywordToken };
            LanguageAPI.Add("CROCO_SPECIAL_DESCRIPTION", 
                $"<style=cIsHealing>Poisonous</style>. <style=cIsHealth>Contagious</style>. " +
                $"Release a deadly disease that deals <style=cIsDamage>{Tools.ConvertDecimal(epidemicDamageCoefficient)} damage</style>. " +
                $"The disease spreads to up to <style=cIsDamage>{epidemicMaxTargets}</style> targets within <style=cIsUtility>{epidemicInitialRange}m</style>.");

            GameObject diseaseProjectilePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Croco/CrocoDiseaseProjectile.prefab").WaitForCompletion();

            ProjectileProximityBeamController beamController = diseaseProjectilePrefab.GetComponent<ProjectileProximityBeamController>();
            if (beamController)
            {
                beamController.attackRange = epidemicSpreadRange;
                ProjectileDiseaseOrbController diseaseOrbController = diseaseProjectilePrefab.AddComponent<ProjectileDiseaseOrbController>();
                diseaseOrbController.procCoefficient = beamController.procCoefficient;
                diseaseOrbController.damageCoefficient = beamController.damageCoefficient;
                diseaseOrbController.bounces = epidemicMaxTargets;
                diseaseOrbController.maxOrbRange = epidemicInitialRange;
                diseaseOrbController.orbSpreadRange = epidemicSpreadRange;
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
