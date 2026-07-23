using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;
using RoR2.ExpansionManagement;

namespace SwanSongExtended.Items
{
    class ManaFlower : ItemBase<ManaFlower>
    {
        public static bool GetBloomConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Natures Gift", "Enables Natures Gift and Blooms");
        }
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return ManaFlower.GetBloomConfig();
        }
        public override string ConfigName => "Items : Natures Gift";
        public static float aspdAmtBase = 0.08f;
        public static float aspdAmtStack = 0.08f;
        public static float cdrAmtBase = 0.08f;
        public static float cdrAmtStack = 0.08f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Nature\u2019s Gift";

        public override string ItemLangTokenName => "BORBOMANAFLOWER";

        public override string ItemPickupDesc => "Slightly increase attack speed and reduce Primary and Secondary cooldowns.";

        public override string ItemFullDescription => $"Increases <style=cIsDamage>attack speed</style> " +
            $"by <style=cIsUtility>{Tools.ConvertDecimal(aspdAmtBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(aspdAmtStack)} per stack)</style>, " +
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
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.CannotSteal };

        public override GameObject ItemModel => LoadDropPrefab("mdlManaFlower");

        public override Sprite ItemIcon => LoadItemIcon("texIconManaFlower");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += ManaFlowerAspd;
        }

        private void ManaFlowerAspd(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                float aspdBoost = aspdAmtBase + aspdAmtStack * (itemCount - 1);

                args.attackSpeedMultAdd += aspdBoost;


                float cdrBoost = (1 - cdrAmtBase) * Mathf.Pow(1 - cdrAmtStack, itemCount - 1);
                args.primarySkill.cooldownMultiplier *= cdrBoost;
                args.secondarySkill.cooldownMultiplier *= cdrBoost;
            }
        }
    }
}
