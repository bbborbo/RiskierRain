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
using static RainrotSharedUtils.Styling;
using System.Linq;
using static MoreStats.StatHooks;
using RainrotSharedUtils;

namespace SwanSongExtended.Changes
{
    public class ChimeraScrap : ReworkBase<ChimeraScrap>
    {
        public override string ConfigName => $"Reworks : {originalItemName} ({this.ItemName})" ;
        public const string originalItemName = "Regenerating Scrap";
        public static ItemDef regenScrap => ChimeraScrap.instance.itemDef;
        public static ItemDef regenScrapConsumed;
        public static int regenScrapCommonCredit = 10;
        public static int regenScrapUncommonCredit = 5;
        public static int regenScrapRareCredit = 2;
        public static int regenScrapBossCredit = 2;
        public static bool shouldSuperScrapOverBuy = false;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_RegeneratingScrap.RegeneratingScrap_asset;

        public override string ItemName => "Chimera Scrap";

        public override string ItemPickupDesc =>
            $"Prioritized when used with {RedText("ANY")} 3D Printer. Produces extra items of lower tiers.";

        public override string ItemFullDesc =>
            $"Does nothing. Prioritized when used with " +
            $"{RedText("ANY")} 3D Printer. Creates {StackColor("(")} " +
            $"{regenScrapCommonCredit} {StackColor("/")} " +
            $"{HealingColor(regenScrapUncommonCredit.ToString())} {StackColor("/")} " +
            $"{RedText(regenScrapRareCredit.ToString())} {StackColor(")")} items, " +
            $"{UtilityColor("depending on the quality of the printer")}.";

        public override void OnItemLoaded(ItemDef item)
        {
            base.OnItemLoaded(item);

            item.tags = new ItemTag[] { ItemTag.CannotDuplicate, ItemTag.InteractableRelated, ItemTag.PriorityScrap, ItemTag.Scrap, ItemTag.Utility, ItemTag.CommandArtifactBlacklist };

            item.tier = ItemTier.Tier3;
            item.deprecatedTier = ItemTier.Tier3;

            Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Regenerating_Scrap.png");
            if (sprite)
                itemDef.pickupIconSprite = sprite;
        }
        public override void Init()
        {
            base.Init();

            SwanSongPlugin.LoadAsync<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_RegeneratingScrap.RegeneratingScrapConsumed_asset, (itemDef) =>
            {
                regenScrapConsumed = itemDef;

                LanguageAPI.Add(itemDef.nameToken,
                    ItemName + " (Consumed)");
                LanguageAPI.Add(itemDef.pickupToken,
                    "Did you think it would come back?");
                LanguageAPI.Add(itemDef.descriptionToken,
                    "Did you think it would come back?");
            });
        }
        public override void PostInit()
        {
            base.PostInit();
            SwanSongPlugin.LoadAsync<CraftableDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Recipes.cdScrapRed_asset, (cdScrapRed) =>
            {
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Items_ExtraStatsOnLevelUp.ExtraStatsOnLevelUp_asset, 
                    out RecipeIngredient prayerBeads);
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Scrap.ScrapGreen_asset,
                    out RecipeIngredient greenScrap);

                cdScrapRed.recipes = new Recipe[] { CraftingUtils.MakeRecipe(prayerBeads, greenScrap) };
            });
            SwanSongPlugin.LoadAsync<CraftableDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Recipes.cdRegeneratingScrap_asset, (cdRegenScrap) =>
            {
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Infusion.Infusion_asset,
                    out RecipeIngredient infusion);
                CraftingUtils.LoadAsIngredient<ItemDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Scrap.ScrapYellow_asset,
                    out RecipeIngredient scrapYellow);
                cdRegenScrap.recipes = new Recipe[] { CraftingUtils.MakeRecipe(scrapYellow, scrapYellow) };
            });
        }
        public override void Hooks()
        {
            On.RoR2.CostTypeDef.IsAffordable += SuperScrapIsAffordable;
            On.RoR2.CostTypeDef.PayCost_PayCostContext_PayCostResults += SuperScrapPayCost;
            On.RoR2.CharacterMaster.TryRegenerateScrap += SuperScrapRegenerate;
            GetMoreStatCoefficients += ChimeraScrapCount;
        }
        private void SuperScrapPayCost(On.RoR2.CostTypeDef.orig_PayCost_PayCostContext_PayCostResults orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults payCostResults)
        {
            Inventory activatorInventory = context.activatorInventory;
            if(activatorInventory != null)
            {
                if(self.costTypeIndex == CostTypeIndex.WhiteItem || self.costTypeIndex == CostTypeIndex.GreenItem || self.costTypeIndex == CostTypeIndex.RedItem || self.costTypeIndex == CostTypeIndex.BossItem)
                {
                    int regenScrapCount = activatorInventory.GetItemCountEffective(regenScrap.itemIndex);
                    if (regenScrapCount > 0)
                    {
                        int cost = context.cost;
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
                            cost = remainder;
                            orig(self, context, payCostResults);
                            activatorInventory.GiveItemPermanent(regenScrap.itemIndex, regenScrapCount - 1);
                        }
                        else if (printerCredit > cost)
                        {
                            SuperScrapPaymentController sspc; 
                            if(context.purchasedObject.TryGetComponent(out sspc) == false)
                                sspc = context.purchasedObject.AddComponent<SuperScrapPaymentController>();

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
            orig(self, context, payCostResults);
        }
        private void ChimeraScrapCount(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.inventory)
            {
                args.scrapWhiteCountAdd += GetCount(sender) * regenScrapCommonCredit;
                args.scrapGreenCountAdd += GetCount(sender) * regenScrapUncommonCredit;
                args.scrapRedCountAdd += GetCount(sender) * regenScrapRareCredit;
                args.scrapYellowCountAdd += GetCount(sender) * regenScrapBossCredit;
            }
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
                        return (hasEnoughRegenScrap || activatorInventory.HasAtLeastXTotalRemovablePermanentItemsOfTier(self.itemTier, cost - printerCredits));
                    }
                }
            }
            // this runs if only one of the other ifs are false
            return orig(self, cost, activator);
        }
    }
}
