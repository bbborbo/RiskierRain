using EntityStates;
using EntityStates.Commando;
using RiskierRain.SurvivorTweaks;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.States.Commando
{
    class UltraDash : BaseState
	{
		private Vector3 forwardDirection;

		private GameObject slideEffectInstance;

		private bool startedStateGrounded;

		public override void OnEnter()
		{
			base.OnEnter();
			Util.PlaySound(SlideState.soundString, base.gameObject);
			if (base.inputBank && base.characterDirection)
			{
				base.characterDirection.forward = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
			}
			if (SlideState.jetEffectPrefab)
			{
				Transform transform = base.FindModelChild("LeftJet");
				Transform transform2 = base.FindModelChild("RightJet");
				if (transform)
				{
					UnityEngine.Object.Instantiate<GameObject>(SlideState.jetEffectPrefab, transform);
				}
				if (transform2)
				{
					UnityEngine.Object.Instantiate<GameObject>(SlideState.jetEffectPrefab, transform2);
				}
			}
			base.characterBody.SetSpreadBloom(0f, false);
			this.PlayAnimation("Body", "Jump");
			Vector3 velocity = base.characterMotor.velocity;
			velocity.y = base.characterBody.jumpPower * CommandoTweaks.slideJumpMultiplier;
			base.characterMotor.velocity = velocity;
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.isAuthority)
			{
				if (base.inputBank && base.characterDirection)
				{
					base.characterDirection.moveVector = base.inputBank.moveVector;
					this.forwardDirection = base.characterDirection.forward;
				}
				if (base.characterMotor)
				{
					float num2 = SlideState.jumpforwardSpeedCoefficientCurve.Evaluate(base.fixedAge / CommandoTweaks.slideJumpDuration) * CommandoTweaks.slideJumpMultiplier;
					base.characterMotor.rootMotion += num2 * this.moveSpeedStat * this.forwardDirection * Time.fixedDeltaTime;
				}
				if (base.fixedAge >= CommandoTweaks.slideJumpDuration)
				{
					this.outer.SetNextStateToMain();
				}
			}
		}

		public override void OnExit()
		{
			this.PlayImpactAnimation();
			base.OnExit();
		}

		private void PlayImpactAnimation()
		{
			Animator modelAnimator = base.GetModelAnimator();
			int layerIndex = modelAnimator.GetLayerIndex("Impact");
			if (layerIndex >= 0)
			{
				modelAnimator.SetLayerWeight(layerIndex, 1f);
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}
	}
}
