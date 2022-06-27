using System;
using System.Collections.Generic;
using System.Text;
using RiskierRain.Skills;
using EntityStates.Huntress.HuntressWeapon;
using EntityStates.Huntress.Weapon;
using EntityStates.Mage.Weapon;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace RiskierRain.EntityState.Huntress
{
    class FireExplosiveArrow : BaseThrowBombState
    {
		public static GameObject muzzleFlash = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashHuntress");
		public static GameObject muzzleFlashCrit = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/muzzleflashes/MuzzleflashHuntressFlurry");

		public bool isCrit;
		public static float maxDamage = 3.2f;
		public static float minDamage = 0.8f;

        public override void OnEnter()
		{
			this.projectilePrefab = isCrit ? ExplosiveArrowSkill.critArrowPrefab : ExplosiveArrowSkill.regularArrowPrefab;
			this.muzzleflashEffectPrefab = null;
			this.baseDuration = 0.1f;
			this.selfForce = 1000f;

			maxDamageCoefficient = maxDamage;
			minDamageCoefficient = minDamage;
			base.OnEnter();

			EffectManager.SimpleMuzzleFlash(isCrit ? muzzleFlashCrit : muzzleFlash, base.gameObject, new FireSeekingArrow().muzzleString, false);
            if (isCrit)
            {
				base.AddRecoil(-1.0f * FireArrowSnipe.recoilAmplitude, -1.6f * FireArrowSnipe.recoilAmplitude, 
					-0.2f * FireArrowSnipe.recoilAmplitude, 0.2f * FireArrowSnipe.recoilAmplitude);
				Util.PlaySound(new FireArrowSnipe().fireSoundString, base.gameObject);
			}
            else
			{
				base.AddRecoil(-0.1f * FireArrowSnipe.recoilAmplitude, -0.4f * FireArrowSnipe.recoilAmplitude,
					-0.1f * FireArrowSnipe.recoilAmplitude, 0.1f * FireArrowSnipe.recoilAmplitude);
				Util.PlaySound(new FireSeekingArrow().attackSoundString, base.gameObject);
			}
		}
        public override void PlayThrowAnimation()
		{
			base.PlayThrowAnimation();
			//base.PlayAnimation("Gesture, Override", "FireSeekingArrow");
			//base.PlayAnimation("Gesture, Additive", "FireSeekingArrow");
			//base.PlayAnimation("Gesture, Override", "FireArrowSnipe");
			//base.PlayAnimation("Gesture, Additive", "FireArrowSnipe");
			//base.PlayAnimation("FullBody, Additive", "BufferEmpty");)
		}

		public override void ModifyProjectile(ref FireProjectileInfo projectileInfo)
        {
			projectileInfo.crit = isCrit;
            base.ModifyProjectile(ref projectileInfo);
        }
    }
}
