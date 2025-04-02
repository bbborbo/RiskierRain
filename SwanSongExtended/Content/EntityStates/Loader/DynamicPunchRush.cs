using EntityStates;
using EntityStates.Loader;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.States.Loader
{
    class DynamicPunchRush : BaseSkillState
    {
        private float stopwatch;
		private float entryDuration;
		public static float baseEntryDuration = .2f;
		private float flamethrowerDuration;
		public static float baseFlamethrowerDuration = 10f;
		private ChildLocator childLocator;
		private Transform leftMuzzleTransform;
		private Transform rightMuzzleTransform;
		public float tickFrequency;
		private float tickDamageCoefficient = 1.2f;
		private static int PrepFlamethrowerStateHash = Animator.StringToHash("PrepFlamethrower");
		private static int ExitFlamethrowerStateHash = Animator.StringToHash("ExitFlamethrower");
		private static int FlamethrowerParamHash = Animator.StringToHash("Flamethrower.playbackRate");
		//public static string endAttackSoundString;
		private Transform leftFlamethrowerTransform;
		private Transform rightFlamethrowerTransform;
		public static float radius = 10;
		public static float force = 20f;
		//public static GameObject impactEffectPrefab;
		public static float procCoefficientPerTick = 0.3f;
		[SerializeField]
		public float maxDistance = 10;
		private bool hasBegunFlamethrower;
		//public static string startAttackSoundString;
		private static int FlamethrowerStateHash => BaseChargeFist.ChargePunchIntroStateHash;// Animator.StringToHash("Flamethrower");
		[SerializeField]
		//public GameObject flamethrowerEffectPrefab;
		private float flamethrowerStopwatch;

		public static float baseTickFrequency = 50f;

		public override void OnEnter()
		{
			base.OnEnter();
			this.stopwatch = 0f;
			this.entryDuration = baseEntryDuration / this.attackSpeedStat;
			this.flamethrowerDuration = baseFlamethrowerDuration;
			Transform modelTransform = base.GetModelTransform();

			tickFrequency = baseTickFrequency / this.attackSpeedStat;

			if (base.characterBody)
			{
				base.characterBody.SetAimTimer(this.entryDuration + this.flamethrowerDuration + 1f);
			}
			if (modelTransform)
			{
				this.childLocator = modelTransform.GetComponent<ChildLocator>();
				this.leftMuzzleTransform = this.childLocator.FindChild("MuzzleLeft");
				this.rightMuzzleTransform = this.childLocator.FindChild("MuzzleRight");
			}
			base.PlayAnimation("Gesture, Additive", PrepFlamethrowerStateHash, FlamethrowerParamHash, this.entryDuration);
		}
		public override void OnExit()
		{
			//Util.PlaySound(endAttackSoundString, base.gameObject);
			base.PlayCrossfade("Gesture, Additive", ExitFlamethrowerStateHash, 0.1f);
			if (this.leftFlamethrowerTransform)
			{
				EntityState.Destroy(this.leftFlamethrowerTransform.gameObject);
			}
			if (this.rightFlamethrowerTransform)
			{
				EntityState.Destroy(this.rightFlamethrowerTransform.gameObject);
			}
			base.OnExit();
		}
		private void FireGauntlet(string muzzleString)
		{
			Ray aimRay = base.GetAimRay();
			if (base.isAuthority)
			{
				BulletAttack bulletAttack = new BulletAttack();
				bulletAttack.owner = base.gameObject;
				bulletAttack.weapon = base.gameObject;
				bulletAttack.origin = aimRay.origin;
				bulletAttack.aimVector = aimRay.direction;
				bulletAttack.minSpread = 0f;
				bulletAttack.damage = this.tickDamageCoefficient * this.damageStat;
				bulletAttack.force = force;
				bulletAttack.muzzleName = muzzleString;
				//bulletAttack.hitEffectPrefab = impactEffectPrefab;
				bulletAttack.isCrit = Util.CheckRoll(this.critStat, base.characterBody.master);
				bulletAttack.radius = radius;
				bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
				bulletAttack.stopperMask = LayerIndex.world.mask;
				bulletAttack.procCoefficient = procCoefficientPerTick;
				bulletAttack.maxDistance = this.maxDistance;
				bulletAttack.smartCollision = true;
				bulletAttack.damageType = DamageType.Generic;
				bulletAttack.allowTrajectoryAimAssist = false;
				bulletAttack.damageType.damageSource = DamageSource.Special;
				bulletAttack.Fire();
				
			}
		}
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			this.stopwatch += base.GetDeltaTime();
			if (this.stopwatch >= this.entryDuration && !this.hasBegunFlamethrower)
			{
				this.hasBegunFlamethrower = true;
				//Util.PlaySound(startAttackSoundString, base.gameObject);
				base.PlayAnimation("Gesture, Additive", FlamethrowerStateHash, FlamethrowerParamHash, this.flamethrowerDuration);
				//if (this.childLocator)
				//{
				//	Transform transform = this.childLocator.FindChild("MuzzleLeft");
				//	Transform transform2 = this.childLocator.FindChild("MuzzleRight");
				//	if (transform)
				//	{
				//		this.leftFlamethrowerTransform = UnityEngine.Object.Instantiate<GameObject>(this.flamethrowerEffectPrefab, transform).transform;
				//	}
				//	if (transform2)
				//	{
				//		this.rightFlamethrowerTransform = UnityEngine.Object.Instantiate<GameObject>(this.flamethrowerEffectPrefab, transform2).transform;
				//	}
				//	if (this.leftFlamethrowerTransform)
				//	{
				//		this.leftFlamethrowerTransform.GetComponent<ScaleParticleSystemDuration>().newDuration = this.flamethrowerDuration;
				//	}
				//	if (this.rightFlamethrowerTransform)
				//	{
				//		this.rightFlamethrowerTransform.GetComponent<ScaleParticleSystemDuration>().newDuration = this.flamethrowerDuration;
				//	}
				//}
				this.FireGauntlet("MuzzleCenter");
			}
			if (this.hasBegunFlamethrower)
			{
				this.flamethrowerStopwatch += Time.deltaTime;
				float num = 1f / tickFrequency / this.attackSpeedStat;
				if (this.flamethrowerStopwatch > num)
				{
					this.flamethrowerStopwatch -= num;
					this.FireGauntlet("MuzzleCenter");
				}
				//this.UpdateFlamethrowerEffect();
			}
			if (!ShouldKeepPunchingAuthority())
			{
				Debug.Log("stopwatch = " + stopwatch + ", duration = " + flamethrowerDuration);
				this.outer.SetNextStateToMain();
				return;
			}
		}
		//private void UpdateFlamethrowerEffect()
		//{
		//	Ray aimRay = base.GetAimRay();
		//	Vector3 direction = aimRay.direction;
		//	Vector3 direction2 = aimRay.direction;
		//	if (this.leftFlamethrowerTransform)
		//	{
		//		this.leftFlamethrowerTransform.forward = direction;
		//	}
		//	if (this.rightFlamethrowerTransform)
		//	{
		//		this.rightFlamethrowerTransform.forward = direction2;
		//	}
		//}
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}
		protected virtual bool ShouldKeepPunchingAuthority()
		{
			return base.IsKeyDownAuthority();
		}
	}
}
