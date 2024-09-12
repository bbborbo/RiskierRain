using BepInEx;
using EntityStates.GrandParent;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRain.CoreModules;
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
            IL.RoR2.HealthComponent.TakeDamageProcess += RevokePlanulaRights;
            On.RoR2.CharacterBody.OnInventoryChanged += AddPlanulaItemBehavior;
			On.RoR2.GrandParentSunController.Start += SunTeamFilter;
			On.RoR2.GlobalEventManager.OnCharacterDeath += AddPlanulaCharge;

			LanguageAPI.Add("ITEM_PARENTEGG_PICKUP", "Standing still creates a sun that burns enemies. Kill to charge the sun.");
			LanguageAPI.Add("ITEM_PARENTEGG_DESC", $"Killing enemies grants 1 charge. After standing still for 2 seconds with 3 or more charges, begin to " +
				$"burn all nearby enemies for {Tools.ConvertDecimal(PlanulaSunBehavior.burnDuration + 1 / PlanulaSunBehavior.burnInterval)} " +
				$"(+{Tools.ConvertDecimal(1 / PlanulaSunBehavior.burnInterval)} per stack) damage " +
				$"within {PlanulaSunBehavior.burnDistanceBase}m. Consumes 1 charge every 2 seconds.");
		}

        private void AddPlanulaCharge(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
			CharacterBody attackerBody = damageReport.attackerBody;
			if(attackerBody != null)
            {
				Inventory inv = attackerBody.inventory;
				if(inv != null)
                {
					int itemCount = inv.GetItemCount(RoR2Content.Items.ParentEgg);
					if(itemCount > 0)
					{
						attackerBody.AddBuff(CoreModules.Assets.planulaChargeBuff);
					}
                }
            }
			orig(self, damageReport);
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
		float buffCostStopwatch = 0;
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
			int buffCount = body.GetBuffCount(CoreModules.Assets.planulaChargeBuff);
			bool flag = stack > 0 && this.body.notMovingStopwatch >= 2f;
			float radius = burnDistanceBase + (burnDistanceStack * (stack));

            if (flag && buffCount > 0)
            {
                if (sunInstance)
                {
					buffCostStopwatch += Time.fixedDeltaTime;
					while(buffCostStopwatch > burnInterval)
                    {
						body.RemoveBuff(CoreModules.Assets.planulaChargeBuff);
						buffCostStopwatch -= burnInterval;
                    }
                }
                else if (buffCount > 2)
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
					buffCostStopwatch = 0;
				}
            }
            else
            {
				DestroySun();
			}

			if (this.sunInstance != flag)
			{
				if (flag)
				{
				}
				else
				{
					DestroySun();
				}
			}
		}

		private void OnDisable()
        {
            DestroySun();
        }

        private void DestroySun()
        {
            if (this.sunInstance)
            {
                UnityEngine.Object.Destroy(this.sunInstance);
				this.sunInstance = null;
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
