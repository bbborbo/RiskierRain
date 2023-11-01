using BepInEx.Configuration;
using R2API;
using RiskierRain.Changes.Components;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class ChimeraScrap : ItemBase<ChimeraScrap>
    {
        int regenScrapCommonCredit = 10;
        int regenScrapUncommonCredit = 5;
        int regenScrapRareCredit = 2;
        int regenScrapBossCredit = 1;
        public static bool shouldSuperScrapOverBuy = false;

        public override string ItemName => "Chimera Scrap";

        public override string ItemLangTokenName => "SUPERSCRAP";

        public override string ItemPickupDesc => "Prioritized when used with <style=cIsHealth>ANY</style> 3D Printer. Creates extra items for lower tiers.";

        public override string ItemFullDescription => $"Does nothing. Prioritized when used with " +
                $"<style=cIsHealth>ALL</style> 3D Printers. " +
                $"Creates <style=cStack>(</style>{regenScrapCommonCredit}<style=cStack> / " +
                $"<style=cIsHealing>{regenScrapUncommonCredit}</style> / " +
                $"<style=cIsHealth>{regenScrapRareCredit}</style>)</style> items, " +
                $"depending on the quality of the printer.";

        public override string ItemLore => "<style=cMono>//--AUTO-TRANSCRIPTION FROM UES [Redacted] --//</style>\n\n\"Hey, Joe, how's the work in engineering?\"\n\n\"Terrible. We have a shipment of this... weird, prototype material. Some kind of metal? They want us to make stuff out of it, which isn't too bad. Thing is, no matter how much I take, there always seems to be more. Did you know I made twenty-five hundred units of .300 caliber rounds from a 10 kilo crate of metal?\"\n\n\"How much!?\"\n\n\"Right!? I feel like I'm losing my mind. It's not even half-way empty. Hell, I bet there's more in there than when I started!\"\n\n\"Well, at least you won't have to worry about running out...\"";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[]{ ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.InteractableRelated, ItemTag.PriorityScrap };

        public override BalanceCategory Category => BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CostTypeDef.IsAffordable += SuperScrapIsAffordable;
            On.RoR2.CostTypeDef.PayCost += SuperScrapPayCost;
            On.RoR2.CharacterMaster.TryRegenerateScrap += SuperScrapRegenerate;
        }

        public override void Init(ConfigFile config)
        {
            RiskierRainPlugin.RetierItem(DLC1Content.Items.RegeneratingScrap);
            CreateItem();
            CreateLang();
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

        private CostTypeDef.PayCostResults SuperScrapPayCost(On.RoR2.CostTypeDef.orig_PayCost orig, CostTypeDef self, int cost, Interactor activator, GameObject purchasedObject, Xoroshiro128Plus rng, ItemIndex avoidedItemIndex)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (self.costStringFormatToken == "COST_ITEM_FORMAT" && activatorBody != null && self.itemTier != ItemTier.Lunar)
            {
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory)
                {
                    int regenScrapCount = activatorInventory.GetItemCount(ItemsDef.itemIndex);
                    if (regenScrapCount > 0)
                    {
                        CostTypeDef.PayCostResults payCostResults = new CostTypeDef.PayCostResults();

                        activatorInventory.RemoveItem(ItemsDef.itemIndex, 1);
                        activatorInventory.GiveItem(DLC1Content.Items.RegeneratingScrapConsumed, 1);
                        CharacterMasterNotificationQueue.SendTransformNotification(activatorBody.master,
                            ItemsDef.itemIndex, DLC1Content.Items.RegeneratingScrapConsumed.itemIndex,
                            CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);

                        int printerCredit = GetSuperScrapPrinterCredit(self.itemTier);
                        if (cost > printerCredit)
                        {
                            int remainder = cost - printerCredit;
                            payCostResults = orig(self, remainder, activator, purchasedObject, rng, avoidedItemIndex);
                        }
                        else if (printerCredit > cost)
                        {
                            SuperScrapPaymentController sspc = purchasedObject.AddComponent<SuperScrapPaymentController>();
                            sspc.paymentCreditsRemaining = printerCredit - cost;
                        }

                        int n = Mathf.Min(cost, printerCredit);
                        for (int i = 0; i < n; i++)
                        {
                            payCostResults.itemsTaken.Add(ItemsDef.itemIndex);
                        }

                        return payCostResults;
                    }
                }
            }
            // this runs if only one of the other ifs are false
            return orig(self, cost, activator, purchasedObject, rng, avoidedItemIndex);
        }

        private bool SuperScrapIsAffordable(On.RoR2.CostTypeDef.orig_IsAffordable orig, CostTypeDef self, int cost, Interactor activator)
        {
            CharacterBody activatorBody = activator.GetComponent<CharacterBody>();
            if (self.costStringFormatToken == "COST_ITEM_FORMAT" && activatorBody != null)
            {
                Inventory activatorInventory = activatorBody.inventory;
                if (activatorInventory)
                {
                    int regenScrapCount = activatorInventory.GetItemCount(ItemsDef.itemIndex);
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
