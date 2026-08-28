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
            On.RoR2.CharacterBody.FixedUpdate += CharacterBodyDelayOutOfCombat;
            On.RoR2.TeleportHelper.TeleportBody_TeleportBodyArgs += TeleportBodyDelayOutOfCombat;
        }

        private static void TeleportBodyDelayOutOfCombat(On.RoR2.TeleportHelper.orig_TeleportBody_TeleportBodyArgs orig, TeleportHelper.TeleportBodyArgs bodyArgs)
        {
            orig(bodyArgs);
            DelayOutOfCombat(bodyArgs.body);
        }

        private static void CharacterBodyDelayOutOfCombat(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            orig(self);

            if (GetInvincible(self))
            {
                DelayOutOfCombat(self, self.outOfDangerStopwatch - Time.fixedDeltaTime);
            }

            bool GetInvincible(CharacterBody self)
            {
                return 
                    self.HasBuff(RoR2Content.Buffs.HiddenInvincibility) 
                    || self.HasBuff(RoR2Content.Buffs.Immune) 
                    || self.HasBuff(RoR2Content.Buffs.Intangible) 
                    || self.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage)
                    || self.HasBuff(RoR2Content.Buffs.Cloak)
                    || (self.TryGetComponent(out CharacterModel model) == true && model.invisibilityCount > 0)
                    ;
            }
        }

        private static void DelayOutOfCombat(CharacterBody self, float stopwatch = 0)
        {
            if (self.isPlayerControlled || self.teamComponent.teamIndex == TeamIndex.Player)
                return;
            self.outOfDangerStopwatch = Mathf.Clamp(stopwatch, 0, 5f);
            self.outOfDanger = false;
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
