﻿using SwanSongExtended.Skills;
using EntityStates;
using EntityStates.Captain.Weapon;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.States.Captain
{
    class PocketWormhole : BaseSkillState
	{
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


		public override void OnEnter()
		{
			base.OnEnter();
			this.exitDuration = baseExitDuration / this.attackSpeedStat;
			this.enterDuration = baseEnterDuration / this.attackSpeedStat;
			base.StartAimMode(this.exitDuration + 2f, false);
			if (chargeEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(chargeEffectPrefab, base.gameObject, targetMuzzle, false);
			}
			Util.PlayAttackSpeedSound(enterSoundString, base.gameObject, this.attackSpeedStat);
			base.PlayAnimation("Gesture, Additive", "FireTazer", "FireTazer.playbackRate", this.exitDuration);
			base.PlayAnimation("Gesture, Override", "FireTazer", "FireTazer.playbackRate", this.exitDuration);
		}

		private void Fire()
		{
			this.hasFired = true;
			Util.PlaySound(FireTazer.attackString, base.gameObject);
			base.AddRecoil(-1f * FireTazer.recoilAmplitude, -1.5f * FireTazer.recoilAmplitude, -0.25f * FireTazer.recoilAmplitude, 0.25f * FireTazer.recoilAmplitude);
			base.characterBody.AddSpreadBloom(FireTazer.bloom);
			Ray aimRay = base.GetAimRay();
			if (FireTazer.muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(FireTazer.muzzleflashEffectPrefab, base.gameObject, FireTazer.targetMuzzle, false);
			}
			if (NetworkServer.active)
			{
				Vector3 footPosition = this.characterBody.footPosition;
				float num = 2f;
				float num2 = num * 2f;
				float maxDistance = PocketWormholeSkill.maxTunnelDistance;
				Rigidbody attackerRigidbody = base.GetComponent<Rigidbody>();
				if (!attackerRigidbody)
				{
					activatorSkillSlot.AddOneStock();
					return;
				}

				Vector3 position = base.transform.position;

				Vector3 pointBPositionAttempt;

				RaycastHit raycastHit;
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
						activatorSkillSlot.AddOneStock();
						return;
					}
					pointBPosition = position + pointBDirection * raycastHit2.distance;
				}

				GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Zipline"));
				ZiplineController component2 = gameObject.GetComponent<ZiplineController>();
				component2.SetPointAPosition(position + pointBDirection * num);
				component2.SetPointBPosition(pointBPosition);
				gameObject.AddComponent<DestroyOnTimer>().duration = PocketWormholeSkill.maxTunnelDuration + baseExitDuration;
				NetworkServer.Spawn(gameObject);
			}
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge >= this.enterDuration && !this.hasFired)
			{
				this.Fire();
			}
			if (base.fixedAge >= this.enterDuration + this.exitDuration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
				return;
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}
	}
}
