﻿using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.ExpansionManagement;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class VoidIchorYellow : ItemBase<VoidIchorYellow>
    {
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        float regenBase = 0.8f;
        float regenStack = 0.8f;
        public override string ItemName => "Metamorphic Ichor (Yellow)";

        public override string ItemLangTokenName => "ICHORYELLOW";

        public override string ItemPickupDesc => $"Gain health regeneration. {VoidColor("Corrupts all Soldier's Syringes and Violet Ichors")}.";

        public override string ItemFullDescription => $"Increase {HealingColor("base health regeneration")} by " +
            $"{HealingColor($"{regenBase} hp/s")} {StackText($"+{regenStack} hp/s")}. " +
            $"{VoidColor("Corrupts all Soldier's Syringes and Violet Ichors")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorY.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_ICHORYELLOW.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
            RecalculateStatsAPI.GetStatCoefficients += IchorRegenBoost;
        }

        private void IchorRegenBoost(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            args.baseRegenAdd += regenBase + (regenStack * (itemCount - 1)) * (1 + sender.level * 0.2f);
        }
        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Syringe, //consumes syringe
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            ItemDef.Pair rockPaperScissors = new ItemDef.Pair()
            {
                itemDef1 = VoidIchorViolet.instance.ItemsDef, //:3
                itemDef2 = instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(rockPaperScissors);
            orig();
        }
    }
}
