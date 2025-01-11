using BepInEx.Configuration;
using SwanSongExtended.States.Huntress;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Events;
using EntityStates;
using RoR2.Skills;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Skills
{
    class LaserrangSkill : SkillBase<LaserrangSkill>
    {
        public override float BaseCooldown => 7f;
        public override InterruptPriority InterruptPriority => InterruptPriority.Skill;

        public override Type BaseSkillDef => typeof(SkillDef);

        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;
        #region config
        public override string ConfigName => "Skills : Huntress : LaserRang";

        public static GameObject boomerangPrefab;
        [AutoConfig("Max Boomerang Fly-Out Time", 0.3f)]
        public static float maxFlyOutTime = 0.3f; //0.6f
        [AutoConfig("Boomerang Scale Factor", 0.3f)]
        public static float boomerangScale = 0.3f; //1.0f
        [AutoConfig("Boomerang Speed", 90f)]
        public static float boomerangSpeed = 90f; //1.0f

        [AutoConfig("Damage Coefficient", 5f)]
        public static float damageCoefficient = 5f;
        [AutoConfig("Proc Coefficient", 0.8f)]
        public static float procCoefficient = 0.8f;
        [AutoConfig("Base Duration", 1.35f)]
        public static float baseDuration = 1.35f;
        [AutoConfig("Force", 150f)]
        public static float force = 150f;
        [AutoConfig("AntiGravity Strength", 20f)]
        public static float antiGravStrength = 20f;
        #endregion

        public override string SkillName => "Laser-Rang";

        public override string SkillDescription => $"{DamageColor("Slayer")}. " +
            $"Throw a {DamageColor("piercing")} boomerang that slices through enemies " +
            $"for {DamageValueText(damageCoefficient)}. " +
            $"Can {DamageColor("strike")} enemies again on the way back.";

        public override string SkillLangTokenName => "HUNTRESSLASERRANG";

        public override UnlockableDef UnlockDef => null;

        public override Sprite Icon => null;

        public override Type ActivationState => typeof(ThrowLaserrang);

        public override string CharacterName => "HuntressBody";

        public override SkillSlot SkillSlot => SkillSlot.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData();

        public override void Init()
        {
            KeywordTokens = new string[1] { "KEYWORD_SLAYER" };
            CreateProjectile();
            base.Init();
        }
        public override void Hooks()
        {

        }

        private void CreateProjectile()
        {
            boomerangPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/Sawmerang").InstantiateClone("HuntressLaserrang", true);
            GameObject ghost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/GlaiveGhost").InstantiateClone("HuntressLaserrangGhost", false);
            boomerangPrefab.transform.localScale = Vector3.one * boomerangScale;

            BoomerangProjectile bp = boomerangPrefab.GetComponent<BoomerangProjectile>();
            bp.travelSpeed = boomerangSpeed;
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
            poa.overlapProcCoefficient = procCoefficient;

            Content.AddProjectilePrefab(boomerangPrefab);
        }
    }
}
