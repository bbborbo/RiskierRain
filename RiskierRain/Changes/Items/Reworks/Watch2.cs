using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class Watch2 : ItemBase<Watch2>
    {
        public override string ItemName => "Delicate Wristwatch";

        public override string ItemLangTokenName => "WATCH2";

        public override string ItemPickupDesc => "Increase critical strike chance for a short time after being hit. Breaks on low health.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[]{ ItemTag.Damage };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => throw new NotImplementedException();

        public override Sprite ItemIcon => throw new NotImplementedException();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {

        }
    }
}
