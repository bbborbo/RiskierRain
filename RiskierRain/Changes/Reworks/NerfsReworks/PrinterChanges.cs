using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API.Utils;
using RoR2;
using System.Linq;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static GameObject whitePrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Duplicator");
        public static GameObject greenPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorLarge");
        public static GameObject redPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorMilitary");
        public static GameObject scrapper = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Scrapper");

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
                            if (card.spawnCard.name.ToLowerInvariant() == interactableNameLowered)
                                card.selectionWeight = newWeight;   
                            if(maxPerStage >= 0)
                            {
                                SpawnCard isc = card.spawnCard;
                                if(isc is InteractableSpawnCard)
                                {
                                    ((InteractableSpawnCard)isc).maxSpawnsPerStage = maxPerStage;
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

        public int equipBarrelWeightS1 = 20;//2
        public int equipBarrelLimitS1 = 2;//-1
        public int equipBarrelWeight = 6;//2
        public int equipBarrelLimit = -1;//-1
        public int equipShopWeightS3 = 20;//2
        public int equipShopLimitS3 = 5;//-1
        public int equipShopWeight = 4;//2
        public int equipShopLimit = -1;//-1
        private void EquipBarrelOccurrenceHook(DccsPool pool, StageInfo currentStage)
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

        public int scrapperWeight = 100;//12
        public int scrapperLimit = 3;//-1
        private void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string scrapperName = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();//.ToLower();

            bool isPrinterStage = OnScrapperStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Scrapper Stage: " + isPrinterStage);

            if (isPrinterStage)
            {
                ChangeInteractableWeightForPool(pool, scrapperName, scrapperWeight, scrapperLimit);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                RemoveExistingInteractable(pool, scrapperName);
            }
        }

        public int printerGreenWeight = 15;//6
        public int printerGreenLimit = 4;//-1
        public int printerRedWeight = 4;//1
        public int printerRedLimit = 1;//-1
        public int printerRedWeightS5 = 100;//1
        public int printerRedLimitS5 = 2;//-1
        private void PrinterOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string printerWhite = DirectorAPI.Helpers.InteractableNames.Printer3D.ToLowerInvariant();//.ToLower();
            string printerGreen = DirectorAPI.Helpers.InteractableNames.Printer3DLarge.ToLowerInvariant();//.ToLower();
            string printerRed = DirectorAPI.Helpers.InteractableNames.PrinterMiliTech.ToLowerInvariant();//.ToLower();

            bool isPrinterStage = OnPrinterStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Printer Stage: " + isPrinterStage);

            if (isPrinterStage)
            {
                //ChangeInteractableWeightForPool(printerWhite, 12 /*idk what it is in vanilla*/, pool);
                ChangeInteractableWeightForPool(pool, printerGreen, printerGreenWeight, printerGreenLimit);
                if(currentStage.stage == DirectorAPI.Stage.SkyMeadow)
                    ChangeInteractableWeightForPool(pool, printerRed, printerRedWeightS5, printerRedLimitS5);
                else
                    ChangeInteractableWeightForPool(pool, printerRed, printerRedWeight, printerRedLimit);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                RemoveExistingInteractable(pool, printerWhite);
                RemoveExistingInteractable(pool, printerGreen);
                RemoveExistingInteractable(pool, printerRed);
            }
        }

        #region bools
        private bool OnPrinterStage(DirectorAPI.Stage stage)
        {
            return !OnScrapperStage(stage);
        }
        private bool OnScrapperStage(DirectorAPI.Stage stage)
        {
            return IsStageOne(stage)
                || IsStageThree(stage);
        }

        private bool IsStageOne(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.TitanicPlains
                || stage == DirectorAPI.Stage.DistantRoost
                || stage == DirectorAPI.Stage.SiphonedForest;
        }

        private bool IsStageThree(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.RallypointDelta
                || stage == DirectorAPI.Stage.ScorchedAcres
                || stage == DirectorAPI.Stage.SulfurPools;
        }
        #endregion
    }
}
