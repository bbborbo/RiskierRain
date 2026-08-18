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
using SurvivorTweaks.Modules;
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

        [AutoConfig("Huntress : Base Damage Stat", "Scales 20% per level. Vanilla is 12", 12f)]
        public static float baseDamage = 12f; //12

        [AutoConfig("Ability Tweaks (Secondary) : Laser Glaive : Damage Coefficient", "Expressed as a percentage (eg 3.4 is 340%). Vanilla is 2.5", 3.4f)]
        public static float glaiveBaseDamage = 3.4f; //2.5f
        [AutoConfig("Ability Tweaks (Secondary) : Laser Glaive : Damage Multiplier Per Bounce", "Exponential. Vanilla is 1.1", 1.1f)]
        public static float glaiveBounceDamage = 1.1f; //1.1f

        [AutoConfig("Ability Tweaks (Special) : Armor Boost While Aiming", 
            "If true, Huntress gains a small armor boost (value unbeknownst to me) while aiming either of her Special abilities. Vanilla is false", true)]
        public static bool huntressUltProtection = true;

        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Base Cooldown", "Expressed in seconds. Vanilla is 12", 22f)]
        public static float arrowRainCooldown = 22; //12
        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Damage Area Radius", "Expressed in meters. Vanilla is 7.5", 14f)]
        public static float arrowRainRadius = 14; // 7.5f
        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Tick Proc Coefficient", "Vanilla is 0.2", 0.3f)]
        public static float arrowRainProcCoeff = 0.3f; //0.2f
        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Damage Coefficient Per Second", "Expressed as a percentage (eg 4.0 is 400%). Vanilla is 3.3", 14f)]
        public static float arrowRainDamageCoeffPerSecond = 4f; //3.3f
        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Tick Frequency", "Expressed in ticks per second. Vanilla is 3", 4f)]
        public static float arrowRainHitFrequency = 4f; //3f
        [AutoConfig("Ability Tweaks (Special) : Arrow Rain : Damage Area Duration", "Maximum duration of damage area. Expressed in seconds. Vanilla is 6", 8f)]
        public static float arrowRainLifetime = 8f; //6f

        [AutoConfig("Ability Tweaks (Special) : Ballista : Base Cooldown", "Expressed in seconds. Vanilla is 12", 18f)]
        public static float ballistaCooldown = 18; //12
        [AutoConfig("Ability Tweaks (Special) : Ballista : Damage Coefficient", "Expressed as a percentage (eg 7.0 is 700%). Vanilla is 9", 7f)]
        public static float ballistaDamageCoefficient = 7f; //9
        [AutoConfig("Ability Tweaks (Special) : Ballista : Proc Coefficient", "Vanilla is 1", 2f)]
        public static float ballistaProcCoefficient = 2.0f; //1.0f
        [AutoConfig("Ability Tweaks (Special) : Ballista : Slayer Damage Type", "If true, Ballista deals more damage to targets with lower health. Vanilla is false", true)]
        public static bool ballistaSlayer = true;

        public override string survivorName => "Huntress";

        public override string bodyName => "HuntressBody";

        public override void Init()
        {
            base.Init();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Huntress.HuntressBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);

                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseDamage = baseDamage;
                body.levelDamage = body.baseDamage * 0.2f;

                ChangeVanillaPrimary(primary);
                ChangeVanillaSecondaries(secondary);
                ChangeVanillaUtilities(utility);
                ChangeVanillaSpecials(special);
            });
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
            if (huntressUltProtection)
            {
                On.EntityStates.Huntress.BaseArrowBarrage.OnEnter += AddHuntressUltProtection;
                On.EntityStates.Huntress.BaseArrowBarrage.OnExit += RemoveHuntressUltProtection;
            }

            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Huntress.HuntressArrowRain_prefab, (arrowRainPrefab) =>
            {
                family.variants[0].skillDef.baseRechargeInterval = arrowRainCooldown;
                ArrowRain.arrowRainRadius = arrowRainRadius;

                arrowRainPrefab.transform.localScale = Vector3.one * 2 * arrowRainRadius;
                ProjectileDotZone arrowRainDotZone = arrowRainPrefab.GetComponent<ProjectileDotZone>();
                if (arrowRainDotZone != null)
                {
                    arrowRainDotZone.damageCoefficient = arrowRainDamageCoeffPerSecond / (2.2f * arrowRainHitFrequency);
                    arrowRainDotZone.resetFrequency = arrowRainHitFrequency;
                    arrowRainDotZone.overlapProcCoefficient = arrowRainProcCoeff;
                    arrowRainDotZone.lifetime = arrowRainLifetime;
                }
            });
            On.EntityStates.Huntress.ArrowRain.OnEnter += BuffArrowRain;
            LanguageAPI.Add("HUNTRESS_SPECIAL_DESCRIPTION", $"<style=cIsUtility>Teleport</style> into the sky. " +
                $"Target an area to rain arrows, <style=cIsUtility>slowing</style> all enemies and " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(arrowRainDamageCoeffPerSecond)} damage per second</style>.");

            SkillDef ballista = family.variants[1].skillDef;
            ballista.baseRechargeInterval = ballistaCooldown;
            if(ballistaSlayer)
                ballista.keywordTokens = new string[] { "KEYWORD_SLAYER" };
            On.EntityStates.GenericBulletBaseState.OnEnter += BallistaBuff;
            if(ballistaSlayer)
                On.EntityStates.Huntress.Weapon.FireArrowSnipe.ModifyBullet += BallistaDamageType;
            LanguageAPI.Add("HUNTRESS_SPECIAL_ALT1_DESCRIPTION", 
                (ballistaSlayer == true ? $"<style=cIsDamage>Slayer</style>. " : "") +
                $"<style=cIsUtility>Teleport</style> backwards into the sky. " +
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
