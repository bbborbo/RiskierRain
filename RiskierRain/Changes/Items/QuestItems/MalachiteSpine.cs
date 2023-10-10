using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class MalachiteSpine : ItemBase<MalachiteSpine>
    {
        public override string ItemName => "Malachite Spine";

        public override string ItemLangTokenName => "MALACHITESPINE";

        public override string ItemPickupDesc => "You feel a presence watching you...";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.Cleansable};

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlSpine.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            return;
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
