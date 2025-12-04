using BepInEx;
using EntityStates.MoonElevator;
using SwanSongExtended.Items;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Components;

namespace SwanSongExtended
{
	public partial class SwanSongPlugin
	{
		public static float pillarDropOffset = 2.5f;
		public static float pillarDropForce = 20f;
		public static int baseRewardCount = 1;
		public static bool scaleRewardsByPlayerCount = true;

		void MakePillarsFun()
		{
			//On.RoR2.MoonBatteryMissionController.OnBatteryCharged += PillarsDropItems;
			On.RoR2.MoonBatteryMissionController.Awake += ReduceRequiredPillars;

			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryBlood_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Blood);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryDesign_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Design);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryMass_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Mass);
			Addressables.LoadAssetAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatterySoul_prefab).Completed 
				+= (ctx) => AddPillarItemDrop(ctx.Result, PillarItemDropper.PillarType.Soul);
			void AddPillarItemDrop(GameObject pillarPrefab, PillarItemDropper.PillarType pillarType)
			{
				PillarItemDropper dropper = pillarPrefab.AddComponent<PillarItemDropper>();
				dropper.pillarType = pillarType;
			}
		}

		private void ReduceRequiredPillars(On.RoR2.MoonBatteryMissionController.orig_Awake orig, MoonBatteryMissionController self)
		{
			orig(self);
			self._numRequiredBatteries = 2;
			foreach(GameObject pillarObject in self.moonBatteries)
			{
				PillarItemDropper dropper = pillarObject.GetComponent<PillarItemDropper>();
                if (!dropper)
				{
					dropper = pillarObject.AddComponent<PillarItemDropper>();

					PillarItemDropper.PillarType pickup = PillarItemDropper.PillarType.None;
					string fullName = pillarObject.name;
					string pillarType = fullName.Substring(11, 4);
					switch (pillarType)
					{
						default:
							break;
						case "Mass":
							pickup = PillarItemDropper.PillarType.Mass;
							break;
						case "Desi":
							pickup = PillarItemDropper.PillarType.Design;
							break;
						case "Bloo":
							pickup = PillarItemDropper.PillarType.Blood;
							break;
						case "Soul":
							pickup = PillarItemDropper.PillarType.Soul;
							break;
					}

					dropper.pillarType = pickup;
				}

				dropper.shouldDropItem = true;
            }
		}

		private void PillarsDropItems(On.RoR2.MoonBatteryMissionController.orig_OnBatteryCharged orig, RoR2.MoonBatteryMissionController self, RoR2.HoldoutZoneController holdoutZone)
		{
			//Debug.Log("A");
			int participatingPlayerCount = Run.instance.participatingPlayerCount;

			if (participatingPlayerCount != 0)
            {
				//Debug.Log("B");
				DropPillarItemFromHoldout(holdoutZone, participatingPlayerCount);
            }

            self.Network_numChargedBatteries = self._numChargedBatteries + 1;
			if (self._numChargedBatteries >= self._numRequiredBatteries && NetworkServer.active)
			{
				for (int i = 0; i < self.batteryHoldoutZones.Length; i++)
				{
					if (self.batteryHoldoutZones[i].enabled)
					{
						self.batteryHoldoutZones[i].FullyChargeHoldoutZone();
						self.batteryHoldoutZones[i].onCharged.RemoveListener(new UnityAction<HoldoutZoneController>(self.OnBatteryCharged));
					}
				}
				/*self.batteryHoldoutZones = new HoldoutZoneController[0];
				for (int j = 0; j < self.batteryStateMachines.Length; j++)
				{
					if (!(self.batteryStateMachines[j].state is MoonBatteryComplete))
					{
						self.batteryStateMachines[j].SetNextState(new MoonBatteryDisabled());
					}
				}*/
				for (int k = 0; k < self.elevatorStateMachines.Length; k++)
				{
					self.elevatorStateMachines[k].SetNextState(new InactiveToReady());
				}
			}
		}
		public static void DropPillarItemFromHoldout(HoldoutZoneController holdoutZone)
		{
			Log.Warning(holdoutZone.gameObject.name + "3");
			DropPillarItemFromHoldout(holdoutZone, Run.instance ? Run.instance.participatingPlayerCount : 0);
        }

        public static void DropPillarItemFromHoldout(HoldoutZoneController holdoutZone, int participatingPlayerCount)
		{
			Log.Warning(holdoutZone.gameObject.name + "4");
			if (participatingPlayerCount == 0)
				return;
			Vector3 dropPosition = holdoutZone.gameObject.transform.position + Vector3.up * pillarDropOffset;
			PickupIndex pickupIndex = GetPickupIndexFromPillarType(holdoutZone.gameObject);

            if (pickupIndex != PickupIndex.none)
			{
				Log.Warning(holdoutZone.gameObject.name + "5");
				int num = baseRewardCount;
                if (scaleRewardsByPlayerCount)
                {
                    num *= participatingPlayerCount;
                }

                float angle = 360f / (float)num;
                Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up)
                    * (Vector3.up * pillarDropForce + Vector3.forward * 5f);
                Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
                int i = 0;
                while (i < num)
				{
					Log.Warning(holdoutZone.gameObject.name + "6");
					PickupDropletController.CreatePickupDroplet(pickupIndex, dropPosition, vector);
                    i++;
                    vector = rotation * vector;
                }
            }
        }

        public static PickupIndex GetPickupIndexFromPillarType(GameObject pillar)
		{
			ItemBase pickup = null;
			string fullName = pillar.name;
			string pillarType = fullName.Substring(11, 4);
			switch (pillarType)
			{
				default:
					break;
				case "Mass":
					pickup = (MassAnomaly.instance);
					break;
				case "Desi":
					pickup = (DesignAnomaly.instance);
					break;
				case "Bloo":
					pickup = (BloodAnomaly.instance);
					break;
				case "Soul":
					pickup = (SoulAnomaly.instance);
					break;
			}
			if(pickup != null)
			{
				ItemIndex itemsIndex = pickup.ItemsDef.itemIndex;
				return PickupCatalog.FindPickupIndex(itemsIndex);
			}
			Debug.Log("No pickup index found!");
			return PickupIndex.none;
		}
	}
}
