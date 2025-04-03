using System;
using System.Collections.Generic;
using System.Text;
using EntityStates;
using EntityStates.Loader;
using RoR2;
using SwanSongExtended.Skills;
using UnityEngine;

namespace SwanSongExtended.States.Loader
{
    class DynamicPunchJab : LoaderMeleeAttack
    {
		public static float damageCoefficient = 3f;
		public static float procCoefficient = 1f;
		public static float force = 30;
		public static float selfForce = 2000;


		public override void OnEnter()
		{
			
			if (base.isAuthority)
			{
				this.duration = .5f / this.attackSpeedStat;
			}
			base.OnEnter();
		}
		public override string GetHitBoxGroupName()
		{
			return "Punch";
		}
		public override void AuthorityFixedUpdate()
		{
			base.AuthorityFixedUpdate();
		}
		public override void PlayAnimation()
		{
			base.PlayAnimation();
			base.PlayAnimation("FullBody, Override", BaseSwingChargedFist.ChargePunchStateHash, BaseSwingChargedFist.ChargePunchParamHash, .5f);//duration
		}
		public override void AuthorityModifyOverlapAttack(OverlapAttack overlapAttack)
		{
			base.AuthorityModifyOverlapAttack(overlapAttack);
			if(overlapAttack == null)
            {
				Debug.Log("overlapattack = null owmp");
				return;
            }
			overlapAttack.damage = damageCoefficient * this.damageStat;
			overlapAttack.forceVector = base.GetAimRay().direction * 1;
			overlapAttack.damageType = DamageType.Stun1s;
			overlapAttack.damageType.damageSource = DamageSource.Utility;
			
		}
		public override void OnMeleeHitAuthority()
		{
			
			for (int i = 0; i < base.hitResults.Count; i++)
			{
				//appply armor break
				CharacterBody body = base.hitResults[i].healthComponent.body;
				body.AddTimedBuffAuthority(DynamicPunchSkill.loaderArmorBreak.buffIndex, 6);
				//apply force to heavy fuckers
				float num = 1f;
				if (body.characterMotor)
				{
					num = body.characterMotor.mass;
				}
				else if (body.healthComponent.GetComponent<Rigidbody>())
				{
					num = base.rigidbody.mass;
				}
				body.healthComponent.TakeDamageForce(base.GetAimRay().direction * force * num, false, true);
			}
			
			base.OnMeleeHitAuthority();
		}
        public override void OnExit()
        {
            base.OnExit();
            
        }
		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}

		private static int ChargePunchStateHash = Animator.StringToHash("ChargePunch");
		private static int ChargePunchParamHash = Animator.StringToHash("ChargePunch.playbackRate");
	}
}
