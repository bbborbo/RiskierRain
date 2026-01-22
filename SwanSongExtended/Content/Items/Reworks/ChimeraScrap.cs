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
using static MoreStats.StatHooks;

namespace SwanSongExtended.Items
{
    class ChimeraScrap : ItemBase<ChimeraScrap>
    {
        public override AssetBundle assetBundle => SwanSongPlugin.retierAssetBundle;
        public static ItemDef usedItemDef;
        #region config

        public override string ConfigName => "Reworks : Regenerating Scrap";

        [AutoConfig("Common Item Credit", 10)]
        public static int regenScrapCommonCredit = 10;
        [AutoConfig("Uncommon Item Credit", 5)]
        public static int regenScrapUncommonCredit = 5;
        [AutoConfig("Rare Item Credit", 2)]
        public static int regenScrapRareCredit = 2;
        [AutoConfig("Boss Item Credit", 1)]
        public static int regenScrapBossCredit = 1;
        [AutoConfig("Should Chimera Scrap Overbuy", "If set to true, Chimera Scraps payment will round up in the case of large item costs ie Lunar Cauldrons", false)]
        public static bool shouldSuperScrapOverBuy = false;
        #endregion

        public override string ItemName => "Chimera Scrap";

        public override string ItemLangTokenName => "SUPERSCRAP";

        public override string ItemPickupDesc => $"Prioritized when used with {RedText("ANY")} 3D Printer. Creates extra items for lower tiers.";

        public override string ItemFullDescription => $"Does nothing. Prioritized when used with " +
                $"{RedText("ALL")} 3D Printers. Creates {StackColor("(")} " +
                $"{regenScrapCommonCredit} {StackColor("/")} " +
                $"{HealingColor(regenScrapUncommonCredit.ToString())} {StackColor("/")} " +
                $"{RedText(regenScrapRareCredit.ToString())} {StackColor(")")} items, " +
                $"{UtilityColor("depending on the quality of the printer")}.";

        public override string ItemLore => "<style=cMono>//--AUTO-TRANSCRIPTION FROM UES [Redacted] --//</style>\n\n\"Hey, Joe, how's the work in engineering?\"\n\n\"Terrible. We have a shipment of this... weird, prototype material. Some kind of metal? They want us to make stuff out of it, which isn't too bad. Thing is, no matter how much I take, there always seems to be more. Did you know I made twenty-five hundred units of .300 caliber rounds from a 10 kilo crate of metal?\"\n\n\"How much!?\"\n\n\"Right!? I feel like I'm losing my mind. It's not even half-way empty. Hell, I bet there's more in there than when I started!\"\n\n\"Well, at least you won't have to worry about running out...\"";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[]{ ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.InteractableRelated, ItemTag.PriorityScrap, ItemTag.Scrap, ItemTag.CannotDuplicate, ItemTag.CannotCopy };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/RegeneratingScrap/PickupRegeneratingScrap.prefab").WaitForCompletion();

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/Regenerating_Scrap.png");
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();
        public override bool CanBeTemporary => false;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.RegeneratingScrap);
            On.RoR2.CostTypeDef.IsAffordable += SuperScrapIsAffordable;
            On.RoR2.CostTypeDef.PayCost += SuperScrapPayCost;
            On.RoR2.CharacterMaster.TryRegenerateScrap += SuperScrapRegenerate;
            GetMoreStatCoefficients += ChimeraScrapCount;
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

        public override void Init()
        {
            SwanSongPlugin.RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_RegeneratingScrap.RegeneratingScrap_asset);// nameof(DLC1Content.Items.RegeneratingScrap));

            SwanSongPlugin.LoadAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_RegeneratingScrap.texRegeneratingScrapConsumedIcon_png, (icon) =>
            {
                usedItemDef = CreateNewUntieredItem("SUPERSCRAPUSED", icon, itemTags: ItemTags);
                DoLangForItem(usedItemDef, ItemName + " (Consumed)", "It's dead and not coming back.",
                    "It's dead and not coming back.");
            });
            base.Init();
        }

        private void SuperScrapRegenerate(On.RoR2.CharacterMaster.orig_TryRegenerateScrap orig, CharacterMaster self)
        {
            //You thought there would be something here?
        }

        private int GetSuperScrapPrinterCredit(ItemTier tier)
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

        private void SuperScrapPayCost2(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
        {
            orig(self, context, result);
        }
        private void SuperScrapPayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults result)
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
                    int regenScrapCount = activatorInventory.GetItemCountEffective(ChimeraScrap.instance.ItemsDef.itemIndex);
                    if (regenScrapCount > 0)
                    {
                        CostTypeDef.PayCostResults payCostResults = new CostTypeDef.PayCostResults();

                        Inventory.ItemTransformation.TryTransformResult tryTransformResult;
                        new Inventory.ItemTransformation
                        {
                            originalItemIndex = ItemsDef.itemIndex,
                            newItemIndex = usedItemDef.itemIndex,
                            maxToTransform = 1,
                            transformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Suppressed
                        }.TryTransform(activatorInventory, out tryTransformResult);

                        int printerCredit = GetSuperScrapPrinterCredit(self.itemTier);

                        if (cost > printerCredit)
                        {
                            activatorInventory.RemoveItemPermanent(ChimeraScrap.instance.ItemsDef.itemIndex, regenScrapCount - 1);
                            int remainder = cost - printerCredit;
                            context.cost = remainder;
                            orig(self, context, result);
                            activatorInventory.GiveItemPermanent(ChimeraScrap.instance.ItemsDef.itemIndex, regenScrapCount - 1);
                        }
                        else if (printerCredit > cost)
                        {
                            SuperScrapPaymentController sspc = purchasedObject.AddComponent<SuperScrapPaymentController>();

                            sspc.paymentCreditsRemaining = printerCredit - cost;
                        }
                        

                        Inventory.ItemAndStackValues stack = new Inventory.ItemAndStackValues();
                        stack.itemIndex = ChimeraScrap.instance.ItemsDef.itemIndex;
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

        private bool SuperScrapIsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (self.costStringFormatToken == "COST_ITEM_FORMAT" && activatorBody != null)
            {
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory)
                {
                    int regenScrapCount = activatorInventory.GetItemCountEffective(ChimeraScrap.instance.ItemsDef.itemIndex);
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
