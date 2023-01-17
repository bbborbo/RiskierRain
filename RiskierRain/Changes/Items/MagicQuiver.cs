using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class MagicQuiver : ItemBase
    {
        BuffIndex utilityBeltLockout;

        float refundChargeChanceBase = 10;
        float refundChargeChanceStack = 10;
        float refundChanceCourtesy = 5;
        float endChanceMultiplier = 0.5f;

        public override string ItemName => "Magic Quiver";

        public override string ItemLangTokenName => "MAGICQUIVER";

        public override string ItemPickupDesc => "Using skills has a chance to not consume stock.";

        public override string ItemFullDescription => $"Grants a <style=cIsDamage>{refundChargeChanceBase}%</style> " +
            $"<style=cStack>(+{refundChargeChanceStack}% per stack)</style> chance to not consume a charge on skill cast. " +
            $"<style=cIsUtility>Unaffected by luck</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist };
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => LoadDropPrefab("Quiver");

        public override Sprite ItemIcon => LoadItemIcon("texIconQuiver");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            On.RoR2.BuffCatalog.Init += UtilityBeltBuffStuff;
            On.RoR2.CharacterBody.OnSkillActivated += OnSkillActivated;
            On.RoR2.SkillLocator.ApplyAmmoPack += ClearUtilityBeltDebuffOnBandolier;
        }

        private void UtilityBeltBuffStuff(On.RoR2.BuffCatalog.orig_Init orig)
        {
            orig();

            utilityBeltLockout = BuffCatalog.FindBuffIndex("UtilityBeltCooldown");

            if(Tools.isLoaded("com.TeamCloudburst.Cloudburst") == true)
            {
                //Cloudburst.Items.Green.MagiciansEarrings.blacklist.Add(utilityBeltLockout);
            }
            else
            {
                //flamethrower lang fix
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }

        private void ClearUtilityBeltDebuffOnBandolier(On.RoR2.SkillLocator.orig_ApplyAmmoPack orig, SkillLocator self)
        {
            orig(self);
            if (self.utility)
            {
                CharacterBody body = self.utility.characterBody;

                if(utilityBeltLockout != BuffIndex.None)
                    body.ClearTimedBuffs(utilityBeltLockout);
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

                        if (skill == self.skillLocator.utility)
                        {
                            self.ClearTimedBuffs(utilityBeltLockout);
                        }
                    }
                }
            }
        }
    }
}
