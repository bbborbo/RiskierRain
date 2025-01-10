using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class GammaKnifeStatBoost : ItemBase<GammaKnifeStatBoost>
    {
        public override bool lockEnabled => true;
        public override string ConfigName => "";
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        public override bool IsHidden => false;
        public override string ItemName => "Fake-Soul Butter";

        public override string ItemLangTokenName => "GAMMAKNIFESTATBOOST";

        public override string ItemPickupDesc => "Cut the skin, and bend the truth...";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.CannotCopy, ItemTag.CannotDuplicate, ItemTag.CannotSteal, ItemTag.WorldUnique };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }
    }
}
