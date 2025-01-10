using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class VoidIgnitionTank : ItemBase<VoidIgnitionTank>
    {
        public override bool lockEnabled => true;
        public override bool isEnabled => false;
        public override string ItemName => "Supercoagulant Floater";

        public override string ItemLangTokenName => "VOIDIGNITIONTANK";

        public override string ItemPickupDesc => "Bleed lasts longer. Heavy hits inflict multiple stacks of bleed.";

        public override string ItemFullDescription => "later";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage};

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = DLC1Content.Items.StrengthenBurn, //consumes ignition tank
                itemDef2 = VoidIgnitionTank.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
    }
}
