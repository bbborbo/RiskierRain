using EntityStates;
using EntityStates.Huntress;
using EntityStates.Huntress.HuntressWeapon;
using EntityStates.Huntress.Weapon;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SurvivorTweaks.SurvivorTweaks
{
    class HuntressTweaks : SurvivorTweakBase<HuntressTweaks>
    {
        public static bool isLoaded;
        public static GameObject arrowRainPrefab;

        public override string survivorName => "Huntress";

        public override string bodyName => "HuntressBody";

        static float baseDamage = 14f; //12

        static float glaiveBaseDamage = 3.4f; //2.5f
        static float glaiveBounceDamage = 1.1f; //1.1f


        static int arrowRainCooldown = 22; //12
        static float arrowRainRadius = 14; // 7.5f
        static float arrowRainProcCoeff = 0.3f; //0.2f
        static float arrowRainDamageCoeffPerSecond = 4f; //3.3f
        static float arrowRainHitFrequency = 4f; //3f
        static float arrowRainLifetime = 8f; //6f

        static int ballistaCooldown = 18; //12
        static float ballistaDamageCoefficient = 8f; //9
        static float ballistaProcCoefficient = 2.0f; //1.0f

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);

            CharacterBody body = bodyObject.GetComponent<CharacterBody>();
            body.baseDamage = baseDamage;
            body.levelDamage = body.baseDamage * 0.2f;

            ChangeVanillaPrimary(primary);
            ChangeVanillaSecondaries(secondary);
            ChangeVanillaUtilities(utility);
            ChangeVanillaSpecials(special);
        }

        private void ChangeVanillaPrimary(SkillFamily family)
        {
        }

        private void ChangeVanillaSecondaries(SkillFamily family)
        {
            LanguageAPI.Add("HUNTRESS_SECONDARY_DESCRIPTION", $"Throw a seeking glaive that bounces " +
                $"up to <style=cIsDamage>6</style> times " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(glaiveBaseDamage)} damage</style>. " +
                $"Damage increases by <style=cIsDamage>{Tools.ConvertDecimal(glaiveBounceDamage - 1)}</style> per bounce.");
            On.EntityStates.Huntress.HuntressWeapon.ThrowGlaive.OnEnter += BuffGlaive;
            On.RoR2.Orbs.LightningOrb.PickNextTarget += ChangeGlaiveTargeting;
            On.RoR2.Orbs.LightningOrb.Begin += ChangeGlaiveProperties;
        }

        private void ChangeGlaiveProperties(On.RoR2.Orbs.LightningOrb.orig_Begin orig, RoR2.Orbs.LightningOrb self)
        {
            orig(self);
            if (self.lightningType != RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
                return;

            self.canBounceOnSameTarget = false;
        }

        private HurtBox ChangeGlaiveTargeting(On.RoR2.Orbs.LightningOrb.orig_PickNextTarget orig, RoR2.Orbs.LightningOrb self, Vector3 position)
        {
            if(self.lightningType != RoR2.Orbs.LightningOrb.LightningType.HuntressGlaive)
                return orig(self, position);

            int lastBounce = self.bouncedObjects.Count;
            int i = lastBounce % 2;
            if(self.bouncedObjects.Count > i)
            {
                HealthComponent hc = self.bouncedObjects[i];
                if (hc != null && hc.alive)
                {
                    HurtBox hb = hc.body.mainHurtBox;
                    if (hb)
                    {
                        return hb;
                    }
                    else
                        Log.Error("glaive orb target has no hurtbox!");
                }
            }

            HurtBox newTarget = orig(self, position);
            if(newTarget != null)
            {
                if (self.bouncedObjects.Count > i)
                    self.bouncedObjects[i] = newTarget.healthComponent;
            }
            return newTarget;
        }

        private void BuffGlaive(On.EntityStates.Huntress.HuntressWeapon.ThrowGlaive.orig_OnEnter orig, ThrowGlaive self)
        {
            ThrowGlaive.damageCoefficient = glaiveBaseDamage;
            ThrowGlaive.damageCoefficientPerBounce = glaiveBounceDamage;
            orig(self);
        }

        private void ChangeVanillaUtilities(SkillFamily family)
        { 
        }

        void ChangeVanillaSpecials(SkillFamily family)
        {
            On.EntityStates.Huntress.BaseArrowBarrage.OnEnter += AddHuntressUltProtection;
            On.EntityStates.Huntress.BaseArrowBarrage.OnExit += RemoveHuntressUltProtection;

            arrowRainPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Huntress/HuntressArrowRain.prefab").WaitForCompletion();
            family.variants[0].skillDef.baseRechargeInterval = arrowRainCooldown;
            ArrowRain.arrowRainRadius = arrowRainRadius;
            On.EntityStates.Huntress.ArrowRain.OnEnter += BuffArrowRain;

            arrowRainPrefab.transform.localScale = Vector3.one * 2 * arrowRainRadius;
            ProjectileDotZone arrowRainDotZone = arrowRainPrefab?.GetComponent<ProjectileDotZone>();
            if(arrowRainDotZone != null)
            {
                arrowRainDotZone.damageCoefficient = arrowRainDamageCoeffPerSecond / (2.2f * arrowRainHitFrequency);
                arrowRainDotZone.resetFrequency = arrowRainHitFrequency;
                LanguageAPI.Add("HUNTRESS_SPECIAL_DESCRIPTION", $"<style=cIsUtility>Teleport</style> into the sky. " +
                    $"Target an area to rain arrows, <style=cIsUtility>slowing</style> all enemies and " +
                    $"dealing <style=cIsDamage>{Tools.ConvertDecimal(arrowRainDamageCoeffPerSecond)} damage per second</style>.");
                arrowRainDotZone.overlapProcCoefficient = arrowRainProcCoeff;
                arrowRainDotZone.lifetime = arrowRainLifetime;
            }

            SkillDef ballista = family.variants[1].skillDef;
            ballista.baseRechargeInterval = ballistaCooldown;
            ballista.keywordTokens = new string[] { "KEYWORD_SLAYER" };
            On.EntityStates.GenericBulletBaseState.OnEnter += BallistaBuff;
            On.EntityStates.Huntress.Weapon.FireArrowSnipe.ModifyBullet += BallistaDamageType;
            LanguageAPI.Add("HUNTRESS_SPECIAL_ALT1_DESCRIPTION", $"<style=cIsDamage>Slayer</style>. <style=cIsUtility>Teleport</style> backwards into the sky. " +
                $"Fire up to <style=cIsDamage>3</style> energy bolts, " +
                $"dealing <style=cIsDamage>3x{Tools.ConvertDecimal(ballistaDamageCoefficient)} damage</style>.");
        }

        private void BallistaDamageType(On.EntityStates.Huntress.Weapon.FireArrowSnipe.orig_ModifyBullet orig, FireArrowSnipe self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damageType.damageType |= DamageType.BonusToLowHealth;
        }

        private void AddHuntressUltProtection(On.EntityStates.Huntress.BaseArrowBarrage.orig_OnEnter orig, BaseArrowBarrage self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody)
            {
                self.characterBody.AddBuff(RoR2Content.Buffs.SmallArmorBoost);
            }
        }

        private void RemoveHuntressUltProtection(On.EntityStates.Huntress.BaseArrowBarrage.orig_OnExit orig, BaseArrowBarrage self)
        {
            orig(self);
            if (NetworkServer.active && self.characterBody && self.characterBody.HasBuff(RoR2Content.Buffs.SmallArmorBoost))
            {
                self.characterBody.RemoveBuff(RoR2Content.Buffs.SmallArmorBoost);
            }
        }

        private void BallistaBuff(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if(self is FireArrowSnipe)
            {
                self.damageCoefficient = ballistaDamageCoefficient;
                self.procCoefficient = ballistaProcCoefficient;
            }
            orig(self);
        }

        private void BuffArrowRain(On.EntityStates.Huntress.ArrowRain.orig_OnEnter orig, EntityStates.Huntress.ArrowRain self)
        {
            ArrowRain.arrowRainRadius = arrowRainRadius;
            orig(self);
        }
    }
}
