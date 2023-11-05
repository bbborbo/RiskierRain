using RiskierRain.Components;
using RiskierRain.Equipment;
using EntityStates;
using EntityStates.TeleporterHealNovaController;
using RoR2;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.States.LeechingHealNovaController
{
    public class LeechingHealNovaPulse : BaseState
	{
		//public static AnimationCurve novaRadiusCurve;
		public static float duration = 1f;
		private Transform effectTransform;
		private LeechingHealNovaPulse.HealPulse healPulse;
		private float radius;

		public static float baseRadius = 25f;
		public static float baseHealFraction = 0.25f; // based on the receiver's max health
		public static float maxHealFraction = 1.5f; // based on the source's max health

		internal LeechingHealingPulseComponent leechingHealingPulseComponent;

		public override void OnEnter()
		{
			base.OnEnter();
			this.radius = baseRadius + leechingHealingPulseComponent.bodyRadius;

			TeamFilter component = base.GetComponent<TeamFilter>();
			TeamIndex teamIndex = component ? component.teamIndex : TeamIndex.None;
			if (NetworkServer.active)
			{
				float maxHeal = leechingHealingPulseComponent.maxHealth * maxHealFraction;

				this.healPulse = new HealPulse(base.transform.position,
					this.radius, baseHealFraction, maxHeal, 
					leechingHealingPulseComponent.procCoefficient, duration, teamIndex);
			}
			this.effectTransform = base.transform.Find("PulseEffect");
			if (this.effectTransform)
			{
				this.effectTransform.gameObject.SetActive(true);
			}
		}

		public override void OnExit()
		{
			if (this.effectTransform)
			{
				this.effectTransform.gameObject.SetActive(false);
			}
			base.OnExit();
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			if (NetworkServer.active)
			{
				this.healPulse.Update(Time.fixedDeltaTime);
				if (duration < base.fixedAge)
				{
					Destroy(this.outer.gameObject);
				}
			}
		}

		public override void Update()
		{
			if (this.effectTransform)
			{
				float num = baseRadius * TeleporterHealNovaPulse.novaRadiusCurve.Evaluate(base.fixedAge / LeechingHealNovaPulse.duration);
				this.effectTransform.localScale = new Vector3(num, num, num);
			}
		}

		private class HealPulse
		{
			private readonly List<HealthComponent> healedTargets = new List<HealthComponent>();
			private readonly SphereSearch sphereSearch;
			private float rate;
			private float t;
			private float finalRadius;
			private TeamMask teamMask;
			private readonly List<HurtBox> hurtBoxesList = new List<HurtBox>();

			private float baseHealFraction; //heal fraction
			private float healMax;
			private float procCoeff;

			public HealPulse(Vector3 origin, float finalRadius, float baseHeal, float healMax, float procCoeff, float duration, TeamIndex teamIndex)
			{
				this.sphereSearch = new SphereSearch
				{
					mask = LayerIndex.entityPrecise.mask,
					origin = origin,
					queryTriggerInteraction = QueryTriggerInteraction.Collide,
					radius = 0f
				};
				this.finalRadius = finalRadius;
				this.rate = 1f / duration;
				this.teamMask = default(TeamMask);
				this.teamMask.AddTeam(teamIndex);

				this.baseHealFraction = baseHeal;
				this.healMax = healMax;
				this.procCoeff = procCoeff;
			}
			public bool isFinished
			{
				get
				{
					return this.t >= 1f;
				}
			}

			public void Update(float deltaTime)
			{
				this.t += this.rate * deltaTime;
				this.t = ((this.t > 1f) ? 1f : this.t);
				this.sphereSearch.radius = this.finalRadius * TeleporterHealNovaPulse.novaRadiusCurve.Evaluate(this.t);
				this.sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(this.teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(this.hurtBoxesList);
				int i = 0;
				int count = this.hurtBoxesList.Count;
				while (i < count)
				{
					HealthComponent healthComponent = this.hurtBoxesList[i].healthComponent;
					if (!this.healedTargets.Contains(healthComponent))
					{
						this.healedTargets.Add(healthComponent);
                        if (!LeechingAspect.instance.IsElite(healthComponent.body))
						{
							this.HealTarget(healthComponent);
						}
					}
					i++;
				}
				this.hurtBoxesList.Clear();
			}

			private void HealTarget(HealthComponent target)
			{
				float baseHeal = baseHealFraction * target.fullHealth;
				float endHeal = Mathf.Min(baseHeal, healMax) * procCoeff;

				target.Heal(endHeal, default(ProcChainMask));
				Util.PlaySound("Play_item_proc_TPhealingNova_hitPlayer", target.gameObject);
			}
		}
	}
}
