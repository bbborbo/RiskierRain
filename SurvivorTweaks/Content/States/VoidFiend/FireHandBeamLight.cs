using EntityStates;
using EntityStates.VoidJailer.Weapon;
using EntityStates.VoidSurvivor.Weapon;
using SurvivorTweaks.SurvivorTweaks;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using RoR2.Skills;
using RoR2.Projectile;

namespace SurvivorTweaks.States.VoidFiend
{
	public class FireHandBeamLight : BaseSkillState, SteppedSkillDef.IStepSetter
	{
		public GameObject muzzleflashEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorBeamMuzzleflash_prefab).WaitForCompletion();
		public GameObject hitEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorBeamImpact_prefab).WaitForCompletion();
		public GameObject tracerEffectPrefab = Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_VoidSurvivor.VoidSurvivorBeamTracer_prefab).WaitForCompletion();
		public GameObject projectilePrefab => ViendTweaks.viendPrimaryDamagePool;

		public static float damageCoefficientLight = 3.8f;
		public static float damageCoefficientHeavy = 3.8f;
		public static float poolDamageCoefficientPerSecond = 2.5f;
		public float maxDistance = 1000; //1000
		public float force = 1000; //1000
		public int bulletCount = 1; //1
		public float bulletRadius = 2; //2
		public float baseDurationLight = 0.8f; //0.6f
		public float baseDurationHeavy = 1.1f; //0.6f
		public string attackSoundString = "Play_voidman_m1_shoot";
		public float recoilAmplitudeLight = 2f; //1
		public float recoilAmplitudeHeavy = 3.5f; //1
		public float spreadBloomValue = 0.2f; //0.2f
		public float maxSpread = 3; //3
		public static string muzzle = "MuzzleHandBeam";
		public string animationLayerName = "LeftArm, Override";
		public string animationStateName = "FireHandBeam";
		public string animationPlaybackRateParam = "HandBeam.playbackRate";
		public float trajectoryAimAssistMultiplier = 0.25f; //0.75f

		public void SetStep(int i)
		{
			step = i % ViendTweaks.primaryStepCount;
		}
		private int step = 0;
		private bool isHeavyAttack => step == ViendTweaks.primaryStepCount - 1;
		private float baseDuration;
		private float duration;
		private float damageCoefficient;
		private Transform muzzleTransform => FindModelChild(muzzle);

		public override void OnEnter()
		{
			base.OnEnter();
			this.damageCoefficient = isHeavyAttack ? damageCoefficientHeavy : damageCoefficientLight;
			this.baseDuration = isHeavyAttack ? baseDurationHeavy : baseDurationLight;
			this.duration = this.baseDuration / this.attackSpeedStat;
			//Ray aimRay = base.GetAimRay();
			CalcBeamPath(out Ray aimRay, out Vector3 beamEnd);//
			base.PlayAnimation(this.animationLayerName, this.animationStateName, this.animationPlaybackRateParam, this.duration, 0f);
			float recoilAmplitude = isHeavyAttack ? recoilAmplitudeHeavy : recoilAmplitudeLight;
			base.AddRecoil(-1f * recoilAmplitude, -2f * recoilAmplitude, -0.5f * recoilAmplitude, 0.5f * recoilAmplitude);
			base.StartAimMode(aimRay, 2f, false);
			Util.PlaySound(this.attackSoundString, base.gameObject);
			if (this.muzzleflashEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(this.muzzleflashEffectPrefab, base.gameObject, muzzle, false);
			}
			if (base.isAuthority)
			{
				BulletAttack bulletAttack = new BulletAttack();
				bulletAttack.owner = base.gameObject;
				bulletAttack.weapon = base.gameObject;
				bulletAttack.origin = aimRay.origin;
				bulletAttack.aimVector = aimRay.direction;
				bulletAttack.muzzleName = muzzle;
				bulletAttack.maxDistance = this.maxDistance;
				bulletAttack.minSpread = 0f;
				bulletAttack.maxSpread = base.characterBody.spreadBloomAngle;
				bulletAttack.radius = this.bulletRadius;
				bulletAttack.falloffModel = BulletAttack.FalloffModel.None;
				bulletAttack.smartCollision = true;
				bulletAttack.damage = this.damageCoefficient * this.damageStat;
				bulletAttack.procCoefficient = 1f / (float)this.bulletCount;
				bulletAttack.force = this.force;
				bulletAttack.isCrit = Util.CheckRoll(this.critStat, base.characterBody.master);
				bulletAttack.damageType = DamageType.SlowOnHit;
				bulletAttack.damageType.damageSource = DamageSource.Primary;
				bulletAttack.tracerEffectPrefab = this.tracerEffectPrefab;
				bulletAttack.hitEffectPrefab = this.hitEffectPrefab;
				bulletAttack.trajectoryAimAssistMultiplier = this.trajectoryAimAssistMultiplier;
				bulletAttack.stopperMask = LayerIndex.CommonMasks.interactable;
				bulletAttack.Fire();

                if (isHeavyAttack)
				{
					FireProjectileInfo fireProjectileInfo = default(FireProjectileInfo);
					fireProjectileInfo.projectilePrefab = this.projectilePrefab;
					fireProjectileInfo.position = beamEnd + Vector3.up * 1;
					fireProjectileInfo.owner = base.gameObject;
					fireProjectileInfo.damage = this.damageStat * poolDamageCoefficientPerSecond * 0.5f;
					fireProjectileInfo.crit = Util.CheckRoll(this.critStat, base.characterBody.master);
					ProjectileManager.instance.FireProjectile(fireProjectileInfo);
				}
			}
			base.characterBody.AddSpreadBloom(this.spreadBloomValue);
		}

		public override void OnExit()
		{
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (base.fixedAge >= this.duration && base.isAuthority)
			{
				this.outer.SetNextStateToMain();
				return;
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}

		protected void CalcBeamPath(out Ray beamRay, out Vector3 beamEndPos)
		{
			Ray aimRay = base.GetAimRay();
			float num = float.PositiveInfinity;
			RaycastHit[] array = Physics.RaycastAll(aimRay, maxDistance, LayerIndex.CommonMasks.bullet, QueryTriggerInteraction.Ignore);
			Transform root = base.GetModelTransform().root;
			for (int i = 0; i < array.Length; i++)
			{
				ref RaycastHit ptr = ref array[i];
				float distance = ptr.distance;
				if (distance < num && ptr.collider.transform.root != root)
				{
					num = distance;
				}
			}
			num = Mathf.Min(num, maxDistance);
			beamEndPos = aimRay.GetPoint(num);
			Vector3 position = this.muzzleTransform.position;
			beamRay = new Ray(position, beamEndPos - position);
		}
	}
}
