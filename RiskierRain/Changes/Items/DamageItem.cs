using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class DamageItem : ItemBase
    {
        public static float damageIncreaseBase = 0.09f;
        public static float damageIncreaseStack = 0.09f;
        public override string ItemName => "Enchanted Whetstone"; //malware stick

        public override string ItemLangTokenName => "BORBO_DAMAGEITEM";

        public override string ItemPickupDesc => "Increase the damage you deal.";

        public override string ItemFullDescription => $"Increase your <style=cIsDamage>base damage</style> " +
            $"by <style=cIsDamage>{Tools.ConvertDecimal(damageIncreaseBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(damageIncreaseStack)} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Damage };
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfDamage;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += this.GiveBonusDamage;
        }

        private void GiveBonusDamage(CharacterBody sender, StatHookEventArgs args)
        {
            int count = GetCount(sender);
            if (count > 0)
            {
                args.damageMultAdd += damageIncreaseBase + (damageIncreaseStack * (count - 1));
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
