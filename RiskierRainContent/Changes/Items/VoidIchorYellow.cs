﻿using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class VoidIchorYellow : ItemBase<VoidIchorYellow>
    {
        float regenBase = .6f;
        float regenStack = .6f;
        public override string ItemName => "Metamorphic Ichor (Yellow)";

        public override string ItemLangTokenName => "ICHORYELLOW";

        public override string ItemPickupDesc => "Gain health regeneration. Corrupts all Soldier's Syringes.";

        public override string ItemFullDescription => "Gain health regeneration. Corrupts all Soldier's Syringes.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing};

        public override GameObject ItemModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorY.prefab");

        public override Sprite ItemIcon => CoreModules.Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_ICHORYELLOW.png");
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
            if(itemCount > 0)
                args.baseRegenAdd += regenBase + (regenStack * (itemCount - 1));
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();

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
