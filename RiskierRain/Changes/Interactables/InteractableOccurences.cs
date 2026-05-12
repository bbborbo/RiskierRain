using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static RoR2.GivePickupsOnStart;
using static R2API.RecalculateStatsAPI;
using R2API;
using RiskierRain.Components;
using static R2API.DirectorAPI;
using System.Linq;
using On.EntityStates.CaptainSupplyDrop;
using SwanSongExtended;
using MonoMod.RuntimeDetour;
using System.Reflection;
using Stage = RoR2.Stage;
using RainrotSharedUtils.Shelters;
using RainrotSharedUtils.Difficulties;

namespace RiskierRain.Changes
{
    public static partial class InteractableChanges
    {
        #region api or whatever
        //code belonds to r2api
        public static void ChangeInteractableWeightForPool(DccsPool pool, string interactableNameLowered, int newWeight, int maxPerStage = -1)
        {
            //Debug.Log($"Changing {interactableNameLowered} card weight!");
            if (pool)
            {
                Helpers.ForEachPoolEntryInDccsPool(pool, (poolEntry) =>
                {
                    for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                    {
                        var cards = poolEntry.dccs.categories[i].cards.ToList();
                        foreach (DirectorCard card in cards)
                        {
                            SpawnCard spawnCard = card.spawnCard;
                            if (spawnCard.name.ToLowerInvariant() == interactableNameLowered)
                            {
                                card.selectionWeight = newWeight;

                                if (maxPerStage >= 0 && spawnCard is InteractableSpawnCard)
                                {
                                    ((InteractableSpawnCard)spawnCard).maxSpawnsPerStage = maxPerStage;
                                }
                            }
                        }
                        poolEntry.dccs.categories[i].cards = cards.ToArray();
                    }
                });
            }
        }

        //code belonds to r2api
        private static void RemoveExistingInteractable(DccsPool pool, string interactableNameLowered)
        {
            if (pool)
            {
                Helpers.ForEachPoolEntryInDccsPool(pool, (poolEntry) =>
                {
                    for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                    {
                        var cards = poolEntry.dccs.categories[i].cards.ToList();
                        cards.RemoveAll((card) => card.spawnCard.name.ToLowerInvariant() == interactableNameLowered);
                        poolEntry.dccs.categories[i].cards = cards.ToArray();
                    }
                });
            }
        }
        private static bool OnPrinterStage(DirectorAPI.Stage stage)
        {
            return !OnScrapperStage(stage)
                || IsModdedPrinterStage(stage);//modded stages?
        }
        private static bool OnScrapperStage(DirectorAPI.Stage stage)
        {
            return IsStageOne(stage)
                || IsStageThree(stage)
                || IsModdedScrapperStage(stage);//modded stages?
        }

        private static bool IsStageOne(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.TitanicPlains
                || stage == DirectorAPI.Stage.DistantRoost
                || stage == DirectorAPI.Stage.SiphonedForest;
        }

        private static bool IsStageThree(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.RallypointDelta
                || stage == DirectorAPI.Stage.ScorchedAcres
                || stage == DirectorAPI.Stage.SulfurPools;
        }
        private static bool IsStageFive(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.SkyMeadow
                || stage == DirectorAPI.Stage.HelminthHatchery;
        }
        private static bool IsModdedPrinterStage(DirectorAPI.Stage stage)//this shit dont work im goin to bed
        {
            return stage == ParseInternalStageName("drybasin")
                || stage == ParseInternalStageName("slumberingsatellite");

        }
        private static bool IsModdedScrapperStage(DirectorAPI.Stage stage)
        {
            return stage == ParseInternalStageName("FBLScene");
        }
        #endregion

        #region printers
        public static GameObject whitePrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Duplicator");
        public static GameObject greenPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorLarge");
        public static GameObject redPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorMilitary");
        public static GameObject scrapper = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Scrapper");

        public static int printerGreenWeight = 20;//6
        public static int printerGreenLimit = 4;//-1
        public static int printerRedWeight = 4;//1
        public static int printerRedLimit = 1;//-1
        public static int printerRedWeightS5 = 1000;//1
        public static int printerRedLimitS5 = 2;//-1
        public static void PrinterScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string printerWhite = DirectorAPI.Helpers.InteractableNames.Printer3D.ToLowerInvariant();//.ToLower();
            string printerGreen = DirectorAPI.Helpers.InteractableNames.Printer3DLarge.ToLowerInvariant();//.ToLower();
            string printerRed = DirectorAPI.Helpers.InteractableNames.PrinterMiliTech.ToLowerInvariant();//.ToLower();
            string scrapper = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();

            bool isStageFive = IsStageFive(currentStage.stage);

            ChangeInteractableWeightForPool(pool, printerGreen, printerGreenWeight, printerGreenLimit);
            if (isStageFive)
            {
                ChangeInteractableWeightForPool(pool, printerRed, printerRedWeightS5, printerRedLimitS5);
                RemoveExistingInteractable(pool, scrapper);
            }
            else
            {
                ChangeInteractableWeightForPool(pool, printerRed, printerRedWeight, printerRedLimit);
            }
        }
        public static void PrinterOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string printerWhite = DirectorAPI.Helpers.InteractableNames.Printer3D.ToLowerInvariant();//.ToLower();
            string printerGreen = DirectorAPI.Helpers.InteractableNames.Printer3DLarge.ToLowerInvariant();//.ToLower();
            string printerRed = DirectorAPI.Helpers.InteractableNames.PrinterMiliTech.ToLowerInvariant();//.ToLower();
            string scrapper = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();

            bool isPrinterStage = OnPrinterStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Printer Stage: " + isPrinterStage);
            if (isPrinterStage)
            {
                //ChangeInteractableWeightForPool(printerWhite, 12 /*idk what it is in vanilla*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, "") || IsModdedScrapperStage(currentStage.stage))
            {
                RemoveExistingInteractable(pool, printerWhite);
                RemoveExistingInteractable(pool, printerGreen);
                RemoveExistingInteractable(pool, printerRed);
            }
        }
        #endregion
        #region scrappers
        public static int scrapperWeight = 1000;//12
        public static int scrapperLimit = 3;//-1

        public static int doubleChestWeight = 15; //idk

        public static void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string scrapperName = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();//.ToLower();

            string doubleChestName = DirectorAPI.Helpers.InteractableNames.AdaptiveChest.ToLowerInvariant();

            bool isScrapperStage = OnScrapperStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Scrapper Stage: " + isPrinterStage);

            if (isScrapperStage)
            {
                ChangeInteractableWeightForPool(pool, scrapperName, scrapperWeight, scrapperLimit);
                ChangeInteractableWeightForPool(pool, doubleChestName, doubleChestWeight);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, "") || IsModdedPrinterStage(currentStage.stage))
            {
                RemoveExistingInteractable(pool, scrapperName);
            }
        }
        #endregion

        #region equipment barrels
        public static int equipBarrelWeightS1 = 30;//2
        public static int equipBarrelLimitS1 = 3;//-1
        public static int equipBarrelWeight = 10;//2
        public static int equipBarrelLimit = 2;//-1
        public static int equipShopWeightS3 = 20;//2
        public static int equipShopLimitS3 = 3;//-1
        public static int equipShopWeight = 8;//2
        public static int equipShopLimit = 2;//-1
        public static void EquipBarrelOccurrenceHook(DccsPool pool, StageInfo currentStage)
        {
            string barrelName = DirectorAPI.Helpers.InteractableNames.EquipmentBarrel.ToLower();
            if (IsStageOne(currentStage.stage))
            {
                ChangeInteractableWeightForPool(pool, barrelName, equipBarrelWeightS1, equipBarrelLimitS1);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(pool, barrelName, equipBarrelWeight, equipBarrelLimit);
            }

            string shopName = DirectorAPI.Helpers.InteractableNames.TripleShopEquipment.ToLower();
            if (IsStageThree(currentStage.stage))
            {
                ChangeInteractableWeightForPool(pool, shopName, equipShopWeightS3, equipShopLimitS3);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(pool, shopName, equipShopWeight, equipShopLimit);
            }
        }
        #endregion

        #region lunar pods
        public static int lunarPodWeightS1 = 20;//2
        public static int lunarPodLimitS1 = 6;//-1
        public static int lunarPodWeight = 6;//2
        public static int lunarPodLimit = 2;//-1
        public static void LunarPodOccurrenceHook(DccsPool pool, StageInfo currentStage)
        {
            string podName = DirectorAPI.Helpers.InteractableNames.LunarPod.ToLower();
            if (IsStageOne(currentStage.stage))
            {
                ChangeInteractableWeightForPool(pool, podName, lunarPodWeightS1, lunarPodLimitS1);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(pool, podName, lunarPodWeight, lunarPodLimit);
            }
        }
        #endregion

        #region newt altar
        public static float newtAltarChance = 0.3f;

        public static void NerfBazaarStuff()
        {
            On.RoR2.SceneDirector.Start += SceneDirector_Start;
        }

        public static void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector director)
        {
            orig(director);

            if (NetworkServer.active && SceneInfo.instance.sceneDef.baseSceneName != "bazaar")
            {
                List<GameObject> randomNewts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue" || obj.name == "NewtStatue (1)" || obj.name == "NewtStatue (2)" || obj.name == "NewtStatue (3)" || obj.name == "NewtStatue (4)").ToList();
                List<GameObject> guaranteedNewts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue, Guarantee" || obj.name == "NewtStatue, Guaranteed" || obj.name == "NewtStatue (Permanent)").ToList();

                randomNewts.Concat(guaranteedNewts);

                foreach (var newt in randomNewts)
                {
                    if (newtAltarChance >= 1 || director.rng.nextNormalizedFloat <= newtAltarChance)
                    {
                        newt.SetActive(true);
                        break;
                    }
                    else
                        newt.SetActive(false);
                }
            }
        }
        #endregion
    }
}
