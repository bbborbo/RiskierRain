using EntityStates;
using EntityStates.Commando.CommandoWeapon;
using RiskierRain.SurvivorTweaks;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.EntityState.Commando
{
    class SoupFire : CommandoBaseSoupState
	{
		public static float force = 600f;
		public static float baseDuration = 0.8f;
		int shotsTotal;
		float durationPerShot;
		public static float damageCoefficient;
		public static GameObject projectilePrefab;
		public static GameObject muzzleflashEffectPrefab;
		public List<HurtBox> targetsList;
		private int fireIndex;
		private float stopwatch;
		bool crit = false;
		public override void OnEnter()
		{
			base.OnEnter();
			//crit = Util.CheckRoll(this.critStat, base.characterBody.master);
			shotsTotal = Mathf.CeilToInt(CommandoTweaks.soupBaseShots * this.attackSpeedStat);
			durationPerShot = SoupFire.baseDuration / shotsTotal;
			FireAtTarget();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();

			if (base.isAuthority)
			{
				this.stopwatch += Time.fixedDeltaTime;
				while (this.stopwatch >= this.durationPerShot)
				{
					this.stopwatch -= this.durationPerShot;
					FireAtTarget();
                    if (fireIndex >= shotsTotal)
					{
						this.outer.SetNextState(new Idle());
						return;
					}
				}
			}
		}

        private void FireAtTarget()
		{
			Ray aimRay = base.GetAimRay();

			if (targetsList.Count > 0)
			{
				HurtBox hurtBox = targetsList[fireIndex % targetsList.Count];
				//HurtBox hurtBox = targetsList[Mathf.FloorToInt(targetsList.Count * fireIndex / shotsTotal)];
				if (hurtBox.healthComponent && hurtBox.healthComponent.alive)
				{
					aimRay.direction = (hurtBox.transform.position - aimRay.origin).normalized;
				}
                else
                {
					targetsList.Remove(hurtBox);
					FireAtTarget();
					return;
                }
			}

			if (fireIndex % 2 == 0)
			{
				this.PlayAnimation("Gesture Additive, Left", "FirePistol, Left");
				this.FireBullet(aimRay, "MuzzleLeft");
			}
			else
			{
				this.PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
				this.FireBullet(aimRay, "MuzzleRight");
			}
			this.fireIndex++;
		}

		private void FireBullet(Ray aimRay, string targetMuzzle)
		{
			if (FirePistol2.muzzleEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(FirePistol2.muzzleEffectPrefab, base.gameObject, targetMuzzle, false);
			}
			base.AddRecoil(-0.4f * FirePistol2.recoilAmplitude, -0.8f * FirePistol2.recoilAmplitude, -0.3f * FirePistol2.recoilAmplitude, 0.3f * FirePistol2.recoilAmplitude);
			StartAimMode(aimRay, 3f, true);
			if (base.isAuthority)
			{
				new BulletAttack
				{
					owner = base.gameObject,
					weapon = base.gameObject,
					origin = aimRay.origin,
					aimVector = aimRay.direction,
					minSpread = 0f,
					maxSpread = base.characterBody.spreadBloomAngle,
					damage = CommandoTweaks.soupDamageCoeff * this.damageStat,
					procCoefficient = CommandoTweaks.soupProcCoeff,
					force = SoupFire.force,
					tracerEffectPrefab = FireBarrage.tracerEffectPrefab,
					muzzleName = targetMuzzle,
					hitEffectPrefab = FireBarrage.hitEffectPrefab,
					isCrit = Util.CheckRoll(this.critStat, base.characterBody.master),
					radius = 0f,
					smartCollision = true,
					damageType = DamageType.Stun1s
				}.Fire();
			}
			base.characterBody.AddSpreadBloom(FireBarrage.spreadBloomValue);
			Util.PlaySound(FireSweepBarrage.fireSoundString, base.gameObject);
		}

		public override void OnExit()
		{
			base.OnExit();
			//base.PlayCrossfade("Gesture, Additive", "ExitHarpoons", 0.1f);
		}
	}
}
