using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class TargetRandomNearbyHelper : ItemBase<TargetRandomNearbyHelper>
    {
        public override string ItemName => "Hidden Targeting Helper";

        public override string ItemLangTokenName => "HIDDENTARGETINGHELPER";

        public override string ItemPickupDesc => "Hidden Targeting Helper";

        public override string ItemFullDescription => "Hidden Targeting Helper";

        public override string ItemLore => "Hidden Targeting Helper";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { };

        public override GameObject ItemModel => null;

        public override Sprite ItemIcon => null;
        public override bool IsHidden => true;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            RoR2.Inventory.onInventoryChangedGlobal += OnInventoryChange;
        }

        private void OnInventoryChange(Inventory inv)
        {
            bool hasMask = inv.GetItemCount(RoR2Content.Items.GhostOnKill) > 0;
            bool hasHarpoon = inv.GetItemCount(RoR2Content.Items.GhostOnKill) > 0;
        }

        public override void Init(ConfigFile config)
        {

        }
    }
}
