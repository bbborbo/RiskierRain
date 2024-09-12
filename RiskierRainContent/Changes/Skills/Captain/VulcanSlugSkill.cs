using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using RiskierRainContent.EntityState.Captain;
using EntityStates;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Skills
{
    class VulcanSlugSkill : SkillBase
    {
        public static int maxStock = 2;
        public static GameObject vulcanSlugPrefab;
        public override string SkillName => "Vulcan Slug";

        public override string SkillDescription => $"Fire an explosive slug that deals " +
            $"<style=cIsDamage>{Tools.ConvertDecimal(VulcanSlug.damageCoefficient)} damage.</style> " +
            $"Charging the attack increases it's <style=cIsUtility>range</style>. Hold up to {maxStock} charges.";

        public override string SkillLangTokenName => "CAPTAINSLUG";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "VulcanSlug";

        public override Type ActivationState => typeof(VulcanSlug);

        public override string CharacterName => "CaptainBody";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Primary;

        public override SimpleSkillData SkillData => new SimpleSkillData
            (
                baseRechargeInterval: 1.5f,
                interruptPriority: InterruptPriority.Skill,
                mustKeyPress: false,
                beginSkillCooldownOnSkillEnd: true,
                baseMaxStock: maxStock
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
            vulcanSlugPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/ToolbotGrenadeLauncherProjectile").InstantiateClone("CaptainVulcanSlug", true);
            vulcanSlugPrefab.transform.localScale = Vector3.one * 0.3f;

            CoreModules.Assets.projectilePrefabs.Add(vulcanSlugPrefab);
        }
    }
}
