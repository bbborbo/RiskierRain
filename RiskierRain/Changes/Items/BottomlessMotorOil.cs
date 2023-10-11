using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class BottomlessMotorOil : ItemBase<BottomlessMotorOil>
    {
        public override string ItemName => "Motor Oil (Bottomless)"; // AKA Motor Spirit

        public override string ItemLangTokenName => "MOTORSPIRIT";

        public override string ItemPickupDesc => "Feel it spray through the lizard nostrils..."; //"DRINK THE FUCKEN GAS AND KILLETH!"

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

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
