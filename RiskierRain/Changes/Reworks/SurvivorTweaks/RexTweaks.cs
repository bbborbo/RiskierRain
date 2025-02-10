using BepInEx;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RainrotSharedUtils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using UnityEngine;
using static RainrotSharedUtils.StatHooks;

namespace RiskierRain.SurvivorTweaks
{
    class RexTweaks : SurvivorTweakModule
    {
        GameObject syringeB = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/SyringeProjectileHealing");
        float syringeDamageCoefficient = 0.8f; // 0.8f
        float syringeHealFraction = 0.3f; // 0.6f

        float barrageDamageCoeff = 6f;

        float brambleHealFraction = 0.07f; // 0.1f

        public override string bodyName => "TreebotBody";

        public override string survivorName => "REX";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            //primary
            syringeB.GetComponent<ProjectileHealOwnerOnDamageInflicted>().fractionOfDamage = syringeHealFraction;
            On.EntityStates.Treebot.Weapon.FireSyringe.OnEnter += NerfSyringe;
            LanguageAPI.Add("TREEBOT_PRIMARY_DESCRIPTION",
                $"Fire 3 syringes for <style=cIsDamage>3x{Tools.ConvertDecimal(syringeDamageCoefficient)} damage</style>. " +
                $"The last syringe <style=cIsDamage>Weakens</style> and <style=cIsHealing>heals for {Tools.ConvertDecimal(syringeHealFraction)} of damage dealt</style>.");

            //secondary
            ChangeVanillaSecondaries(secondary);
            LanguageAPI.Add("TREEBOT_SECONDARY_DESCRIPTION", 
                $"<style=cIsHealth>15% HP</style>. " +
                $"Launch a mortar into the sky for <style=cIsDamage>{Tools.ConvertDecimal(barrageDamageCoeff)} damage</style>.");

            //utility
            On.EntityStates.Treebot.Weapon.FirePlantSonicBoom.OnEnter += NerfBrambleVolley;

            //special
            GetExecutionThreshold += HarvestFinisher;
            On.EntityStates.Treebot.TreebotFireFruitSeed.OnEnter += FireFruitEnter;
            special.variants[0].skillDef.keywordTokens = new string[1] { CoreModules.Assets.executeKeywordToken };
            LanguageAPI.Add("TREEBOT_SPECIAL_ALT1_DESCRIPTION",
                $"<style=cIsHealth>Finisher</style>. Fire a <style=cIsDamage>injection</style> that deals <style=cIsDamage>330% damage</style>. " +
                $"When killed, injected enemies drop multiple " +
                $"<style=cIsHealing>fruits</style> that heal for <style=cIsHealing>25% HP</style>.");
        }

        private void HarvestFinisher(CharacterBody sender, ref float executeThreshold)
        {
            bool hasRexHarvestBuff = sender.HasBuff(RoR2Content.Buffs.Fruiting);
            executeThreshold = ModifyExecutionThreshold(executeThreshold, SharedUtilsPlugin.survivorExecuteThreshold, hasRexHarvestBuff);
        }

        private void NerfSyringe(On.EntityStates.Treebot.Weapon.FireSyringe.orig_OnEnter orig, FireSyringe self)
        {
            FireSyringe.damageCoefficient = syringeDamageCoefficient;
            orig(self);
        }

        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            family.variants[0].skillDef.baseRechargeInterval = 4f;
            family.variants[1].skillDef.baseRechargeInterval = 0.75f;

            On.EntityStates.Treebot.Weapon.FireMortar2.OnEnter += (orig, self) =>
            {
                FireMortar2.damageCoefficient = barrageDamageCoeff;
                orig(self);
            };
        }

        private void NerfBrambleVolley(On.EntityStates.Treebot.Weapon.FirePlantSonicBoom.orig_OnEnter orig, FirePlantSonicBoom self)
        {
            FirePlantSonicBoom.healthFractionPerHit = brambleHealFraction;
            FirePlantSonicBoom.healthCostFraction = 0.2f;
            orig(self);
        }

        private void FireFruitEnter(On.EntityStates.Treebot.TreebotFireFruitSeed.orig_OnEnter orig, EntityStates.Treebot.TreebotFireFruitSeed self)
        {
            self.baseDuration = 0.5f;
            orig(self);
        }
    }
}
