using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.OnHit;

namespace FruityElites.Modules
{
    public static class Hooks
    {
        public static void Init()
        {
            MakeSpawnSlotSpawnsInheritEliteAffix();
        }

        static void MakeSpawnSlotSpawnsInheritEliteAffix()
        {
            On.RoR2.NetworkedBodySpawnSlot.OnSpawnedServer += SpawnSlotMinionsInheritEliteAffix;
        }

        private static  void SpawnSlotMinionsInheritEliteAffix(On.RoR2.NetworkedBodySpawnSlot.orig_OnSpawnedServer orig, NetworkedBodySpawnSlot self, GameObject ownerBodyObject, SpawnCard.SpawnResult spawnResult, Action<MasterSpawnSlotController.ISlot, SpawnCard.SpawnResult> callback)
        {
            orig(self, ownerBodyObject, spawnResult, callback);

            CharacterBody ownerBody = ownerBodyObject.GetComponent<CharacterBody>();
            if (spawnResult.success && spawnResult.spawnedInstance && ownerBody)
            {
                Inventory component = spawnResult.spawnedInstance.GetComponent<Inventory>();
                if (component)
                {
                    component.CopyEquipmentFrom(ownerBody.inventory);
                }
            }
        }
    }

}
