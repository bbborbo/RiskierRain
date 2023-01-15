using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.CoreModules.StatHooks;

namespace RiskierRain.Items
{
    class ManaFlower : ItemBase
    {
        public static float cdrAmt = 0.08f;
        public override string ItemName => "Nature\u2019s Gift";

        public override string ItemLangTokenName => "BORBOMANAFLOWER";

        public override string ItemPickupDesc => "Reduces cooldowns for your primary and secondary skills.";

        public override string ItemFullDescription => $"Reduce <style=cIsUtility>Primary and Secondary skill cooldowns</style> " +
            $"by <style=cIsUtility>{Tools.ConvertDecimal(cdrAmt)}</style> <style=cStack>(+{Tools.ConvertDecimal(cdrAmt)} per stack)</style>.";

        public override string ItemLore => @"Order: Jupiter Rose
Tracking Number: 58***********
Estimated Delivery: 07/30/2056
Shipping Method: Standard
Shipping Address: 280 Oak Boulevard, Venus
Shipping Details:

Isn’t it pretty?
Just looking at it fills me with energy.
Nature is so magical :)
";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += ManaFlowerCdr;
        }

        private void ManaFlowerCdr(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int itemCount = GetCount(self);
            if (itemCount > 0)
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = Mathf.Pow(1 - cdrAmt, itemCount);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                }
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
