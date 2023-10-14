using BepInEx.Configuration;
using R2API;
using RiskierRain.Skills;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using RiskierRain.CoreModules;
using UnityEngine.Events;

namespace RiskierRain.Enemies.VoidDreamers
{
    class DreamersFlamePillarSkill : SkillBase
    {

        public static GameObject dreamersFlamePillar;
        public static GameObject dreamersFlamePillarWarning;
        public override string SkillName => "Extend field / attack";

        public override string SkillDescription => "";

        public override string SkillLangTokenName => "DREAMERSFLAMEPILLAR";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(DreamersFlamePillarsState);

        public override string CharacterName => "CommandoBody";//foir test

        public override SkillFamilyName SkillSlot => SkillFamilyName.Secondary;

       public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 4,
                interruptPriority: EntityStates.InterruptPriority.Skill
            );

       

        public override void Hooks()
        {
        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
            CreateProjectile();
        }
        private void CreateProjectile()
        {
            dreamersFlamePillar = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherFirePillar.prefab").WaitForCompletion().InstantiateClone("DreamersFlame", true);
            dreamersFlamePillarWarning = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Titan/TitanPreFistProjectile.prefab").WaitForCompletion().InstantiateClone("DreamersFlameGhost", true);
            ProjectileImpactExplosion pie = dreamersFlamePillarWarning.GetComponent<ProjectileImpactExplosion>();
            //UnityEngine.Object.Destroy(pie);
            pie.impactEffect = null;
            //pie.lifetime = 3;
            ProjectileFireChildren pfc = dreamersFlamePillarWarning.AddComponent<ProjectileFireChildren>();
            pfc.childProjectilePrefab = dreamersFlamePillar;
            pfc.timer = pie.lifetime;
            Assets.projectilePrefabs.Add(dreamersFlamePillar);
            Assets.projectilePrefabs.Add(dreamersFlamePillarWarning);
        }
    }

}
