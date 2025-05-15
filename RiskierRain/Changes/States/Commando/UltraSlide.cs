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
    class UltraSlide : BaseSkillState
	{
		private Vector3 forwardDirection;

		private GameObject slideEffectInstance;

		private bool isGrounded;

		public override void OnEnter()
		{
			base.OnEnter();
			if (base.inputBank && base.characterDirection)
			{
				forwardDirection = ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
				base.characterDirection.forward = forwardDirection;
			}
			if (base.characterMotor)
			{
				this.isGrounded = base.characterMotor.isGrounded;
			}
			base.characterBody.SetSpreadBloom(0f, false);
			if (!this.isGrounded)
			{
				EnterDashState();
				return;
			}

			Util.PlaySound(SlideState.soundString, base.gameObject);
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
			base.PlayAnimation("Body", "SlideForward", "SlideForward.playbackRate", CommandoTweaks.slideMaxDuration);
			if (SlideState.slideEffectPrefab)
			{
				Transform parent = base.FindModelChild("Base");
				this.slideEffectInstance = UnityEngine.Object.Instantiate<GameObject>(SlideState.slideEffectPrefab, parent);
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.isAuthority)
			{
				if (base.inputBank.jump.wasDown)
                {
                    EnterDashState();
                    return;
                }

                if (base.fixedAge >= CommandoTweaks.slideMaxDuration || (!base.IsKeyDownAuthority() && base.fixedAge > 1))
				{
					this.outer.SetNextStateToMain();
					return;
				}
				base.PlayAnimation("Body", "SlideForward", "SlideForward.playbackRate", 0.2f);

				if (base.inputBank && base.characterDirection)
				{
					this.forwardDirection = (forwardDirection + base.inputBank.moveVector * CommandoTweaks.slideStrafeMultiplier).normalized;// base.characterDirection.forward;
					base.characterDirection.moveVector = forwardDirection;
				}
				if (base.characterMotor)
				{
					float num2 = SlideState.forwardSpeedCoefficientCurve.Evaluate(base.fixedAge / CommandoTweaks.slideMaxDuration) * CommandoTweaks.slideSpeedMultiplier;
					base.characterMotor.rootMotion = num2 * this.moveSpeedStat * this.forwardDirection * Time.fixedDeltaTime;
				}
			}
		}

        private void EnterDashState()
        {
            this.outer.SetNextState(new UltraDash());
        }

        public override void OnExit()
		{
			this.PlayImpactAnimation();
			if (this.slideEffectInstance)
			{
				EntityStates.EntityState.Destroy(this.slideEffectInstance);
			}
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
