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

        public static void ChangeInteractableWeightForPool(string interactableNameLowered, int newWeight, DccsPool pool)
        {
            Helpers.ForEachPoolEntryInDccsPool(pool, (poolEntry) =>
            {
                for (int i = 0; i < poolEntry.dccs.categories.Length; i++)
                {
                    var cards = poolEntry.dccs.categories[i].cards.ToList();
                    foreach(DirectorCard card in cards)
                    {
                        if (card.spawnCard.name.ToLowerInvariant() == interactableNameLowered)
                            card.selectionWeight = newWeight;
                    }
                    poolEntry.dccs.categories[i].cards = cards.ToArray();
                }
            });
        }

        private void EquipBarrelOccurrenceHook(DccsPool pool, StageInfo currentStage)
        {
            string barrelName = DirectorAPI.Helpers.InteractableNames.EquipmentBarrel.ToLower();
            if (IsStageOne(currentStage.stage))
            {
                ChangeInteractableWeightForPool(barrelName, 6 /*2*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(barrelName, 2 /*2*/, pool);
            }

            string shopName = DirectorAPI.Helpers.InteractableNames.TripleShopEquipment.ToLower();
            if (IsStageThree(currentStage.stage))
            {
                ChangeInteractableWeightForPool(shopName, 10 /*2*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                ChangeInteractableWeightForPool(shopName, 2 /*2*/, pool);
            }
        }
        private void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string scrapperName = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLower();
            if (OnScrapperStage(currentStage.stage))
            {
                ChangeInteractableWeightForPool(scrapperName, 25 /*12*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                DirectorAPI.Helpers.RemoveExistingInteractable(scrapperName);
            }
        }
        private void PrinterOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string printerWhite = DirectorAPI.Helpers.InteractableNames.Printer3D.ToLower();
            string printerGreen = DirectorAPI.Helpers.InteractableNames.Printer3DLarge.ToLower();
            string printerRed = DirectorAPI.Helpers.InteractableNames.PrinterMiliTech.ToLower();

            if (OnPrinterStage(currentStage.stage))
            {
                //ChangeInteractableWeightForPool(printerWhite, 12 /*idk what it is in vanilla*/, pool);
                ChangeInteractableWeightForPool(printerGreen, 10 /*6*/, pool);
                ChangeInteractableWeightForPool(printerRed, (currentStage.stage == DirectorAPI.Stage.SkyMeadow) ? 12 : 3 /*1*/, pool);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, ""))
            {
                DirectorAPI.Helpers.RemoveExistingInteractable(printerWhite);
                DirectorAPI.Helpers.RemoveExistingInteractable(printerGreen);
                DirectorAPI.Helpers.RemoveExistingInteractable(printerRed);
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
