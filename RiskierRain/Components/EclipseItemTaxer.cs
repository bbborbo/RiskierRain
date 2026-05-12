using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Changes.Components
{
    public class EclipseItemTaxer : MonoBehaviour
    {
        public static ItemDef itemTaxResult => RoR2Content.Items.ExtraLifeConsumed;
        public CharacterMaster master;
        public int lastItemCount = 0;

        public void TaxItems()
        {
            int newItemCount = master.inventory.GetTotalItemCount() - master.inventory.GetTotalItemCountOfTier(ItemTier.NoTier);
            int itemCountGained = newItemCount - lastItemCount;
            lastItemCount = newItemCount;

            if (itemCountGained <= 0)
                return;

            int itemsToTax = Mathf.CeilToInt((float)itemCountGained * RiskierRain.Changes.DifficultyChanges.eclipseItemTaxPercent);//RiskierRainPlugin.eclipseItemTaxCount;//
            for (int i = 0; i < itemsToTax; i++)
            {
                if (master.inventory.TryTransformRandomItem(new Inventory.TryTransformRandomItemArgs
                {
                    forbidPermanent = false,
                    forbidTemporary = true,
                    filter = new Inventory.TryTransformRandomItemArgs.FilterDelegate(TransformationFilter),
                    rng = Run.instance.stageRng
                }, out Inventory.TryTransformRandomItemsResult result))
                {
                    CharacterMasterNotificationQueue.SendTransformNotification(
                        master,
                        result.originalItemIndex,
                        result.newItemIndex,
                        CharacterMasterNotificationQueue.TransformationType.Suppressed);
                }
            }
        }


        public static ItemIndex TransformationFilter(Inventory.TryTransformRandomItemArgs.FilterArgs args)
        {
            ItemDef itemDef = ItemCatalog.GetItemDef(args.itemIndex);
            if (itemDef.tier != ItemTier.NoTier && itemDef.canRemove)
            {
                return itemTaxResult.itemIndex;
            }
            return ItemIndex.None;
        }
    }
}
