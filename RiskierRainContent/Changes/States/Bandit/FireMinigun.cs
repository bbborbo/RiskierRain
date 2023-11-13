using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Bandit2.Weapon;
using RoR2;
using UnityEngine;

namespace RiskierRainContent.EntityState.Bandit
{
    class FireMinigun : GenericBulletBaseState
    {
        public static string muzzle = "MuzzleShotgun";
        public static string soundString = "Play_bandit2_m1_rifle";
        public static GameObject muzzleFlash = LegacyResourcesAPI.Load<GameObject > ("prefabs/effects/muzzleflashes/MuzzleflashBandit2");
        public static GameObject tracerEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/tracers/TracerBandit2Rifle");
        public static GameObject hitEffect = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/impacteffects/HitsparkBandit");
        public static float damageCoeff = 1.6f;

        public override void OnEnter()
        {
            baseDuration = 0.25f;
            damageCoefficient = damageCoeff;
            

            recoilAmplitudeY = 1;
            recoilAmplitudeX = 0.3f;

            fireSoundString = soundString;
            muzzleFlashPrefab = muzzleFlash;
            tracerEffectPrefab = tracerEffect;
            hitEffectPrefab = hitEffect;
            muzzleName = muzzle;

            base.OnEnter();

            base.PlayAnimation("Gesture, Additive", "FireMainWeapon", "FireMainWeapon.playbackRate", this.duration);
        }

        public override void ModifyBullet(BulletAttack bulletAttack)
        {
            base.ModifyBullet(bulletAttack);
            bulletAttack.falloffModel = BulletAttack.FalloffModel.DefaultBullet;
            bulletAttack.radius += 0.1f;
            bulletAttack.maxDistance = 200;
        }
    }
}
