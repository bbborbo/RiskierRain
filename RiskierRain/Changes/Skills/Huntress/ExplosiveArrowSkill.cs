using BepInEx.Configuration;
using RiskierRain.CoreModules;
using RiskierRain.EntityState.Huntress;
using EntityStates.Huntress.HuntressWeapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Skills
{
    class ExplosiveArrowSkill : SkillBase
    {
        public static GameObject regularArrowPrefab;
        public static GameObject critArrowPrefab;
        public static GameObject critBombletsPrefab;

        public static int critBombletCount = 3;
        public static float critBombletDamageCoefficient = 0.4f;
        public override string SkillName => "Primed Shot";

        public override string SkillDescription => $"<style=cIsDamage>Agile</style>. " +
            $"Draw back a heavy arrow for <style=cIsDamage>{Tools.ConvertDecimal(FireExplosiveArrow.minDamage)}" +
            $"-{Tools.ConvertDecimal(FireExplosiveArrow.maxDamage)} damage</style>. " +
            $"Perfectly timed shots ALWAYS <style=cIsDamage>Critical Strike</style>, " +
            $"dropping bomblets for <style=cIsDamage>{critBombletCount}x{Tools.ConvertDecimal(critBombletDamageCoefficient)} damage</style>.";

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

            RegisterEntityState(typeof(FireExplosiveArrow));
        }

        private void CreateProjectile()
        {
            //regularArrowPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Arrow");
            //critArrowPrefab = regularArrowPrefab;
            regularArrowPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Arrow").InstantiateClone("BorboHuntressExplosiveArrowNormal", true);
            //GameObject arrowGhost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/ArrowGhost");

            #region arrow 1
            ProjectileController pc = regularArrowPrefab.GetComponent<ProjectileController>();
            pc.ghostPrefab.transform.localScale *= 5f;
            pc.procCoefficient = 1.0f;

            ProjectileSimple ps = regularArrowPrefab.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = 120f;
            ps.lifetime = 5f;
            ps.enableVelocityOverLifetime = false;
            ps.updateAfterFiring = false;

            ProjectileStickOnImpact psoi1 = regularArrowPrefab.AddComponent<ProjectileStickOnImpact>();
            psoi1.alignNormals = false;
            #endregion


            critArrowPrefab = regularArrowPrefab.InstantiateClone("BorboHuntressExplosiveArrowCrit", true);
            critBombletsPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/CryoCanisterBombletsProjectile").InstantiateClone("BorboHuntressExplosiveArrowBomblets", true);

            #region arrow 2
            ProjectileSingleTargetImpact psti = critArrowPrefab.GetComponent<ProjectileSingleTargetImpact>();
            UnityEngine.Object.Destroy(psti);

            ProjectileImpactExplosion pie1 = critArrowPrefab.AddComponent<ProjectileImpactExplosion>();
            pie1.lifetime = ps.lifetime;
            pie1.lifetimeAfterImpact = 0f;
            pie1.timerAfterImpact = false;
            pie1.destroyOnEnemy = true;
            pie1.destroyOnWorld = true;
            pie1.blastRadius = 7f;
            pie1.impactEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/IgniteDirectionalExplosionVFX");
            pie1.childrenCount = critBombletCount;
            pie1.childrenProjectilePrefab = critBombletsPrefab;
            pie1.childrenDamageCoefficient = 1 / FireExplosiveArrow.maxDamage;
            pie1.fireChildren = true;
            pie1.blastProcCoefficient = 1.0f;
            pie1.blastDamageCoefficient = 1.0f;

            ProjectileStickOnImpact psoi2 = critArrowPrefab.GetComponent<ProjectileStickOnImpact>();
            UnityEngine.Object.Destroy(psoi2);
            #endregion

            #region bomblets
            ProjectileImpactExplosion pie2 = critBombletsPrefab.GetComponent<ProjectileImpactExplosion>();
            pie2.lifetime = 0.2f;
            pie2.lifetimeAfterImpact = 0.3f;
            pie1.timerAfterImpact = false;
            pie2.destroyOnEnemy = false;
            pie2.destroyOnWorld = false;
            pie2.blastRadius = 4f;
            pie2.blastProcCoefficient = 0.3f;
            pie2.blastDamageCoefficient = critBombletDamageCoefficient;

            ProjectileDamage pd = critBombletsPrefab.GetComponent<ProjectileDamage>();
            pd.damageType = DamageType.Generic;
            #endregion

            Assets.projectilePrefabs.Add(regularArrowPrefab);
            Assets.projectilePrefabs.Add(critArrowPrefab);
            Assets.projectilePrefabs.Add(critBombletsPrefab);
        }
    }
}
