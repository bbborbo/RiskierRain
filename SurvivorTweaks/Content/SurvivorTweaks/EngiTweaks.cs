using EntityStates;
using EntityStates.Engi.EngiBubbleShield;
using EntityStates.Engi.EngiWeapon;
using EntityStates.Engi.Mine;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RainrotSharedUtils;
using RainrotSharedUtils.Shelters;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SurvivorTweaks.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SurvivorTweaks.Modules.Language.Styling;

namespace SurvivorTweaks.SurvivorTweaks
{
    class EngiTweaks : SurvivorTweakBase<EngiTweaks>
    {
        private static bool grenadesRequireCharge => grenadeChargeTime > 0;
        private static GameObject bubbleShieldPrefab;

        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Base Cooldown", "Expressed in seconds. Vanilla is 0", 1.2f)]
        public static float grenadeCooldown = 1.2f;
        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Damage Coefficient", "Expressed as a percentage (eg 1.3 is 130%). Vanilla is 1", 1.3f)]
        public static float grenadeDamage = 1.3f; //1.0f
        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Projectile Stun Chance", "Expressed as a chance out of 100. Vanilla is 0", 25f)]
        public static float grenadeStunChance = 25; //0
        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Max Projectile Count", "Vanilla is 8", 3)]
        public static int grenadeCount = 3;
        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Max Charge Time", "Set to 0 to skip charge. Vanilla is 2", 0)]
        public static float grenadeChargeTime = 0f;
        [AutoConfig("Ability Tweaks (Primary) : Bouncing Grenades : Base Max Stock", "Vanilla is N/A", 1)]
        public static int grenadeStock = 1;

        [AutoConfig("Ability Tweaks (Secondary) : Pressure Mines : Arming Duration", "Expressed in seconds. Vanilla is 3", 2)]
        public static float mineArmingDuration = 2f;//3f
        [AutoConfig("Ability Tweaks (Secondary) : Pressure Mines : Detection Range (Armed)", "Expressed in seconds. Vanilla is 7.5", 7.5)]
        public static float mineArmedTriggerRange = 7.5f;//7.5f
        [AutoConfig("Ability Tweaks (Secondary) : Pressure Mines : Blast Range (Armed)", "Expressed in meters. Vanilla is 7.5", 10)]
        public static float mineArmedBlastRange = 10f;//7.5f

        [AutoConfig("Ability Tweaks (Utility) : Bubble Shield : Shield Range", "Expressed in meters. Vanilla is 10", 15)]
        public static float bubbleShieldRadius = 15;//10
        [AutoConfig("Ability Tweaks (Utility) : Bubble Shield : Kit Slow", "If true, Bubble Shield will apply Kit Slow to enemies within range. Vanilla is false", true)]
        public static bool bubbleKitSlow = true;
        public override string survivorName => "Engineer";

        public override string bodyName => "ENGIBODY";

        public override void Init()
        {
            ShelterUtilsModule.UseCustomShelters = true;
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Engi.EngiBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                //primary
                DoPrimary(primary);
                //utility
                DoUtility(utility);
            });

            //secondary
            IL.EntityStates.Engi.Mine.Detonate.Explode += DetonationRadiusBoost;
            On.EntityStates.Engi.Mine.MineArmingWeak.FixedUpdate += ChangeMineArmTime;
        }

        private void DoPrimary(SkillFamily primary)
        {
            SkillDef nade = primary.variants[0].skillDef;
            nade.cancelSprintingOnActivation = false;
            nade.keywordTokens = new string[] { "KEYWORD_AGILE", "KEYWORD_STUNNING" };
            if(grenadesRequireCharge)
                nade.activationState = new SerializableEntityStateType(typeof(FireGrenades));
            if(grenadeCooldown > 0)
            {
                nade.stockToConsume = 1;
                nade.baseMaxStock = grenadeStock;
                nade.rechargeStock = grenadeStock;
                nade.baseRechargeInterval = grenadeCooldown;
                nade.beginSkillCooldownOnSkillEnd = true;
                nade.resetCooldownTimerOnUse = false;
            }

            //primary
            LanguageAPI.Add(nade.skillDescriptionToken, 
                $"<style=cIsUtility>Agile</style>. <style=cIsDamage>Stunning</style>. " +
                $"Fire <style=cIsDamage>{grenadeCount}</style> grenades that deal " +
                $"<style=cIsDamage>{ConvertDecimal(grenadeDamage)} damage</style> each.");

            On.EntityStates.Engi.EngiWeapon.FireGrenades.OnEnter += GrenadeStats;
            IL.EntityStates.Engi.EngiWeapon.FireGrenades.FireGrenade += GrenadeStunChance;
        }

        private void GrenadeStats(On.EntityStates.Engi.EngiWeapon.FireGrenades.orig_OnEnter orig, FireGrenades self)
        {
            FireGrenades.damageCoefficient = grenadeDamage;
            self.grenadeCountMax = grenadeCount;
            orig(self);
        }

        private void GrenadeStunChance(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<ProjectileManager>(nameof(ProjectileManager.FireProjectile))
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<FireProjectileInfo, EntityState, FireProjectileInfo>>((projectileInfo, self) =>
            {
                if(Util.CheckRoll(grenadeStunChance, self.characterBody.master))
                {
                    projectileInfo.damageTypeOverride = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.Primary);
                }
                projectileInfo.force = 100f;
                return projectileInfo;
            });
        }

        private void DoUtility(SkillFamily slot)
        {
            LanguageAPI.Add("ENGI_UTILITY_DESCRIPTION", 
                $"<style=cIsUtility>Sheltering</style>. " +
                $"Place an <style=cIsUtility>impenetrable shield</style> that " +
                $"blocks all incoming damage" +
                (bubbleKitSlow == true ? $", and <style=cIsUtility>slows enemies</style> inside." : ".")
                );

            SkillDef bubbleSkill = slot.variants[0].skillDef;
            bubbleSkill.keywordTokens = new string[] { SharedUtilsPlugin.shelterKeywordToken };

            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Engi.EngiBubbleShield_prefab, (bubbleShield) =>
            {
                bubbleShieldPrefab = bubbleShield;

                Transform bubble = bubbleShieldPrefab.transform.Find("Collision");//FindChild(Deployed.childLocatorString).gameObject;
                bubble.localScale = Vector3.one * bubbleShieldRadius * 2;

                ShelterProviderBehavior shelter = bubble.gameObject.AddComponent<ShelterProviderBehavior>();
                if (shelter)
                {
                    shelter.fallbackRadius = bubbleShieldRadius;
                }

                if(bubbleKitSlow == true)
                {
                    BuffWard buffWard = bubble.gameObject.AddComponent<BuffWard>();
                    buffWard.buffDef = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow50.asset").WaitForCompletion();
                    buffWard.buffDuration = 0.3f;
                    buffWard.interval = 0.2f;
                    buffWard.radius = bubbleShieldRadius;
                    buffWard.invertTeamFilter = true;
                }
            });
            //On.EntityStates.Engi.EngiWeapon.FireMines.OnEnter += ReplaceBubbleShieldPrefab;
            On.EntityStates.Engi.EngiBubbleShield.Deployed.FixedUpdate += BubbleBuffwardTeam;
        }

        private void BubbleBuffwardTeam(On.EntityStates.Engi.EngiBubbleShield.Deployed.orig_FixedUpdate orig, Deployed self)
        {
            bool deployed = self.hasDeployed;
            orig(self);
            if(!deployed && self.hasDeployed)
            {
                BuffWard buffWard = self.gameObject.GetComponentInChildren<BuffWard>();
                if(buffWard != null)
                {
                    buffWard.teamFilter = self.outer.GetComponent<TeamFilter>();
                }
            }
        }

        private void ReplaceBubbleShieldPrefab(On.EntityStates.Engi.EngiWeapon.FireMines.orig_OnEnter orig, EntityStates.Engi.EngiWeapon.FireMines self)
        {
            if(self is FireBubbleShield)
            {
                self.projectilePrefab = bubbleShieldPrefab;
            }
            orig(self);
        }

        private void ChangeMineArmTime(On.EntityStates.Engi.Mine.MineArmingWeak.orig_FixedUpdate orig, MineArmingWeak self)
        {
            MineArmingWeak.duration = mineArmingDuration;
            orig(self);
        }

        private void DetonationRadiusBoost(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchStfld<BlastAttack>(nameof(BlastAttack.radius))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, EntityState, float>>((startRadius, state) =>
            {
                if(state.projectileController?.teamFilter?.teamIndex == TeamIndex.Player)
                    return mineArmedBlastRange;
                return startRadius;
            });
        }
    }
}
