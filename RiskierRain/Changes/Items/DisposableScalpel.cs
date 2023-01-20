using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class DisposableScalpel : ItemBase<DisposableScalpel>
    {
        public static int bonusDropChance = 50;
        public override string ItemName => "Obsidian Scalpel";

        public override string ItemLangTokenName => "BOSSITEMCONSUMABLE";

        public override string ItemPickupDesc => "Powerful enemes have a much greater chance of dropping a trophy. Consumed on drop.";

        public override string ItemFullDescription => $"Every <style=cIsDamage>Champion</style> " +
            $"you kill has an <style=cIsUtility>additional {bonusDropChance}%</style> chance " +
            $"to drop a <style=cIsDamage>trophy</style>. " +
            $"Consumes <style=cIsUtility>1</style> stack when a Trophy drops.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnKillEffect, ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override BalanceCategory Category => BalanceCategory.StateOfDifficulty;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
        public static void ConsumeScalpel(CharacterBody attackerBody)
        {
            attackerBody.inventory.RemoveItem(DisposableScalpel.instance.ItemsDef);
            attackerBody.inventory.GiveItem(BrokenScalpel.instance.ItemsDef);
            CharacterMasterNotificationQueue.PushItemTransformNotification(attackerBody.master,
                DisposableScalpel.instance.ItemsDef.itemIndex, BrokenScalpel.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.Default);
        }
    }
}
