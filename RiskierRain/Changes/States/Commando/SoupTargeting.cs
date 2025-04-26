using EntityStates.Engi.EngiMissilePainter;
using RiskierRain.SurvivorTweaks;
using RoR2;
using RoR2.Skills;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.States.Commando
{
    public class SoupTargeting : CommandoBaseSoupState
	{
		public static float stackInterval = 0.125f;
		public static GameObject crosshairOverridePrefab = Paint.crosshairOverridePrefab;
		public static GameObject stickyTargetIndicatorPrefab = Paint.stickyTargetIndicatorPrefab;
		public static string enterSoundString = Paint.enterSoundString;
		public static string exitSoundString = Paint.exitSoundString;
		public static string loopSoundString = Paint.loopSoundString;
		public static string lockOnSoundString = Paint.lockOnSoundString;
		public static string stopLoopSoundString = Paint.stopLoopSoundString;
		public static float maxAngle = Paint.maxAngle;
		public static float maxDistance = Paint.maxDistance;

		private List<HurtBox> targetsList;
		private Dictionary<HurtBox, SoupTargeting.IndicatorInfo> targetIndicators;
		private Indicator stickyTargetIndicator;

		private SkillDef confirmTargetDummySkillDef;
		private SkillDef cancelTargetingDummySkillDef;

		private bool releasedKeyOnce;
		private float stackStopwatch;
		private CrosshairUtils.OverrideRequest crosshairOverrideRequest;
		private BullseyeSearch search;
		private bool queuedFiringState;
		private uint loopSoundID;
		private HealthComponent previousHighlightTargetHealthComponent;
		private HurtBox previousHighlightTargetHurtBox;

		public override void OnEnter()
		{
			base.OnEnter();

			if (base.isAuthority)
			{
				//initialize targeting on authority
				this.targetsList = new List<HurtBox>();
				this.targetIndicators = new Dictionary<HurtBox, SoupTargeting.IndicatorInfo>();
				this.stickyTargetIndicator = new Indicator(base.gameObject, SoupTargeting.stickyTargetIndicatorPrefab);
				this.search = new BullseyeSearch();
			}

			//play animations/sounds
			base.PlayCrossfade("Gesture, Additive", "PrepHarpoons", 0.1f);
			Util.PlaySound(SoupTargeting.enterSoundString, base.gameObject);
			this.loopSoundID = Util.PlaySound(SoupTargeting.loopSoundString, base.gameObject);

			//set crosshair
			if (SoupTargeting.crosshairOverridePrefab)
			{
				this.crosshairOverrideRequest = CrosshairUtils.RequestOverrideForBody(base.characterBody, SoupTargeting.crosshairOverridePrefab, CrosshairUtils.OverridePriority.Skill);
			}

			//set skill overrides (these skills dont have an activation state, they just stop the original skills from being used temporarily)
			this.confirmTargetDummySkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("EngiConfirmTargetDummy"));
			this.cancelTargetingDummySkillDef = SkillCatalog.GetSkillDef(SkillCatalog.FindSkillIndexByName("EngiCancelTargetingDummy"));
			base.skillLocator.primary.SetSkillOverride(this, this.confirmTargetDummySkillDef, GenericSkill.SkillOverridePriority.Contextual);
			base.skillLocator.secondary.SetSkillOverride(this, this.cancelTargetingDummySkillDef, GenericSkill.SkillOverridePriority.Contextual);
		}

		public override void OnExit()
		{
			if (base.isAuthority && !this.outer.destroying && !this.queuedFiringState)
			{
				base.activatorSkillSlot.AddOneStock();
				for (int i = 0; i < this.targetsList.Count; i++)
				{
					//refund stock used to be in here for engi harpoons
				}
			}
			//unset skill overrides
			base.skillLocator.secondary.UnsetSkillOverride(this, this.cancelTargetingDummySkillDef, GenericSkill.SkillOverridePriority.Contextual);
			base.skillLocator.primary.UnsetSkillOverride(this, this.confirmTargetDummySkillDef, GenericSkill.SkillOverridePriority.Contextual);

			//disable target indicators
			if (this.targetIndicators != null)
			{
				foreach (KeyValuePair<HurtBox, SoupTargeting.IndicatorInfo> keyValuePair in this.targetIndicators)
				{
					keyValuePair.Value.indicator.active = false;
				}
			}
			if (this.stickyTargetIndicator != null)
			{
				this.stickyTargetIndicator.active = false;
			}

			//disable crosshair override
			CrosshairUtils.OverrideRequest overrideRequest = this.crosshairOverrideRequest;
			if (overrideRequest != null)
			{
				overrideRequest.Dispose();
			}

			//play sounds/aniamtions
			base.PlayCrossfade("Gesture, Additive", "ExitHarpoons", 0.1f);
			Util.PlaySound(SoupTargeting.exitSoundString, base.gameObject);
			Util.PlaySound(SoupTargeting.stopLoopSoundString, base.gameObject);
			base.OnExit();
		}

		private void AddTargetAuthority(HurtBox hurtBox)
		{
			//if an enemy is already targeted, dont re-add them (this is unique from thermal harpoons targeting)
			if (this.targetIndicators.TryGetValue(hurtBox, out _))
			{
				return;
			}

			//create new indicator info
			SoupTargeting.IndicatorInfo indicatorInfo = new SoupTargeting.IndicatorInfo
			{
				indicator = new SoupTargeting.CommandoSoupIndicator(base.gameObject, LegacyResourcesAPI.Load<GameObject>("Prefabs/EngiMissileTrackingIndicator"))
			};
			indicatorInfo.indicator.targetTransform = hurtBox.transform;
			indicatorInfo.indicator.active = true;
			Util.PlaySound(SoupTargeting.lockOnSoundString, base.gameObject);

			this.targetIndicators[hurtBox] = indicatorInfo;
			this.targetsList.Add(hurtBox);
			//base.activatorSkillSlot.DeductStock(1);
		}

		private void RemoveTargetAtAuthority(int i)
		{
			HurtBox key = this.targetsList[i];
			this.targetsList.RemoveAt(i);
			SoupTargeting.IndicatorInfo indicatorInfo;
			if (this.targetIndicators.TryGetValue(key, out indicatorInfo))
			{
				this.targetIndicators[key] = indicatorInfo;
				indicatorInfo.indicator.active = false;
				this.targetIndicators.Remove(key);
			}
		}

		//this gets called from fixedupdate - basically constantly maintaining the target list for invalid targets
		private void CleanTargetsList()
		{
			//clean invalid targets
			for (int i = this.targetsList.Count - 1; i >= 0; i--)
			{
				HurtBox hurtBox = this.targetsList[i];
				if (!hurtBox.healthComponent || !hurtBox.healthComponent.alive)
				{
					this.RemoveTargetAtAuthority(i);
				}
			}
			//clean targets past target limit
			for (int j = this.targetsList.Count - 1; j >= CommandoTweaks.soupMaxTargets; j--)
			{
				this.RemoveTargetAtAuthority(j);
			}
		}

		public override void FixedUpdate()
		{
			base.FixedUpdate();
			base.characterBody.SetAimTimer(3f);
			if (base.isAuthority)
			{
				this.AuthorityFixedUpdate();
			}
		}

		private void GetCurrentTargetInfo(out HurtBox currentTargetHurtBox, out HealthComponent currentTargetHealthComponent)
		{
			Ray aimRay = base.GetAimRay();
			this.search.filterByDistinctEntity = true;
			this.search.filterByLoS = true;
			this.search.minDistanceFilter = 0f;
			this.search.maxDistanceFilter = SoupTargeting.maxDistance;
			this.search.minAngleFilter = 0f;
			this.search.maxAngleFilter = SoupTargeting.maxAngle;
			this.search.viewer = base.characterBody;
			this.search.searchOrigin = aimRay.origin;
			this.search.searchDirection = aimRay.direction;
			this.search.sortMode = BullseyeSearch.SortMode.DistanceAndAngle;
			this.search.teamMaskFilter = TeamMask.GetUnprotectedTeams(base.GetTeam());
			this.search.RefreshCandidates();
			this.search.FilterOutGameObject(base.gameObject);
			foreach (HurtBox hurtBox in this.search.GetResults())
			{
				if (hurtBox.healthComponent && hurtBox.healthComponent.alive)
				{
					currentTargetHurtBox = hurtBox;
					currentTargetHealthComponent = hurtBox.healthComponent;
					return;
				}
			}
			currentTargetHurtBox = null;
			currentTargetHealthComponent = null;
		}

		private void AuthorityFixedUpdate()
		{
			//remove invalid targets
			this.CleanTargetsList();
			bool startFireMode = false;
			HurtBox hurtBox;
			HealthComponent y;
			this.GetCurrentTargetInfo(out hurtBox, out y);

			//while a hurtbox is targeted
			if (hurtBox)
			{
				this.stackStopwatch += Time.fixedDeltaTime;

				//if primary is being held down, and the stack timer is big enough, or primary was just pressed, add the target
				if (base.inputBank.skill1.down 
					&& (y != previousHighlightTargetHealthComponent
					|| this.stackStopwatch >= SoupTargeting.stackInterval / this.attackSpeedStat 
					|| base.inputBank.skill1.justPressed))
				{
					this.stackStopwatch = 0f;
					this.AddTargetAuthority(hurtBox);
				}
			}
			//release primary to start firing
			if (base.inputBank.skill1.justReleased)
			{
				startFireMode = true;
			}
			//cancel target mode immediately - not setting targetModeEnding means it will clear all targets and refund stock
			if (base.inputBank.skill2.justReleased)
			{
				this.outer.SetNextStateToMain();
				return;
			}

			//press special again to start firing
			if (base.inputBank.skill4.justReleased)
			{
				if (this.releasedKeyOnce)
				{
					startFireMode = true;
				}
				this.releasedKeyOnce = true;
			}
			if (hurtBox != this.previousHighlightTargetHurtBox)
			{
				this.previousHighlightTargetHurtBox = hurtBox;
				this.previousHighlightTargetHealthComponent = y;
				this.stickyTargetIndicator.targetTransform = ((hurtBox && base.activatorSkillSlot.stock != 0) ? hurtBox.transform : null);
				this.stackStopwatch = 0f;
			}
			this.stickyTargetIndicator.active = this.stickyTargetIndicator.targetTransform;

			//queue firing state if the target mode is ending
			if (startFireMode)
			{
				//if no targets, cancel the state instead
				if (targetsList.Count == 0)
				{
					this.outer.SetNextStateToMain();
					return;
				}
				this.queuedFiringState = true;
				this.outer.SetNextState(new SoupFire
				{
					targetsList = this.targetsList,
					activatorSkillSlot = base.activatorSkillSlot
				});
			}
		}

		private struct IndicatorInfo
		{
			public SoupTargeting.CommandoSoupIndicator indicator;
		}

		private class CommandoSoupIndicator : Indicator
		{
			public override void UpdateVisualizer()
			{
				base.UpdateVisualizer();
			}

			public CommandoSoupIndicator(GameObject owner, GameObject visualizerPrefab) : base(owner, visualizerPrefab)
			{
			}
		}
	}
}
