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
    class FlowerCrown : ItemBase<FlowerCrown>
    {
        float maxHealthIncreaseBase = 0.15f;
        float maxHealthIncreaseStack = 0.1f;
        float maxShieldIncreaseBase = 0.15f;
        float maxShieldIncreaseStack = 0.03f;
        float regenIncreaseBase = 1f;
        float regenIncreaseStack = 1f;
        float moveSpeedIncreaseBase = 0.1f;
        float moveSpeedIncreaseStack = 0.1f;

        public override string ItemName => "Flower Crown";

        public override string ItemLangTokenName => "FLOWERCROWN";

        public override string ItemPickupDesc => "Gain a boost to ALL health stats.";

        public override string ItemFullDescription => $"Increases your <style=cIsHealing>maximum health</style> by <style=cIsHealing>{Tools.ConvertDecimal(maxHealthIncreaseBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(maxHealthIncreaseStack)} per stack)</style>, " +
            $"<style=cIsHealing>maximum shield</style> by <style=cIsHealing>{Tools.ConvertDecimal(maxShieldIncreaseBase)}</style> of max health " +
            $"<style=cStack>(+{Tools.ConvertDecimal(maxShieldIncreaseStack)} per stack)</style>, " +
            $"<style=cIsHealing>base health regeneration</style> by <style=cIsHealing>{regenIncreaseBase} hp/s</style> " +
            $"<style=cStack>(+{regenIncreaseStack} hp/s per stack)</style>, " +
            $"and <style=cIsHealing>movement speed</style> by <style=cIsHealing>{Tools.ConvertDecimal(moveSpeedIncreaseBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(moveSpeedIncreaseStack)} per stack)</style>. ";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfHealth;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return IDR;
        }

        public override void Hooks()
        {
            GetStatCoefficients += FlowerCrownStats;
        }

        private void FlowerCrownStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                args.healthMultAdd += maxHealthIncreaseBase + maxHealthIncreaseStack * (itemCount - 1);
                args.baseShieldAdd += (maxShieldIncreaseBase + maxShieldIncreaseStack * (itemCount - 1)) * sender.maxHealth;
                args.baseRegenAdd += regenIncreaseBase + regenIncreaseStack * (itemCount - 1);
                args.moveSpeedMultAdd += moveSpeedIncreaseBase + moveSpeedIncreaseStack * (itemCount - 1);
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
