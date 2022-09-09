using BepInEx;
using EntityStates.GrandParent;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain
{
	internal partial class RiskierRainPlugin : BaseUnityPlugin
	{
        void ReworkPlanula()
        {
            IL.RoR2.HealthComponent.TakeDamage += RevokePlanulaRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddPlanulaItemBehavior;
			On.RoR2.GrandParentSunController.Start += SunTeamFilter;

			LanguageAPI.Add("ITEM_PARENTEGG_PICKUP", "Burn all nearby enemies after standing still for 2 seconds.");
			LanguageAPI.Add("ITEM_PARENTEGG_DESC", $"After standing still for 2 seconds, begin to " +
				$"burn all nearby enemies for {Tools.ConvertDecimal(PlanulaSunBehavior.burnDuration + 1 / PlanulaSunBehavior.burnInterval)} " +
				$"(+{Tools.ConvertDecimal(1 / PlanulaSunBehavior.burnInterval)} per stack) damage " +
				$"within {PlanulaSunBehavior.burnDistanceBase}m.");
		}

        private void SunTeamFilter(On.RoR2.GrandParentSunController.orig_Start orig, GrandParentSunController self)
        {
			self.bullseyeSearch.teamMaskFilter.RemoveTeam(self.teamFilter.teamIndex);
        }

        private void AddPlanulaItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);

            self.AddItemBehavior<PlanulaSunBehavior>(self.inventory.GetItemCount(RoR2Content.Items.ParentEgg));
        }

        private void RevokePlanulaRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdflda<RoR2.HealthComponent>("itemCounts"),
                x => x.MatchLdfld<RoR2.HealthComponent.ItemCounts>("parentEgg")
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }
    public class PlanulaSunBehavior : RoR2.CharacterBody.ItemBehavior
	{
		public static float burnDistanceBase = 100;
		public static float burnDistanceStack = 0;
		public static float burnInterval = 1f;
		public static float burnDuration = 2f;


		private GameObject sunInstance;

		public Vector3? sunSpawnPosition;

		public static float sunPrefabDiameter = 10f;
		public static float sunPlacementMinDistance = 3f;
		public static float sunPlacementIdealAltitudeBonus = 10f;

		void Start()
        {
			sunPlacementMinDistance += body.radius;
			sunPlacementIdealAltitudeBonus += body.radius;
		}

		private void FixedUpdate()
		{
			if (!NetworkServer.active)
			{
				return;
			}
			int stack = this.stack;
			bool flag = stack > 0 && this.body.notMovingStopwatch >= 2f;
			float radius = burnDistanceBase + (burnDistanceStack * (stack));
			if (this.sunInstance != flag)
			{
				if (flag)
				{
					this.sunSpawnPosition = FindSunSpawnPosition(body.corePosition);

					bool flag2 = this.sunSpawnPosition != null;
					if (flag2)
					{
						this.sunInstance = this.CreateSun(this.sunSpawnPosition.Value);
					}
					RoR2.TeamFilter component = this.sunInstance.GetComponent<RoR2.TeamFilter>();
					component.teamIndex = body.teamComponent.teamIndex;

					RoR2.GrandParentSunController component2 = this.sunInstance.GetComponent<RoR2.GrandParentSunController>();
					component2.burnDuration = burnInterval * stack;
					component2.nearBuffDuration = burnDuration;
					component2.maxDistance = radius;
					component2.minimumStacksBeforeApplyingBurns = 0;
					component2.cycleInterval = burnInterval;

					sunInstance.transform.Find("AreaIndicator").localScale = Vector3.one * radius / 5;
				}
				else
				{
					UnityEngine.Object.Destroy(this.sunInstance);
					this.sunInstance = null;
				}
			}
		}

		private void OnDisable()
		{
			if (this.sunInstance)
			{
				UnityEngine.Object.Destroy(this.sunInstance);
			}
		}

		private GameObject CreateSun(Vector3 sunSpawnPosition)
		{
			GameObject sun = UnityEngine.Object.Instantiate<GameObject>(ChannelSun.sunPrefab, sunSpawnPosition, Quaternion.identity);
			sun.GetComponent<GenericOwnership>().ownerObject = base.gameObject;
			NetworkServer.Spawn(sun);
			return sun;
		}
		public static Vector3? FindSunSpawnPosition(Vector3 searchOrigin)
		{
			Vector3? vector = searchOrigin;
			if (vector != null)
			{
				Vector3 value = vector.Value;
				float num = sunPlacementIdealAltitudeBonus;
				float num2 = sunPrefabDiameter * 0.5f;
				RaycastHit raycastHit;
				if (Physics.Raycast(value, Vector3.up, out raycastHit, ChannelSun.sunPlacementIdealAltitudeBonus + num2, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
				{
					num = Mathf.Clamp(raycastHit.distance - num2, 0f, num);
				}
				value.y += num;
				return new Vector3?(value);
			}
			return null;
		}
	}
}
