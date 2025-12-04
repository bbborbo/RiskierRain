using SurvivorTweaks.Skills;
using EntityStates;
using EntityStates.Captain.Weapon;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using EntityStates.Mage.Weapon;
using RoR2.UI;

namespace SurvivorTweaks.States.Captain
{
    class PocketWormhole : BaseSkillState
	{
		public static GameObject endpointIndicatorPrefab = ChargeMeteor.areaIndicatorPrefab;
		public static GameObject projectilePrefab;
		public static GameObject muzzleflashEffectPrefab;
		public static GameObject chargeEffectPrefab;

		public float baseExitDuration => PocketWormholeSkill.baseExitDuration;
		private float exitDuration;
		public static float baseEnterDuration = PocketWormholeSkill.baseEnterDuration;
		private float enterDuration;
		private bool hasFired;

		public static string enterSoundString;
		public static string attackString;

		public static float recoilAmplitude;
		public static float bloom;
		public static string targetMuzzle = FireTazer.targetMuzzle;
		float releaseTime = -1;

		Vector3 startpointPosition;
		Vector3 _endpointPosition;
		public Vector3 endpointPosition
		{
            get
            {
				return _endpointPosition;
            }
			private set
			{
				_endpointPosition = value;
				if (endpointIndicatorInstance)
					endpointIndicatorInstance.transform.position = value;
            }
		}
		GameObject endpointIndicatorInstance;
		private bool disableIndicator = false;
		private CrosshairUtils.OverrideRequest crosshairOverrideRequest;
		bool _validPlacement;
		public bool validPlacement
		{
			get
			{
				return _validPlacement;
			}
			private set
			{
				UpdateCrosshair(value);
				_validPlacement = value;
				//if (endpointIndicatorInstance)
				//	endpointIndicatorInstance.SetActive(value);
			}
		}


		public override void OnEnter()
		{
			base.OnEnter();
			if (endpointIndicatorPrefab != null && isAuthority)
			{
				this.endpointIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(endpointIndicatorPrefab);
				UpdateEndpointIndicator();
				UpdateCrosshair(false);
			}
			this.exitDuration = baseExitDuration / this.attackSpeedStat;
			this.enterDuration = baseEnterDuration / this.attackSpeedStat;
			if (chargeEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(chargeEffectPrefab, base.gameObject, targetMuzzle, false);
			}
			Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, this.attackSpeedStat);
			base.PlayCrossfade("Gesture, Override", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.enterDuration, 0.1f);
			base.PlayCrossfade("Gesture, Additive", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.enterDuration, 0.1f);
		}

        private void UpdateEndpointIndicator()
		{
			if (this.endpointIndicatorInstance && !disableIndicator)
			{
				this.endpointIndicatorInstance.transform.localScale = Vector3.one * characterBody.bestFitRadius;
				this.endpointIndicatorInstance.SetActive(true);
			}
		}

		void UpdateCrosshair(bool newValue)
        {
			return;
			if (fixedAge < this.enterDuration)
				newValue = false;
			if (validPlacement != newValue || this.crosshairOverrideRequest == null)
			{
				CrosshairUtils.OverrideRequest overrideRequest = this.crosshairOverrideRequest;
				if (overrideRequest != null)
				{
					overrideRequest.Dispose();
				}
				GameObject crosshairPrefab = this.validPlacement ? PrepWall.goodCrosshairPrefab : PrepWall.badCrosshairPrefab;
				this.crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, crosshairPrefab, CrosshairUtils.OverridePriority.Skill);
			}
		}

		private void UpdateAimInfo()
		{
			Vector3 footPosition = this.characterBody.footPosition;
			float num = FireWormhole.minDistance;
			float num2 = num * 2f;
			float maxDistance = PocketWormholeSkill.maxTunnelDistance;
			Rigidbody attackerRigidbody = this.rigidbody;
			if (!attackerRigidbody)
			{
				//activatorSkillSlot.AddOneStock();
				validPlacement = false;
				return;
			}

			Vector3 position = base.transform.position;

			Vector3 pointBPositionAttempt;

			RaycastHit raycastHit;
			Ray aimRay = base.GetAimRay();
			if (Physics.Raycast(aimRay, out raycastHit, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
			{
				pointBPositionAttempt = raycastHit.point + raycastHit.normal * num;
			}
			else
			{
				if (base.inputBank)
				{
					pointBPositionAttempt = inputBank.aimOrigin + inputBank.aimDirection.normalized * maxDistance;
				}
				else
				{
					pointBPositionAttempt = transform.position + transform.forward.normalized * maxDistance;
				}
			}

			Vector3 distanceToPointB = pointBPositionAttempt - position;
			Vector3 pointBDirection = distanceToPointB.normalized;
			Vector3 pointBPosition = pointBPositionAttempt;

			RaycastHit raycastHit2;
			if (attackerRigidbody.SweepTest(pointBDirection, out raycastHit2, distanceToPointB.magnitude))
			{
				if (raycastHit2.distance < num2)
				{
					//activatorSkillSlot.AddOneStock();
					validPlacement = false;
				}
                else
                {
					validPlacement = true;
                }
				pointBPosition = position + pointBDirection * raycastHit2.distance;
			}
            else
            {
				validPlacement = true;
            }

			startpointPosition = (position + pointBDirection * num);
			endpointPosition = (pointBPosition);
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();

			if (!base.isAuthority)
				return;

			base.StartAimMode(this.enterDuration, false);
			//if not fired 
			if (base.fixedAge < this.enterDuration)
				return;

			UpdateAimInfo();

			//if after min duration
			if (!this.IsKeyDownAuthority())
			{
				this.hasFired = true;
				releaseTime = this.fixedAge;
				base.StartAimMode(this.exitDuration + 2f, false);
				this.Fire();
			}
		}

		private void Fire()
		{
			this.endpointIndicatorInstance.SetActive(false);
			CrosshairUtils.OverrideRequest overrideRequest = this.crosshairOverrideRequest;
			if (overrideRequest != null)
			{
				overrideRequest.Dispose();
			}

			//Log.Warning(validPlacement);
			//if (!validPlacement)
            //{
			//	activatorSkillSlot.AddOneStock();
			//	this.outer.SetNextStateToMain();
			//	return;
            //}				
			FireWormhole state = new FireWormhole();
			state.activatorSkillSlot = this.activatorSkillSlot;
			state.startPos = this.startpointPosition;
			state.endpointPos = this.endpointPosition;
			this.outer.SetNextState(state);
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}
	}
}
