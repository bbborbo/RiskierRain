using BepInEx.Configuration;
using R2API;
using RiskierRain.Skills;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Enemies.VoidDreamers
{
    class DreamersFlamePillarSkill : SkillBase
    {

        public static GameObject dreamersFlamePillar;
        public static GameObject dreamersFlamePillarGhost;
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
            dreamersFlamePillar = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherFirePillar.prefab").WaitForCompletion().InstantiateClone("DreamersFlame", true);
            dreamersFlamePillarGhost = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Brother/BrotherFirePillarGhost.prefab").WaitForCompletion().InstantiateClone("DreamersFlameGhost", true);


        }
    }
}
