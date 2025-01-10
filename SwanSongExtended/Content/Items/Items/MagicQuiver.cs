using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace SwanSongExtended.Items
{
    class MagicQuiver : ItemBase
    {
        float refundChargeChanceBase = 10;
        float refundChargeChanceStack = 10;
        float refundChanceCourtesy = 5;
        float endChanceMultiplier = 0.5f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;
        public override string ItemName => "Magic Quiver";

        public override string ItemLangTokenName => "MAGICQUIVER";

        public override string ItemPickupDesc => "Using skills has a chance to not consume stock.";

        public override string ItemFullDescription => $"Grants a <style=cIsDamage>{refundChargeChanceBase}%</style> " +
            $"<style=cStack>(+{refundChargeChanceStack}% per stack)</style> chance to not consume a charge on skill cast. " +
            $"<style=cIsUtility>Unaffected by luck</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override GameObject ItemModel => LoadDropPrefab("Quiver");

        public override Sprite ItemIcon => LoadItemIcon("texIconQuiver");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnSkillActivated += OnSkillActivated;
            On.RoR2.SkillLocator.ApplyAmmoPack += ClearUtilityBeltDebuffOnBandolier;
        }

        private void ClearUtilityBeltDebuffOnBandolier(On.RoR2.SkillLocator.orig_ApplyAmmoPack orig, SkillLocator self)
        {
            orig(self);
            if (self.utility)
            {
                CharacterBody body = self.utility.characterBody;
            }
        }

        private void OnSkillActivated(On.RoR2.CharacterBody.orig_OnSkillActivated orig, RoR2.CharacterBody self, RoR2.GenericSkill skill)
        {
            orig(self, skill);

            if (self.inventory != null && skill.CanApplyAmmoPack())
            {
                int quiverCount = self.inventory.GetItemCount(this.ItemsDef);
                if (quiverCount > 0)
                {
                    float totalRefundChance = refundChargeChanceBase + (refundChargeChanceStack * (quiverCount - 1)) + refundChanceCourtesy;
                    float endRefundChance = Util.ConvertAmplificationPercentageIntoReductionPercentage(totalRefundChance / endChanceMultiplier) * endChanceMultiplier;

                    if (Util.CheckRoll(endRefundChance, 0))
                    {
                        skill.AddOneStock();
                    }
                }
            }
        }
    }
}
