using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class FlowerCrown : ItemBase<FlowerCrown>
    {
        public static float healthIncreaseBase = 0.12f;
        public static float healthIncreaseStack = 0.12f;

        public override string ItemName => "Flower Crown";

        public override string ItemLangTokenName => "FLOWERCROWN";

        public override string ItemPickupDesc => "Increases your maximum health.";

        public override string ItemFullDescription => $"Increases {HealingColor("maximum health")} by {HealingColor(healthIncreaseBase.AsPercent())} {StackText("+" + healthIncreaseStack.AsPercent())}.";

        public override string ItemLore => @"Order: Flower Crown
Tracking Number: 10***********
Estimated Delivery: 11/02/2056
Shipping Method:  Priority/Fragile
Shipping Address: Floor 16, Spiral Gardens, Earth
Shipping Details:

Thank you for always sending us gifts. I made some of them into this flower crown for you. Wearing it makes me feel strong, like nothing can get me down. Hopefully it does the same for you! <3
";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => LoadDropPrefab("mdlFlowerCrown");

        public override Sprite ItemIcon => LoadItemIcon("texIconFlowerCrown");


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            GetStatCoefficients += FlowerCrownStats;
        }

        private void FlowerCrownStats(CharacterBody sender, StatHookEventArgs args)
        {
            args.healthMultAdd += GetStackValue(healthIncreaseBase, healthIncreaseStack, GetCount(sender));
        }
    }
}
