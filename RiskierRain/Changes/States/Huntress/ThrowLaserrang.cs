using RiskierRain.Skills;
using EntityStates;
using EntityStates.Huntress.HuntressWeapon;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.EntityState.Huntress
{
    class ThrowLaserrang : BaseState
	{
		public static float baseDuration = 1.35f;
		public static float damageCoefficient = 5f;
		public static float force = 150f;
		private float duration;

		public static GameObject chargePrefab;
		public static GameObject muzzleFlashPrefab;
		private GameObject chargeEffect;
		public static string attackSoundString;

		public static float smallHopStrength;
		public static float antigravityStrength = 20;

		private Animator animator;
		private Transform modelTransform;
		private ChildLocator childLocator;
		private float stopwatch;
		private bool hasTriedToThrowGlaive;
		private bool hasSuccessfullyThrownGlaive;


		public override void OnEnter()
		{
			base.OnEnter();
			this.stopwatch = 0f;
			this.duration = baseDuration / this.attackSpeedStat;
			this.modelTransform = base.GetModelTransform();
			this.animator = base.GetModelAnimator();
			Util.PlayAttackSpeedSound(ThrowGlaive.attackSoundString, base.gameObject, this.attackSpeedStat);

			if (base.characterMotor && ThrowGlaive.smallHopStrength != 0f)
			{
				base.characterMotor.velocity.y = ThrowGlaive.smallHopStrength;
			}
			base.PlayAnimation("FullBody, Override", "ThrowGlaive", "ThrowGlaive.playbackRate", this.duration);
			if (this.modelTransform)
			{
				this.childLocator = this.modelTransform.GetComponent<ChildLocator>();
				if (this.childLocator)
				{
					Transform transform = this.childLocator.FindChild("HandR");
					if (transform && ThrowGlaive.chargePrefab)
					{
						this.chargeEffect = UnityEngine.Object.Instantiate<GameObject>(ThrowGlaive.chargePrefab, transform.position, transform.rotation);
						this.chargeEffect.transform.parent = transform;
					}
				}
			}
			if (base.characterBody)
			{
				base.characterBody.SetAimTimer(this.duration);
			}
		}

		public override void OnExit()
		{
			base.OnExit();
			if (this.chargeEffect)
			{
				Destroy(this.chargeEffect);
			}
			int layerIndex = this.animator.GetLayerIndex("Impact");
			if (layerIndex >= 0)
			{
				this.animator.SetLayerWeight(layerIndex, 1.5f);
				this.animator.PlayInFixedTime("LightImpact", layerIndex, 0f);
			}
			if (!this.hasTriedToThrowGlaive)
			{
				this.FireGlaive();
			}
			if (!this.hasSuccessfullyThrownGlaive && NetworkServer.active)
			{
				base.skillLocator.secondary.AddOneStock();
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.stopwatch += Time.fixedDeltaTime;
			if (!this.hasTriedToThrowGlaive && this.animator.GetFloat("ThrowGlaive.fire") > 0f)
			{
				if (this.chargeEffect)
				{
                    Destroy(this.chargeEffect);
				}
				this.FireGlaive();
			}
			CharacterMotor characterMotor = base.characterMotor;
			characterMotor.velocity.y = characterMotor.velocity.y + antigravityStrength * Time.fixedDeltaTime * (1f - this.stopwatch / this.duration);
			if (this.stopwatch >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
				return;
			}
		}

		void FireGlaive()
		{
			if (!NetworkServer.active || this.hasTriedToThrowGlaive)
			{
				return;
			}

			this.hasTriedToThrowGlaive = true;

			Ray aimRay = base.GetAimRay();
			Vector3 position = aimRay.origin;
			Quaternion rotation = Util.QuaternionSafeLookRotation(aimRay.direction);
			if (modelTransform)
			{
				ChildLocator component = modelTransform.GetComponent<ChildLocator>();
				if (component)
				{
					Transform transform = component.FindChild("HandR");
					if (transform)
					{
						position = transform.position;
					}
				}
			}

			EffectManager.SimpleMuzzleFlash(ThrowGlaive.muzzleFlashPrefab, base.gameObject, "HandR", true);

			ProjectileManager.instance.FireProjectile(LaserrangSkill.boomerangPrefab,
				position, rotation, base.gameObject,
				this.damageStat * damageCoefficient, force,
				Util.CheckRoll(this.critStat, base.characterBody.master),
				DamageColorIndex.Default, null, -1f);

			this.hasSuccessfullyThrownGlaive = true;
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}
	}
}
