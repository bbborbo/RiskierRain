using EntityStates.MiniMushroom;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.ExpansionManagement;
using System.Runtime.CompilerServices;
using UnityEngine.Networking;

using EntityStates.Captain.Weapon;
using EntityStates.ArtifactShell;
using EntityStates.LemurianMonster;
using EntityStates.VagrantMonster;
using EntityStates.LunarWisp;
using EntityStates.BeetleGuardMonster;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Mage.Weapon;
using EntityStates.Loader;
using EntityStates.MiniMushroom;
using EntityStates.ChildMonster;
using RoR2.Orbs;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Linq;
using EntityStates.Bell.BellWeapon;

namespace RainrotSharedUtils.MoreProjectiles
{
    public static partial class MoreProjectilesHooks
    {
        public static void MissileArtifact_FireHuntressSeekingArrow(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int orbLoc = 0;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out orbLoc),
                x => x.MatchCallOrCallvirt<RoR2.Orbs.OrbManager>(nameof(RoR2.Orbs.OrbManager.AddOrb))
                );
            if (!b)
            {
                Debug.LogError("IABM Huntress fail");
                return;
            }
            c.Emit(OpCodes.Ldloc, orbLoc);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Action<Orb, EntityState>>((orb, state) =>
            {
                if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(state.characterBody))
                    return;
                OrbManager.instance.AddOrb(orb);
                OrbManager.instance.AddOrb(orb);
            });
        }
        #region simples
        public static void MissileArtifact_SeekerPunch(On.EntityStates.Seeker.SpiritPunch.orig_FireGauntlet orig, EntityStates.Seeker.SpiritPunch self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, self.damageCoefficient, self.projectilePrefab);
        }
        public static void MissileArtifact_SonSurvivorSpike(On.EntityStates.FalseSon.LunarSpikes.orig_FireLunarSpike orig, EntityStates.FalseSon.LunarSpikes self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, self.damageCoefficient, self.projectilePrefab);
        }
        public static void MissileArtifact_LodrPylon(On.EntityStates.Loader.ThrowPylon.orig_OnEnter orig, EntityStates.Loader.ThrowPylon self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, ThrowPylon.damageCoefficient, ThrowPylon.projectilePrefab);
        }
        public static void MissileArtifact_ArtiBolts(On.EntityStates.Mage.Weapon.FireFireBolt.orig_FireGauntlet orig, EntityStates.Mage.Weapon.FireFireBolt self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, self.damageCoefficient, self.projectilePrefab);
        }
        public static void MissileArtifact_CaptainTazer(On.EntityStates.Captain.Weapon.FireTazer.orig_Fire orig, EntityStates.Captain.Weapon.FireTazer self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, FireTazer.damageCoefficient, FireTazer.projectilePrefab);
        }
        public static void MissileArtifact_ViendSecondary(On.EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase.orig_FireProjectiles orig, EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, self.damageCoefficient, self.projectilePrefab);
        }
        #endregion
        public static void MissileArtifact_ChefCleaver(On.EntityStates.Chef.Dice.orig_OnEnter orig, EntityStates.Chef.Dice self)
        {
            orig(self);
            if (self.isAuthority)
            {
                if (!self.hasBoost && MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                {
                    Ray aimRay = self.GetAimRay();

                    Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                    Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                    FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                    fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                    fireProjectileInfo.position = aimRay.origin;
                    fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                    fireProjectileInfo.owner = self.gameObject;
                    fireProjectileInfo.damage = self.damageStat * self.damageCoefficient;
                    fireProjectileInfo.force = self.force;
                    fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);

                    FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                    fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction);
                    fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction);
                    if (!NetworkServer.active && self.chefController)
                    {
                        self.chefController.CacheCleaverProjectileFireInfo(fireProjectileInfo);
                    }
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                    if (!NetworkServer.active && self.chefController)
                    {
                        self.chefController.CacheCleaverProjectileFireInfo(fireProjectileInfo2);
                    }
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                }
            }
        }

        public static void MissileArtifact_RailerPistol(On.EntityStates.Railgunner.Weapon.FirePistol.orig_FireBullet orig, EntityStates.Railgunner.Weapon.FirePistol self, Ray aimRay)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self, aimRay);
                return;
            }

            self.StartAimMode(aimRay, 2f, false);
            Util.PlaySound(self.fireSoundString, self.gameObject);
            EffectManager.SimpleMuzzleFlash(self.muzzleFlashPrefab, self.gameObject, self.muzzleName, false);
            self.PlayAnimation(self.animationLayerName, self.animationStateName, self.animationPlaybackRateParam, self.duration);
            self.AddRecoil(self.recoilYMin, self.recoilYMax, self.recoilXMin, self.recoilXMax);
            if (self.isAuthority)
            {
                float num = 0f;
                if (self.characterBody)
                {
                    num = self.characterBody.spreadBloomAngle;
                }
                Quaternion rhs = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.forward);
                Quaternion rhs2 = Quaternion.AngleAxis(UnityEngine.Random.Range(0f, self.baseInaccuracyDegrees + num), Vector3.left);
                Quaternion rotation = Util.QuaternionSafeLookRotation(aimRay.direction, Vector3.up) * rhs * rhs2;
                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = self.projectilePrefab,
                    position = aimRay.origin,
                    rotation = rotation,
                    owner = self.gameObject,
                    damage = self.damageStat * self.damageCoefficient,
                    crit = self.RollCrit(),
                    force = self.force,
                    procChainMask = default(ProcChainMask),
                    damageColorIndex = DamageColorIndex.Default
                };
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);

                self.characterBody.characterMotor.ApplyForce(-self.selfKnockbackForce * aimRay.direction, false, false);
            }
            self.characterBody.AddSpreadBloom(self.spreadBloomValue);
        }

        public static void MissileArtifact_ThrowBombHeresy(On.EntityStates.Mage.Weapon.BaseThrowBombState.orig_Fire orig, EntityStates.Mage.Weapon.BaseThrowBombState self)
        {
            orig(self);
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                if (self.isAuthority)
                {
                    //hooks of heresy
                    if (self is EntityStates.GlobalSkills.LunarNeedle.ThrowLunarSecondary)
                    {
                        Ray aimRay = self.GetAimRay();
                        if (self.projectilePrefab != null)
                        {
                            float num = Util.Remap(self.charge, 0f, 1f, self.minDamageCoefficient, self.maxDamageCoefficient);
                            float num2 = self.charge * self.force;
                            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                            {
                                projectilePrefab = self.projectilePrefab,
                                position = aimRay.origin,
                                rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                                owner = self.gameObject,
                                damage = self.damageStat * num,
                                force = num2,
                                crit = self.RollCrit()
                            };
                            self.ModifyProjectile(ref fireProjectileInfo);
                            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                            MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
                        }
                        if (self.characterMotor)
                        {
                            self.characterMotor.ApplyForce(aimRay.direction * (-self.selfForce * self.charge), false, false);
                        }
                        return;
                    }
                }
            }
        }

        public static void MissileArtifact_ViendCorruptSecondary(On.EntityStates.VoidSurvivor.Weapon.FireCorruptDisks.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.FireCorruptDisks self)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                self.projectileCount = 3;
                self.yawPerProjectile = projectileSpread;
            }
            orig(self);
        }

    }
}
