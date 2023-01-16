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
        public static void ChangeInteractableWeightForPool(string interactableNameLowered, int newWeight, DccsPool pool)
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

        public int equipBarrelWeightS1 = 6;//2
        public int equipBarrelWeight = 2;//2
        public int equipShopWeightS3 = 10;//2
        public int equipShopWeight = 2;//2
        private void EquipBarrelOccurrenceHook(DccsPool pool, StageInfo currentStage)
        {
            string barrelName = DirectorAPI.Helpers.InteractableNames.EquipmentBarrel.ToLower();
            if (IsStageOne(currentStage.stage))
            {
                ChangeInteractableWeightForPool(barrelName, equipBarrelWeightS1 /*2*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(barrelName, equipBarrelWeight /*2*/, pool);
            }

            string shopName = DirectorAPI.Helpers.InteractableNames.TripleShopEquipment.ToLower();
            if (IsStageThree(currentStage.stage))
            {
                ChangeInteractableWeightForPool(shopName, equipShopWeightS3 /*2*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(shopName, equipShopWeight /*2*/, pool);
            }
        }

        public int scrapperWeight = 25;//12
        private void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string scrapperName = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();//.ToLower();

            bool isPrinterStage = OnScrapperStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Scrapper Stage: " + isPrinterStage);

            if (isPrinterStage)
            {
                ChangeInteractableWeightForPool(scrapperName, scrapperWeight, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                RemoveExistingInteractable(pool, scrapperName);
            }
        }

        public int printerGreenWeight = 10;//6
        public int printerRedWeight = 3;//1
        public int printerRedWeightS5 = 15;//1
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
                ChangeInteractableWeightForPool(printerGreen, printerGreenWeight, pool);
                ChangeInteractableWeightForPool(printerRed, 
                    (currentStage.stage == DirectorAPI.Stage.SkyMeadow) 
                    ? printerRedWeightS5 : printerRedWeight, pool);
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
