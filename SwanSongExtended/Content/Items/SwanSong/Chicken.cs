using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
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
        public static float stackMaxHealth = 0.2f;
        public static float baseRegenPenalty = 5f;
        public static float stackRegenPenalty = 5f;
        public static float regenPenaltyChance = 40f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Raw Chicken";

        public override string ItemLangTokenName => "CHICKEN";

        public override string ItemPickupDesc => "Substantially increase max health, at risk of food poisoning.";

        public override string ItemFullDescription => $"Increase your <style=cIsHealing>maximum health</style> by " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(baseMaxHealth)}</style> <style=cStack>(+{Tools.ConvertDecimal(stackMaxHealth)} per stack)</style>. " +
            $"At the start of each stage, has a <style=cIsHealing>{regenPenaltyChance}%</style> chance to inflict " +
            $"<style=cIsHealing>food poisoning</style>, reducing your <style=cIsHealing>base health regeneration</style> " +
            $"by <style=cIsHealing>-{baseRegenPenalty} hp/s</style> <style=cStack>(-{baseRegenPenalty} hp/s per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

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
            //On.RoR2.CharacterBody.Start += DoFoodPoisoning;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    int itemCount = GetCount(self);

                    self.AddItemBehavior<ChickenBehavior>(itemCount);
                }
            }
        }

        private void DoFoodPoisoning(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            int chickenCount = GetCount(self);
            if(chickenCount > 0)
            {
            }
        }

        private void ChickenStats(CharacterBody sender, StatHookEventArgs args)
        {
            int chickenCount = GetCount(sender);
            if (chickenCount > 0)
                args.healthMultAdd += baseMaxHealth + stackMaxHealth * (chickenCount - 1);
            int poisonCount = sender.GetBuffCount(foodPoisoning);
            if (poisonCount > 0)
                args.baseRegenAdd -= (baseRegenPenalty + stackRegenPenalty * (poisonCount - 1)) * (1 + 0.2f * sender.level);
        }
    }
    public class ChickenBehavior : CharacterBody.ItemBehavior
    {
        private void Start()
        {
            if (body)
            {
                float regenPenaltyChance = Chicken.regenPenaltyChance;
                if (Run.instance.stageClearCount < 1)
                    regenPenaltyChance = Math.Min(0.01f, Chicken.regenPenaltyChance);
                else if (Run.instance.stageClearCount < 2)
                    regenPenaltyChance = Chicken.regenPenaltyChance / 2;

                if (!Util.CheckRoll(100 - regenPenaltyChance, body.master))
                {
                    for (int i = 0; i < this.stack; i++)
                    {
                        body.AddBuff(Chicken.foodPoisoning.buffIndex);
                    }
                }
            }
        }
    }
}
