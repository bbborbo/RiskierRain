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

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        GameObject awu = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/SuperRoboBallBossBody");
        CharacterBody awuBody;
        float awuArmor = 40;
        float awuAdditionalArmor = 0;
        int awuAdaptiveArmorCount = 1;

        float costExponent = 1.6f;
        float costConstant = 0.5f;

        float bonusGold = 1.2f;
        int goldChestTypeCost = 10;
        int bigDroneTypeCost = 8;

        PurchaseInteraction smallChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestDamage.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestHealing.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction smallCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestUtility.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        PurchaseInteraction bigChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest2/Chest2.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        //big category chest is 'categorychest2healing' and such


        MultiShopController smallShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShop/TripleShop.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController bigShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        MultiShopController equipmentShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab").WaitForCompletion().GetComponent<MultiShopController>();


        int smallChestTypeCost = 20; //25
        int smallShopTypeCost = 30; //25
        int smallCategoryChestTypeCost = 25; //30
        int bigChestTypeCost = 45; //50
        int bigShopTypeCost = 55; //50

        void FixMoneyScaling()
        {
            ChestCostScaling();
            ChestRebalance();
            TeleporterEnemyRewards();
        }

        private void BloodShrineRewardRework()
        {
            On.RoR2.ShrineBloodBehavior.Start += ShrineBloodBehavior_Start;
        }

        private void TeleporterEnemyRewards()
        {
            On.RoR2.TeleporterInteraction.Awake += ReduceTeleDirectorReward;
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

        private void EliteGoldReward()
        {
            On.RoR2.DeathRewards.Awake += FixEliteGoldReward;
        }

        #region Blood Shrines
        private static int teamMaxHealth;
        private const float totalHealthFraction = 2.18f; // health bars
        private static float chestAmount = 2; // chests per health bar
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
                float moneyTotal = baseCost * chestAmount; //target money granted by the shrine
                float maxMulti = moneyTotal / teamMaxHealth; //express target money as a fraction of the max health of the team

                if (maxMulti > 0)//0.5f)
                    instance.goldToPaidHpRatio = maxMulti;
            }
        }
        public static int lastChestBaseCost = 25;
        private void GetChestCostForStage(On.RoR2.Run.orig_BeginStage orig, Run self)
        {
            lastChestBaseCost = Run.instance.GetDifficultyScaledCost(25);
            orig(self);
        }
        #endregion

        #region Economy
        private float teleporterEnemyRewardCoefficient = 0.5f;
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
                    baseCost = 25 * goldChestTypeCost; //10, originally 16
                    break;
                case 14:
                    baseCost = 25 * bigDroneTypeCost; //8, originally 14
                    break;
            }

            float costMultiplierExponential = Mathf.Pow(difficultyCoefficient, costExponent);
            float costMultiplierLinear = (difficultyCoefficient * 2.5f - 1.5f);

            float endMultiplier = costMultiplierExponential;
            if (costMultiplierLinear < costMultiplierExponential)
            {
                //endMultiplier = costMultiplierLinear;
                //Debug.Log("Using Liner multiplier!");
            }

            return (int)((float)baseCost * endMultiplier);
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

        }

        #endregion

        #region State of Difficulty
        void FixMoneyAndExpRewards()
        {
            On.RoR2.DeathRewards.Awake += FixMoneyAndExpRewards;
        }

        private void FixMoneyAndExpRewards(On.RoR2.DeathRewards.orig_Awake orig, RoR2.DeathRewards self)
        {
            orig(self);
            float boost = GetAmbientLevelBoost();
            float ambientLevel = Run.instance.ambientLevel;
            float ambientLevelBoostCorrection = 1f;

            float actualLevelStat = 1 + (0.3f * ambientLevel);
            float intendedLevelStat = 1 + (0.3f * (ambientLevel - boost * ambientLevelBoostCorrection));
            float rewardMult = intendedLevelStat / actualLevelStat;

            self.goldReward = (uint)((float)self.expReward * rewardMult);
            self.expReward = (uint)((float)self.expReward * rewardMult);
        }
        #endregion

        #region void 
        GameObject voidCradlePrefab;
        int cradleHealthCost = 20; //50
        void VoidCradleRework()
        {
            voidCradlePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/VoidChest/VoidChest.prefab").WaitForCompletion();
            if (voidCradlePrefab)
            {
                PurchaseInteraction cradleInteraction = voidCradlePrefab.GetComponent<PurchaseInteraction>();
                if (cradleInteraction)
                {
                    cradleInteraction.cost = cradleHealthCost;
                    cradleInteraction.setUnavailableOnTeleporterActivated = true;
                }
            }
            On.RoR2.CostTypeDef.PayCost += VoidCradlePayCostHook;
            GetStatCoefficients += VoidCradleCurse;
        }

        private void VoidCradleCurse(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(Assets.voidCradleCurse);
            args.baseCurseAdd += 0.25f * buffCount;
        }

        private CostTypeDef.PayCostResults VoidCradlePayCostHook(On.RoR2.CostTypeDef.orig_PayCost orig, 
            CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {

            if(purchasedObject.GetComponent<GenericDisplayNameProvider>()?.displayToken == "VOID_CHEST_NAME")
            {
                CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
                if (activatorBody)
                {
                    activatorBody.AddBuff(Assets.voidCradleCurse);
                    cost = 0;
                }
            }
            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }
        #endregion

        #region Stage Credits
        public float interactableCreditsMultiplier = 1.5f;
        public void IncreaseStageInteractableCredits(DirectorAPI.StageSettings settings, DirectorAPI.StageInfo currentStage)
        {
            settings.SceneDirectorInteractableCredits = (int)(settings.SceneDirectorInteractableCredits * interactableCreditsMultiplier);
        }
        public float monsterCreditsMultiplier = 1.5f;
        public void IncreaseStageMonsterCredits(DirectorAPI.StageSettings settings, DirectorAPI.StageInfo currentStage)
        {
            settings.SceneDirectorMonsterCredits = (int)(settings.SceneDirectorMonsterCredits * monsterCreditsMultiplier);
        }
        #endregion
    }
}
