using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;

namespace SwanSongExtended.Items
{
    class FlowerCrown : ItemBase<FlowerCrown>
    {
        public static float shieldPercentBase = 0.12f;
        public static float rechargeRateIncrease = 1.5f;

        float moveSpeedIncreaseBase = 0.18f;
        float moveSpeedIncreaseStack = 0.18f;
        int armorIncreaseBase = 20;
        int armorIncreaseStack = 30;

        public override string ItemName => "Flower Crown";

        public override string ItemLangTokenName => "FLOWERCROWN";

        public override string ItemPickupDesc => "Increase shield recharge rate. Gain armor while shields are active, or gain movement speed.";

        public override string ItemFullDescription => $"Gain a <style=cIsHealing>shield</style> equal to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(shieldPercentBase)}</style> of your maximum health. " +
            $"Reduces <style=cIsUtility>shield recharge delay</style> " +
            $"by <style=cIsUtility>{rechargeRateIncrease}s</style>. " +
            $"While shields are active, increase " +
            $"<style=cIsHealing>armor</style> by <style=cIsHealing>{armorIncreaseBase}</style> " +
            $"<style=cStack>(+{armorIncreaseStack} per stack)</style>. " +
            $"While shields are down, increase <style=cIsUtility>movement speed</style> by <style=cIsHealing>{Tools.ConvertDecimal(moveSpeedIncreaseBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(moveSpeedIncreaseStack)} per stack)</style>. ";

        public override string ItemLore => @"Order: Flower Crown
Tracking Number: 10***********
Estimated Delivery: 11/02/2056
Shipping Method:  Priority/Fragile
Shipping Address: Floor 16, Spiral Gardens, Earth
Shipping Details:

Thank you for always sending us gifts. I made some of them into this flower crown for you. Wearing it makes me feel strong, like nothing can get me down. Hopefully it does the same for you! <3
";

        public override ItemTier Tier => ItemTier.Tier2;
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
            GetMoreStatCoefficients += FlowerCrownRecharge;
            GetStatCoefficients += FlowerCrownStats;
            //On.RoR2.CharacterBody.FixedUpdate += BatteryDelayReduction;
            //IL.RoR2.HealthComponent.ServerFixedUpdate += BatteryRechargeIncrease;
        }

        private void FlowerCrownRecharge(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.shieldDelaySecondsIncreaseAddPreMult -= rechargeRateIncrease;
            }
        }

        private void FlowerCrownStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                HealthComponent hc = sender.healthComponent;
                if (hc != null)
                {
                    args.baseShieldAdd += sender.maxHealth * shieldPercentBase;//(shieldPercentBase + (shieldPercentStack * (itemCount - 1)));

                    if (Fuse.HasShield(hc))
                    {
                        args.armorAdd += armorIncreaseBase + armorIncreaseStack * (itemCount - 1);
                    }
                    else
                    {
                        args.moveSpeedMultAdd += moveSpeedIncreaseBase + moveSpeedIncreaseStack * (itemCount - 1);
                    }
                }
            }
        }
    }
}
