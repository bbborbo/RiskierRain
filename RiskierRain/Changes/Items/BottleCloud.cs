using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static RiskierRain.JumpStatHook;

namespace RiskierRain.Items
{
    class BottleCloud : ItemBase<BottleCloud>
    {
        public override string ItemName => "Cloud In A Bottle";

        public override string ItemLangTokenName => "CLOUDBOTTLE";

        public override string ItemPickupDesc => "Gain an extra jump.";

        public override string ItemFullDescription => "Gain an extra jump.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            JumpStatCoefficient += CloudJump;
            OnJumpEvent += CloudOnJump;
        }

        private void CloudJump(CharacterBody sender, ref int jumpCount)
        {
            if (GetCount(sender) > 0)
            {
                jumpCount += 1;
            }
        }

        private void CloudOnJump(CharacterMotor obj)
        { 

        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            //CreateBuff();
            Hooks();
        }
    }
}
