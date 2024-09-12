using BepInEx.Configuration;
using R2API;
using RiskierRainContent.Skills;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Projectile;
using RiskierRainContent.CoreModules;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Enemies.VoidDreamers
{
    class VoidDreamerSkill : SkillBase
    {
        public static GameObject dreamOrbPrefab;

        public override string SkillName => "Orb Volley";

        public override string SkillDescription => "";

        public override string SkillLangTokenName => "DREAMERSVOLLEY";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(DreamersVolleyState);

        public override string CharacterName => "CommandoBody";//foir test

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 3,
                interruptPriority: EntityStates.InterruptPriority.Skill
            );

        public override void Hooks()
        {
            ;
        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateSkill();
            CreateProjectile();
        }

        private void CreateProjectile()
        {
            //RoR2/DLC1/VoidBarnacle/VoidBarnacleBullet.prefab
            dreamOrbPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidBarnacle/VoidBarnacleBullet.prefab").WaitForCompletion().InstantiateClone("DreamOrb", true);

            ProjectileSteerTowardTarget stt = dreamOrbPrefab.GetComponent<ProjectileSteerTowardTarget>();
            UnityEngine.Object.Destroy(stt);//fuck ur homing
            ProjectileSimple ps = dreamOrbPrefab.GetComponent<ProjectileSimple>();
            ps.lifetime = 10;


            CoreModules.Assets.projectilePrefabs.Add(dreamOrbPrefab);
        }
    }
}
