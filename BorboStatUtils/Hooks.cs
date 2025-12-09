using RainrotSharedUtils.Components;
using RainrotSharedUtils.Status;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RainrotSharedUtils
{
    public static class Hooks
    {
        public static void DoHooks()
        {
            On.RoR2.IcicleAuraController.Awake += AuraControllerFix;
            On.RoR2.BuffWard.BuffTeam += ApplyDotWard;
            On.RoR2.Projectile.ProjectileOverlapLimitHits.CountOverlapHits += DecayProjectileOverlapDamage;
            On.RoR2.Projectile.ProjectileOverlapLimitHits.OnEnable += DecayProjectileRecordInitialDamage;
            On.EntityStates.ShockState.OnExit += ShockSparkOnExit;
            On.EntityStates.ShockState.OnEnter += ShockBuffEnter;
        }

        private static void DecayProjectileRecordInitialDamage(On.RoR2.Projectile.ProjectileOverlapLimitHits.orig_OnEnable orig, RoR2.Projectile.ProjectileOverlapLimitHits self)
        {
            orig(self);
            if (self is ProjectileOverlapDecayDamage)
            {
                (self as ProjectileOverlapDecayDamage).initialDamageCoefficient = self.projectileOverlapAttack.damageCoefficient;
                (self as ProjectileOverlapDecayDamage).initialProcCoefficient = self.projectileOverlapAttack.overlapProcCoefficient;
            }
        }

        private static void DecayProjectileOverlapDamage(On.RoR2.Projectile.ProjectileOverlapLimitHits.orig_CountOverlapHits orig, RoR2.Projectile.ProjectileOverlapLimitHits self)
        {
            orig(self);
            if(self is ProjectileOverlapDecayDamage)
            {
                ProjectileOverlapDecayDamage decayDamage = self as ProjectileOverlapDecayDamage;
                if (self.hitCount >= self.hitLimit)
                    return;
                self.projectileOverlapAttack.damageCoefficient = decayDamage.initialDamageCoefficient 
                    * decayDamage.firstHitDamageMultiplier * Mathf.Pow(decayDamage.onHitDamageMultiplier, self.hitCount - 1);
                self.projectileOverlapAttack.overlapProcCoefficient = decayDamage.initialProcCoefficient 
                    * decayDamage.firstHitDamageMultiplier * Mathf.Pow(decayDamage.onHitDamageMultiplier, self.hitCount - 1);
            }
        }

        private static void AuraControllerFix(On.RoR2.IcicleAuraController.orig_Awake orig, IcicleAuraController self)
        {
            orig(self);
            if(self.buffWard && self.buffWard is DotWard dotWard)
            {
                dotWard.ownerObject = self.cachedOwnerInfo.gameObject;
                dotWard.ownerBody = self.cachedOwnerInfo.characterBody;
            }
        }

        #region dot ward
        private static void ApplyDotWard(On.RoR2.BuffWard.orig_BuffTeam orig, RoR2.BuffWard self, IEnumerable<RoR2.TeamComponent> recipients, float radiusSqr, Vector3 currentPosition)
        {
            if (!(self is DotWard dotWard))
            {
                orig(self, recipients, radiusSqr, currentPosition);
                return;
            }

            if (!NetworkServer.active)
            {
                return;
            }
            if (dotWard.dotIndex == DotController.DotIndex.None)
            {
                return;
            }

            GameObject owner = dotWard.ownerObject;
            CharacterBody body = dotWard.ownerBody;
            Inventory inv = dotWard.ownerInventory;

            foreach (TeamComponent teamComponent in recipients)
            {
                Vector3 vector = teamComponent.transform.position - currentPosition;
                if (self.shape == BuffWard.BuffWardShape.VerticalTube)
                {
                    vector.y = 0f;
                }
                if (vector.sqrMagnitude <= radiusSqr)
                {
                    CharacterBody component = teamComponent.GetComponent<CharacterBody>();
                    if (component && (!self.requireGrounded || !component.characterMotor || component.characterMotor.isGrounded))
                    {
                        InflictDotInfo inflictDotInfo = new InflictDotInfo
                        {
                            attackerObject = owner,
                            victimObject = component.gameObject,
                            totalDamage = new float?(dotWard.damageCoefficient * body.damage),
                            damageMultiplier = 1f,
                            dotIndex = dotWard.dotIndex,
                            maxStacksFromAttacker = null
                        };

                        if (inv != null)
                            StrengthenBurnUtils.CheckDotForUpgrade(inv, ref inflictDotInfo);

                        DotController.InflictDot(ref inflictDotInfo);
                    }
                }
            }
        }
        #endregion

        #region shock
        private static void ShockSparkOnExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            if (ShockUtilsModule.UseShockSparks)
            {
                //entry health fraction
                float damageTaken = self.healthFraction - self.healthComponent.combinedHealthFraction;
                if (damageTaken >= self.healthFractionToForceExit)
                {
                    GameObject lastHitAttacker = self.healthComponent.lastHitAttacker;
                    if (lastHitAttacker != null)
                    {
                        CharacterBody attackerBody = lastHitAttacker.GetComponent<CharacterBody>();
                        if (attackerBody)
                        {
                            Debug.Log("break make spark");
                            NebulaPickup.CreateBoosterPickup(self.transform.position, attackerBody.teamComponent.teamIndex, RainrotSharedUtils.Assets.sparkBoosterObject, 1);
                        }
                    }
                }
            }
            orig(self);
        }

        private static void ShockHit(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (ShockUtilsModule.UseShockSparks && damageInfo.damageType.damageType.HasFlag(DamageType.Shock5s))
            {
                //GameObject attacker = damageInfo.attacker;
                self.body.AddTimedBuff(Assets.shockMarker, Assets.shockMarkerDuration);//add authority
            }
            orig(self, damageInfo);
        }


        private static void ShockBuffEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self)
        {
            orig(self);
            if (!ShockUtilsModule.UseShockSparks)
                return;
            self.healthFractionToForceExit = ShockUtilsModule.shockForceExitFraction;
        }

        private static void ShockBuffExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            if (ShockUtilsModule.UseShockSparks && self != null && self.characterBody != null)
            {
                if (self.characterBody.HasBuff(Assets.shockMarker))//it breaks here!
                {
                    HealthComponent hcVictim = self.healthComponent;
                    GameObject attackerObject = hcVictim.lastHitAttacker;
                    if (attackerObject == null)
                    {
                    }
                    else
                    {
                        CharacterBody attacker = attackerObject.GetComponent<CharacterBody>();
                        if (attacker != null)
                        {
                            if (attacker.maxShield > 0 && attacker.healthComponent?.shield != attacker.maxShield)
                            {
                                ShockHeal(attacker.healthComponent);
                            }

                        }
                    }
                }
            }
            //self.characterBody.RemoveBuff(Assets.shockMarker);
            orig(self);
        }
        private static void ShockHeal(HealthComponent attacker)
        {
            if (!attacker.body.HasBuff(Assets.shockHealCooldown))
            {
                float missingShieldPercent = (attacker.body.maxShield - attacker.shield) / attacker.body.maxShield;
                float maxShieldPercent = attacker.body.maxShield / attacker.fullCombinedHealth;
                int cooldownToApply = (int)((maxShieldPercent * missingShieldPercent) * 20);
                for (int i = 0; i < cooldownToApply; i++)
                {
                    attacker.body.AddTimedBuff(Assets.shockHealCooldown, i + 1);
                }
                attacker.ForceShieldRegen(); //the buff runs out slightly before the shockstate does, for some reason. im gonna call it a feature for now
            }
        }
        #endregion
    }
}
