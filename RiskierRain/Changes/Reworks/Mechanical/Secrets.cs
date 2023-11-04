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

        //RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset

        public static void AddSecrets()
        {
            InteractableSpawnCard greenPrinterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset").WaitForCompletion();
            InteractableSpawnCard bigChestSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Chest2/iscChest2.asset").WaitForCompletion();
            InteractableSpawnCard lunarPodSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/LunarChest/iscLunarChest.asset").WaitForCompletion();
            InteractableSpawnCard flameAltarSpawnCard = FlameAltar.instance.customInteractable.spawnCard;

            //ancient loft
            SpawnSecret("ancientloft", greenPrinterSpawnCard, new Vector3(-86, 29, 34));
            SpawnSecret("ancientloft", bigChestSpawnCard, new Vector3(-104, 106, 265));
            SpawnSecret("ancientloft", greenPrinterSpawnCard, new Vector3(-68, 40, -59));
            //foggyswamp
            SpawnSecret("foggyswamp", greenPrinterSpawnCard, new Vector3(257, 84, -140));
            SpawnSecret("foggyswamp", greenPrinterSpawnCard, new Vector3(145, -75, -75));
            SpawnSecret("foggyswamp", greenPrinterSpawnCard, new Vector3(-108, -104, -138));
            SpawnSecret("foggyswamp", greenPrinterSpawnCard, new Vector3(-86, 29, 34));
            SpawnSecret("foggyswamp", bigChestSpawnCard, new Vector3(-128, -127, 98));
            SpawnSecret("foggyswamp", lunarPodSpawnCard, new Vector3(-7 - 130, -356));
            //goolake
            SpawnSecret("goolake", bigChestSpawnCard, new Vector3(22, -158, -371));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(-7, -81, -174));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(221, -100, 296));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(351, -78, 108));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(118, -91, -7));
            SpawnSecret("goolake", bigChestSpawnCard, new Vector3(174, -11, -252));
            //wispgraveyard
            Vector3[] wispGraveyardSpots = new Vector3[4];
            wispGraveyardSpots[0] = new Vector3(-412, 6, -20);
            wispGraveyardSpots[1] = new Vector3(-418, 6, -67);
            wispGraveyardSpots[2] = new Vector3(-383, 6, -102);
            wispGraveyardSpots[3] = new Vector3(-421, 6, -39);
            SpawnSemiRandom("wispgraveyard", flameAltarSpawnCard, wispGraveyardSpots);
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
