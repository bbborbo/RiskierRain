using BepInEx.Configuration;
using R2API;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items.Helpers
{
    class GalleryItemDrop : ItemBase<GalleryItemDrop>
    {
        public override string ItemName => "Gallery Item Drop";

        public override string ItemLangTokenName => "GALLERY_ITEM_DROP";

        public override string ItemPickupDesc => "You shouldn't see this lol";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.CannotSteal };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += GalleryReward;
        }

        private void GalleryReward(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, RoR2.GlobalEventManager self, RoR2.DamageReport damageReport)
        {
            orig(self, damageReport);
            CharacterBody victim = damageReport.victimBody;
            if (victim.inventory?.GetItemCount(this.ItemsDef) > 0)
            {
                RollReward(victim);
            }
        }

        private void RollReward(CharacterBody body)
        {
            int i = UnityEngine.Random.RandomRangeInt(0, 9);
            if (i > 1)
            {
                Debug.Log("roll failed loser");
                return;
            }
            if (body == null)
            {
                Debug.Log("body null");
                return;
            }
            if (body.inventory == null)
            {
                Debug.Log("inv null??");
                return;
            }
            PickupIndex pickupIndex = PickupIndex.none;
            GenerateWeightedSelection(body.inventory);
            this.rng = new Xoroshiro128Plus(Run.instance.treasureRng.nextUlong);
            pickupIndex = PickupDropTable.GenerateDropFromWeightedSelection(rng, weightedSelection);
            dropletOrigin = body.gameObject.transform;
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, Vector3.zero);
        }
        private void GenerateWeightedSelection(Inventory inv)
        {
            weightedSelection = new WeightedSelection<PickupIndex>();
            foreach (ItemIndex itemIndex in inv.itemAcquisitionOrder)
            {
                ItemDef itemDef = ItemCatalog.GetItemDef(itemIndex);
                PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(itemIndex);
                if (this.CanSelectItem(itemDef))
                {
                    weightedSelection.AddChoice(pickupIndex, 0.9f);
                    Debug.Log("added an item!");
                }
            }
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex(DLC1Content.Items.RandomlyLunar.itemIndex), 0.1f);
            weightedSelection.AddChoice(PickupCatalog.FindPickupIndex("LunarCoin.Coin0"), 1);
        }


        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private bool CanSelectItem(ItemDef itemDef)
        {
            if (itemDef.tier == ItemTier.NoTier)
            {
                return false;
            }
            foreach (ItemTag value in this.requiredItemTags)
            {
                if (Array.IndexOf<ItemTag>(itemDef.tags, value) == -1)
                {
                    return false;
                }
            }
            foreach (ItemTag value2 in this.bannedItemTags)
            {
                if (Array.IndexOf<ItemTag>(itemDef.tags, value2) != -1)
                {
                    return false;
                }
            }
            return itemDef.canRemove;
        }
        public ItemTag[] requiredItemTags = Array.Empty<ItemTag>();
        public ItemTag[] bannedItemTags = Array.Empty<ItemTag>();

        WeightedSelection<PickupIndex> weightedSelection;
        private Xoroshiro128Plus rng;
        public Transform dropletOrigin;
    }
   
}
