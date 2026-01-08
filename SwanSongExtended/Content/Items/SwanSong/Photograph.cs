using On.RoR2.Items;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class Photograph : ItemBase<Photograph>
    {
        public static BuffDef photographCritBuff;
        public static float photographCritFreeBase = 0f;
        public static float photographCritFreeStack = 0f;
        public static float photographCritBase = 15f;
        public static float photographCritStack = 10f;
        public static int photographMaxPrintsBase = 2;
        public static int photographMaxPrintsStack = 1;
        public override string ItemName => "Photograph";

        public override string ItemLangTokenName => "PHOTOGRAPH";

        public override string ItemPickupDesc => "Printing items increases critical strike chance and damage. Resets at the start of each stage.";

        public override string ItemFullDescription => $"Spending items at any printer increases " +
            $"{DamageColor("critical strike chance")} and {DamageColor("critical strike damage")} " +
            $"by {DamageColor($"+{photographCritBase}%")} {StackText($"+{photographCritStack}%")}, " +
            $"up to {UtilityColor($"{photographMaxPrintsBase} times")} {StackText($"+{photographMaxPrintsStack}")}. " +
            $"Resets at the start of each stage.";

        public override string ItemLore => $"Did you get your photos printed?\n\n\"Bogos binted?\"\n\nHuh?\n\n\"Download GreenAlienHead\"";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.DevotionBlacklist, ItemTag.InteractableRelated };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }

        public override void Init()
        {
            photographCritBuff = Content.CreateAndAddBuff(
                "bdPhotographCrit",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.magenta,
                true,
                false,
                BuffDef.StackingDisplayMethod.Percentage
                );
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase += PhotographOnNonMoneyPurchase;
            GetStatCoefficients += PhotographCritBonus;
        }

        private void PhotographCritBonus(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.critAdd += photographCritFreeBase + (photographCritFreeStack * (itemCount - 1));
                args.critDamageMultAdd += photographCritFreeBase + (photographCritFreeStack * (itemCount - 1));
            }
            int buffCount = sender.GetBuffCount(photographCritBuff);
            if (buffCount > 0)
            {
                args.critAdd += buffCount;
                args.critDamageMultAdd += buffCount * 0.01f;
            }
        }

        private void PhotographOnNonMoneyPurchase(MultiShopCardUtils.orig_OnNonMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            PhotographOnPrinterPurchase(context);
            orig(context);
        }

        private void PhotographOnPrinterPurchase(CostTypeDef.PayCostContext context)
        {
            if (context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.WhiteItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.GreenItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.RedItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.BossItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment)
                    )
                return;

            int itemCount = GetCount(context.activatorInventory);
            if (itemCount <= 0)
                return;

            float critBonus = photographCritBase + (photographCritFreeStack * (itemCount - 1));
            int maxTimes = photographMaxPrintsBase + (photographMaxPrintsStack * (itemCount - 1));
            int maxBuff = maxTimes * Mathf.FloorToInt(critBonus);
            int buffCount = context.activatorBody.GetBuffCount(photographCritBuff);
            if (buffCount >= maxBuff)
                return;

            for (int i = 0; i < critBonus; i++)
            {
                context.activatorBody.AddBuff(photographCritBuff);
            }
        }
    }
}