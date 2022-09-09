using EntityStates;
using EntityStates.Captain.Weapon;
using EntityStates.Mage.Weapon;
using EntityStates.Treebot.Weapon;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.EntityState.Captain
{
    class ThermalCannonFire : BaseSkillState
	{
		public GameObject fireEffectPrefab = new FireSonicBoom().fireEffectPrefab;
		public string sound;
		public string muzzle;

		public static float damageCoefficient = 2f;
		public static float procCoefficient = 0.75f;
		public static float burnDuration = 5;
		public float backupDistance = 3;
		public float maxDistance = 40;

		public float baseDuration = 0.5f;
		private float duration;

		public float fieldOfView = new FireSonicBoom().fieldOfView;
		public float idealDistanceToPlaceTargets = new FireSonicBoom().idealDistanceToPlaceTargets;
		public float liftVelocity = new FireSonicBoom().liftVelocity / 5;
		public float groundKnockbackDistance = new FireSonicBoom().groundKnockbackDistance / 5;
		public float airKnockbackDistance = new FireSonicBoom().airKnockbackDistance / 7;
		public static AnimationCurve shoveSuitabilityCurve = FireSonicBoom.shoveSuitabilityCurve;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			Util.PlaySound(Flamethrower.endAttackSoundString, base.gameObject);
			DoSonicBoom();
		}

		public void DoSonicBoom()
		{
			base.AddRecoil(-1f * FireTazer.recoilAmplitude, -1.5f * FireTazer.recoilAmplitude, -0.25f * FireTazer.recoilAmplitude, 0.25f * FireTazer.recoilAmplitude);
			base.characterBody.AddSpreadBloom(FireTazer.bloom);
			base.PlayAnimation("Gesture, Additive", "FireCaptainShotgun");
			base.PlayAnimation("Gesture, Override", "FireCaptainShotgun");
			Util.PlaySound(this.sound, base.gameObject);

			Ray aimRay = base.GetAimRay();
			if (!string.IsNullOrEmpty(this.muzzle))
			{
				EffectManager.SimpleMuzzleFlash(this.fireEffectPrefab, base.gameObject, this.muzzle, false);
			}
			else
			{
				EffectManager.SpawnEffect(this.fireEffectPrefab, new EffectData
				{
					origin = aimRay.origin,
					rotation = Quaternion.LookRotation(aimRay.direction)
				}, false);
			}
			aimRay.origin -= aimRay.direction * this.backupDistance;
			if (NetworkServer.active)
			{
				BullseyeSearch bullseyeSearch = new BullseyeSearch();
				bullseyeSearch.teamMaskFilter = TeamMask.all;
				bullseyeSearch.maxAngleFilter = this.fieldOfView * 0.3f;
				bullseyeSearch.maxDistanceFilter = this.maxDistance + backupDistance;
				bullseyeSearch.searchOrigin = aimRay.origin;
				bullseyeSearch.searchDirection = aimRay.direction;
				bullseyeSearch.sortMode = BullseyeSearch.SortMode.Distance;
				bullseyeSearch.filterByLoS = false;
				bullseyeSearch.RefreshCandidates();
				bullseyeSearch.FilterOutGameObject(base.gameObject);
				IEnumerable<HurtBox> enumerable = bullseyeSearch.GetResults().Where(new Func<HurtBox, bool>(Util.IsValid)).Distinct(default(HurtBox.EntityEqualityComparer));
				TeamIndex team = base.GetTeam();
				foreach (HurtBox hurtBox in enumerable)
				{
					if (FriendlyFireManager.ShouldSplashHitProceed(hurtBox.healthComponent, team))
					{
						Vector3 vector = hurtBox.transform.position - aimRay.origin;
						float magnitude = vector.magnitude;
						float magnitude2 = new Vector2(vector.x, vector.z).magnitude;
						Vector3 vector2 = vector / magnitude;
						float num = 1f;
						CharacterBody body = hurtBox.healthComponent.body;
						if (body.characterMotor)
						{
							num = body.characterMotor.mass;
						}
						else if (hurtBox.healthComponent.GetComponent<Rigidbody>())
						{
							num = base.rigidbody.mass;
						}
						float num2 = FireSonicBoom.shoveSuitabilityCurve.Evaluate(num);
						this.AddDebuff(body);
						body.RecalculateStats();
						float acceleration = body.acceleration;
						Vector3 a = vector2;
						float d = Trajectory.CalculateInitialYSpeedForHeight(
							Mathf.Abs(this.idealDistanceToPlaceTargets - magnitude), -acceleration) * Mathf.Sign(this.idealDistanceToPlaceTargets - magnitude);
						a *= d;
						a.y = this.liftVelocity;

						DamageInfo damageInfo = new DamageInfo
						{
							attacker = base.gameObject,
							damage = this.CalculateDamage(),
							position = hurtBox.transform.position,
							procCoefficient = this.CalculateProcCoefficient()
						};
						hurtBox.healthComponent.TakeDamageForce(a * (num * num2) / 2, true, true);
						hurtBox.healthComponent.TakeDamage(damageInfo);
						GlobalEventManager.instance.OnHitEnemy(damageInfo, hurtBox.healthComponent.gameObject);
					}
				}
			}
			if (base.isAuthority && base.characterBody && base.characterBody.characterMotor)
			{
				float height = base.characterBody.characterMotor.isGrounded ? this.groundKnockbackDistance : this.airKnockbackDistance;
				float num3 = base.characterBody.characterMotor ? base.characterBody.characterMotor.mass : 1f;
				float acceleration2 = base.characterBody.acceleration;
				float num4 = Trajectory.CalculateInitialYSpeedForHeight(height, -acceleration2);
				base.characterBody.characterMotor.ApplyForce(-num4 * num3 * aimRay.direction, false, false);
			}
		}

		protected virtual void AddDebuff(CharacterBody body)
		{
			InflictDotInfo inflictDotInfo = new InflictDotInfo
			{
				attackerObject = body.gameObject,
				victimObject = base.characterBody.gameObject,
				totalDamage = new float?(burnDuration),
				damageMultiplier = 1f,
				dotIndex = DotController.DotIndex.Burn
			};
			StrengthenBurnUtils.CheckDotForUpgrade(base.characterBody.inventory, ref inflictDotInfo);
			DotController.InflictDot(ref inflictDotInfo);

			SetStateOnHurt component = body.healthComponent.GetComponent<SetStateOnHurt>();
			if (component == null)
			{
				return;
			}
			component.SetStun(-1f);
		}

		protected virtual float CalculateDamage()
		{
			return damageStat * damageCoefficient;
		}

		protected virtual float CalculateProcCoefficient()
		{
			return procCoefficient;
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (fixedAge >= duration)
			{
                if (IsKeyDownAuthority() && activatorSkillSlot.stock > 0 && !characterBody.isSprinting)
				{
					ThermalCannonFire state = new ThermalCannonFire();
					state.activatorSkillSlot = this.activatorSkillSlot;
					this.outer.SetNextState(state);

					this.activatorSkillSlot.stock -= 1;
					this.activatorSkillSlot.rechargeStopwatch = 0f;
				}
                else
                {
					this.outer.SetNextStateToMain();
				}
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.Skill;
		}
	}
}
