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
        GameObject healPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/HealPack");
        float toothDuration = 15; //5

        GameObject ammoPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/AmmoPack");
        GameObject moneyPack = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/BonusMoneyPack");

        public void FixPickupStats()
        {
            BuffPickupRange(healPack);
            BuffPickupRange(ammoPack);
            BuffPickupRange(moneyPack);

            On.RoR2.GravitatePickup.OnTriggerEnter += ChangeGravitateTargetBehavior;
        }

        private void MonsterToothDurationBuff()
        {
            healPack.GetComponent<DestroyOnTimer>().duration = toothDuration;
            healPack.GetComponent<BeginRapidlyActivatingAndDeactivating>().delayBeforeBeginningBlinking = (toothDuration - 2f);
        }

        private void ChangeGravitateTargetBehavior(On.RoR2.GravitatePickup.orig_OnTriggerEnter orig, GravitatePickup self, Collider other)
        {
            if (NetworkServer.active && TeamComponent.GetObjectTeam(other.gameObject) == self.teamFilter.teamIndex)
            {
                if (self./*private*/gravitateTarget)
                {
                    if (other.gameObject.transform == self./*private*/gravitateTarget)
                        return;

                    HealthComponent targetHealthComponent = self./*private*/gravitateTarget.GetComponent<HealthComponent>();
                    if (targetHealthComponent && targetHealthComponent.body.isPlayerControlled)
                        return;
                }

                HealthComponent component = other.gameObject.GetComponent<HealthComponent>();
                if (component != null && (self.gravitateAtFullHealth || component.health < component.fullHealth))
                {
                    if (component.body.isPlayerControlled)
                    {
                        self./*private*/gravitateTarget = other.gameObject.transform;
                        return;
                    }
                }

                if (!self./*private*/gravitateTarget)
                {
                    if (self.gravitateAtFullHealth)
                    {
                        self./*private*/gravitateTarget = other.gameObject.transform;
                    }
                }
            }
        }

        void BuffPickupRange(GameObject pack)
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