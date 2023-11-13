using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using RiskierRainContent.EntityState.Bandit;
using EntityStates;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Skills
{
    class NailBombSkill : SkillBase
    {
        public static int baseMaxStock = 2;
        public static GameObject nailBombProjectile;

        public override string SkillName => "Nail Bomb";

        public override string SkillDescription => $"<style=cIsDamage>Stunning.</style> Throw a delayed explosive " +
            $"for <style=cIsDamage>{Tools.ConvertDecimal(ThrowNailBomb.damageCoeff)} damage</style> and <style=cIsHealth>shredding</style> nearby enemies. " +
            $"Hold up to {baseMaxStock}.";

        public override string SkillLangTokenName => "BANDITNAILBOMB";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(ThrowNailBomb);

        public override string CharacterName => "Bandit2Body";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Secondary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 3,
                baseMaxStock: baseMaxStock,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: false,
                beginSkillCooldownOnSkillEnd: false
            );

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            KeywordTokens = new string[] { "KEYWORD_STUNNING", Assets.shredKeywordToken };
            CreateLang();
            CreateSkill();
            Hooks();
            CreateProjectile();
        }

        private void CreateProjectile()
        {
            nailBombProjectile = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/BanditGrenadeProjectile").InstantiateClone("BanditNailBomb", true);
            ProjectileFuse fuse = nailBombProjectile.AddComponent<ProjectileFuse>();
            fuse.fuse = 0.3f;

            ProjectileImpactExplosion pie = nailBombProjectile.GetComponent<ProjectileImpactExplosion>();
            pie.blastRadius = 7f;
            pie.bonusBlastForce = Vector3.up * 500;
            pie.blastProcCoefficient = 0.5f;

            ProjectileDamage pd = nailBombProjectile.GetComponent<ProjectileDamage>();
            pd.damageType |= DamageType.BypassArmor;
            pd.damageType |= DamageType.Stun1s;
            //pd.force = 2000;

            ProjectileSimple ps = nailBombProjectile.GetComponent<ProjectileSimple>();
            ps.desiredForwardSpeed = 40f;

            Rigidbody rb = nailBombProjectile.GetComponent<Rigidbody>();
            rb.useGravity = true;

            AntiGravityForce antigrav = nailBombProjectile.AddComponent<AntiGravityForce>();
            antigrav.antiGravityCoefficient = 0.15f;
            antigrav.rb = rb;

            Assets.projectilePrefabs.Add(nailBombProjectile);
        }
    }
}
