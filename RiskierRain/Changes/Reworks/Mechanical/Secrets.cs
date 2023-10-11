using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using System.Linq;
using UnityEngine.AddressableAssets;
using R2API;
using UnityEngine.Networking;
using RiskierRain.Interactables;
using RiskierRain;

namespace RiskierRain
{
    public static class Secrets 
    {
        const bool DEBUG = true;

        #region secret spawns
        public static void AddSecrets(List<DirectorCard> directorCards)
        {
            
        }
        #endregion

        #region utils
        public static void SpawnSecret(string scene, SpawnCard spawnCard, Vector3 pos, float chance = -1)
        {
            Stage.onStageStartGlobal += self =>
            {
                Vector3 rot = default;
                SceneDef abc = self.sceneDef;
                if (abc == null)
                {
                    return;
                }
                if (self.sceneDef.cachedName != scene) return;
                if (chance != -1)
                {
                    if (!RollForSecret(chance))
                    {
                        return;
                    }
                }
                bool floor = CheckForGeometry(pos + new Vector3(0, 2, 0));
                if (!floor)
                {
                    Debug.Log("no floor!!");
                    return;
                }
                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule() { placementMode = DirectorPlacementRule.PlacementMode.Direct };
                if (spawnCard == null) Debug.Log("spawncardnullwtf");
                if (directorPlacementRule == null) Debug.Log("placementrulenulwtf");
                GameObject spawnedInstance = spawnCard.DoSpawn(pos, Quaternion.Euler(rot), new DirectorSpawnRequest(spawnCard, directorPlacementRule, Run.instance.runRNG)).spawnedInstance;
                spawnedInstance.transform.eulerAngles = rot;
                if (spawnedInstance)
                {
                    PurchaseInteraction component = spawnedInstance.GetComponent<PurchaseInteraction>();
                    if (component && component.costType == CostTypeIndex.Money) component.Networkcost = Run.instance.GetDifficultyScaledCost(component.cost);
                }
                NetworkServer.Spawn(spawnedInstance);
            };
            //Debug.Log($"added a spawn for {spawnCard.prefab.name} at {scene}");
        }
        
        public static void SpawnSemiRandom(string scene, SpawnCard spawnCard, Vector3[] posList, float chance = -1)
        {
            Stage.onStageStartGlobal += self =>
            {
                Vector3 rot = default;
                if (self.sceneDef.cachedName != scene) return;

                if (chance != -1)
                {
                    if (!RollForSecret(chance))
                    {
                        return;
                    }
                }

                DirectorPlacementRule directorPlacementRule = new DirectorPlacementRule() { placementMode = DirectorPlacementRule.PlacementMode.Direct };
                GameObject spawnedInstance = spawnCard.DoSpawn(SemiRandomLocation(posList), Quaternion.Euler(rot), new DirectorSpawnRequest(spawnCard, directorPlacementRule, Run.instance.runRNG)).spawnedInstance;
                spawnedInstance.transform.eulerAngles = rot;
                if (spawnedInstance)
                {
                    PurchaseInteraction component = spawnedInstance.GetComponent<PurchaseInteraction>();
                    if (component && component.costType == CostTypeIndex.Money) component.Networkcost = Run.instance.GetDifficultyScaledCost(component.cost);
                }
                NetworkServer.Spawn(spawnedInstance);
            };
            //Debug.Log($"added a spawn for {spawnCard.prefab.name} at {scene}");

        }
        public static Vector3 SemiRandomLocation(Vector3[] locations)
        {
            int a = locations.Length;
            int b = UnityEngine.Random.RandomRangeInt(0, a);
            Vector3 value = locations[b];
            if (DEBUG)
            {
                Debug.LogWarning($"{a} spots, chose spot {b}");
            }
            return value;
        }
        public static bool RollForSecret(float chance)
        {
            return UnityEngine.Random.RandomRange(0, 1f) < chance;
        }

        public static bool CheckForGeometry(Vector3 cords)
        {
            return Physics.Raycast(cords, Vector3.down, 5);
        }
        #endregion
    }
}
