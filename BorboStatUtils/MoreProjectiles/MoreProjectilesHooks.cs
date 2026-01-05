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

namespace RainrotSharedUtils.MoreProjectiles
{
    public static class MoreProjectilesHooks
    {
        public const float missileSpread = 45;
        public const float projectileSpread = 20;

        public static void OverrideIcbmMissiles(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLoc = 0;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoreMissile"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(OverrideIcbmMissiles), 1);
                return;
            }
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _)
                );
            if (!b2)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(OverrideIcbmMissiles), 2);
                return;
            }
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<int, CharacterBody, int>>((icbmCount, body) =>
            {
                if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(body))
                    icbmCount += 1;
                return icbmCount;
            });

            //bool b = c.TryGotoNext(MoveType.Before,
            //    x => x.MatchLdloc(0),
            //    x => x.MatchLdcI4(0),
            //    x => x.MatchBle(out _)
            //    );
            //c.Remove();
            //c.EmitDelegate<Func<int>>(() =>
            //{
            //    return RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact) ? 1 : 0;
            //});
        }

        public static void MissileArtifact_FireProjectile(On.EntityStates.GenericProjectileBaseState.orig_FireProjectile orig, EntityStates.GenericProjectileBaseState self)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                if (self.isAuthority)
                {
                    bool isValidState = false;
                    bool isVertical = false;
                    float spread = projectileSpread;

                    //alpha construct, phase round, scrap cannon
                    if (self is EntityStates.MinorConstruct.Weapon.FireConstructBeam
                        || self is EntityStates.Commando.CommandoWeapon.FireFMJ
                        || self is EntityStates.Toolbot.FireGrenadeLauncher)
                    {
                        isValidState = true;
                    }
                    else
                    {
                        //blind pest
                        if (self is EntityStates.FlyingVermin.Weapon.Spit)
                        {
                            isValidState = true;
                            isVertical = true;
                        }

                        //barnacle
                        if (self is EntityStates.VoidBarnacle.Weapon.Fire)
                        {
                            isValidState = true;
                            spread = missileSpread;
                        }
                    }

                    if (isValidState)
                    {
                        Ray aimRay = self.GetAimRay();
                        aimRay = self.ModifyProjectileAimRay(aimRay);
                        aimRay.direction = Util.ApplySpread(aimRay.direction, self.minSpread, self.maxSpread, 1f, 1f, 0f, self.projectilePitchBonus);

                        FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                        {
                            projectilePrefab = self.projectilePrefab,
                            position = aimRay.origin,
                            rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                            owner = self.gameObject,
                            damage = self.damageStat * self.damageCoefficient,
                            crit = Util.CheckRoll(self.critStat, self.characterBody.master),
                            force = self.force
                        };
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                        Vector3 axis = Vector3.Cross(Vector3.up, aimRay.direction);
                        if(!isVertical)
                            axis = Vector3.Cross(aimRay.direction, axis);

                        MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, spread, axis);
                        return;
                    }
                }
            }
            orig(self);
        }

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
            c.EmitDelegate<Action<Orb>>((orb) =>
            {
                OrbManager.instance.AddOrb(orb);
                OrbManager.instance.AddOrb(orb);
            });
        }

        public static void MissileArtifact_Shuriken(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<ProjectileManager>(nameof(ProjectileManager.FireProjectileWithoutDamageType))
                );
            if (!b)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_Shuriken));
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<ProjectileManager, GameObject, Vector3, Quaternion, GameObject, float, float, bool, DamageColorIndex, GameObject, float, PrimarySkillShurikenBehavior>>(
                (projectileManagerInstance, projectilePrefab, origin, rotation, owner, damage, force, crit, damageColorIndex, target, speedOverride, behavior) =>
                {
                    projectileManagerInstance.FireProjectileWithoutDamageType(projectilePrefab, origin, rotation, owner, damage, force, crit, damageColorIndex, target, speedOverride);

                    if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(behavior.body))
                    {
                        Ray aimRay = behavior.GetAimRay();
                        FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                        {
                            projectilePrefab = projectilePrefab,
                            position = origin,
                            rotation = rotation,
                            owner = owner,
                            damage = damage,
                            crit = crit
                        };
                        MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
                    }
                });
        }
        public static void MissileArtifact_VerminSpit(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int aimRayLoc = 0;
            int damageLoc = 0;
            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<BaseState>(nameof(BaseState.GetAimRay)),
                x => x.MatchStloc(out aimRayLoc)
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 1);
                return;
            }
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdfld<BaseState>(nameof(BaseState.damageStat)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(out damageLoc)
                );
            if (!b2)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 2);
                return;
            }
            bool b4 = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<ProjectileManager>(nameof(ProjectileManager.FireProjectileWithoutDamageType))
                );
            if (!b4)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_VerminSpit), 3);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, aimRayLoc);
            c.Emit(OpCodes.Ldloc, damageLoc);
            c.EmitDelegate<Action<EntityStates.FlyingVermin.Weapon.Spit, Ray, float>>((self, aimRay, damage) =>
            {
                if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                {
                    FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                    {
                        projectilePrefab = self.projectilePrefab,
                        position = aimRay.origin,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                        owner = self.gameObject,
                        damage = damage,
                        crit = Util.CheckRoll(self.critStat, self.characterBody.master)
                    };
                    Vector3 axis = Vector3.Cross(Vector3.up, aimRay.direction);
                    MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread, axis);
                }
            });
        }

        #region simples
        public static void MissileArtifact_ChildSpark(On.EntityStates.ChildMonster.SparkBallFire.orig_FireBomb orig, EntityStates.ChildMonster.SparkBallFire self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, SparkBallFire.bombDamageCoefficient, SparkBallFire.projectilePrefab);
        }
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
        public static void MissileArtifact_VagrantTrackingBomb(On.EntityStates.VagrantMonster.FireTrackingBomb.orig_FireBomb orig, EntityStates.VagrantMonster.FireTrackingBomb self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, FireTrackingBomb.bombDamageCoefficient, FireTrackingBomb.projectilePrefab);
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

        public static void MissileArtifact_GreaterWispFireCannons(On.EntityStates.GreaterWispMonster.FireCannons.orig_OnEnter orig, EntityStates.GreaterWispMonster.FireCannons self)
        {
            orig(self);
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                return;
            Ray aimRay = self.GetAimRay();
            if (self.isAuthority && self.modelLocator && self.modelLocator.modelTransform)
            {
                ChildLocator component = self.modelLocator.modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    int childIndex = component.FindChildIndex("MuzzleLeft");
                    int childIndex2 = component.FindChildIndex("MuzzleRight");
                    Transform transform = component.FindChild(childIndex);
                    Transform transform2 = component.FindChild(childIndex2);
                    if (transform)
                    {
                        FireProjectilesFromTransform(transform);
                    }
                    if (transform2)
                    {
                        FireProjectilesFromTransform(transform2);
                    }
                }
            }

            void FireProjectilesFromTransform(Transform transform)
            {
                if (transform == null)
                    return;
                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = rhs;// Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = self.projectilePrefab,
                    position = transform.position,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    owner = self.gameObject,
                    damage = self.damageStat * self.damageCoefficient,
                    crit = Util.CheckRoll(self.characterBody.crit, self.characterBody.master)
                };
                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread, axis);
            }
        }

        public static void MissileArtifact_BrotherFistSlam(On.EntityStates.BrotherMonster.FistSlam.orig_OnEnter orig, EntityStates.BrotherMonster.FistSlam self)
        {
            orig(self);
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                EntityStates.BrotherMonster.FistSlam.waveProjectileCount *= 2;
            }
        }

        public static void MissileArtifact_BrotherUltChannelState(On.EntityStates.BrotherMonster.UltChannelState.orig_OnEnter orig, EntityStates.BrotherMonster.UltChannelState self)
        {
            orig(self);
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                EntityStates.BrotherMonster.UltChannelState.waveProjectileCount += 2;
            }
        }

        public static void MissileArtifact_BrotherWeaponSlam(On.EntityStates.BrotherMonster.WeaponSlam.orig_OnEnter orig, EntityStates.BrotherMonster.WeaponSlam self)
        {
            orig(self);
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                EntityStates.BrotherMonster.WeaponSlam.waveProjectileCount += 2;
            }
        }

        public static void MissileArtifact_GupDeathEnter(On.EntityStates.Gup.BaseSplitDeath.orig_OnEnter orig, EntityStates.Gup.BaseSplitDeath self)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                self.spawnCount = 3;
            }
            orig(self);
        }

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

        public static void MissileArtifact_GrandpaVacuum(On.EntityStates.GrandParentBoss.FireSecondaryProjectile.orig_Fire orig, EntityStates.GrandParentBoss.FireSecondaryProjectile self)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self);
                return;
            }

            self.hasFired = true;
            if (self.muzzleEffectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(self.muzzleEffectPrefab, self.gameObject, self.muzzleName, false);
            }
            if (self.isAuthority && self.projectilePrefab)
            {
                Ray aimRay = self.GetAimRay();
                Transform modelTransform = self.GetModelTransform();
                if (modelTransform)
                {
                    ChildLocator component = modelTransform.GetComponent<ChildLocator>();
                    if (component)
                    {
                        aimRay.origin = component.FindChild(self.muzzleName).transform.position;
                    }
                }

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * self.damageCoefficient;
                fireProjectileInfo.force = self.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, missileSpread);
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

        public static void MissileArtifact_ThrowBomb(On.EntityStates.Mage.Weapon.BaseThrowBombState.orig_Fire orig, EntityStates.Mage.Weapon.BaseThrowBombState self)
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

        public static void MissileArtifact_DunestriderRoller(On.EntityStates.ClayBoss.FireTarball.orig_FireSingleTarball orig, EntityStates.ClayBoss.FireTarball self, string targetMuzzle)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self, targetMuzzle);
                return;
            }
            self.PlayCrossfade("Body", "FireTarBall", 0.1f);
            Util.PlaySound(FireTarball.attackSoundString, self.gameObject);
            self.aimRay = self.GetAimRay();
            if (self.modelTransform)
            {
                ChildLocator component = self.modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    Transform transform = component.FindChild(targetMuzzle);
                    if (transform)
                    {
                        self.aimRay.origin = transform.position;
                    }
                }
            }
            self.AddRecoil(-1f * FireTarball.recoilAmplitude, -2f * FireTarball.recoilAmplitude, -1f * FireTarball.recoilAmplitude, 1f * FireTarball.recoilAmplitude);
            if (FireTarball.effectPrefab)
            {
                EffectManager.SimpleMuzzleFlash(FireTarball.effectPrefab, self.gameObject, targetMuzzle, false);
            }
            if (self.isAuthority)
            {
                Vector3 axis = Vector3.up;
                Vector3 forward = Vector3.ProjectOnPlane(self.aimRay.direction, axis);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = FireTarball.projectilePrefab;
                fireProjectileInfo.position = self.aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * FireTarball.damageCoefficient;
                fireProjectileInfo.force = 0;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                Ray aimRay = new Ray();
                aimRay.origin = self.aimRay.origin;
                aimRay.direction = forward;

                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, missileSpread);
            }
            self.characterBody.AddSpreadBloom(FireTarball.spreadBloomValue);
        }

        public static void MissileArtifact_BeetleGuardRoller(On.EntityStates.BeetleGuardMonster.FireSunder.orig_FixedUpdate orig, EntityStates.BeetleGuardMonster.FireSunder self)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self);
                return;
            }
            self.fixedAge += Time.fixedDeltaTime;
            if (self.modelAnimator && self.modelAnimator.GetFloat("FireSunder.activate") > 0.5f && !self.hasAttacked)
            {
                if (self.isAuthority && self.modelTransform)
                {
                    Ray aimRay = self.GetAimRay();
                    aimRay.origin = self.handRTransform.position;

                    Vector3 axis = Vector3.up;

                    FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                    fireProjectileInfo.projectilePrefab = FireSunder.projectilePrefab;
                    fireProjectileInfo.position = aimRay.origin;
                    fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                    fireProjectileInfo.owner = self.gameObject;
                    fireProjectileInfo.damage = self.damageStat * FireSunder.damageCoefficient;
                    fireProjectileInfo.force = FireSunder.forceMagnitude;
                    fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                    MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
                }
                self.hasAttacked = true;
                EntityState.Destroy(self.rightHandChargeEffect);
            }
            if (self.fixedAge >= self.duration && self.isAuthority)
            {
                self.outer.SetNextStateToMain();
                return;
            }
        }

        public static void MissileArtifact_ChimeraSeekingBomb(On.EntityStates.LunarWisp.SeekingBomb.orig_FireBomb orig, EntityStates.LunarWisp.SeekingBomb self)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self);
                return;
            }
            Util.PlaySound(SeekingBomb.fireBombSoundString, self.gameObject);
            Ray aimRay = self.GetAimRay();
            Transform modelTransform = self.GetModelTransform();
            if (modelTransform)
            {
                ChildLocator component = modelTransform.GetComponent<ChildLocator>();
                if (component)
                {
                    aimRay.origin = component.FindChild(SeekingBomb.muzzleName).transform.position;
                }
            }
            if (self.isAuthority)
            {
                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = SeekingBomb.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * SeekingBomb.bombDamageCoefficient;
                fireProjectileInfo.force = SeekingBomb.bombForce;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, missileSpread);
            }
            Util.PlaySound(SeekingBomb.spinDownSoundString, self.gameObject);
            self.PlayCrossfade("Gesture", "BombStop", 0.2f);
        }

        public static void MissileArtifact_TitanRock(On.RoR2.TitanRockController.orig_Fire orig, TitanRockController self)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.ownerCharacterBody))
            {
                orig(self);
                return;
            }
            if (NetworkServer.active && self.ownerInputBank)
            {
                Vector3 position = self.fireTransform.position;
                Vector3 forward = self.ownerInputBank.aimDirection;
                RaycastHit raycastHit;
                if (Util.CharacterRaycast(self.owner, new Ray(self.ownerInputBank.aimOrigin, self.ownerInputBank.aimDirection),
                    out raycastHit, float.PositiveInfinity, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.UseGlobal))
                {
                    forward = raycastHit.point - position;
                }
                float baseDamage = self.ownerCharacterBody ? self.ownerCharacterBody.damage : 1f;

                ProjectileManager.instance.FireProjectile(new FireProjectileInfo
                {
                    crit = self.isCrit,
                    damage = self.damageCoefficient * baseDamage,
                    damageColorIndex = DamageColorIndex.Default,
                    force = self.damageForce,
                    owner = self.owner,
                    position = position,
                    projectilePrefab = self.projectilePrefab,
                    rotation = Util.QuaternionSafeLookRotation(forward),
                    target = null
                });

                Vector3 rhs = Vector3.Cross(Vector3.up, forward);
                Vector3 axis = Vector3.Cross(forward, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = position;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(forward);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageCoefficient * baseDamage;
                fireProjectileInfo.force = self.damageForce;
                fireProjectileInfo.crit = self.isCrit;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                Ray aimRay = new Ray();
                aimRay.origin = position;
                aimRay.direction = forward;
                MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
            }
        }

        public static void MissileArtifact_LemurianFireball(On.EntityStates.LemurianMonster.FireFireball.orig_OnEnter orig, EntityStates.LemurianMonster.FireFireball self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, FireFireball.damageCoefficient, FireFireball.projectilePrefab);
        }

        public static void MissileArtifact_FireMeatballs(On.RoR2.WormBodyPositions2.orig_FireMeatballs orig, WormBodyPositions2 self, Vector3 impactNormal, Vector3 impactPosition, Vector3 forward, int meatballCount, float meatballAngle, float meatballForce)
        {
            if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                meatballCount *= 2;
            }
            orig(self, impactNormal, impactPosition, forward, meatballCount, meatballAngle, meatballForce);
        }

        public static void MissileArtifact_ReliquaryFlares(On.EntityStates.ArtifactShell.FireSolarFlares.orig_FixedUpdate orig, EntityStates.ArtifactShell.FireSolarFlares self)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self);
                return;
            }
            if (NetworkServer.active)
            {
                float num = self.duration / (float)self.projectileCount;
                if (self.fixedAge >= (float)self.projectilesFired * num)
                {
                    self.projectilesFired++;
                    FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                    fireProjectileInfo.owner = self.gameObject;
                    fireProjectileInfo.position = self.transform.position + self.currentRotation * Vector3.forward * FireSolarFlares.radius;
                    fireProjectileInfo.rotation = self.currentRotation;
                    fireProjectileInfo.projectilePrefab = FireSolarFlares.projectilePrefab;
                    fireProjectileInfo.fuseOverride = FireSolarFlares.projectileFuse;
                    fireProjectileInfo.useFuseOverride = true;
                    fireProjectileInfo.speedOverride = FireSolarFlares.projectileSpeed;
                    fireProjectileInfo.useSpeedOverride = true;
                    fireProjectileInfo.damage = self.damageStat * FireSolarFlares.projectileDamageCoefficient;
                    fireProjectileInfo.force = FireSolarFlares.projectileForce;
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                    Vector3 axis = Vector3.up;
                    Ray aimRay = new Ray();
                    aimRay.origin = fireProjectileInfo.position;
                    aimRay.direction = self.currentRotation * Vector3.forward;

                    MoreProjectilesModule.FireWarfareProjectiles(aimRay, fireProjectileInfo, missileSpread, axis);

                    self.currentRotation *= self.deltaRotation;
                }
                if (self.fixedAge >= self.duration)
                {
                    self.outer.SetNextStateToMain();
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

        public static void MissileArtifact_MushrumSporeGrenade(On.EntityStates.MiniMushroom.SporeGrenade.orig_FireGrenade orig, EntityStates.MiniMushroom.SporeGrenade self, string targetMuzzle)
        {
            if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
            {
                orig(self, targetMuzzle);
                return;
            }
            Ray aimRay = self.GetAimRay();
            Ray ray = new Ray(aimRay.origin, Vector3.up);
            Transform transform = self.FindModelChild(targetMuzzle);
            if (transform)
            {
                ray.origin = transform.position;
            }
            BullseyeSearch bullseyeSearch = new BullseyeSearch();
            bullseyeSearch.searchOrigin = aimRay.origin;
            bullseyeSearch.searchDirection = aimRay.direction;
            bullseyeSearch.filterByLoS = false;
            bullseyeSearch.teamMaskFilter = TeamMask.allButNeutral;
            if (self.teamComponent)
            {
                bullseyeSearch.teamMaskFilter.RemoveTeam(self.teamComponent.teamIndex);
            }
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
            bullseyeSearch.RefreshCandidates();
            HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
            bool flag = false;
            Vector3 a = Vector3.zero;
            RaycastHit raycastHit;
            if (hurtBox)
            {
                a = hurtBox.transform.position;
                flag = true;
            }
            else if (Physics.Raycast(aimRay, out raycastHit, 1000f, LayerIndex.world.mask | LayerIndex.entityPrecise.mask, QueryTriggerInteraction.Ignore))
            {
                a = raycastHit.point;
                flag = true;
            }
            float magnitude = SporeGrenade.projectileVelocity;
            if (flag)
            {
                Vector3 vector = a - ray.origin;
                Vector2 a2 = new Vector2(vector.x, vector.z);
                float magnitude2 = a2.magnitude;
                Vector2 vector2 = a2 / magnitude2;
                if (magnitude2 < SporeGrenade.minimumDistance)
                {
                    magnitude2 = SporeGrenade.minimumDistance;
                }
                if (magnitude2 > SporeGrenade.maximumDistance)
                {
                    magnitude2 = SporeGrenade.maximumDistance;
                }
                float y = Trajectory.CalculateInitialYSpeed(SporeGrenade.timeToTarget, vector.y);
                float num = magnitude2 / SporeGrenade.timeToTarget;
                Vector3 direction = new Vector3(vector2.x * num, y, vector2.y * num);
                magnitude = direction.magnitude;
                ray.direction = direction;
            }
            Quaternion rotation = Util.QuaternionSafeLookRotation(ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f);
            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
            {
                projectilePrefab = SporeGrenade.projectilePrefab,
                position = ray.origin,
                rotation = rotation,
                owner = self.gameObject,
                damage = self.damageStat * SporeGrenade.damageCoefficient,
                crit = Util.CheckRoll(self.critStat, self.characterBody.master),
                force = 0,
                speedOverride = magnitude
            };
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);

            Vector3 axis = self.inputBank ? self.inputBank.aimDirection : self.characterBody.transform.position;
            FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
            fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * (ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f));
            ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

            FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
            fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * (ray.direction + UnityEngine.Random.insideUnitSphere * 0.05f));
            ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
        }
    }
}
