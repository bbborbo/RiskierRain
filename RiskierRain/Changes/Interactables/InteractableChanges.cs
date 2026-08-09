using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2BepInExPack.GameAssetPaths.Version_1_39_0;
using UnityEngine.AddressableAssets;
using On.EntityStates.CaptainSupplyDrop;
using RainrotSharedUtils.Shelters;

namespace RiskierRain.Changes
{
    public static partial class InteractableChanges
    {
        public static void Initialize()
        {
            //ChangeHackingCriteria();
            DirectorAPI.InteractableActions += PrinterScrapperOccurrenceHook;
            //DirectorAPI.InteractableActions += PrinterOccurrenceHook;
            //DirectorAPI.InteractableActions += ScrapperOccurrenceHook;
            DirectorAPI.InteractableActions += EquipBarrelOccurrenceHook;
            DirectorAPI.InteractableActions += LunarPodOccurrenceHook;
            //NerfBazaarStuff();
            GoldShrineRework();
            //ReworkSoulShrine();
            BloodShrineRewardRework();
            ChangeHalcyoniteShrineGoldRequirements();

            //interactable gold costs
            ChestRebalance();
        }

        #region soul shrine / shrine of shaping

        public static int soulShrineLuckIncrease = 1;
        public static GameObject soulShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC2/ShrineColossusAccess.prefab").WaitForCompletion();
        public static void ReworkSoulShrine()
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

        private static void SoulShrineLuckBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdsfld("RoR2.DLC2Content/Buffs", "ExtraLifeBuff")
                );
            if (!b)
            {
                DebugBreakpoint(nameof(SoulShrineLuckBuff));
                return;
            }
            c.Remove();
            c.Remove();
            //c.Emit(OpCodes.Ldsfld, CoreModules.Assets.soulShrineLuckBuff);
            c.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                body.AddBuff(CoreModules.Assets.soulShrineLuckBuff);
            });
        }
        #endregion

        #region Gold Shrine
        public static GameObject goldShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineGoldshoresAccess/ShrineGoldshoresAccess.prefab").WaitForCompletion();
        public static int goldShrineCost = 5;
        public static void GoldShrineRework()
        {
            if (goldShrine == null)
            {
                Debug.Log("goldshrine null!! uh oh!!!!");
                return;
            }

            PurchaseInteraction goldShrineInteraction = goldShrine.GetComponent<PurchaseInteraction>();
            if (goldShrineInteraction == null)
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
        private static int bloodShrineBaseGoldReward = 25;
        private static int totalBloodGoldValue = 60; // amount of gold awarded for using the shrine all times
        private const float totalHealthFraction = 2.18f; // health bars
        private static float chestsPerHealthBar = 2; // number of chest costs awarded per health bar

        private static void BloodShrineRewardRework()
        {
            IL.RoR2.ShrineBloodBehavior.AddShrineStack += ShrineBloodReward;
            //On.RoR2.ShrineBloodBehavior.Start += ShrineBloodBehavior_Start;
        }

        private static void ShrineBloodReward(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            int rewardLoc = 1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<ShrineBloodBehavior>(nameof(ShrineBloodBehavior.goldToPaidHpRatio))
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out rewardLoc)
                );
            c.EmitDelegate<Func<uint>>(() =>
            {
                return (uint)Run.instance.GetDifficultyScaledCost(bloodShrineBaseGoldReward, RoR2.Stage.instance.entryDifficultyCoefficient);
            });
            c.Emit(OpCodes.Stloc, rewardLoc);
        }

        /// <summary>
        /// obsolete
        /// </summary>
        private static void ShrineBloodBehavior_Start(On.RoR2.ShrineBloodBehavior.orig_Start orig, ShrineBloodBehavior self)
        {
            orig(self);
            if (NetworkServer.active) self.StartCoroutine(WaitForPlayerBody(self));
        }

        /// <summary>
        /// obsolete
        /// </summary>
        static System.Collections.IEnumerator WaitForPlayerBody(ShrineBloodBehavior instance)
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

        #region hacking criteria
        private static void ChangeHackingCriteria()
        {
            On.EntityStates.CaptainSupplyDrop.HackingMainState.PurchaseInteractionIsValidTarget += BlacklistGoldChest;
        }

        private static bool BlacklistGoldChest(HackingMainState.orig_PurchaseInteractionIsValidTarget orig, PurchaseInteraction purchaseInteraction)
        {
            if (purchaseInteraction.displayNameToken == "GOLDCHEST_NAME")
                return false;
            return orig(purchaseInteraction);
        }
        #endregion

        #region halcyonite shrine
        public static int halcyoniteShrineLowGoldCost = 30;//75
        public static int halcyoniteShrineMidGoldCost = 60;//150
        public static int halcyoniteShrineMaxGoldCost = 90;//300
        public static float halcyoniteShrineRadius = 30;//30

        public static void ChangeHalcyoniteShrineGoldRequirements()
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

        public static void ShrineHalcyoniteShelterEnd(On.EntityStates.ShrineHalcyonite.ShrineHalcyoniteFinished.orig_OnEnter orig, EntityStates.ShrineHalcyonite.ShrineHalcyoniteFinished self)
        {
            orig(self);
            ShelterProviderBehavior shelter = self.gameObject.GetComponent<ShelterProviderBehavior>();
            if (shelter)
            {
                shelter.enabled = false;
            }
        }

        public static void ShrineHalcyoniteShelterStart(On.EntityStates.ShrineHalcyonite.ShrineHalcyoniteNoQuality.orig_OnEnter orig, EntityStates.ShrineHalcyonite.ShrineHalcyoniteNoQuality self)
        {
            orig(self);
            ShelterProviderBehavior shelter = self.gameObject.GetComponent<ShelterProviderBehavior>();
            if (shelter)
            {
                shelter.enabled = true;
            }
        }
        #endregion

        #region costs
        public static PurchaseInteraction smallChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest1/Chest1.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction smallCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestDamage.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction smallCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestHealing.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction smallCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CategoryChest/CategoryChestUtility.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction bigChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Chest2/Chest2.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction bigCategoryChestDamage = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Damage Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction bigCategoryChestHealing = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Healing Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction bigCategoryChestUtility = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/CategoryChest2/CategoryChest2Utility Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction casinoChest = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/CasinoChest/CasinoChest.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction chanceShrine = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChance.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction chanceShrineSnowy = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChanceSnowy Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        public static PurchaseInteraction chanceShrineSandy = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ShrineChance/ShrineChanceSandy Variant.prefab").WaitForCompletion().GetComponent<PurchaseInteraction>();
        //big category chest is 'categorychest2healing' and such


        public static MultiShopController smallShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShop/TripleShop.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        public static MultiShopController bigShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopLarge/TripleShopLarge.prefab").WaitForCompletion().GetComponent<MultiShopController>();
        public static MultiShopController equipmentShop = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/TripleShopEquipment/TripleShopEquipment.prefab").WaitForCompletion().GetComponent<MultiShopController>();

        public static string discountChestPrefix = "Bargain";
        public static int smallChestTypeCost = 20; //25
        public static int smallShopTypeCost = 35; //25
        public static int smallCategoryChestTypeCost = 25; //30
        public static int bigChestTypeCost = 45; //50
        public static int bigShopTypeCost = 70; //50
        public static int bigCategoryChestTypeCost = 50; //60
        public static int casinoChestTypeCost = 30; //50; cost is incurred twice
        public static int chanceShrineTypeCost = 15; //17
        public static int goldChestTypeCost = 250; //400
        public static int bigDroneTypeCost = 160; //250
        static float costExponent = 1.00f;

        private static void ChestRebalance()
        {
            On.RoR2.Run.GetDifficultyScaledCost_int_float += ChangeScaledCost;
            if (smallChest != null)
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
            if (chanceShrine != null)
            {
                chanceShrine.cost = chanceShrineTypeCost;
                chanceShrineSandy.cost = chanceShrineTypeCost;
                chanceShrineSnowy.cost = chanceShrineTypeCost;
            }
        }

        private static int ChangeScaledCost(On.RoR2.Run.orig_GetDifficultyScaledCost_int_float orig, RoR2.Run self, int baseCost, float difficultyCoefficient)
        {
            //this is hardcoded for force spawned interactables like the gold chest on abyssal
            //not gonna do it for the large chest on verdant falls though because $50 is a common price :/
            //im just cool with the modded collateral from these two ig
            //2r4r is no stranger to a little bit of collateral
            switch (baseCost)
            {
                //tc-280 drone
                case 350:
                    baseCost = InteractableChanges.bigDroneTypeCost;
                    break;
                //the gold chest on stage 4
                case 400:
                    baseCost = InteractableChanges.goldChestTypeCost;
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
        #endregion
    }
}
