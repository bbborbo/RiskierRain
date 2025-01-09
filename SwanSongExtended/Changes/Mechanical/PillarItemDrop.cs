using BepInEx;
using EntityStates.MoonElevator;
using SwanSongExtended.Items;
using RoR2;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Networking;

namespace SwanSongExtended
{
	public partial class SwanSongPlugin
	{
		public static GameObject massPillar = Resources.Load<GameObject>("prefabs/networkedobjects/MoonBatteryMass");
		public static GameObject designPillar = Resources.Load<GameObject>("prefabs/networkedobjects/MoonBatteryDesign");
		public static GameObject bloodPillar = Resources.Load<GameObject>("prefabs/networkedobjects/MoonBatteryBlood");
		public static GameObject soulPillar = Resources.Load<GameObject>("prefabs/networkedobjects/MoonBatterySoul");

		public static float pillarDropOffset = 2.5f;
		public static float pillarDropForce = 20f;
		public static int baseRewardCount = 1;
		public static bool scaleRewardsByPlayerCount = true;


		void MakePillarsFun()
		{
			On.RoR2.MoonBatteryMissionController.OnBatteryCharged += PillarsDropItems;
			On.RoR2.MoonBatteryMissionController.Awake += ReduceRequiredPillars;
		}

		private void ReduceRequiredPillars(On.RoR2.MoonBatteryMissionController.orig_Awake orig, MoonBatteryMissionController self)
		{
			orig(self);
			self._numRequiredBatteries = 2;
		}

		private void PillarsDropItems(On.RoR2.MoonBatteryMissionController.orig_OnBatteryCharged orig, RoR2.MoonBatteryMissionController self, RoR2.HoldoutZoneController holdoutZone)
		{
			//Debug.Log("A");
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			Vector3 dropPosition = holdoutZone.gameObject.transform.position + Vector3.up * pillarDropOffset;

			if (participatingPlayerCount != 0 && dropPosition != null)
			{
				//Debug.Log("B");
				PickupIndex pickupIndex = GetPickupIndexFromPillarType(holdoutZone.gameObject);

				if (pickupIndex != PickupIndex.none)
				{
					//Debug.Log("C");
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
						//Debug.Log("D");
						PickupDropletController.CreatePickupDroplet(pickupIndex, dropPosition, vector);
						i++;
						vector = rotation * vector;
					}
				}
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
