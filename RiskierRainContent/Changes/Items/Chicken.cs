using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace RiskierRainContent.Items
{
    class Chicken : ItemBase<Chicken>
    {
        public static BuffDef foodPoisoning;
        public static float baseMaxHealth = 0.3f;
        public static float stackMaxHealth = 0.3f;
        public static float baseRegenPenalty = 3f;
        public static float stackRegenPenalty = 3f;
        public static float regenPenaltyChance = 40f;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Raw Chicken";

        public override string ItemLangTokenName => "CHICKEN";

        public override string ItemPickupDesc => "Substantially increase max health, at risk of food poisoning.";

        public override string ItemFullDescription => $"Increase your <style=cIsHealing>maximum health</style> by " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(baseMaxHealth)}</style> <style=cStack>(+{Tools.ConvertDecimal(stackMaxHealth)} per stack)</style>. " +
            $"At the start of each stage, has a <style=cIsHealing>{regenPenaltyChance}%</style> chance to inflict " +
            $"<style=cIsHealing>food poisoning</style>, reducing your <style=cIsHealing>base health regeneration</style> " +
            $"by <style=cIsHealing>-{baseRegenPenalty} hp/s</style> (-{baseRegenPenalty} hp/s per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += ChickenStats;
            On.RoR2.CharacterBody.Start += DoFoodPoisoning;
        }

        private void DoFoodPoisoning(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            int chickenCount = GetCount(self);
            if(chickenCount > 0)
            {
                if(!Util.CheckRoll(100 - regenPenaltyChance, self.master))
                {
                    for(int i = 0; i < chickenCount; i++)
                    {
                        self.AddBuff(foodPoisoning.buffIndex);
                    }
                }
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

        public override void Init(ConfigFile config)
        {
            CreateBuff();
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void CreateBuff()
        {
            foodPoisoning = ScriptableObject.CreateInstance<BuffDef>();

            foodPoisoning.buffColor = Color.gray;
            foodPoisoning.isDebuff = true;
            foodPoisoning.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Croco/texBuffRegenBoostIcon.tif").WaitForCompletion();
            foodPoisoning.canStack = true;

            CoreModules.Assets.buffDefs.Add(foodPoisoning);
        }
    }
}
