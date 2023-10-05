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

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        GameObject awu = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/SuperRoboBallBossBody");
        CharacterBody awuBody;
        float awuArmor = 40;
        float awuAdditionalArmor = 0;
        int awuAdaptiveArmorCount = 1;

        float costExponent = 1.8f;


        PurchaseInteraction smallChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestDamage.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestHealing.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestUtility.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest2/Chest2.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction casinoChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CasinoChest/CasinoChest.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction chanceShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        //big category chest is 'categorychest2healing' and such


        MultiShopController smallShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShop/TripleShop.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController bigShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController equipmentShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab").WaitForCompletion().GetComponent<MultiShopController>();


        int smallChestTypeCost = 20; //25
        int smallShopTypeCost = 40; //25
        int smallCategoryChestTypeCost = 25; //30
        int bigChestTypeCost = 45; //50
        int bigShopTypeCost = 90; //50
        int bigCategoryChestTypeCost = 50; //60
        int goldChestTypeCost = 200; //400
        int bigDroneTypeCost = 160; //250
        int casinoChestTypeCost = 25; //50; make this 40 once they dont suck
        int chanceShrineTypeCost = 15; //17

        void FixMoneyScaling()
        {
            ChestCostScaling();
            ChestRebalance();
            TeleporterEnemyRewards();
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
                        adaptiveArmor.itemString = RoR2Content.Items.AdaptiveArmor.nameToken;

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

                float baseCost = lastChestBaseCost; //cost of a small chest
                float moneyTotal = baseCost * chestsPerHealthBar; //target money granted by the shrine
                float maxMulti = moneyTotal / teamMaxHealth; //express target money as a fraction of the max health of the team

                if (maxMulti > 0)//0.5f)
                    instance.goldToPaidHpRatio = maxMulti;
            }
        }
        public static int lastChestBaseCost = 20;
        private void GetChestCostForStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            lastChestBaseCost = Run.instance.GetDifficultyScaledCost(smallChestTypeCost);
            orig(self);
        }
        #endregion

        #region Economy
        private float teleporterEnemyRewardCoefficient = 0.4f;
        private void TeleporterEnemyRewards()
        {
            On.RoR2.TeleporterInteraction.Awake += ReduceTeleDirectorReward;
        }
        private void ReduceTeleDirectorReward(On.RoR2.TeleporterInteraction.orig_Awake orig, TeleporterInteraction self)
        {
            orig(self);
            if (self.bonusDirector)
            {
                self.bonusDirector.expRewardCoefficient *= teleporterEnemyRewardCoefficient;
            }
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


        private void EliteGoldReward()
        {
            On.RoR2.DeathRewards.Awake += FixEliteGoldReward;
        }
        private void FixEliteGoldReward(On.RoR2.DeathRewards.orig_Awake orig, RoR2.DeathRewards self)
        {
            orig(self);
            CharacterBody body = self.GetComponent<CharacterBody>();
            if (!body || !body.inventory) { return; }

            int bonusHealthCount = body.inventory.GetItemCount(RoR2Content.Items.BoostHp);
            if (bonusHealthCount > 0)
            {
                if (bonusHealthCount <= 70)
                {
                    //self.goldReward /= 0;
                }
                else if (bonusHealthCount <= 200)
                {
                    self.goldReward /= 3;
                }
                else
                {
                    self.goldReward /= 9;
                }
            }
        }

        private void ChestRebalance()
        {
            if(smallChest != null)
            {
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
                bigChest.cost = bigChestTypeCost;
            }
            if (bigShop != null)
            {
                bigShop.baseCost = bigShopTypeCost;
            }
            if (casinoChest != null)
            {
                casinoChest.cost = casinoChestTypeCost;
            }
            if (chanceShrine != null)
            {
                chanceShrine.cost = chanceShrineTypeCost;
            }

        }
        #endregion

        #region State of Difficulty
        public static float goldGainMultiplier = 0.08f;
        void FixMoneyAndExpRewards()
        {
            On.RoR2.DeathRewards.Awake += FixMoneyAndExpRewards;
        }

        private void FixMoneyAndExpRewards(On.RoR2.DeathRewards.orig_Awake orig, RoR2.DeathRewards self)
        {
            orig(self);
            float boost = GetAmbientLevelBoost();
            float ambientLevel = Run.instance.ambientLevel;
            // less than 1 allows for enemies to drop slightly more gold due to ALB
            // greater than 1 is kinda pointless but it overcorrects for ALB
            float ambientLevelBoostCorrection = 1f;

            float actualLevelStat = 1 + (0.3f * ambientLevel);
            float intendedLevelStat = 1 + (0.3f * (ambientLevel - boost * ambientLevelBoostCorrection));
            float rewardMult = intendedLevelStat / actualLevelStat;

            self.goldReward = (uint)((float)self.goldReward * rewardMult * goldGainMultiplier);
            self.expReward = (uint)((float)self.expReward * rewardMult);
        }
        #endregion

        #region void 
        GameObject voidCradlePrefab;
        public static float cradleHealthCost = 0.2f; //50
        static float _cradleHealthCost;
        void VoidCradleRework()
        {
            _cradleHealthCost = (1 / (1 - cradleHealthCost)) - 1;
            voidCradlePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidChest/VoidChest.prefab").WaitForCompletion();
            if (voidCradlePrefab)
            {
                PurchaseInteraction cradleInteraction = voidCradlePrefab.GetComponent<PurchaseInteraction>();
                if (cradleInteraction)
                {
                    cradleInteraction.cost = (int)(cradleHealthCost * 100);
                    cradleInteraction.setUnavailableOnTeleporterActivated = true;
                }
                voidCradlePrefab.AddComponent<InteractableCurseController>();
            }
            On.RoR2.CostTypeDef.PayCost += VoidCradlePayCostHook;
            GetStatCoefficients += VoidCradleCurse;
        }

        private void VoidCradleCurse(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(Assets.voidCradleCurse);
            if(buffCount > 0)
            {
                args.baseCurseAdd += _cradleHealthCost * buffCount;
            }
        }

        private CostTypeDef.PayCostResults VoidCradlePayCostHook(On.RoR2.CostTypeDef.orig_PayCost orig, 
            CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            if(purchasedObject.GetComponent<GenericDisplayNameProvider>()?.displayToken == "VOID_CHEST_NAME")
            {
                cost = 0;
            }
            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }
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
        public int equipBarrelLimitS1 = 4;//-1
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
        private void ScrapperOccurrenceHook(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            string scrapperName = DirectorAPI.Helpers.InteractableNames.Scrapper.ToLowerInvariant();//.ToLower();

            bool isPrinterStage = OnScrapperStage(currentStage.stage);
            //Debug.Log(currentStage.stage.ToString() + " Is Scrapper Stage: " + isPrinterStage);

            if (isPrinterStage)
            {
                ChangeInteractableWeightForPool(pool, scrapperName, scrapperWeight, scrapperLimit);
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
                if (currentStage.stage == DirectorAPI.Stage.SkyMeadow)
                    ChangeInteractableWeightForPool(pool, printerRed, printerRedWeightS5, printerRedLimitS5);
                else
                    ChangeInteractableWeightForPool(pool, printerRed, printerRedWeight, printerRedLimit);
            }
            else if (!currentStage.CheckStage(DirectorAPI.Stage.Custom, "") || IsModdedScrapperStage(currentStage.stage))
            {
                RemoveExistingInteractable(pool, printerWhite);
                RemoveExistingInteractable(pool, printerGreen);
                RemoveExistingInteractable(pool, printerRed);
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
    }
}