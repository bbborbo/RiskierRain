using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class BrokenScalpel : ItemBase<BrokenScalpel>
    {
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Broken Scalpel";

        public override string ItemLangTokenName => "BROKENSCALPEL";

        public override string ItemPickupDesc => "The blade has shattered into a hundred fragments.";

        public override string ItemFullDescription => "The blade has shattered...";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlScalpelBroken.prefab");

        public override Sprite ItemIcon => CoreModules.Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BROKENSCALPEL.png");

        public override ItemTag[] ItemTags => new ItemTag[] { };

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
