using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using On.RoR2.Items;
using RoR2.Orbs;
using RoR2.ExpansionManagement;
using UnityEngine.AddressableAssets;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class GreedyRing : ItemBase<GreedyRing>
    {
        public override bool isEnabled => true;
        public static BuffDef greedyRingBuff;
        public static float greedyDurationBase = 20;
        public static float greedyDurationStack = 0;
        public static float greedyMoneyBase = 0.15f;
        public static float greedyMoneyStack = 0.075f;
        public static float greedyRegenBase = 3f;
        public static float greedyRegenStack = 3f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Greedy Ring";

        public override string ItemLangTokenName => "BORBODISCOUNT";

        public override string ItemPickupDesc => $"Gain extra money and regeneration after opening chests.";

        public override string ItemFullDescription => $"After spending {DamageColor("money")}, gain a Rebate for {greedyDurationBase} seconds. " +
            $"While your Rebate is active, increases income by {greedyMoneyBase.AsPercent()} {StackText(greedyMoneyStack.AsPercent())} " +
            $"and base health regeneration by {HealingColor(greedyRegenBase + "hp/s")} {StackText(greedyRegenStack + "hp/s")}.";

        public override string ItemLore => @"Order: Mapel Coupon Getter (Lite)
Tracking Number: 06***********
Estimated Delivery: 11/04/2056
Shipping Method:  Priority
Shipping Address: 308, Belfast Station, Earth
Shipping Details:

Alright, here you are. As promised, it (legally of course) views the code on electronic purchases to find promo codes and discounts and apply them to the purchase, no user input needed! It’s very legal.
It may be a little scuffed in practice, since you need to have the full cost on hand and the coupon codes might not be the best ones offered. Legal reasons for that, naturally. Oh, and there’s a limit on how many purchases you can do- you gotta wait a bit for it to refresh if you wanna get those coupons.
Of course, you can always buy the premium version for unlimited discounts~
";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] 
            { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.OnStageBeginEffect, ItemTag.InteractableRelated };

        public override GameObject ItemModel => LoadDropPrefab("mdlGreedyRing");

        public override Sprite ItemIcon => LoadItemIcon("texIconPickupITEM_BORBODISCOUNT");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            greedyRingBuff = Content.CreateAndAddBuff(
                "bdGreedyCoupon",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.8f, 0.0f),
                true, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += GreedyRegen;
            MultiShopCardUtils.OnMoneyPurchase += GreedyRingRefund;
            On.RoR2.CharacterMaster.GiveMoney += GreedyRingRebate;
        }

        private void GreedyRingRebate(On.RoR2.CharacterMaster.orig_GiveMoney orig, CharacterMaster self, uint amount)
        {
            if (self.hasBody)
            {
                CharacterBody body = self.GetBody();
                if (body.HasBuff(greedyRingBuff))
                {
                    float amt = (float)amount * (1 + greedyMoneyBase);
                    amount = (uint)amt;
                    amt = amt - (float)amount;
                    if (Util.CheckRoll0To1(amt, self))
                        amount++;
                }
            }
            orig(self, amount);
        }

        private void GreedyRegen(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(greedyRingBuff);
            if (buffCount <= 0)
                return;

            int stack = GetCount(sender);
            if (stack <= 0)
                return;

            float regenBase = GetStackValue(greedyRegenBase, greedyRegenStack, stack);
            args.baseRegenAdd += regenBase;
            args.levelRegenAdd += regenBase * 0.2f;
        }

        private void GreedyRingRefund(MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            orig(context);
            CharacterMaster activatorMaster = context.activatorMaster;
            if (activatorMaster && activatorMaster.hasBody && context.cost > 0 && NetworkServer.active)
            {
                CharacterBody body = activatorMaster.GetBody();
                int stack = GetCount(body);
                if (stack > 0)
                {
                    float duration = GetStackValue(greedyDurationBase, greedyDurationStack, stack);
                    body.AddTimedBuff(greedyRingBuff, duration);
                }
            }
        }
    }
}
