using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using RiskierRainContent.EntityState.Huntress;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;

namespace RiskierRainContent.Skills
{
    class LaserrangSkill : SkillBase
    {
        public static GameObject boomerangPrefab;
        static float maxFlyOutTime = 0.3f; //0.6f
        static float boomerangScale = 0.3f; //1.0f

        public override string SkillName => "Laser-Rang";

        public override string SkillDescription => $"<style=cIsDamage>Slayer</style>. " +
            $"Throw a <style=cIsDamage>piercing</style> boomerang that slices through enemies " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(ThrowLaserrang.damageCoefficient)}</style> damage. " +
            $"Can <style=cIsDamage>strike</style> enemies again on the way back.";

        public override string SkillLangTokenName => "HUNTRESSLASERRANG";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(ThrowLaserrang);

        public override string CharacterName => "HuntressBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 7,
                interruptPriority: EntityStates.InterruptPriority.Skill
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[1] { "KEYWORD_SLAYER" };

            CreateLang();
            CreateSkill();
            CreateProjectile();
        }

        private void CreateProjectile()
        {
            boomerangPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Sawmerang").InstantiateClone("HuntressLaserrang", true);
            GameObject ghost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/GlaiveGhost").InstantiateClone("HuntressLaserrangGhost", false);
            boomerangPrefab.transform.localScale = Vector3.one * boomerangScale;

            BoomerangProjectile bp = boomerangPrefab.GetComponent<BoomerangProjectile>();
            bp.travelSpeed = 90f;
            bp.transitionDuration = 0.8f;
            bp.distanceMultiplier = maxFlyOutTime;

            ProjectileController pc = bp.GetComponent<ProjectileController>();
            pc.ghostPrefab = ghost;

            ProjectileDamage pd = bp.GetComponent<ProjectileDamage>();
            pd.damageType |= DamageType.BonusToLowHealth;

            ProjectileDotZone pdz = boomerangPrefab.GetComponent<ProjectileDotZone>();
            /*pdz.overlapProcCoefficient = 0.8f;
            pdz.damageCoefficient = 1f;
            pdz.resetFrequency = 1 / (maxFlyOutTime + bp.transitionDuration);
            pdz.fireFrequency = 20f;*/
            UnityEngine.Object.Destroy(pdz);

            ProjectileOverlapAttack poa = boomerangPrefab.GetComponent<ProjectileOverlapAttack>();
            poa.damageCoefficient = 1f;
            poa.overlapProcCoefficient = 0.8f;

            Assets.projectilePrefabs.Add(boomerangPrefab);
        }
    }
}
