using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using RoR2.Items;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class Chicken : ItemBase<Chicken>
    {
        public static BuffDef foodPoisoning;
        public static float baseMaxHealth = 0.3f;
        public static float stackMaxHealth = 0.3f;
        public static float baseRegenPenalty = 2f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Raw Chicken";

        public override string ItemLangTokenName => "CHICKEN";

        public override string ItemPickupDesc => "Increases max health. Try to keep it down!";

        public override string ItemFullDescription => $"Increase your <style=cIsHealing>maximum health</style> by " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(baseMaxHealth)}</style> <style=cStack>(+{Tools.ConvertDecimal(stackMaxHealth)} per stack)</style>. " +
            $"<style=cIsHealing>Suffer food poisoning</style>, reducing your <style=cIsHealing>base health regeneration</style> " +
            $"by <style=cIsHealing>-{baseRegenPenalty} hp/s</style> for 120 seconds.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => LoadDropPrefab("mdlChicken");

        public override Sprite ItemIcon => LoadItemIcon("texIconChicken");
        public override bool CanBeTemporary => false;
        public override bool HalcyonShrineBias => false;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            foodPoisoning = Content.CreateAndAddBuff(
                "bdFoodPoisoning",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Croco/texBuffRegenBoostIcon.tif").WaitForCompletion(),
                Color.magenta,
                true, true);
            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += ChickenStats;
        }

        private void ChickenStats(CharacterBody sender, StatHookEventArgs args)
        {
            int chickenCount = GetCount(sender);
            if (chickenCount > 0)
                args.healthMultAdd += baseMaxHealth + stackMaxHealth * (chickenCount - 1);
            int poisonCount = sender.GetBuffCount(foodPoisoning);
            if (poisonCount > 0)
                args.baseRegenAdd -= (baseRegenPenalty * (poisonCount - 1)) * (1 + 0.2f * sender.level);
        }
    }
    public class RawChickenBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => Chicken.instance.ItemsDef;
        public int duration = 120;

        private void Start()
        {
            body.AddTimedBuff(Chicken.foodPoisoning, duration);
        }
    }
}
