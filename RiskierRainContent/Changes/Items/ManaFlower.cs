using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.CoreModules.StatHooks;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class ManaFlower : ItemBase
    {
        public static float cdrAmtBase = 0.08f;
        public static float cdrAmtStack = 0.06f;
        public override string ItemName => "Nature\u2019s Gift";

        public override string ItemLangTokenName => "BORBOMANAFLOWER";

        public override string ItemPickupDesc => "It's pretty, oh so pretty...";

        public override string ItemFullDescription => $"Increases <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsUtility>{Tools.ConvertDecimal(cdrAmtBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(cdrAmtStack)} per stack)</style>, " +
            $"and reduces <style=cIsUtility>Primary and Secondary skill cooldowns</style> " +
            $"by <style=cIsUtility>{Tools.ConvertDecimal(cdrAmtBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(cdrAmtStack)} per stack)</style>.";

        public override string ItemLore => @"Order: Jupiter Rose
Tracking Number: 58***********
Estimated Delivery: 07/30/2056
Shipping Method: Standard
Shipping Address: 280 Oak Boulevard, Venus
Shipping Details:

Isn’t it pretty?
Just looking at it fills me with energy.
Nature is so magical :)";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlNaturesGift.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BORBOMANAFLOWER.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += ManaFlowerCdr;
            GetStatCoefficients += ManaFlowerAspd;
        }

        private void ManaFlowerAspd(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                float aspdBoost = cdrAmtBase + cdrAmtStack * (itemCount - 1);

                args.attackSpeedMultAdd += aspdBoost;
            }
        }

        private void ManaFlowerCdr(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int itemCount = GetCount(self);
            if (itemCount > 0)
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = (1 - cdrAmtBase) * Mathf.Pow(1 - cdrAmtStack, itemCount - 1);

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
