using BepInEx;
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
        static GameObject healPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/HealPack");
        static GameObject ammoPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/AmmoPack");
        static GameObject moneyPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/BonusMoneyPack");

        public static void FixPickupStats()
        {
            BuffPickupRange(healPack);
            BuffPickupRange(ammoPack);
            BuffPickupRange(moneyPack);

            On.RoR2.GravitatePickup.OnTriggerEnter += ChangeGravitateTargetBehavior;
        }

        private static void ChangeGravitateTargetBehavior(On.RoR2.GravitatePickup.orig_OnTriggerEnter orig, GravitatePickup self, Collider other)
        {
            if (NetworkServer.active && TeamComponent.GetObjectTeam(other.gameObject) == self.teamFilter.teamIndex)
            {
                if (self.gravitateTarget)
                {
                    if (other.gameObject.transform == self.gravitateTarget)
                        return;

                    HealthComponent targetHealthComponent = self.gravitateTarget.GetComponent<HealthComponent>();
                    if (targetHealthComponent && targetHealthComponent.body.isPlayerControlled)
                        return;
                }

                HealthComponent component = other.gameObject.GetComponent<HealthComponent>();
                if (component != null && (self.gravitateAtFullHealth || component.health < component.fullHealth))
                {
                    if (component.body.isPlayerControlled)
                    {
                        self.gravitateTarget = other.gameObject.transform;
                        return;
                    }
                }

                if (!self.gravitateTarget)
                {
                    if (self.gravitateAtFullHealth)
                    {
                        self.gravitateTarget = other.gameObject.transform;
                    }
                }
            }
        }

        public static void BuffPickupRange(GameObject pack)
        {
            GravitatePickup gravPickup = pack.GetComponentInChildren<GravitatePickup>();
            if(gravPickup != null)
            {
                Collider gravitateTrigger = gravPickup.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale *= 2.5f;
                }
            }
            else
            {
                Debug.Log($"GameObject {pack.name} has no GravitatePickup component!");
            }
        }
    }
}