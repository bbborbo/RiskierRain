using BepInEx;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RainrotSharedUtils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SurvivorTweaks.Modules;
using System;
using UnityEngine;
using static MoreStats.StatHooks;

namespace SurvivorTweaks.SurvivorTweaks
{
    class RexTweaks : SurvivorTweakBase<RexTweaks>
    {
        [AutoConfig("Ability Tweaks (Primary) : Directive Inject : Damage Coefficient Per Projectile", "Expressed as a percentage (eg 0.8 is 80%). Vanilla is 0.8", 0.8f)]
        public static float syringeDamageCoefficient = 0.8f; // 0.8f
        [AutoConfig("Ability Tweaks (Primary) : Directive Inject : Heal Fraction (Final Projectile)", "Expressed as a percentage (eg 0.3 is 30%). Vanilla is 0.6", 0.3f)]
        public static float syringeHealFraction = 0.3f; // 0.6f

        [AutoConfig("Ability Tweaks (Secondary) : Seed Barrage (Mortar) : Damage Coefficient", "Expressed as a percentage (eg 6.0 is 60%). Vanilla is 4.5", 6.0f)]
        public static float mortarDamageCoeff = 6f;//4.5f
        [AutoConfig("Ability Tweaks (Secondary) : Seed Barrage (Mortar) : Base Cooldown", "Expressed in seconds. Vanilla is 0.5", 0.5f)]
        public static float mortarCooldown = 0.5f;//0.5f
        [AutoConfig("Ability Tweaks (Secondary) : Directive Drill : Base Cooldown", "Expressed in seconds. Vanilla is 6", 4f)]
        public static float drillCooldown = 4f;//6f
        [AutoConfig("Ability Tweaks (Secondary) : Directive Drill : Base Max Stock", "Vanilla is 1", 1)]
        public static int drillMaxStock = 1; //1

        [AutoConfig("Ability Tweaks (Utility) : Bramble Volley : Heal Fraction", "Expressed as a percentage (eg 0.07 is 7%). Vanilla is 0.1", 0.07f)]
        public float brambleHealFraction = 0.07f; // 0.1f

        public override string bodyName => "TreebotBody";

        public override string survivorName => "REX";

        public override void Init()
        {
            base.Init();
            GetBodyObject();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                ChangeVanillaSecondaries(secondary);
                special.variants[0].skillDef.keywordTokens = new string[1] { SharedUtilsPlugin.executeKeywordToken };
            });

            //primary
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Treebot.SyringeProjectileHealing_prefab, (syringeB) =>
            {
                if (syringeB.TryGetComponent(out ProjectileHealOwnerOnDamageInflicted phoodi))
                {
                    phoodi.fractionOfDamage = syringeHealFraction;
                }
            });
            On.EntityStates.Treebot.Weapon.FireSyringe.OnEnter += NerfSyringe;
            LanguageAPI.Add("TREEBOT_PRIMARY_DESCRIPTION",
                $"Fire 3 syringes for <style=cIsDamage>3x{Tools.ConvertDecimal(syringeDamageCoefficient)} damage</style>. " +
                $"The last syringe <style=cIsDamage>Weakens</style> and <style=cIsHealing>heals for {Tools.ConvertDecimal(syringeHealFraction)} of damage dealt</style>.");

            //secondary
            LanguageAPI.Add("TREEBOT_SECONDARY_DESCRIPTION", 
                $"<style=cIsHealth>15% HP</style>. " +
                $"Launch a mortar into the sky for <style=cIsDamage>{Tools.ConvertDecimal(mortarDamageCoeff)} damage</style>.");

            //utility
            On.EntityStates.Treebot.Weapon.FirePlantSonicBoom.OnEnter += NerfBrambleVolley;

            //special
            GetMoreStatCoefficients += HarvestFinisher;
            On.EntityStates.Treebot.TreebotFireFruitSeed.OnEnter += FireFruitEnter;
            LanguageAPI.Add("TREEBOT_SPECIAL_ALT1_DESCRIPTION",
                $"<style=cIsHealth>Finisher</style>. Fire a <style=cIsDamage>injection</style> that deals <style=cIsDamage>330% damage</style>. " +
                $"When killed, injected enemies drop multiple " +
                $"<style=cIsHealing>fruits</style> that heal for <style=cIsHealing>25% HP</style>.");
        }

        private void HarvestFinisher(CharacterBody sender, MoreStatHookEventArgs args)
        {
            bool hasRexHarvestBuff = sender.HasBuff(RoR2Content.Buffs.Fruiting);
            args.ModifyBaseExecutionThreshold(SharedUtilsPlugin.GetSurvivorExecuteThreshold(sender.isBoss), hasRexHarvestBuff);
        }

        private void NerfSyringe(On.EntityStates.Treebot.Weapon.FireSyringe.orig_OnEnter orig, FireSyringe self)
        {
            FireSyringe.damageCoefficient = syringeDamageCoefficient;
            orig(self);
        }
        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            SkillDef drill = family.variants[0].skillDef;
            drill.baseRechargeInterval = 4f;
            drill.baseMaxStock = drillMaxStock;

            SkillDef mortar = family.variants[1].skillDef;
            mortar.baseRechargeInterval = mortarCooldown;

            On.EntityStates.Treebot.Weapon.FireMortar2.OnEnter += (orig, self) =>
            {
                FireMortar2.damageCoefficient = mortarDamageCoeff;
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
