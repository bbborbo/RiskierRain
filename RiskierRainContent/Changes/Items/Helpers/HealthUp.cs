﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;

namespace RiskierRainContent.Items.Helpers
{
    class HealthUp : ItemBase<HealthUp>
    {
        public override string ItemName => "HealthUp";

        public override string ItemLangTokenName => "HEALTHUP";

        public override string ItemPickupDesc => "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.CannotSteal };

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += ApplyStats;
        }

        private void ApplyStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.healthMultAdd += 0.1f * itemCount;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
