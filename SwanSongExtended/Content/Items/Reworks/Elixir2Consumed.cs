using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
{
    class Elixir2Consumed : ItemBase<Elixir2Consumed>
    {
        public static float moveSpeedBuff => Elixir2.moveSpeedBuff;
        public static float attackSpeedBuff => Elixir2.attackSpeedBuff;
        public static float cooldownReduction => Elixir2.cooldownReduction;
        public override string ItemName => "Empty Flask";

        public override string ItemLangTokenName => "LEGALLYDISTINCTBOTTLE";

        public override string ItemPickupDesc => "An empty flask. You feel lightweight.";

        public override string ItemFullDescription => $"Increases attack speed by {Tools.ConvertDecimal(attackSpeedBuff)} (+{Tools.ConvertDecimal(attackSpeedBuff)} per stack), " +
            $"movement speed by {Tools.ConvertDecimal(moveSpeedBuff)} (+{Tools.ConvertDecimal(moveSpeedBuff)} per stack), " +
            $"and reduces cooldowns by {Tools.ConvertDecimal(cooldownReduction)} (-{Tools.ConvertDecimal(cooldownReduction)} per stack). ";

        public override string ItemLore => "Nothing to see here.";

        public override ItemTier Tier => ItemTier.NoTier;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");
        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/HealingPotion/texHealingPotionConsumed.png").WaitForCompletion();

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.OnStageBeginEffect };

        public override string ConfigName => "";

        public override AssetBundle assetBundle => null;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterMaster.OnServerStageBegin += TryRegenerateElixir;
            GetStatCoefficients += BerserkerBrewBuff;
            On.RoR2.CharacterBody.RecalculateStats += BerserkerBrewCdr;
        }

        private void BerserkerBrewCdr(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int stack = GetCount(self);
            if (stack > 0)
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = Mathf.Pow(1 - cooldownReduction, stack);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    Tools.ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.utility, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.special, cdrBoost);
                }
            }
        }

        private void BerserkerBrewBuff(CharacterBody sender, StatHookEventArgs args)
        {
            int stack = GetCount(sender);
            if(stack > 0)
            {
                args.attackSpeedMultAdd += attackSpeedBuff * stack;
                args.moveSpeedMultAdd += attackSpeedBuff * stack;
            }
        }

        private void TryRegenerateElixir(On.RoR2.CharacterMaster.orig_OnServerStageBegin orig, CharacterMaster self, Stage stage)
        {
            orig(self, stage);
            if (NetworkServer.active)
            {
                int count = GetCount(self);
                if (count > 0)
                {
                    TransformPotions(count, self);
                }
            }
        }
        private void TransformPotions(int count, CharacterMaster master)
        {
            Inventory inv = master.inventory;
            inv.RemoveItem(instance.ItemsDef, count);
            inv.GiveItem(Elixir2.instance.ItemsDef, count);

            CharacterMasterNotificationQueue.SendTransformNotification(
                master, instance.ItemsDef.itemIndex,
                Elixir2.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.RegeneratingScrapRegen);
        }
    }
}
