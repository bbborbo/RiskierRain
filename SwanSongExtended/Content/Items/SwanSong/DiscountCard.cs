using On.RoR2.Items;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class DiscountCard : ItemBase<DiscountCard>
    {
        public static ItemDef usedItemDef;
        public static ItemDef spentItemDef;
        public static int cashBackValue = 25;
        private static string fullDescPartial = $"When purchasing from {UtilityColor("multishop")} terminals, " +
            $"the other terminals {UtilityColor("stay open")}, refunding {UtilityColor($"${cashBackValue}")}.";
        public override string ItemName => "Discount Card (2-Punch)";

        public override string ItemLangTokenName => "DISCOUNTCARDNEW";

        public override string ItemPickupDesc => "Gain cash back on shop purchases, up to two times.";

        public override string ItemFullDescription => fullDescPartial + " Usable up to two times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.InteractableRelated, ItemTag.AIBlacklist };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            usedItemDef = CreateNewUntieredItem("DISCOUNTCARDUSED", Resources.Load<Sprite>("textures/miscicons/texWIPIcon"), itemTags: ItemTags);
            DoLangForItem(usedItemDef, "Discount Card (Used)", "Gain cash back on shop purchases. One use remaining.",
                fullDescPartial + " One use remaining.");
            spentItemDef = CreateNewUntieredItem("DISCOUNTCARDSPENT", Resources.Load<Sprite>("textures/miscicons/texWIPIcon"), itemTags: ItemTags);
            DoLangForItem(spentItemDef, "Discount Card (Spent)", "It's just a piece of paper with a bunch of holes.",
                "It's just a piece of paper with a bunch of holes.");
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.Items.MultiShopCardUtils.OnPurchase += DiscountCardOnPurchase;
        }

        private void DiscountCardOnPurchase(MultiShopCardUtils.orig_OnPurchase orig, CostTypeDef.PayCostContext context, int moneyCost)
        {
            orig(context, moneyCost);
			CharacterMaster activatorMaster = context.activatorMaster;
			if(activatorMaster && activatorMaster.hasBody && moneyCost > 0)
            {
				CharacterBody body = activatorMaster.GetBody();
				Inventory inventory = body.inventory;

				if (inventory)
                {
					ItemDef itemDefToUse = usedItemDef;
					ItemDef nextItemDef = spentItemDef;
					if(inventory.GetItemCount(itemDefToUse) <= 0)
                    {
						nextItemDef = itemDefToUse;
						itemDefToUse = instance.ItemsDef;
						if (inventory.GetItemCount(itemDefToUse) <= 0)
							return;
                    }

					bool usedCard = false;

					GameObject purchasedObject = context.purchasedObject;
					ShopTerminalBehavior shopTerminalBehavior = (purchasedObject != null) ? purchasedObject.GetComponent<ShopTerminalBehavior>() : null;
					PurchaseInteraction purchaseInteraction = purchasedObject.GetComponent<PurchaseInteraction>();
					if (shopTerminalBehavior && shopTerminalBehavior.serverMultiShopController && purchaseInteraction)
					{
                        //dont clsoe terminal
                        shopTerminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(purchaseInteraction, false);

                        //cash back
						GoldOrb goldOrb = new GoldOrb();
						Vector3? vector;
						if (purchasedObject == null)
						{
							vector = null;
						}
						else
						{
							Transform transform = purchasedObject.transform;
							vector = ((transform != null) ? new Vector3?(transform.position) : null);
						}
						goldOrb.origin = (vector ?? body.corePosition);
						goldOrb.target = body.mainHurtBox;
						goldOrb.goldAmount = GetCashBackValue();
						OrbManager.instance.AddOrb(goldOrb);

                        DegradeItem(activatorMaster, inventory, itemDefToUse, nextItemDef);
					}
				}
            }
		}

        public void DegradeItem(CharacterMaster master, Inventory inventory, ItemDef currentItem, ItemDef nextItem)
        {
            inventory.RemoveItem(currentItem, 1);
            inventory.GiveItem(nextItem, 1);

            CharacterMasterNotificationQueue.SendTransformNotification(
                master, currentItem.itemIndex,
                nextItem.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.Suppressed);
        }

        private uint GetCashBackValue()
        {
			return (uint)Run.instance.GetDifficultyScaledCost(cashBackValue, Stage.instance.entryDifficultyCoefficient);
		}
    }
}
