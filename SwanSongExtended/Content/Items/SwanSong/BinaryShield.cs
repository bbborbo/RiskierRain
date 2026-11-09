using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;
using static RainrotSharedUtils.Styling;

namespace SwanSongExtended.Items
{
    public class BinaryShield : ItemBase<BinaryShield>
    {
        //public static float shieldPercentBase = 0.12f;
        //public static float rechargeRateIncrease = 1.5f;
        public static float baseShield = 25;
        public static float stackShield = 0;

        float moveSpeedIncreaseFree = 0.00f;
        float moveSpeedIncreaseBase = 0.19f;
        float moveSpeedIncreaseStack = 0.19f;
        //int armorIncreaseBase = 20;
        //int armorIncreaseStack = 30;

        public override string ItemName => "Plated Pereopod";

        public override string ItemLangTokenName => "BINARYSHIELD";

        public override string ItemPickupDesc => "Increase movement speed while shields are down.";

        public override string ItemFullDescription => $"Gain <style=cIsHealing>{baseShield} shield</style>. " +
            $"While shields are down, increase <style=cIsUtility>movement speed</style> by <style=cIsUtility>{(moveSpeedIncreaseBase + moveSpeedIncreaseFree).AsPercent()}</style> " +
            $"<style=cStack>(+{(moveSpeedIncreaseStack + moveSpeedIncreaseFree).AsPercent()} per stack)</style>. " +
            $"<style=cIsVoid>Corrupts all Paul's Goat Hoofs.</style>";

        public override string ItemLore => @"";

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
            if (itemCount <= 0)
                return;
            float speed = moveSpeedIncreaseFree * itemCount;

            HealthComponent hc = sender.healthComponent;
            if (hc != null)
            {
                if(hc.shield <= 0)
                    speed += GetStackValue(moveSpeedIncreaseBase, moveSpeedIncreaseStack, itemCount);
            }
            args.moveSpeedMultAdd += speed;
            args.baseShieldAdd += GetStackValue(baseShield, stackShield, itemCount);
        }
    }
}
