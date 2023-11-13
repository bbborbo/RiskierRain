using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using RiskierRainContent.EntityState.Huntress;
using EntityStates.Huntress.HuntressWeapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Skills
{
    class ExplosiveArrowSkill : SkillBase
    {
        public static GameObject regularArrowPrefab;
        public static GameObject critArrowPrefab;
        public static GameObject critBombletsPrefab;

        public static int critBombletCount = 3;
        public static float critBombletDamageCoefficient = 0.6f;
        public override string SkillName => "Primed Shot";

        public override string SkillDescription => $"<style=cIsDamage>Agile</style>. " +
            $"Draw back a heavy arrow for <style=cIsDamage>{Tools.ConvertDecimal(FireExplosiveArrow.minDamage)}" +
            $"-{Tools.ConvertDecimal(FireExplosiveArrow.maxDamage)} damage</style>. " +
            $"Critical Strikes drop bomblets for " +
            $"<style=cIsDamage>{critBombletCount}x{Tools.ConvertDecimal(critBombletDamageCoefficient)} damage</style>.";

        public override string SkillLangTokenName => "HUNTRESSEXPLOSIVEPRIMARY";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(ChargeExplosiveArrow);

        public override string CharacterName => "HuntressBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                interruptPriority: EntityStates.InterruptPriority.Any,
                cancelSprintingOnActivation: false,
                stockToConsume: 0
            );

        public override void Hooks()
        {
            On.EntityStates.Mage.Weapon.BaseChargeBombState.CalcCharge += ExplosiveArrowChargeOverride;
        }

        private float ExplosiveArrowChargeOverride(On.EntityStates.Mage.Weapon.BaseChargeBombState.orig_CalcCharge orig, EntityStates.Mage.Weapon.BaseChargeBombState self)
        {
            if(self is ChargeExplosiveArrow)
            {
                ChargeExplosiveArrow cea = (ChargeExplosiveArrow)self;
                return Mathf.Clamp01(cea.fixedAge / cea.windUpDuration);
            }
            return orig(self);
        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "KEYWORD_AGILE" };

            CreateLang();
            CreateSkill();
            CreateProjectile();

            Assets.RegisterEntityState(typeof(FireExplosiveArrow));
        }

        private void CreateProjectile()
        {
            //regularArrowPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Arrow");
            //critArrowPrefab = regularArrowPrefab;
            regularArrowPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Arrow").InstantiateClone("BorboHuntressExplosiveArrowNormal", true);
            //GameObject arrowGhost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/ArrowGhost");

            #region arrow 1
            ProjectileController pc = regularArrowPrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab.transform.localScale *= 8f;
            pc.procCoefficient = 1.0f;

            ProjectileSimple ps = regularArrowPrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = 120f;
            ps.lifetime = 5f;
            ps.enableVelocityOverLifetime = false;
            ps.updateAfterFiring = false;

            ProjectileStickOnImpact psoi = regularArrowPrefab.GetComponent<ProjectileStickOnImpact>();
            UnityEngine.Object.Destroy(psoi);

            ProjectileSingleTargetImpact psti = regularArrowPrefab.GetComponent<ProjectileSingleTargetImpact>();
            UnityEngine.Object.Destroy(psti);

            ProjectileImpactExplosion pie1 = regularArrowPrefab.AddComponent<ProjectileImpactExplosion>();
            pie1.lifetime = ps.lifetime;
            pie1.lifetimeAfterImpact = 0f;
            pie1.timerAfterImpact = false;
            pie1.destroyOnEnemy = true;
            pie1.destroyOnWorld = true;
            pie1.blastRadius = 5f;
            pie1.impactEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/IgniteDirectionalExplosionVFX");
            pie1.blastProcCoefficient = 1.0f;
            pie1.blastDamageCoefficient = 1.0f;
            #endregion


            critArrowPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Arrow").InstantiateClone("BorboHuntressExplosiveArrowCrit", true); //regularArrowPrefab.InstantiateClone("BorboHuntressExplosiveArrowCrit", true);
            critBombletsPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CryoCanisterBombletsProjectile").InstantiateClone("BorboHuntressExplosiveArrowBomblets", true);

            #region arrow 2
            ProjectileController pc2 = critArrowPrefab.GetComponent<ProjectileController>();
            pc2.procCoefficient = 1.0f;

            ProjectileSimple ps2 = critArrowPrefab.GetComponent<ProjectileSimple>();
            ps2.desiredForwardSpeed = 120f;
            ps2.lifetime = 5f;
            ps2.enableVelocityOverLifetime = false;
            ps2.updateAfterFiring = false;

            ProjectileStickOnImpact psoi2 = critArrowPrefab.GetComponent<ProjectileStickOnImpact>();
            if(psoi2)
                UnityEngine.Object.Destroy(psoi2);

            ProjectileSingleTargetImpact psti2 = critArrowPrefab.GetComponent<ProjectileSingleTargetImpact>();
            if(psti2)
                UnityEngine.Object.Destroy(psti2);

            ProjectileImpactExplosion pie2 = critArrowPrefab.AddComponent<ProjectileImpactExplosion>();
            pie2.lifetime = ps2.lifetime;
            pie2.lifetimeAfterImpact = 0f;
            pie2.timerAfterImpact = false;
            pie2.destroyOnEnemy = true;
            pie2.destroyOnWorld = true;
            pie2.blastRadius = 5f;
            pie2.impactEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/IgniteDirectionalExplosionVFX");
            pie2.blastProcCoefficient = 1.0f;
            pie2.blastDamageCoefficient = 1.0f;
            pie2.blastRadius = 8f;
            pie2.childrenProjectilePrefab = critBombletsPrefab;
            pie2.fireChildren = true;
            pie2.childrenDamageCoefficient = 1 / FireExplosiveArrow.maxDamage;
            pie2.childrenCount = critBombletCount;
            #endregion

            #region bomblets
            ProjectileImpactExplosion pie3 = critBombletsPrefab.GetComponent<ProjectileImpactExplosion>();
            pie3.lifetime = 0.2f;
            pie3.lifetimeAfterImpact = 0.3f;
            pie3.timerAfterImpact = false;
            pie3.destroyOnEnemy = false;
            pie3.destroyOnWorld = false;
            pie3.blastRadius = 4f;
            pie3.blastProcCoefficient = 0.3f;
            pie3.blastDamageCoefficient = critBombletDamageCoefficient;

            ProjectileDamage pd = critBombletsPrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.Generic;
            #endregion

            Assets.projectilePrefabs.Add(regularArrowPrefab);
            Assets.projectilePrefabs.Add(critArrowPrefab);
            Assets.projectilePrefabs.Add(critBombletsPrefab);
        }
    }
}
