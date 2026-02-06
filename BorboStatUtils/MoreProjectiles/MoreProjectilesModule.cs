using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RainrotSharedUtils.MoreProjectiles.MoreProjectilesHooks;

namespace RainrotSharedUtils.MoreProjectiles
{
    public static class MoreProjectilesModule
    {
        //public static bool UseCommonEnemyProjectiles = true;
        //public static bool UseRareEnemyProjectiles = true;
        //public static bool UseItemAndEquipmentProjectiles = true;
        //public static bool UseSurvivorProjectiles = true;
        public static bool UseExpensiveProjectiles = true;
        private static bool _hooksEnabled = false;
        private static event MoreProjectilesEventHandler _moreProjectilesProvider;
        public static event MoreProjectilesEventHandler MoreProjectilesProvider
        {
            add
            {
                if (_moreProjectilesProvider == null)
                {
                    _moreProjectilesProvider = new MoreProjectilesEventHandler(value);
                    SetHooks();
                    return;
                }
                _moreProjectilesProvider += value;
            }
            remove
            {
                _moreProjectilesProvider -= value;
            }
        }
        public delegate bool MoreProjectilesEventHandler(CharacterBody sender);
        private static bool idk(CharacterBody sender)
        {
            return false;
        }
        public static bool IsMoreProjectilesActiveForBody(CharacterBody sender)
        {
            if(!_hooksEnabled)
                return false;

            foreach(MoreProjectilesEventHandler mpeh in _moreProjectilesProvider.GetInvocationList())
            {
                if (mpeh.Invoke(sender))
                    return true;
            }

            return false;
        }

        private static void SetHooks()
        {
            if (_hooksEnabled)
                return;
            _hooksEnabled = true;

            IL.RoR2.MissileUtils.FireMissile_Vector3_CharacterBody_ProcChainMask_GameObject_float_bool_GameObject_DamageColorIndex_Vector3_float_bool += OverrideIcbmMissiles;

            #region stuff
            //construct, pest, varnacle, phase round, scrap launcher
            On.EntityStates.GenericProjectileBaseState.FireProjectile += MissileArtifact_FireProjectile;
            IL.EntityStates.FlyingVermin.Weapon.Spit.FireProjectile += MissileArtifact_VerminSpit;
            //hooks of heresy
            On.EntityStates.Mage.Weapon.BaseThrowBombState.Fire += MissileArtifact_ThrowBombHeresy;

            //shuriken
            IL.RoR2.PrimarySkillShurikenBehavior.FireShuriken += MissileArtifact_Shuriken;
            EquipmentsLol();

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
            //seeker punch
            On.EntityStates.Seeker.SpiritPunch.FireGauntlet += MissileArtifact_SeekerPunch;
            //false son spikes
            On.EntityStates.FalseSon.LunarSpikes.FireLunarSpike += MissileArtifact_SonSurvivorSpike;
            //chef cleavers
            On.EntityStates.Chef.Dice.OnEnter += MissileArtifact_ChefCleaver;
            //acrid spit
            On.EntityStates.Croco.FireSpit.OnEnter += (orig, self) =>
            {
                orig(self);
                if (self is EntityStates.Croco.FireDiseaseProjectile)
                    return;
                FireWarfareProjectilesSimple(self.characterBody, self.damageStat, self.projectilePrefab);
            };
            //huntress arrow
            IL.EntityStates.Huntress.HuntressWeapon.FireSeekingArrow.FireOrbArrow += MissileArtifact_FireHuntressSeekingArrow;

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
            //child spark
            On.EntityStates.ChildMonster.SparkBallFire.FireBomb += MissileArtifact_ChildSpark;
            //gup
            On.EntityStates.Gup.BaseSplitDeath.OnEnter += MissileArtifact_GupDeathEnter;
            //mithrix
            On.EntityStates.BrotherMonster.FistSlam.OnEnter += MissileArtifact_BrotherFistSlam;
            On.EntityStates.BrotherMonster.WeaponSlam.OnEnter += MissileArtifact_BrotherWeaponSlam;
            On.EntityStates.BrotherMonster.UltChannelState.OnEnter += MissileArtifact_BrotherUltChannelState;
            //greater wisp
            On.EntityStates.GreaterWispMonster.FireCannons.OnEnter += MissileArtifact_GreaterWispFireCannons;
            //brass contraption
            IL.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FixedUpdate += MissileArtifact_ChargeTrioBomb;
            On.EntityStates.Bell.BellWeapon.ChargeTrioBomb.FindTargetChildTransformFromBombIndex += MissileArtifact_FindTrioBombTransform;
            #endregion
        }

        public static void FireWarfareProjectilesSimple(CharacterBody body, float damageCoefficient, string assetGuid)
        {
            GameObject projectilePrefab = Addressables.LoadAssetAsync<GameObject>(assetGuid).WaitForCompletion();
            FireWarfareProjectilesSimple(body, damageCoefficient, projectilePrefab);
        }
        public static void FireWarfareProjectilesSimple(CharacterBody body, float damageCoefficient, GameObject projectilePrefab)
        {
            if (body.hasEffectiveAuthority && IsMoreProjectilesActiveForBody(body))
            {
                Ray aimRay = body.equipmentSlot.GetAimRay();

                FireProjectileInfo fireProjectileInfo = new FireProjectileInfo
                {
                    projectilePrefab = projectilePrefab,
                    position = aimRay.origin,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    owner = body.gameObject,
                    damage = body.damage * damageCoefficient,
                    crit = Util.CheckRoll(body.crit, body.master)
                };

                FireWarfareProjectiles(aimRay, fireProjectileInfo, projectileSpread);
            }
        }
        internal static void FireWarfareProjectiles(Ray aimRay, FireProjectileInfo fireProjectileInfo, float spread)
        {
            Vector3 rhs = Vector3.Cross(Vector3.up, aimRay.direction);
            Vector3 axis = Vector3.Cross(aimRay.direction, rhs);
            FireWarfareProjectiles(aimRay, fireProjectileInfo, spread, axis);
        }
        internal static void FireWarfareProjectiles(Ray aimRay, FireProjectileInfo fireProjectileInfo, float spread, Vector3 axis)
        {
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(spread, axis) * aimRay.direction);
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(Quaternion.AngleAxis(-spread, axis) * aimRay.direction);
            ProjectileManager.instance.FireProjectile(fireProjectileInfo);
        }
    }
}
