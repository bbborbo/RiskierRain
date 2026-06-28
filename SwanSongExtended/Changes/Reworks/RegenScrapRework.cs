using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Components;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using System.Linq;

namespace SwanSongExtended
{
    public static class RegenScrapRework
    {
        public static ItemDef regenScrap;
        public static ItemDef regenScrapConsumed;
        public static int regenScrapCommonCredit = 10;
        public static int regenScrapUncommonCredit = 5;
        public static int regenScrapRareCredit = 2;
        public static int regenScrapBossCredit = 1;
        public static bool shouldSuperScrapOverBuy = false;
        public static void ReworkRegeneratingScrap()
        {
            SwanSongPlugin.LoadAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_RegeneratingScrap.RegeneratingScrap_asset, (itemDef) =>
            {
                regenScrap = itemDef;
                regenScrap.tags = new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.InteractableRelated, ItemTag.PriorityScrap, ItemTag.Scrap, ItemTag.CannotDuplicate, ItemTag.CannotCopy };

                LanguageAPI.Add(itemDef.pickupToken,
                    $"Prioritized when used with {RedText("ANY")} 3D Printer. Produces extra items of lower tiers.");
                LanguageAPI.Add(itemDef.descriptionToken, 
                    $"Does nothing. Prioritized when used with " +
                    $"{RedText("ANY")} 3D Printer. Creates {StackColor("(")} " +
                    $"{regenScrapCommonCredit} {StackColor("/")} " +
                    $"{HealingColor(regenScrapUncommonCredit.ToString())} {StackColor("/")} " +
                    $"{RedText(regenScrapRareCredit.ToString())} {StackColor(")")} items, " +
                    $"{UtilityColor("depending on the quality of the printer")}.");
            });
            SwanSongPlugin.LoadAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_RegeneratingScrap.RegeneratingScrapConsumed_asset, (itemDef) =>
            {
                regenScrapConsumed = itemDef;

                LanguageAPI.Add(itemDef.pickupToken,
                    "It has served its purpose to you.");
                LanguageAPI.Add(itemDef.descriptionToken, $"Does nothing. Prioritized when used with " +
                    "It has served its purpose to you.");
            });
            SwanSongPlugin.LoadAsync<CraftableDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Recipes.cdRegeneratingScrap_asset, (cdRegenScrap) =>
            {
                RecipeIngredient anyRed = new RecipeIngredient();
                anyRed.type = IngredientTypeIndex.AnyItem;
                anyRed.itemTier = ItemTier.Tier3;
                anyRed.forbiddenTags = new ItemTag[] { ItemTag.Scrap, ItemTag.PriorityScrap, ItemTag.WorldUnique, ItemTag.ObjectiveRelated };
                RecipeIngredient anyScrap = new RecipeIngredient();
                anyScrap.type = IngredientTypeIndex.AnyItem;
                anyScrap.requiredTags = new ItemTag[] { ItemTag.Scrap };
                RecipeIngredient anyPriorityScrap = new RecipeIngredient();
                anyPriorityScrap.type = IngredientTypeIndex.AnyItem;
                anyPriorityScrap.requiredTags = new ItemTag[] { ItemTag.PriorityScrap };

                Recipe newRecipe1 = new Recipe();
                newRecipe1.craftableDef = cdRegenScrap;
                newRecipe1.priority = -1;
                newRecipe1.ingredients = new RecipeIngredient[]
                {
                    anyRed,
                    anyScrap
                };
                Recipe newRecipe2 = new Recipe();
                newRecipe2.craftableDef = cdRegenScrap;
                newRecipe2.priority = -1;
                newRecipe2.ingredients = new RecipeIngredient[]
                {
                    anyRed,
                    anyPriorityScrap
                };
                cdRegenScrap.recipes = new Recipe[] { newRecipe1, newRecipe2 };
            });

            On.RoR2.CostTypeDef.IsAffordable += SuperScrapIsAffordable;
            On.RoR2.CostTypeDef.PayCost += SuperScrapPayCost;
            On.RoR2.CharacterMaster.TryRegenerateScrap += SuperScrapRegenerate;
        }

        public static void SuperScrapRegenerate(On.RoR2.CharacterMaster.orig_TryRegenerateScrap orig, CharacterMaster self)
        {
            //You thought there would be something here?
        }

        public static int GetSuperScrapPrinterCredit(ItemTier tier)
        {
            int printerCredit;
            switch (tier)
            {
                default:
                    printerCredit = 1;
                    break;
                case ItemTier.Tier1:
                    printerCredit = regenScrapCommonCredit;
                    break;
                case ItemTier.Tier2:
                    printerCredit = regenScrapUncommonCredit;
                    break;
                case ItemTier.Tier3:
                    printerCredit = regenScrapRareCredit;
                    break;
                case ItemTier.Boss:
                    printerCredit = regenScrapBossCredit;
                    break;
            }
            return printerCredit;
        }

        public static void SuperScrapPayCost2(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
        {
            orig(self, context, result);
        }
        public static void SuperScrapPayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
        {
            int cost = context.cost;
            Interactor activator = context.activator;
            GameObject purchasedObject = context.purchasedObject;

            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (self.costStringFormatToken == "COST_ITEM_FORMAT" && activatorBody != null && self.itemTier != ItemTier.Lunar)
            {
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory)
                {
                    int regenScrapCount = activatorInventory.GetItemCountEffective(regenScrap.itemIndex);
                    if (regenScrapCount > 0)
                    {
                        CostTypeDef.PayCostResults payCostResults = new CostTypeDef.PayCostResults();

                        Inventory.ItemTransformation.TryTransformResult tryTransformResult;
                        new Inventory.ItemTransformation
                        {
                            originalItemIndex = regenScrap.itemIndex,
                            newItemIndex = regenScrapConsumed.itemIndex,
                            maxToTransform = 1,
                            transformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Suppressed
                        }.TryTransform(activatorInventory, out tryTransformResult);

                        int printerCredit = GetSuperScrapPrinterCredit(self.itemTier);

                        if (cost > printerCredit)
                        {
                            activatorInventory.RemoveItemPermanent(regenScrap.itemIndex, regenScrapCount - 1);
                            int remainder = cost - printerCredit;
                            context.cost = remainder;
                            orig(self, context, result);
                            activatorInventory.GiveItemPermanent(regenScrap.itemIndex, regenScrapCount - 1);
                        }
                        else if (printerCredit > cost)
                        {
                            SuperScrapPaymentController sspc = purchasedObject.AddComponent<SuperScrapPaymentController>();

                            sspc.paymentCreditsRemaining = printerCredit - cost;
                        }


                        Inventory.ItemAndStackValues stack = new Inventory.ItemAndStackValues();
                        stack.itemIndex = regenScrap.itemIndex;
                        stack.stackValues = new Inventory.ItemStackValues();
                        stack.stackValues.permanentStacks = printerCredit;
                        stack.stackValues.totalStacks = printerCredit;

                        payCostResults._itemStacksTaken.Add(stack);

                        return;
                    }
                }
            }
            // this runs if only one of the other ifs are false
            orig(self, context, result);
        }

        public static bool SuperScrapIsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (self.costStringFormatToken == "COST_ITEM_FORMAT" && activatorBody != null)
            {
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory)
                {
                    int regenScrapCount = activatorInventory.GetItemCountEffective(regenScrap);
                    if (regenScrapCount > 0)
                    {
                        int printerCredits = GetSuperScrapPrinterCredit(self.itemTier) * regenScrapCount;
                        bool hasEnoughRegenScrap = printerCredits >= cost;
                        return (hasEnoughRegenScrap || activatorInventory.HasAtLeastXTotalItemsOfTier(self.itemTier, cost - printerCredits));
                    }
                }
            }
            // this runs if only one of the other ifs are false
            return orig(self, cost, activator);
        }
    }
}
