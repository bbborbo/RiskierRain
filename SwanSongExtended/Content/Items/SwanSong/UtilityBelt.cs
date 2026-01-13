using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Skills;
using RoR2.ExpansionManagement;
using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class UtilityBelt : ItemBase<UtilityBelt>
    {
        public static List<string> blacklistedSkillNameTokens = new List<string>(1) { "MAGE_UTILITY_ICE_NAME", "ENGI_SKILL_HARPOON_NAME", "CAPTAIN_UTILITY_NAME", "CAPTAIN_UTILITY_ALT_NAME" };
        static float minBaseCooldown = 2f;
        static float maxBaseCooldown = 20f;

        public static float castBarrierBase = 0.02f;
        public static float castBarrierStack = 0.01f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Utility Knife";

        public override string ItemLangTokenName => "UTILITYBARRIER";

        public override string ItemPickupDesc => "Casting your Utility skill grants a temporary barrier.";

        public override string ItemFullDescription => $"Activating your <style=cIsUtility>Utility skill</style> " +
            $"also grants you <style=cIsHealing>a temporary barrier</style> " +
            $"for <style=cIsHealing>{Tools.ConvertDecimal(castBarrierBase)}</style> of your maximum health " +
            $"<style=cStack>(+{Tools.ConvertDecimal(castBarrierStack)} per stack)</style> " +
            $"per second of the skill's <style=cIsUtility>base cooldown</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlUtilityBelt.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/utilitybelt.png");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
        }

        public static void GiveUtilityBarrier(CharacterBody body, GenericSkill skill)
        {
            if (skill != null)
                GiveUtilityBarrier(body, skill.baseRechargeInterval);
        }
        public static void GiveUtilityBarrier(CharacterBody body, float skillBaseCooldown)
        {
            if (body.healthComponent)
            {
                //body is nullchecked by getcount automatically
                float itemCount = UtilityBelt.instance.GetCount(body);

                if (itemCount > 0f)
                {
                    float barrierFraction = castBarrierBase + castBarrierStack * (itemCount - 1);
                    float scaledBarrierFraction = barrierFraction * Mathf.Clamp(skillBaseCooldown, minBaseCooldown, maxBaseCooldown);
                    // float barrier = castBarrierBase + castBarrierStack * (itemCount - 1);
                    // int adjustedBarrier = (int)(barrier * Mathf.Pow(baseCooldown / 2f, 0.75f));
                    body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth * scaledBarrierFraction);
                }
            }
        }
    }

    public class UtilityKnifeBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => UtilityBelt.instance.ItemsDef;

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

                UtilityBelt.GiveUtilityBarrier(body, effectiveCooldown);
            }
        }
    }
}
