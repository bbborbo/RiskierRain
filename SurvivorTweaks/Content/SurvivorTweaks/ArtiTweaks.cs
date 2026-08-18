using SurvivorTweaks.Modules;
using EntityStates.Mage;
using EntityStates.Mage.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;

namespace SurvivorTweaks.SurvivorTweaks
{
    class ArtiTweaks : SurvivorTweakBase<ArtiTweaks>
    {
        [AutoConfig("Artificer : Base Damage Stat", "Scales 20% per level. Vanilla is 12", 12)]
        public static float artiBaseDamage = 12f; //12f

        [AutoConfig("Ability Tweaks (Passive) : Hover : Use Speed Boost", "Vanilla is false", true)]
        public static bool hoverUseSpeedBoost = true;
        [AutoConfig("Ability Tweaks (Passive) : Hover : Max Gravity", "Expressed in meters per second. Vanilla is idk", -0.2f)]
        public static float hoverFallSpeed = -0.2f; //idk
        [AutoConfig("Ability Tweaks (Passive) : Hover : Speed Boost", "Additive with Goat Hoof. Expressed as a percentage (eg 0.15 is 15%)", 0.15f)]
        public static float jetpackSpeedPercent = 0.15f;

        [AutoConfig("Ability Tweaks (Secondary) : Nanobomb : Max Damage Coefficient", "Expressed as a percentage (eg 14.0 is 1400%). Vanilla is 20", 14)]
        public static float nanobombMaxDamageCoefficient = 14f; //20f

        [AutoConfig("Ability Tweaks (Utility) : Snapfreeze : Base Cooldown", "Expressed in seconds. Vanilla is 12", 8f)]
        public static float snapfreezeBaseCooldown = 8f; //12f
        [AutoConfig("Ability Tweaks (Utility) : Snapfreeze : Projectile Scale", "Vanilla is 1", 2.5f)]
        public static float snapfreezeColliderScale = 2.5f;//1f
        [AutoConfig("Ability Tweaks (Utility) : Snapfreeze : Projectile Blast Radius", "Expressed in meters. Vanilla is 2.5", 4f)]
        public static float snapfreezeBlastRadius = 4f;//2.5f
        [AutoConfig("Ability Tweaks (Utility) : Snapfreeze : Deployment Velocity", "Expressed in meters per second. Affects distance between pillar projectiles. Vanilla is 40", 50f)]
        public static float snapfreezeDeploymentVelocity = 50f;//40

        [AutoConfig("Ability Tweaks (Special) : Flamethrower : Total Damage Coefficient", 
            "Expressed as a percentage (eg 28.0 is 2800%). Vanilla is 20. To match Pre-SOTV damage, set to 34", 28)]
        public static float flamethrowerDamage = 28; //20 vanilla, 34 pre-nerf glory
        [AutoConfig("Ability Tweaks (Special) : Flamethrower : Max Range", "Expressed in meters. Vanilla is 21", 26)]
        public static float flamethrowerRange = 26; //21
        public override string survivorName => "Artificer";

        public override string bodyName => "MageBody";

        public static string flamethrowerDesc;
        public override void Init()
        {
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Mage.MageBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                CharacterBody mageBody = bodyObject.GetComponent<CharacterBody>();
                mageBody.baseDamage = artiBaseDamage;
                mageBody.levelDamage = artiBaseDamage * 0.2f;

                SkillDef snapfreeze = utility.variants[0].skillDef;
                snapfreeze.baseRechargeInterval = snapfreezeBaseCooldown;
            });

            #region Hover

            On.EntityStates.Mage.JetpackOn.OnEnter += (orig, self) =>
            {
                JetpackOn.hoverVelocity = hoverFallSpeed;
                if (NetworkServer.active && hoverUseSpeedBoost)
                {
                    self.characterBody.AddBuff(CommonAssets.jetpackSpeedBoost);
                }
                orig(self);
            };
            On.EntityStates.Mage.JetpackOn.OnExit += (orig, self) =>
            {
                if (NetworkServer.active && self.HasBuff(CommonAssets.jetpackSpeedBoost))
                {
                    self.characterBody.RemoveBuff(CommonAssets.jetpackSpeedBoost);
                }
                orig(self);
            };

            LanguageAPI.Add("MAGE_PASSIVE_DESCRIPTION",
                "Holding the Jump key causes the Artificer to <style=cIsUtility>hover in the air</style>. Move faster while hovering.");
            #endregion

            #region Nanobomb
            On.EntityStates.Mage.Weapon.BaseThrowBombState.OnEnter += (orig, self) =>
            {
                bool isBomb = self is ThrowNovabomb;
                if (isBomb)
                {
                    self.maxDamageCoefficient = nanobombMaxDamageCoefficient;
                }
                orig(self);
            };

            LanguageAPI.Add("MAGE_SECONDARY_LIGHTNING_DESCRIPTION",
                $"<style=cIsDamage>Stunning</style>. Charge up an <style=cIsDamage>exploding</style> nano-bomb that " +
                $"deals <style=cIsDamage>400%-{nanobombMaxDamageCoefficient.AsPercent()}</style> damage.");
            #endregion

            #region Snapfreeze

            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Mage.MageIcewallPillarProjectile_prefab, (iceWallPillarPrefab) =>
            {
                iceWallPillarPrefab.transform.localScale = Vector3.one * snapfreezeColliderScale;
                if(iceWallPillarPrefab.TryGetComponent(out ProjectileImpactExplosion pie))
                {
                    pie.blastRadius = snapfreezeBlastRadius;
                }
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Mage.MageIcewallWalkerProjectile_prefab, (iceWallWalkerPrefab) =>
            {
                if (iceWallWalkerPrefab.TryGetComponent(out ProjectileCharacterController pcc))
                {
                    pcc.velocity = snapfreezeDeploymentVelocity;
                }

                //if (iceWallWalkerPrefab.TryGetComponent(out ProjectileMageFirewallWalkerController pmfwc))
                //{
                //    //pmfwc.curveToCenter = true;
                //}
            });
            #endregion

            #region Flamethrower
            //self.totalDamageCoefficient = flamethrowerDamage; // 20, 34 for pre-nerf
            LanguageAPI.Add("MAGE_SPECIAL_FIRE_DESCRIPTION", $"Burn all enemies in front of you for <style=cIsDamage>{Tools.ConvertDecimal(flamethrowerDamage)} damage</style>. " +
                $"Each hit has a <style=cIsDamage>50% chance</style> to <style=cIsDamage>Ignite</style>.");
            On.EntityStates.Mage.Weapon.Flamethrower.OnEnter += (orig, self) =>
            {
                self.maxDistance = flamethrowerRange;
                self.totalDamageCoefficient = flamethrowerDamage; // 20, 34 for pre-nerf
                orig(self);
            };
                //On.EntityStates.Mage.Weapon.Flamethrower.OnEnter += (orig, self) =>
                //{
                //    self.baseFlamethrowerDuration = 3;
                //    self.tickFrequency = 7;
                //    self.totalDamageCoefficient = 16.23f;
                //    Flamethrower.procCoefficientPerTick = 0.8f;
                //
                //    orig(self);
                //    float aspd = self.attackSpeedStat;
                //    float aspdSqrt = Mathf.Sqrt(aspd);
                //
                //    if (aspd != 0)
                //    {
                //        float damageCoeff = self.totalDamageCoefficient * aspdSqrt;
                //        float endDuration = self.baseFlamethrowerDuration / aspdSqrt;
                //
                //        //total ticks increases by aspdSqrt, end duration
                //        float totalTicks = self.baseFlamethrowerDuration * self.tickFrequency * aspdSqrt;
                //
                //        //self.flamethrowerDuration = endDuration;
                //        //self.tickDamageCoefficient = (damageCoeff / totalTicks);
                //        self.tickFrequency *= aspdSqrt;
                //    }
                //};
                //flamethrowerDesc = "Burn all enemies in front of you for <style=cIsDamage>1700% damage</style>. " +
                //    "Each hit has a <style=cIsDamage>50% chance to ignite</style>.";
                //LanguageAPI.Add("MAGE_SPECIAL_FIRE_DESCRIPTION",
                //    flamethrowerDesc);
            #endregion
        }
    }
}
