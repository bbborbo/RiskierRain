using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Interactables
{
    static class CombatEncounterHelper
    {

        public static GameObject MethodOne(PurchaseInteraction purchaseInteraction, Interactor activator) // RENAME LATER
        {
            bool value = false;
            Vector3 vector = Vector3.zero;
            Quaternion rotation = Quaternion.identity;
            Transform transform = purchaseInteraction.gameObject.transform;
            if (transform)
            {
                vector = transform.position;
                rotation = transform.rotation;
            }

            GameObject gameObject = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/Encounters/MonstersOnShrineUseEncounter");
            if (gameObject == null)
            {
                return null;
            }
            GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(gameObject, vector, Quaternion.identity);
            NetworkServer.Spawn(gameObject2);
            CombatDirector component6 = gameObject2.GetComponent<CombatDirector>();
            component6.gameObject.AddComponent<RiskierRainCombatDirector>();
            if (!(component6 && Stage.instance))
            {
                return null;
            }
            float monsterCredit = 40f * Stage.instance.entryDifficultyCoefficient;
            DirectorCard directorCard = component6.SelectMonsterCardForCombatShrine(monsterCredit);
            if (directorCard != null)
            {
                component6.CombatShrineActivation(activator, monsterCredit, directorCard);

                EffectData effectData = new EffectData
                {
                    origin = vector,
                    rotation = rotation
                };

                GameObject monstersOnShrineUse = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/MonstersOnShrineUse");

                EffectManager.SpawnEffect(monstersOnShrineUse, effectData, true);
                value = true;
            }
            //NetworkServer.Destroy(gameObject2);

            return gameObject2;
        }


        public static void MethodTwo()
        {

        }
    }

    class RiskierRainCombatDirector : MonoBehaviour
    {

    }
}
