using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.AddressableAssets;
using R2API;
using R2API.Utils;
using RoR2.Projectile;
using UnityEngine;
using RoR2;
using System.Linq;
using System.Security.Permissions;
using System.Security;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using RoR2.ExpansionManagement;
using ModularEclipse;
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

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MissileRework
{
    public partial class MissileReworkPlugin
    {
        public static ArtifactDef MissileArtifact = null;
        public const float missileSpread = 45;
        public const float projectileSpread = 25;

        private void CreateArtifact()
        {
            MissileArtifact = ScriptableObject.CreateInstance<ArtifactDef>();

            MissileArtifact.cachedName = "BorboWarfare";
            MissileArtifact.nameToken = "ARTIFACT_MISSILE_NAME";
            MissileArtifact.descriptionToken = "ARTIFACT_MISSILE_DESC";
            MissileArtifact.smallIconSelectedSprite = assetBundle.LoadAsset<Sprite>("Assets/warfare.png");
            MissileArtifact.smallIconDeselectedSprite = assetBundle.LoadAsset<Sprite>("Assets/warfaredeactivated.png");
            MissileArtifact.unlockableDef = null;
            MissileArtifact.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();

            LanguageAPI.Add(MissileArtifact.nameToken, "Artifact of Warfare");
            LanguageAPI.Add(MissileArtifact.descriptionToken, "Triple most projectile attacks.");
            ContentAddition.AddArtifactDef(MissileArtifact);

            //compatibility with Modular Eclipse
            if (ModularEclipseLoaded)
            {
                ModularEclipseCompat();
            }

            IL.RoR2.MissileUtils.FireMissile_Vector3_CharacterBody_ProcChainMask_GameObject_float_bool_GameObject_DamageColorIndex_Vector3_float_bool += OverrideIcbmMissiles;
            DoMissileArtifactEffects();
        }


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void ModularEclipseCompat()
        {
            ModularEclipsePlugin.SetArtifactDefaultWhitelist(MissileArtifact, true);
        }
        private void OverrideIcbmMissiles(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _)
                );
            c.Remove();
            c.EmitDelegate<Func<int>>(() =>
            {
                return RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact) ? 1 : 0;
            });
        }

        private void DoMissileArtifactEffects()
        {
            On.EntityStates.GenericProjectileBaseState.FireProjectile += MissileArtifact_FireProjectile;
            On.EntityStates.Mage.Weapon.BaseThrowBombState.Fire += MissileArtifact_ThrowBomb;

            //shuriken
            On.RoR2.PrimarySkillShurikenBehavior.FireShuriken += MissileArtifact_Shuriken;

            //viend m2
            On.EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase.FireProjectiles += MissileArtifact_ViendSecondary;
            On.EntityStates.VoidSurvivor.Weapon.FireCorruptDisks.OnEnter += MissileArtifact_ViendCorruptSecondary;
            //captain tazer
            On.EntityStates.Captain.Weapon.FireTazer.Fire += MissileArtifact_CaptainTazer;
            //arti bolt
            On.EntityStates.Mage.Weapon.FireFireBolt.FireGauntlet += MissileArtifact_ArtiBolts;
            //lodr pylon
            On.EntityStates.Loader.ThrowPylon.OnEnter += MissileArtifact_LodrPylon;
            //railer pistol
            On.EntityStates.Railgunner.Weapon.FirePistol.FireBullet += MissileArtifact_RailerPistol;

            //mushrum spore grenade
            On.EntityStates.MiniMushroom.SporeGrenade.FireGrenade += MissileArtifact_MushrumSporeGrenade;
            //reliquary solar flares
            On.EntityStates.ArtifactShell.FireSolarFlares.FixedUpdate += MissileArtifact_ReliquaryFlares;
            //worm meatball
            On.RoR2.WormBodyPositions2.FireMeatballs += MissileArtifact_FireMeatballs;
            //titan rock
            On.RoR2.TitanRockController.Fire += MissileArtifact_TitanRock;
            //lemurian
            On.EntityStates.LemurianMonster.FireFireball.OnEnter += MissileArtifact_LemurianFireball;
            //vagrant tracking bomb
            On.EntityStates.VagrantMonster.FireTrackingBomb.FireBomb += MissileArtifact_VagrantTrackingBomb;
            //chimera tracking bomb
            On.EntityStates.LunarWisp.SeekingBomb.FireBomb += MissileArtifact_ChimeraSeekingBomb;
            //beetle guard roller
            On.EntityStates.BeetleGuardMonster.FireSunder.FixedUpdate += MissileArtifact_BeetleGuardRoller;
            //dunestrider roller
            On.EntityStates.ClayBoss.FireTarball.FireSingleTarball += MissileArtifact_DunestriderRoller;
            //grandpa vaccuum
            On.EntityStates.GrandParentBoss.FireSecondaryProjectile.Fire += MissileArtifact_GrandpaVacuum;
        }

        #region skill hooks
        private void MissileArtifact_Shuriken(On.RoR2.PrimarySkillShurikenBehavior.orig_FireShuriken orig, PrimarySkillShurikenBehavior self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.body.damage * (3f + 1f * (float)self.stack);
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.crit = Util.CheckRoll(self.body.crit, self.body.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction) * self.GetRandomRollPitch();
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction) * self.GetRandomRollPitch();
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }
        private void MissileArtifact_GrandpaVacuum(On.EntityStates.GrandParentBoss.FireSecondaryProjectile.orig_Fire orig, EntityStates.GrandParentBoss.FireSecondaryProjectile self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);

                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction);
                fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
            }
        }

        private void MissileArtifact_RailerPistol(On.EntityStates.Railgunner.Weapon.FirePistol.orig_FireBullet orig, EntityStates.Railgunner.Weapon.FirePistol self, Ray aimRay)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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

                Vector3 rhs3 = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs3);

                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction) * rhs * rhs2;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

                FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction) * rhs * rhs2;
                ProjectileManager.instance.FireProjectile(fireProjectileInfo3);

                self.characterBody.characterMotor.ApplyForce(-self.selfKnockbackForce * aimRay.direction, false, false);
            }
            self.characterBody.AddSpreadBloom(self.spreadBloomValue);
        }

        private void MissileArtifact_ThrowBomb(On.EntityStates.Mage.Weapon.BaseThrowBombState.orig_Fire orig, EntityStates.Mage.Weapon.BaseThrowBombState self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                if (self.isAuthority)
                {
                    //alpha construct, blind pest, phase round, scrap cannon
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

                            Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                            Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                            FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                            fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                            ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

                            FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                            fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                            ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
                        }
                        if (self.characterMotor)
                        {
                            self.characterMotor.ApplyForce(aimRay.direction * (-self.selfForce * self.charge), false, false);
                        }
                        return;
                    }
                }
            }
            orig(self);
        }

        private void MissileArtifact_LodrPylon(On.EntityStates.Loader.ThrowPylon.orig_OnEnter orig, EntityStates.Loader.ThrowPylon self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = ThrowPylon.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * ThrowPylon.damageCoefficient;
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_ArtiBolts(On.EntityStates.Mage.Weapon.FireFireBolt.orig_FireGauntlet orig, EntityStates.Mage.Weapon.FireFireBolt self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * self.damageCoefficient;
                fireProjectileInfo.force = 0f;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_DunestriderRoller(On.EntityStates.ClayBoss.FireTarball.orig_FireSingleTarball orig, EntityStates.ClayBoss.FireTarball self, string targetMuzzle)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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

                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * forward);
                fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * forward);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
            }
            self.characterBody.AddSpreadBloom(FireTarball.spreadBloomValue);
        }

        private void MissileArtifact_BeetleGuardRoller(On.EntityStates.BeetleGuardMonster.FireSunder.orig_FixedUpdate orig, EntityStates.BeetleGuardMonster.FireSunder self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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

                    FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                    FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                    fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                    fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
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

        private void MissileArtifact_ChimeraSeekingBomb(On.EntityStates.LunarWisp.SeekingBomb.orig_FireBomb orig, EntityStates.LunarWisp.SeekingBomb self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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

                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction);
                fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
            }
            Util.PlaySound(SeekingBomb.spinDownSoundString, self.gameObject);
            self.PlayCrossfade("Gesture", "BombStop", 0.2f);
        }

        private void MissileArtifact_VagrantTrackingBomb(On.EntityStates.VagrantMonster.FireTrackingBomb.orig_FireBomb orig, EntityStates.VagrantMonster.FireTrackingBomb self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 axis = Vector3.up;

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = FireTrackingBomb.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * FireTrackingBomb.bombDamageCoefficient;
                fireProjectileInfo.force = FireTrackingBomb.bombForce;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(missileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-missileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_TitanRock(On.RoR2.TitanRockController.orig_Fire orig, TitanRockController self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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
                ProjectileManager.instance.FireProjectile(self.projectilePrefab, position,
                    Util.QuaternionSafeLookRotation(forward), self.owner, self.damageCoefficient * baseDamage,
                    self.damageForce, self.isCrit, DamageColorIndex.Default, null, -1f);

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

                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * forward);
                fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * forward);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
            }
        }

        private void MissileArtifact_LemurianFireball(On.EntityStates.LemurianMonster.FireFireball.orig_OnEnter orig, EntityStates.LemurianMonster.FireFireball self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = FireFireball.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * FireFireball.damageCoefficient;
                fireProjectileInfo.force = FireFireball.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_FireMeatballs(On.RoR2.WormBodyPositions2.orig_FireMeatballs orig, WormBodyPositions2 self, Vector3 impactNormal, Vector3 impactPosition, Vector3 forward, int meatballCount, float meatballAngle, float meatballForce)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                meatballCount *= 3;
            }
            orig(self, impactNormal, impactPosition, forward, meatballCount, meatballAngle, meatballForce);
        }

        private void MissileArtifact_ReliquaryFlares(On.EntityStates.ArtifactShell.FireSolarFlares.orig_FixedUpdate orig, EntityStates.ArtifactShell.FireSolarFlares self)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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
                    FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                    fireProjectileInfo2.rotation = Quaternion.AngleAxis(missileSpread, axis) * self.currentRotation;
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

                    FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                    fireProjectileInfo3.rotation = Quaternion.AngleAxis(-missileSpread, axis) * self.currentRotation;
                    ProjectileManager.instance.FireProjectile(fireProjectileInfo3);

                    self.currentRotation *= self.deltaRotation;
                }
                if (self.fixedAge >= self.duration)
                {
                    self.outer.SetNextStateToMain();
                }
            }
        }

        private void MissileArtifact_CaptainTazer(On.EntityStates.Captain.Weapon.FireTazer.orig_Fire orig, EntityStates.Captain.Weapon.FireTazer self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = FireTazer.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * FireTazer.damageCoefficient;
                fireProjectileInfo.force = FireTazer.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_ViendSecondary(On.EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase.orig_FireProjectiles orig, EntityStates.VoidSurvivor.Weapon.FireMegaBlasterBase self)
        {
            orig(self);
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                Ray aimRay = self.GetAimRay();

                Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                aimRay.direction = Util.ApplySpread(aimRay.direction, 0f, self.spread, 1f, 1f, 0f, 0f);
                FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
                fireProjectileInfo.projectilePrefab = self.projectilePrefab;
                fireProjectileInfo.position = aimRay.origin;
                fireProjectileInfo.owner = self.gameObject;
                fireProjectileInfo.damage = self.damageStat * self.damageCoefficient;
                fireProjectileInfo.force = self.force;
                fireProjectileInfo.crit = Util.CheckRoll(self.critStat, self.characterBody.master);
                FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;

                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
                ProjectileManager.instance.FireProjectile(fireProjectileInfo2);
            }
        }

        private void MissileArtifact_ViendCorruptSecondary(On.EntityStates.VoidSurvivor.Weapon.FireCorruptDisks.orig_OnEnter orig, EntityStates.VoidSurvivor.Weapon.FireCorruptDisks self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                self.projectileCount = 3;
                self.yawPerProjectile = projectileSpread;
            }
            orig(self);
        }

        private void MissileArtifact_MushrumSporeGrenade(On.EntityStates.MiniMushroom.SporeGrenade.orig_FireGrenade orig, EntityStates.MiniMushroom.SporeGrenade self, string targetMuzzle)
        {
            if (!RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
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

        private void MissileArtifact_FireProjectile(On.EntityStates.GenericProjectileBaseState.orig_FireProjectile orig, EntityStates.GenericProjectileBaseState self)
        {
            if (RunArtifactManager.instance.IsArtifactEnabled(MissileArtifact))
            {
                if (self.isAuthority)
                {
                    //alpha construct, blind pest, phase round, scrap cannon
                    if (self is EntityStates.MinorConstruct.Weapon.FireConstructBeam || self is EntityStates.FlyingVermin.Weapon.Spit
                        || self is EntityStates.VoidBarnacle.Weapon.Fire
                        || self is EntityStates.Commando.CommandoWeapon.FireFMJ || self is EntityStates.Toolbot.FireGrenadeLauncher)
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

                        Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
                        Vector3 axis = Vector3.Cross(aimRay.direction, rhs);

                        FireProjectileInfo fireProjectileInfo2 = fireProjectileInfo;
                        fireProjectileInfo2.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(projectileSpread, axis) * aimRay.direction);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo2);

                        FireProjectileInfo fireProjectileInfo3 = fireProjectileInfo;
                        fireProjectileInfo3.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-projectileSpread, axis) * aimRay.direction);
                        ProjectileManager.instance.FireProjectile(fireProjectileInfo3);
                        return;
                    }
                }
            }
            orig(self);
        }
        #endregion
    }
}
