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
    public class BinaryShield : ItemBase<BinaryShield>
    {
        //public static float shieldPercentBase = 0.12f;
        //public static float rechargeRateIncrease = 1.5f;
        public static float baseShield = 25;

        float moveSpeedIncreaseFree = 0.5f;
        float moveSpeedIncreaseBase = 0.14f;
        float moveSpeedIncreaseStack = 0.14f;
        //int armorIncreaseBase = 20;
        //int armorIncreaseStack = 30;

        public override string ItemName => "Binary Shield";

        public override string ItemLangTokenName => "BINARYSHIELD";

        public override string ItemPickupDesc => "Increase movement speed while shields are down.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style> <style=cStack>(+{baseShield} per stack)</style>. " +
            $"While shields are down, increase <style=cIsUtility>movement speed</style> by <style=cIsUtility>{Tools.ConvertDecimal(moveSpeedIncreaseBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(moveSpeedIncreaseStack)} per stack)</style>. " +
            $"<style=cIsVoid>Corrupts all Paul's Goat Hoofs.</style>";

        public override string ItemLore => @"Order: Flower Crown
Tracking Number: 10***********
Estimated Delivery: 11/02/2056
Shipping Method:  Priority/Fragile
Shipping Address: Floor 16, Spiral Gardens, Earth
Shipping Details:

Thank you for always sending us gifts. I made some of them into this flower crown for you. Wearing it makes me feel strong, like nothing can get me down. Hopefully it does the same for you! <3
";

        public override ItemTier Tier => ItemTier.VoidTier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

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
            int itemCount = GetCount(sender);
            float speed = moveSpeedIncreaseFree * itemCount;

            HealthComponent hc = sender.healthComponent;
            if (hc != null)
            {
                if(hc.shield <= 0)
                    speed += GetStackValue(moveSpeedIncreaseBase, moveSpeedIncreaseStack, itemCount);
            }
            args.moveSpeedMultAdd += speed;
            args.baseShieldAdd += baseShield * itemCount;
        }
    }
}
