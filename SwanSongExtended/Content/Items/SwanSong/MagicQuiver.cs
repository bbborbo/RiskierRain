using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class MagicQuiver : ItemBase<MagicQuiver>
    {
        public static float refundChargeChanceBase = 10;
        public static float refundChargeChanceStack = 10;
        public static float refundChanceCourtesy = 5;
        public static float endChanceMultiplier = 0.5f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Magic Quiver";

        public override string ItemLangTokenName => "MAGICQUIVER";

        public override string ItemPickupDesc => "Using skills has a chance to not consume stock.";

        public override string ItemFullDescription => $"Grants a <style=cIsDamage>{refundChargeChanceBase}%</style> " +
            $"<style=cStack>(+{refundChargeChanceStack}% per stack)</style> chance to not consume a charge on skill cast. " +
            $"<style=cIsUtility>Unaffected by luck</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };

        public override GameObject ItemModel => LoadDropPrefab("mdlMagicQuiver");

        public override Sprite ItemIcon => LoadItemIcon("texIconMagicQuiver");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
        }

        public static void MagicQuiverRefund(CharacterBody self, GenericSkill skill, float multiplier = 1)
        {
            if (self.inventory != null && skill.CanApplyAmmoPack())
            {
                int quiverCount = self.inventory.GetItemCountEffective(MagicQuiver.instance.ItemsDef);
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

    public class MagicQuiverBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => MagicQuiver.instance.ItemsDef;

        void Start()
        {
            body.onSkillActivatedServer += OnSkillActivated;
        }
        void OnDestroy()
        {
            body.onSkillActivatedServer -= OnSkillActivated;
        }
        private void OnSkillActivated(GenericSkill skill)
        {
            if (skill.baseRechargeInterval > 0 && skill.rechargeStock > 0)
            {
                float effectiveCooldown = skill.baseRechargeInterval;
                if (skill.rechargeStock > 1)
                    effectiveCooldown /= skill.rechargeStock;

                MagicQuiver.MagicQuiverRefund(body, skill);
            }
        }
    }
}
