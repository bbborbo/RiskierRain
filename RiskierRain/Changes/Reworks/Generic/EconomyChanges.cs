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

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        GameObject awu = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/SuperRoboBallBossBody");
        CharacterBody awuBody;
        float awuArmor = 40;
        float awuAdditionalArmor = 0;
        int awuAdaptiveArmorCount = 1;

        static float costExponent = 1.3f;
        static float goldRewardMultiplierGlobal = 1.2f;
        static float expRewardMultiplierGlobal = 1;


        PurchaseInteraction smallChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestDamage.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestHealing.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestUtility.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest2/Chest2.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Damage Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Healing Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Utility Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction casinoChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CasinoChest/CasinoChest.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction chanceShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction chanceShrineSnowy = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChanceSnowy Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction chanceShrineSandy = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChanceSandy Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        //big category chest is 'categorychest2healing' and such


        MultiShopController smallShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShop/TripleShop.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController bigShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController equipmentShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab").WaitForCompletion().GetComponent<MultiShopController>();

        string discountChestPrefix = "Bargain";
        int smallChestTypeCost = 20; //25
        int smallShopTypeCost = 35; //25
        int smallCategoryChestTypeCost = 25; //30
        int bigChestTypeCost = 45; //50
        int bigShopTypeCost = 70; //50
        int bigCategoryChestTypeCost = 50; //60
        int goldChestTypeCost = 200; //400
        int bigDroneTypeCost = 160; //250
        int casinoChestTypeCost = 30; //50; cost is incurred twice
        int chanceShrineTypeCost = 15; //17

        void FixMoneyScaling()
        {
            ChestRebalance();
            ChestCostScaling();
            EnemyRewards();
        }

        private void ChestCostScaling()
        {
            On.RoR2.Run.GetDifficultyScaledCost_int_float += ChangeScaledCost;

            // adjusting AWU armor to compensate for chest cost increases
            awuBody = awu.GetComponent<CharacterBody>();
            if (awuBody)
            {
                awuBody.baseArmor = awuArmor;
                if (awuAdaptiveArmorCount <= 0)
                {
                    awuBody.armor += awuAdditionalArmor;
                }
                else
                {
                    GivePickupsOnStart gpos = awuBody.gameObject.AddComponent<GivePickupsOnStart>();
                    if (gpos)
                    {
                        ItemInfo adaptiveArmor = new ItemInfo();
                        adaptiveArmor.count = awuAdaptiveArmorCount;
                        adaptiveArmor.itemString = Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/AdaptiveArmor/AdaptiveArmor.asset").WaitForCompletion().nameToken;

                        gpos.itemInfos = new ItemInfo[1] { adaptiveArmor };
                    }
                }
            }
        }


        #region Gold Shrine
        GameObject goldShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineGoldshoresAccess/ShrineGoldshoresAccess.prefab").WaitForCompletion();
        int goldShrineCost = 5;
        private void GoldShrineRework()
        {
            if(goldShrine == null)
            {
                Debug.Log("goldshrine null!! uh oh!!!!");
                return;
            }

            PurchaseInteraction goldShrineInteraction = goldShrine.GetComponent<PurchaseInteraction>();
            if(goldShrineInteraction == null)
            {
                Debug.Log("goldshrine purchase thing null bwuh");
                return;
            }

            goldShrineInteraction.costType = CostTypeIndex.LunarCoin; // gold
            goldShrineInteraction.cost = goldShrineCost;

        }

        #endregion

        #region Blood Shrines
        private static int teamMaxHealth;
        private static int totalBloodGoldValue = 60; // amount of gold awarded for using the shrine all times
        private const float totalHealthFraction = 2.18f; // health bars
        private static float chestsPerHealthBar = 2; // number of chest costs awarded per health bar

        private void BloodShrineRewardRework()
        {
            On.RoR2.ShrineBloodBehavior.Start += ShrineBloodBehavior_Start;
        }
        private void ShrineBloodBehavior_Start(On.RoR2.ShrineBloodBehavior.orig_Start orig, ShrineBloodBehavior self)
        {
            orig(self);
            if (NetworkServer.active) StartCoroutine(WaitForPlayerBody(self));
        }

        IEnumerator WaitForPlayerBody(ShrineBloodBehavior instance)
        {
            yield return new WaitForSeconds(2);

            if (instance.goldToPaidHpRatio != 0)
            {
                foreach (var playerCharacterMasterController in PlayerCharacterMasterController.instances)
                {
                    var body = playerCharacterMasterController.master.GetBody();

                    if (body)
                    {
                        var maxHealth = body.healthComponent.fullCombinedHealth;
                        if (maxHealth > teamMaxHealth) teamMaxHealth = (int)maxHealth;
                    }
                }

                float moneyTotal = Run.instance.GetDifficultyScaledCost(totalBloodGoldValue, RoR2.Stage.instance.entryDifficultyCoefficient); //target money granted by the shrine
                float maxMulti = moneyTotal / teamMaxHealth; //express target money as a fraction of the max health of the team

                if (maxMulti > 0)//0.5f)
                    instance.goldToPaidHpRatio = maxMulti / totalHealthFraction; //
            }
        }
        #endregion

        #region Economy
        private void EnemyRewards()
        {
            ILHook goldRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_goldReward", (BindingFlags)(-1)), FixGoldRewards);
            ILHook expRewardFix = new ILHook(typeof(DeathRewards).GetMethod("set_expReward", (BindingFlags)(-1)), FixExpRewards);
            //On.RoR2.TeleporterInteraction.Awake += ReduceTeleDirectorReward;
        }

        static float GetCompensatedDifficultyFraction()
        {
            float boost = GetAmbientLevelBoost();

            float entryDiffCoeff = (Stage.instance.entryDifficultyCoefficient - boost);
            if (entryDiffCoeff <= 0)
                return 1;
            return entryDiffCoeff / (Run.instance.compensatedDifficultyCoefficient);
        }

        private static void FixGoldRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<uint, uint>>((money) =>
            {
                float compensated = GetCompensatedDifficultyFraction();
                return (uint)(money * compensated * goldRewardMultiplierGlobal);
            });
            c.Emit(OpCodes.Starg, 1);
        }
        private static void FixExpRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<uint, uint>>((exp) =>
            {
                float compensated = GetCompensatedDifficultyFraction();
                return (uint)(exp * compensated * expRewardMultiplierGlobal);
            });
            c.Emit(OpCodes.Starg, 1);
        }

        private int ChangeScaledCost(On.RoR2.Run.orig_GetDifficultyScaledCost_int_float orig, RoR2.Run self, int baseCost, float difficultyCoefficient)
        {
            int costMultiplier = baseCost / 25;
            switch (costMultiplier)
            {
                case 16:
                    baseCost = goldChestTypeCost; //10, originally 16
                    break;
                case 14:
                    baseCost = bigDroneTypeCost; //8, originally 14
                    break;
            }

            float costMultiplierExponential = Mathf.Pow(difficultyCoefficient, costExponent);
            float costMultiplierLinear = (difficultyCoefficient * 2.5f - 1.5f); //arbitrary, unused

            float endMultiplier = costMultiplierExponential;
            if (costMultiplierLinear < costMultiplierExponential)
            {
                //endMultiplier = costMultiplierLinear;
                //Debug.Log("Using Liner multiplier!");
            }

            return (int)((float)baseCost * endMultiplier);
        }

        private void ChestRebalance()
        {
            if(smallChest != null)
            {
                LanguageAPI.Add("CHEST1_NAME", $"{discountChestPrefix} Chest");
                LanguageAPI.Add("CHEST1_CONTEXT", $"Open discounted chest");
                smallChest.cost = smallChestTypeCost;
            }
            if (smallShop != null)
            {
                smallShop.baseCost = smallShopTypeCost;
            }
            if (smallCategoryChestDamage != null)
            {
                smallCategoryChestDamage.cost = smallCategoryChestTypeCost;
            }
            if (smallCategoryChestHealing != null)
            {
                smallCategoryChestHealing.cost = smallCategoryChestTypeCost;
            }
            if (smallCategoryChestUtility != null)
            {
                smallCategoryChestUtility.cost = smallCategoryChestTypeCost;
            }

            if (bigChest != null)
            {
                LanguageAPI.Add("CHEST2_NAME", $"Large {discountChestPrefix} Chest");
                LanguageAPI.Add("CHEST2_CONTEXT", $"Open discounted large chest");
                bigChest.cost = bigChestTypeCost;
            }
            if (bigShop != null)
            {
                bigShop.baseCost = bigShopTypeCost;
            }
            if (bigCategoryChestDamage != null)
            {
                bigCategoryChestDamage.cost = bigCategoryChestTypeCost;
            }
            if (bigCategoryChestHealing != null)
            {
                bigCategoryChestHealing.cost = bigCategoryChestTypeCost;
            }
            if (bigCategoryChestUtility != null)
            {
                bigCategoryChestUtility.cost = bigCategoryChestTypeCost;
            }

            if (casinoChest != null)
            {
                casinoChest.cost = casinoChestTypeCost;
                //casinoChest.displayNameToken = "Double Chest";//doesnt work
                LanguageAPI.Add("CASINOCHEST_NAME", $"Double Chest");
                LanguageAPI.Add("CASINOCHEST_CONTEXT", $"Open double chest");
                LanguageAPI.Add("CASINOCHEST_DESCRIPTION", $"Costs gold to activate and will display a Common item for a short time. " +
                    $"Purchase again to buy two copies, or wait for it to turn to scrap.");
            }
            if (chanceShrine != null)
            {
                chanceShrine.cost = chanceShrineTypeCost;
                chanceShrineSandy.cost = chanceShrineTypeCost;
                chanceShrineSnowy.cost = chanceShrineTypeCost;
            }
        }
        #endregion

        #region void cradles
        #endregion

        #region Stage Credits
        public float interactableCreditsMultiplier = 1.5f;
        public float monsterCreditsMultiplier = 1.5f;
        public void IncreaseStageInteractableCredits(DirectorAPI.StageSettings settings, DirectorAPI.StageInfo currentStage)
        {
            settings.SceneDirectorInteractableCredits = (int)(settings.SceneDirectorInteractableCredits * interactableCreditsMultiplier);
        }
        public void IncreaseStageMonsterCredits(DirectorAPI.StageSettings settings, DirectorAPI.StageInfo currentStage)
        {
            settings.SceneDirectorMonsterCredits = (int)(settings.SceneDirectorMonsterCredits * monsterCreditsMultiplier);
        }
        #endregion

        #region interactable shit
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
        #endregion

        #region equipment barrels
        public int equipBarrelWeightS1 = 20;//2
        public int equipBarrelLimitS1 = 5;//-1
        public int equipBarrelWeight = 6;//2
        public int equipBarrelLimit = 2;//-1
        public int equipShopWeightS3 = 20;//2
        public int equipShopLimitS3 = 4;//-1
        public int equipShopWeight = 4;//2
        public int equipShopLimit = 2;//-1
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
        #endregion

        #region scrappers
        public int scrapperWeight = 1000;//12
        public int scrapperLimit = 3;//-1

        public int doubleChestWeight = 15; //idk

        private void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
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

        #region printers
        public static GameObject whitePrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Duplicator");
        public static GameObject greenPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorLarge");
        public static GameObject redPrinter = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorMilitary");
        public static GameObject scrapper = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/Scrapper");

        public int printerGreenWeight = 20;//6
        public int printerGreenLimit = 4;//-1
        public int printerRedWeight = 4;//1
        public int printerRedLimit = 1;//-1
        public int printerRedWeightS5 = 1000;//1
        public int printerRedLimitS5 = 2;//-1
        private void PrinterScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
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
        private void PrinterOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
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

        #region lunar pods
        public int lunarPodWeightS1 = 20;//2
        public int lunarPodLimitS1 = 6;//-1
        public int lunarPodWeight = 6;//2
        public int lunarPodLimit = 2;//-1
        private void LunarPodOccurrenceHook(DccsPool pool, StageInfo currentStage)
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

        #region printer stage bools
        private bool OnPrinterStage(DirectorAPI.Stage stage)
        {
            return !OnScrapperStage(stage)
                || IsModdedPrinterStage(stage);//modded stages?
        }
        private bool OnScrapperStage(DirectorAPI.Stage stage)
        {
            return IsStageOne(stage)
                || IsStageThree(stage)
                || IsModdedScrapperStage(stage);//modded stages?
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
        private bool IsStageFive(DirectorAPI.Stage stage)
        {
            return stage == DirectorAPI.Stage.SkyMeadow
                || stage == DirectorAPI.Stage.HelminthHatchery;
        }
        private bool IsModdedPrinterStage(DirectorAPI.Stage stage)//this shit dont work im goin to bed
        {
            return stage == ParseInternalStageName("drybasin")
                || stage == ParseInternalStageName("slumberingsatellite");

        }
        private bool IsModdedScrapperStage(DirectorAPI.Stage stage)
        {
            return stage == ParseInternalStageName("FBLScene");
        }
        #endregion

        #region roulette chest rework

        BasicPickupDropTable doubleChestDropTable = Addressables.LoadAssetAsync<BasicPickupDropTable>("RoR2/Base/CasinoChest/dtCasinoChest.asset").WaitForCompletion();
        InteractableSpawnCard doubleChestSpawnCard = Addressables.LoadAssetAsync<InteractableSpawnCard>("RoR2/Base/CasinoChest/iscCasinoChest.asset").WaitForCompletion();
        public DirectorCard doubleChestDirectorCard;//MOVE THIS SOMEWHERE BETTER LATER :3
        public void DoubleChestHook()
        {
            ChangeDoubleChestDropTable();
            BuildDoubleChestDirectorCard();
            AddDoubleChestToStage1();
            AddDoubleChestSecrets();

            On.RoR2.RouletteChestController.Cycling.OnEnter += DoubleChestOnInteract;
            On.RoR2.RouletteChestController.GetPickupIndexForTime += DoubleChestScrap;
            On.RoR2.RouletteChestController.EjectPickupServer += DoubleChestDoubleLoot;            
        }
        private void AddDoubleChestSecrets()
        {
            //titanic plains 1
            Secrets.SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(-109, -100, 42));//doublechest
            Secrets.SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(133, -100, 29), 0.4f);//big chest maybe
            Secrets.SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(183, -92, -144));//doublechest //bonus mob
            //SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(139, -119, 194));//doublechest queatet
            //SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(64, -115, -264));//lunar pod? very stupid
            Secrets.SpawnSecret("golemplains", doubleChestSpawnCard, new Vector3(100, -155, -342), 0.4f);//doublechest, make chance based

            Vector3[] quartetSpots = new Vector3[5];
            quartetSpots[0] = new Vector3(139, -119, 194);
            quartetSpots[1] = new Vector3(156, -120, -196);
            quartetSpots[2] = new Vector3(152, -112, -222);
            quartetSpots[3] = new Vector3(120, -112, -209);
            quartetSpots[4] = new Vector3(89, -116, -192);
            Secrets.SpawnSemiRandom("golemplains", doubleChestSpawnCard, quartetSpots);

            //titanic plains 2
            Secrets.SpawnSecret("golemplains2", doubleChestSpawnCard, new Vector3(-33, 61, -57));//doublechest make this one a semirandom later
            Secrets.SpawnSecret("golemplains2", doubleChestSpawnCard, new Vector3(-77, 54, -102));//doublechest this too
            Secrets.SpawnSecret("golemplains2", doubleChestSpawnCard, new Vector3(-214, 42, -29), 0.8f);//doublechest
            Secrets.SpawnSecret("golemplains2", doubleChestSpawnCard, new Vector3(141, 60, -4), 0.4f);//doublechest
            Secrets.SpawnSecret("golemplains2", doubleChestSpawnCard, new Vector3(151, 14, -230));//doublechest

            //blackbeach 1
            Secrets.SpawnSecret("blackbeach", doubleChestSpawnCard, new Vector3(-23, -175, -387));//doublechest
            Secrets.SpawnSecret("blackbeach", doubleChestSpawnCard, new Vector3(93, -125, -299));//doublechest
            Secrets.SpawnSecret("blackbeach", doubleChestSpawnCard, new Vector3(31, -213, -120));//doublechest floor issue
            Secrets.SpawnSecret("blackbeach", doubleChestSpawnCard, new Vector3(-288, -16, -181), 0.3f);//doublechest
            Secrets.SpawnSecret("blackbeach", doubleChestSpawnCard, new Vector3(-337, -199, -230), 0.5f);//doublechest

            //blackbeach 2
            Secrets.SpawnSecret("blackbeach2", doubleChestSpawnCard, new Vector3(-101, 28, 11), 0.8f);//doublechest floor issue
            Secrets.SpawnSecret("blackbeach2", doubleChestSpawnCard, new Vector3(-134, 47, -103), 0.4f);//doublechest
            Secrets.SpawnSecret("blackbeach2", doubleChestSpawnCard, new Vector3(12, 88, -126));//doublechest
            Secrets.SpawnSecret("blackbeach2", doubleChestSpawnCard, new Vector3(117, 65, 151));//doublechest floor issue

            //snowyforest
            Secrets.SpawnSecret("snowyforest", doubleChestSpawnCard, new Vector3(-252, 22, 57), 0.5f);//doublechest
            Secrets.SpawnSecret("snowyforest", doubleChestSpawnCard, new Vector3(24, 67, 2));//doublechest
            Secrets.SpawnSecret("snowyforest", doubleChestSpawnCard, new Vector3(-34, 70, -193));//doublechest
            Secrets.SpawnSecret("snowyforest", doubleChestSpawnCard, new Vector3(38, 42, -27), 0.5f);//doublechest

            Vector3[] snowyForestSpots = new Vector3[3];
            snowyForestSpots[0] = new Vector3(136, 53, 191);
            snowyForestSpots[1] = new Vector3(92, 41, -32);
            snowyForestSpots[2] = new Vector3(110, 79, 19);
            Secrets.SpawnSemiRandom("snowyforest", doubleChestSpawnCard, snowyForestSpots);

            //ancientloft
            Secrets.SpawnSecret("ancientloft", doubleChestSpawnCard, new Vector3(165, 62, -31), 0.8f); //doublechest

            //wispgraveyard
            //SpawnSecret("wispgraveyard", doubleChestSpawnCard, new Vector3(46, 29, -62), 0.8f);
            Secrets.SpawnSecret("wispgraveyard", doubleChestSpawnCard, new Vector3(-22, 59, 286));//didnt spawn idk why

            //Vector3[] wispGraveyardSpots = new Vector3[4];
            //wispGraveyardSpots[0] = new Vector3(-412, 6, -20);
            //wispGraveyardSpots[1] = new Vector3(-418, 6, -67);
            //wispGraveyardSpots[2] = new Vector3(-383, 6, -102);
            //wispGraveyardSpots[3] = new Vector3(-421, 6, -39);
            //SpawnSemiRandom("wispgraveyard", doubleChestSpawnCard, wispGraveyardSpots);

            //frozenwall
            Secrets.SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(87, 82, -250), 0.5f);
            Secrets.SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(-104, 35, 49));
            //SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(-139, 50, 7)); idk :3
            //SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(0, 34, 5));
            Secrets.SpawnSecret("frozenwall", doubleChestSpawnCard, new Vector3(196, 25, 32));//DOESNT ALWAYS SPAWN


            //sulfurpools
            Secrets.SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(11, -19, 37));
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(9, -7, -51), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(-155, 27, 46), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(176, 28, 45), 0.5f);
            //SpawnSecret("sulfurpools", doubleChestSpawnCard, new Vector3(94, 22, -133), 0.5f);


        }
        private void BuildDoubleChestDirectorCard()
        {
            doubleChestDirectorCard = DirectorCards.BuildDirectorCard(doubleChestSpawnCard, doubleChestWeight, 0);
        }

        private void AddDoubleChestToStage1()
        {
            DirectorAPI.Helpers.AddNewInteractableToStage(doubleChestDirectorCard, DirectorAPI.InteractableCategory.Chests, DirectorAPI.Stage.TitanicPlains);
            DirectorAPI.Helpers.AddNewInteractableToStage(doubleChestDirectorCard, DirectorAPI.InteractableCategory.Chests, DirectorAPI.Stage.DistantRoost);
            DirectorAPI.Helpers.AddNewInteractableToStage(doubleChestDirectorCard, DirectorAPI.InteractableCategory.Chests, DirectorAPI.Stage.SiphonedForest);
        }

        private void DoubleChestDoubleLoot(On.RoR2.RouletteChestController.orig_EjectPickupServer orig, RouletteChestController self, PickupIndex pickupIndex)
        {
            orig(self, pickupIndex);
            if (pickupIndex == PickupIndex.none)
            {
                return;
            }
            PickupDropletController.CreatePickupDroplet(pickupIndex, self.ejectionTransform.position, self.ejectionTransform.rotation * (self.localEjectionVelocity + new Vector3(2, 0, 0)));
        }

        private PickupIndex DoubleChestScrap(On.RoR2.RouletteChestController.orig_GetPickupIndexForTime orig, RouletteChestController self, Run.FixedTimeStamp time)
        {
            float threshHold = 5;
            bool isFirstItem;

            isFirstItem = (threshHold > (self.bonusTime));

            if (!isFirstItem)
            {
                return PickupCatalog.FindPickupIndex(RoR2Content.Items.ScrapWhite.itemIndex);
            }
            self.bonusTime += 0.01f;
            return orig(self, time);
        }

        private void ChangeDoubleChestDropTable()
        {
            if (doubleChestDropTable == null)
            {
                Debug.Log("droptable null uhhh");
                return;
            }
            doubleChestDropTable.tier1Weight = 1;
            doubleChestDropTable.tier2Weight = 0;
            doubleChestDropTable.tier3Weight = 0;
            doubleChestDropTable.equipmentWeight = 0;
        }

        private void DoubleChestOnInteract(On.RoR2.RouletteChestController.Cycling.orig_OnEnter orig, EntityStates.EntityState self)
        {
            RouletteChestController chestController = self.gameObject.GetComponent<RouletteChestController>();
            //chestController.dropTable = RoR2.MultiShopController.drop
            chestController.maxEntries = 2;
            chestController.bonusTime = 3;

            orig(self);
            
            if (chestController == null)
            {
                Debug.Log("auuuuuh fuck :3");
                return;
            }
            PurchaseInteraction purchaseInteraction = chestController.purchaseInteraction;
            if (purchaseInteraction == null)
            {
                Debug.Log("purchase interaction null 3:");
                return;
            }
            purchaseInteraction.costType = CostTypeIndex.Money;
            purchaseInteraction.cost = Run.instance.GetDifficultyScaledCost(casinoChestTypeCost, RoR2.Stage.instance.entryDifficultyCoefficient);
        }


        #endregion

        #region hacking criteria
        void ChangeHackingCriteria()
        {
            On.EntityStates.CaptainSupplyDrop.HackingMainState.PurchaseInteractionIsValidTarget += BlacklistGoldChest;
        }

        private bool BlacklistGoldChest(HackingMainState.orig_PurchaseInteractionIsValidTarget orig, PurchaseInteraction purchaseInteraction)
        {
            if (purchaseInteraction.displayNameToken == "GOLDCHEST_NAME")
                return false;
            return orig(purchaseInteraction);
        }
        #endregion

        #region halcyonite shrine
        public static int halcyoniteShrineLowGoldCost = 40;//75
        public static int halcyoniteShrineMidGoldCost = 100;//150
        public static int halcyoniteShrineMaxGoldCost = 150;//300


        void ChangeHalcyoniteShrineGoldRequirements()
        {
            GameObject halcyoniteShrinePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineHalcyonite.prefab").WaitForCompletion();
            if (halcyoniteShrinePrefab)
            {
                HalcyoniteShrineInteractable hsi = halcyoniteShrinePrefab.GetComponent<HalcyoniteShrineInteractable>();
                if (hsi)
                {
                    hsi.lowGoldCost = halcyoniteShrineLowGoldCost;
                    hsi.midGoldCost = halcyoniteShrineMidGoldCost;
                    hsi.maxGoldCost = halcyoniteShrineMaxGoldCost;
                }
            }
        }
        #endregion

        #region soul shrine / shrine of shaping

        public static int soulShrineLuckIncrease = 1;
        GameObject soulShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineColossusAccess.prefab").WaitForCompletion();
        void ReworkSoulShrine()
        {
            //if(soulShrine != null)
            //{
            //    PurchaseInteraction pi = soulShrine.GetComponent<PurchaseInteraction>();
            //
            //}

            IL.RoR2.ShrineColossusAccessBehavior.ReviveAlliedPlayers += SoulShrineLuckBuff;
            LanguageAPI.Add("SHRINE_COLOSSUS_DESCRIPTION",
                "An offering of Soul reduces all living Survivors' health by 30%, but revives all dead Survivors and gives +1 extra Luck to all living Survivors.");
        }

        private void SoulShrineLuckBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld("RoR2.DLC2Content/Buffs", "ExtraLifeBuff")
                );
            c.Remove();
            c.Remove();
            //c.Emit(OpCodes.Ldsfld, CoreModules.Assets.soulShrineLuckBuff);
            c.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                body.AddBuff(CoreModules.Assets.soulShrineLuckBuff);
            });
        }
        #endregion
    }
}