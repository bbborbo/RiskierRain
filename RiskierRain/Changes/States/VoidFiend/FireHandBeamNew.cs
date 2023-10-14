using EntityStates;
using EntityStates.VoidJailer.Weapon;
using EntityStates.VoidSurvivor.Weapon;
using RiskierRain.SurvivorTweaks;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.EntityState.VoidFiend
{
    class FireHandBeamNew : ChargeHandBeam
	{
		private GameObject chargeEffectInstance;
		private GameObject chargeEffectPrefab = ChargeFire.chargeVfxPrefab;

		public static float fireBeamDuration = 0.2f;
		public static float maxBaseDuration = 1.2f;
		public static float minBaseDuration = 0.35f;
		float minDuration = 0.2f;
        public override void OnEnter()
		{
			baseDuration = maxBaseDuration - fireBeamDuration;
			minDuration = (minBaseDuration - fireBeamDuration) / characterBody.attackSpeed;
            base.OnEnter();
			Transform modelTransform = base.GetModelTransform();
			if (modelTransform)
			{
				ChildLocator component = modelTransform.GetComponent<ChildLocator>();
				if (component)
				{
					Transform transform = component.FindChild(this.muzzle);
					if (transform && this.chargeEffectPrefab)
					{
						this.chargeEffectInstance = UnityEngine.Object.Instantiate<GameObject>(this.chargeEffectPrefab, transform.position, transform.rotation);
						this.chargeEffectInstance.transform.parent = transform;
						ScaleParticleSystemDuration component2 = this.chargeEffectInstance.GetComponent<ScaleParticleSystemDuration>();
						if (component2)
						{
							component2.newDuration = this.duration;
						}
					}
				}
			}
		}
        public override void FixedUpdate()
		{
			this.fixedAge += Time.fixedDeltaTime;
			if (base.isAuthority)
			{
				if (base.fixedAge > duration)
				{
					FireHandBeam nextState = new FireHandBeam();
					nextState.damageCoefficient = ViendTweaks.primaryChargedDamage;
					nextState.baseDuration = fireBeamDuration;
					this.outer.SetNextState(nextState);
					return;
				}
				if (base.fixedAge > minDuration && !inputBank.skill1.down)
				{
					FireHandBeam nextState = new FireHandBeam();
					nextState.damageCoefficient = ViendTweaks.primaryUnchargedDamage;
					nextState.baseDuration = fireBeamDuration;
					nextState.bulletRadius *= 2f;
					this.outer.SetNextState(nextState);
				}
			}
		}

        public override void OnExit()
        {
            base.OnExit();
			EntityStates.EntityState.Destroy(this.chargeEffectInstance);
		}
    }
}
