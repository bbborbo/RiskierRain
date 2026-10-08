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
using SwanSongExtended.Interactables;
using static FabricatorStandalone.FabricatorPlugin;
using System.Runtime.CompilerServices;

namespace SwanSongExtended
{
    public static class Secrets 
    {
        const bool DEBUG = true;

        #region secret spawns

        //RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset

        public static void AddSecrets()
        {
            if (SwanSongPlugin.fabricatorsLoaded)
                AddDoubleChestSecrets();
            InteractableSpawnCard shrineCombatSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_ShrineCombat.iscShrineCombat_asset).WaitForCompletion();
            InteractableSpawnCard equipBarrelSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_EquipmentBarrel.iscEquipmentBarrel_asset).WaitForCompletion();
            InteractableSpawnCard greenPrinterSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/DuplicatorLarge/iscDuplicatorLarge.asset").WaitForCompletion();
            InteractableSpawnCard bigChestSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/Chest2/iscChest2.asset").WaitForCompletion();
            InteractableSpawnCard lunarPodSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/LunarChest/iscLunarChest.asset").WaitForCompletion();
            InteractableSpawnCard constructConstructSpawnCard = ConstructConstruct.instance.customInteractable.spawnCard;
            InteractableSpawnCard flameAltarSpawnCard = null;//FlameAltar.instance.customInteractable.spawnCard;
            InteractableSpawnCard lunarBrandMakerSpawnCard = null;//LunarBrandMaker.instance.customInteractable.spawnCard;
            InteractableSpawnCard spineSpawnCard = null;//SpineAltar.instance.customInteractable.spawnCard;
            InteractableSpawnCard spineChargerCard = null;//SpineCharger.instance.customInteractable.spawnCard;

            //ancient loft (sanctuary)
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
            SpawnSecret("foggyswamp", constructConstructSpawnCard, new Vector3(258, -150, -170), 0.3f);

            //goolake
            SpawnSecret("goolake", bigChestSpawnCard, new Vector3(22, -158, -371));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(-7, -81, -174));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(221, -100, 296));
            SpawnSecret("goolake", flameAltarSpawnCard, new Vector3(351, -78, 108));
            SpawnSecret("goolake", greenPrinterSpawnCard, new Vector3(118, -91, -7));
            SpawnSecret("goolake", bigChestSpawnCard, new Vector3(174, -11, -252));
            //wispgraveyard
            Vector3[] wispGraveyardSpots = new Vector3[4];
            wispGraveyardSpots[0] = new Vector3(-412, 6, -20);
            wispGraveyardSpots[1] = new Vector3(-418, 6, -67);
            wispGraveyardSpots[2] = new Vector3(-383, 6, -102);
            wispGraveyardSpots[3] = new Vector3(-421, 6, -39);
            SpawnSemiRandom("wispgraveyard", flameAltarSpawnCard, wispGraveyardSpots);

            SpawnSecret("wispgraveyard", lunarBrandMakerSpawnCard, new Vector3(46, 29, -62));
            //frozenWall
            Vector3[] frozenWallCliffSpots = new Vector3[3];
            frozenWallCliffSpots[0] = new Vector3(74, 121, 147);
            frozenWallCliffSpots[1] = new Vector3(66, 115, 109);
            frozenWallCliffSpots[2] = new Vector3(54, 111, 72);
            SpawnSemiRandom("frozenwall", spineSpawnCard, frozenWallCliffSpots);

            SpawnSecret("frozenwall", spineChargerCard, new Vector3(0, 34, 5));
            //sulphurpools
            SpawnSecret("sulfurpools", lunarBrandMakerSpawnCard, new Vector3(9, -7, -51), 0.5f);
            SpawnSecret("sulfurpools", lunarBrandMakerSpawnCard, new Vector3(-155, 27, 46), 0.5f);
            SpawnSecret("sulfurpools", lunarBrandMakerSpawnCard, new Vector3(176, 28, 45), 0.5f);
            SpawnSecret("sulfurpools", lunarBrandMakerSpawnCard, new Vector3(94, 22, -133), 0.5f);

            Vector3[] caveSpots = new Vector3[3];
            caveSpots[0] = new Vector3(23, -35, 65);
            caveSpots[1] = new Vector3(26, -34, 99);
            caveSpots[2] = new Vector3(28, -34, 36);
            SpawnSemiRandom("sulfurpools", constructConstructSpawnCard, caveSpots);
            Vector3[] wallSpots = new Vector3[2];
            wallSpots[0] = new Vector3(173, 2, -154);
            wallSpots[1] = new Vector3(128, 0, -194);
            SpawnSemiRandom("sulfurpools", constructConstructSpawnCard, wallSpots, 0.5f);


        }
        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static void AddDoubleChestSecrets()
        {
            //titanic plains 1
            Secrets.SpawnSecret("golemplains", fabricatorCommonSpawnCard, new Vector3(-109, -100, 42));//doublechest
            Secrets.SpawnSecret("golemplains", fabricatorCommonSpawnCard, new Vector3(133, -100, 29), 0.4f);//big chest maybe
            Secrets.SpawnSecret("golemplains", fabricatorCommonSpawnCard, new Vector3(183, -92, -144));//doublechest //bonus mob
            //SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(139, -119, 194));//doublechest queatet
            //SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(64, -115, -264));//lunar pod? very stupid
            Secrets.SpawnSecret("golemplains", fabricatorCommonSpawnCard, new Vector3(100, -155, -342), 0.4f);//doublechest, make chance based

            Vector3[] quartetSpots = new Vector3[5];
            quartetSpots[0] = new Vector3(139, -119, 194);
            quartetSpots[1] = new Vector3(156, -120, -196);
            quartetSpots[2] = new Vector3(152, -112, -222);
            quartetSpots[3] = new Vector3(120, -112, -209);
            quartetSpots[4] = new Vector3(89, -116, -192);
            Secrets.SpawnSemiRandom("golemplains", fabricatorCommonSpawnCard, quartetSpots);

            //titanic plains 2
            Secrets.SpawnSecret("golemplains2", fabricatorCommonSpawnCard, new Vector3(-33, 61, -57));//doublechest make this one a semirandom later
            Secrets.SpawnSecret("golemplains2", fabricatorCommonSpawnCard, new Vector3(-77, 54, -102));//doublechest this too
            Secrets.SpawnSecret("golemplains2", fabricatorCommonSpawnCard, new Vector3(-214, 42, -29), 0.8f);//doublechest
            Secrets.SpawnSecret("golemplains2", fabricatorCommonSpawnCard, new Vector3(141, 60, -4), 0.4f);//doublechest
            Secrets.SpawnSecret("golemplains2", fabricatorCommonSpawnCard, new Vector3(151, 14, -230));//doublechest

            //blackbeach 1
            Secrets.SpawnSecret("blackbeach", fabricatorCommonSpawnCard, new Vector3(-23, -175, -387));//doublechest
            Secrets.SpawnSecret("blackbeach", fabricatorCommonSpawnCard, new Vector3(93, -125, -299));//doublechest
            Secrets.SpawnSecret("blackbeach", fabricatorCommonSpawnCard, new Vector3(31, -213, -120));//doublechest floor issue
            Secrets.SpawnSecret("blackbeach", fabricatorCommonSpawnCard, new Vector3(-288, -16, -181), 0.3f);//doublechest
            Secrets.SpawnSecret("blackbeach", fabricatorCommonSpawnCard, new Vector3(-337, -199, -230), 0.5f);//doublechest

            //blackbeach 2
            Secrets.SpawnSecret("blackbeach2", fabricatorCommonSpawnCard, new Vector3(-101, 28, 11), 0.8f);//doublechest floor issue
            Secrets.SpawnSecret("blackbeach2", fabricatorCommonSpawnCard, new Vector3(-134, 47, -103), 0.4f);//doublechest
            Secrets.SpawnSecret("blackbeach2", fabricatorCommonSpawnCard, new Vector3(12, 88, -126));//doublechest
            Secrets.SpawnSecret("blackbeach2", fabricatorCommonSpawnCard, new Vector3(117, 65, 151));//doublechest floor issue

            //snowyforest
            Secrets.SpawnSecret("snowyforest", fabricatorCommonSpawnCard, new Vector3(-252, 22, 57), 0.5f);//doublechest
            Secrets.SpawnSecret("snowyforest", fabricatorCommonSpawnCard, new Vector3(24, 67, 2));//doublechest
            Secrets.SpawnSecret("snowyforest", fabricatorCommonSpawnCard, new Vector3(-34, 70, -193));//doublechest
            Secrets.SpawnSecret("snowyforest", fabricatorCommonSpawnCard, new Vector3(38, 42, -27), 0.5f);//doublechest

            Vector3[] snowyForestSpots = new Vector3[3];
            snowyForestSpots[0] = new Vector3(136, 53, 191);
            snowyForestSpots[1] = new Vector3(92, 41, -32);
            snowyForestSpots[2] = new Vector3(110, 79, 19);
            Secrets.SpawnSemiRandom("snowyforest", fabricatorCommonSpawnCard, snowyForestSpots);

            //ancientloft
            Secrets.SpawnSecret("ancientloft", fabricatorCommonSpawnCard, new Vector3(165, 62, -31), 0.8f); //doublechest

            //wispgraveyard
            //SpawnSecret("wispgraveyard", doubleChestSpawnCard, new Vector3(46, 29, -62), 0.8f);
            Secrets.SpawnSecret("wispgraveyard", fabricatorCommonSpawnCard, new Vector3(-22, 59, 286));//didnt spawn idk why

            //Vector3[] wispGraveyardSpots = new Vector3[4];
            //wispGraveyardSpots[0] = new Vector3(-412, 6, -20);
            //wispGraveyardSpots[1] = new Vector3(-418, 6, -67);
            //wispGraveyardSpots[2] = new Vector3(-383, 6, -102);
            //wispGraveyardSpots[3] = new Vector3(-421, 6, -39);
            //SpawnSemiRandom("wispgraveyard", doubleChestSpawnCard, wispGraveyardSpots);

            //frozenwall
            Secrets.SpawnSecret("frozenwall", fabricatorCommonSpawnCard, new Vector3(87, 82, -250), 0.5f);
            Secrets.SpawnSecret("frozenwall", fabricatorCommonSpawnCard, new Vector3(-104, 35, 49));
            //SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(-139, 50, 7)); idk :3
            //SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(0, 34, 5));
            Secrets.SpawnSecret("frozenwall", fabricatorCommonSpawnCard, new Vector3(196, 25, 32));//DOESNT ALWAYS SPAWN


            //sulfurpools
            Secrets.SpawnSecret("sulfurpools", fabricatorCommonSpawnCard, new Vector3(11, -19, 37));
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(9, -7, -51), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(-155, 27, 46), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(176, 28, 45), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(94, 22, -133), 0.5f);


        }
        #endregion

        #region utils
        public static void SpawnSecret(string scene, SpawnCard spawnCard, Vector3 pos, float chance = -1)
        {
            if (spawnCard == null)
                return;
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
            if (spawnCard == null)
                return;
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
