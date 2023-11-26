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
		Ray aimRay;
		public override void OnEnter()
		{
			base.OnEnter();
			//crit = Util.CheckRoll(this.critStat, base.characterBody.master);
			shotsTotal = Mathf.CeilToInt(CommandoTweaks.soupBaseShots * this.attackSpeedStat);
			durationPerShot = SoupFire.baseDuration / shotsTotal;
			aimRay = base.GetAimRay();
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
				if (targetsList.Count == 0)
				{
					this.outer.SetNextState(new Idle());
				}
			}
		}

        private void FireAtTarget()
        {
			if(targetsList.Count > 0)
			{
				HurtBox hurtBox = targetsList[fireIndex % targetsList.Count];
				if (hurtBox.healthComponent && hurtBox.healthComponent.alive)
				{
					if (fireIndex % 2 == 0)
					{
						this.PlayAnimation("Gesture Additive, Left", "FirePistol, Left");
						this.FireBullet(hurtBox, "MuzzleLeft");
					}
					else
					{
						this.PlayAnimation("Gesture Additive, Right", "FirePistol, Right");
						this.FireBullet(hurtBox, "MuzzleRight");
					}
				}
                else
                {
					targetsList.RemoveAt(fireIndex % targetsList.Count);
					FireAtTarget();
					return;
                }
			}
            this.fireIndex++;
        }

        private void FireBullet(HurtBox target, string targetMuzzle)
		{
			if (FirePistol2.muzzleEffectPrefab)
			{
				EffectManager.SimpleMuzzleFlash(FirePistol2.muzzleEffectPrefab, base.gameObject, targetMuzzle, false);
			}
			base.AddRecoil(-0.4f * FirePistol2.recoilAmplitude, -0.8f * FirePistol2.recoilAmplitude, -0.3f * FirePistol2.recoilAmplitude, 0.3f * FirePistol2.recoilAmplitude);
			aimRay.direction = (target.transform.position - aimRay.origin).normalized;
			StartAimMode(this.aimRay, 3f, true);
			if (base.isAuthority)
			{
				new BulletAttack
				{
					owner = base.gameObject,
					weapon = base.gameObject,
					origin = this.aimRay.origin,
					aimVector = aimRay.direction,
					minSpread = 0f,
					maxSpread = base.characterBody.spreadBloomAngle,
					damage = CommandoTweaks.soupDamageCoeff * this.damageStat,
					procCoefficient = 1.0f,
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
			aimRay.direction = base.inputBank.aimDirection;
			StartAimMode(this.aimRay, 3f, true);
			//base.PlayCrossfade("Gesture, Additive", "ExitHarpoons", 0.1f);
		}
	}
}
