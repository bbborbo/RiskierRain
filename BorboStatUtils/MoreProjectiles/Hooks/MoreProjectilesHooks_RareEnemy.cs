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
        public static void MissileArtifact_ChargeTrioBomb(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //ILLabel bellCountLoc = c.DefineLabel();
            //ILLabel bellTransformLoc = c.DefineLabel();
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(3)
                );
            if (!b1)
            {
                SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_ChargeTrioBomb), 1);
                return;
            }
            //c.MarkLabel(bellCountLoc);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, ChargeTrioBomb, int>>((bellCount, self) =>
            {
                if (!MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                    return bellCount;
                return bellCount + 2;
            });
        }
        public static Transform MissileArtifact_FindTrioBombTransform(On.EntityStates.Bell.BellWeapon.ChargeTrioBomb.orig_FindTargetChildTransformFromBombIndex orig, EntityStates.Bell.BellWeapon.ChargeTrioBomb self)
        {
            Transform t = orig(self);
            if (t == null && self.currentBombIndex >= 3)
            {
                t = GenerateTransform(self.transform, self.currentBombIndex);
                //if(t != null)
                //    self.childLocator.AddChild("ProjectilePosition" + self.currentBombIndex, t);
            }
            return t;
            Transform GenerateTransform(Transform parent, int index)
            {
                var baseRadius = 3.8f;
                float startingPos = 0f;
                int firstRingSize = 7;
                float radius = baseRadius;

                float currentStepSize = 2 * Mathf.PI / firstRingSize;
                int step = Mathf.FloorToInt(index / 2);
                int clockwise = (index % 2 == 0) ? 1 : -1;
                float currentPos = startingPos + (currentStepSize * step) * clockwise;

                float x = Mathf.Sin(currentPos) * radius;
                float y = Mathf.Cos(currentPos) * radius;
                float z = 0f;
                Vector3 vector = new Vector3(x, y, z);
                GameObject newGameObject = new GameObject();
                newGameObject.name = "ProjectilePosition" + self.currentBombIndex;
                newGameObject.transform.parent = parent;
                newGameObject.transform.localPosition = vector;
                newGameObject.transform.localScale = Vector3.one;
                newGameObject.transform.rotation = Quaternion.identity;
                Transform newTransform = newGameObject.transform;
                return newTransform;
            }
        }
        #region simples
        public static void MissileArtifact_VagrantTrackingBomb(On.EntityStates.VagrantMonster.FireTrackingBomb.orig_FireBomb orig, EntityStates.VagrantMonster.FireTrackingBomb self)
        {
            orig(self);
            MoreProjectilesModule.FireWarfareProjectilesSimple(self.characterBody, FireTrackingBomb.bombDamageCoefficient, FireTrackingBomb.projectilePrefab);
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

        public static void MissileArtifact_BrotherUltChannelState(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int hookCt = 0;
            while (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<EntityStates.BrotherMonster.UltChannelState>(nameof(EntityStates.BrotherMonster.UltChannelState.waveProjectileCount))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, EntityStates.BrotherMonster.UltChannelState, int>>((projectileCtIn, self) =>
                {
                    if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                        return projectileCtIn + 6;
                    return projectileCtIn;
                });
                hookCt++;
            }
            if (hookCt < 2)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_BrotherUltChannelState), hookCt + 1);
            }
        }

        public static void MissileArtifact_BrotherWeaponSlam(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int hookCt = 0;
            while (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<EntityStates.BrotherMonster.WeaponSlam>(nameof(EntityStates.BrotherMonster.WeaponSlam.waveProjectileCount))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, EntityStates.BrotherMonster.WeaponSlam, int>>((projectileCtIn, self) =>
                {
                    if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                        return projectileCtIn + 2;
                    return projectileCtIn;
                });
                hookCt++;
            }
            if (hookCt < 3)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_BrotherWeaponSlam), hookCt + 1);
            }
        }

        public static void MissileArtifact_BrotherFistSlam(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int hookCt = 0;
            while (c.TryGotoNext(MoveType.After, x => x.MatchLdsfld<EntityStates.BrotherMonster.FistSlam>(nameof(EntityStates.BrotherMonster.FistSlam.waveProjectileCount))))
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, EntityStates.BrotherMonster.FistSlam, int>>((projectileCtIn, self) =>
                {
                    if (MoreProjectilesModule.IsMoreProjectilesActiveForBody(self.characterBody))
                        return projectileCtIn + 6;
                    return projectileCtIn;
                });
                hookCt++;
            }
            if (hookCt < 2)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(MissileArtifact_BrotherFistSlam), hookCt + 1);
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
