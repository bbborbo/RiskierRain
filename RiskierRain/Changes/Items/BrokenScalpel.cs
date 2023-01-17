using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class BrokenScalpel : ItemBase<BrokenScalpel>
    {
        public override string ItemName => "Broken Scalpel";

        public override string ItemLangTokenName => "BROKENSCALPEL";

        public override string ItemPickupDesc => "The blade has shattered into a hundred fragments.";

        public override string ItemFullDescription => "The blade has shattered...";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemTag[] ItemTags => new ItemTag[] { };

        public override BalanceCategory Category => BalanceCategory.None;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
