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

namespace RiskierRainContent.EntityState.Captain
{
    class ThermalCannonPrep : BaseSkillState
	{
		public GameObject fireEffectPrefab = new FireSonicBoom().fireEffectPrefab;
		public string sound;
		public string muzzle;

		private float duration;
		public float baseDuration = 0.7f;

		public override void OnEnter()
		{
			base.OnEnter();
			this.duration = this.baseDuration / this.attackSpeedStat;
			base.PlayCrossfade("Gesture, Override", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.duration, 0.1f);
			base.PlayCrossfade("Gesture, Additive", "ChargeCaptainShotgun", "ChargeCaptainShotgun.playbackRate", this.duration, 0.1f);
			Util.PlaySound(Flamethrower.startAttackSoundString, base.gameObject);
		}
		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (fixedAge >= duration)
			{
				ThermalCannonFire state = new ThermalCannonFire();
				state.activatorSkillSlot = this.activatorSkillSlot;
				this.outer.SetNextState(state);
			}
		}

		public override InterruptPriority GetMinimumInterruptPriority()
		{
			return InterruptPriority.PrioritySkill;
		}
	}
}
