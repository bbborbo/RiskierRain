using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items.Helpers
{
    class Attackspeed : ItemBase<Attackspeed>
    {
        float attackSpeedBuff = 0.1f;
        public override string ItemName => "AttackspeedHelper";

        public override string ItemLangTokenName => "ATTACKSPEEDHELPER";

        public override string ItemPickupDesc => "";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.NoTier;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.CannotSteal };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += AttackSpeedUp;
        }

        private void AttackSpeedUp(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.attackSpeedMultAdd += attackSpeedBuff * itemCount;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateLang();
            CreateItem();
            Hooks();
        }
    }
}
