using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Skills;
using RoR2.ExpansionManagement;
using static R2API.RecalculateStatsAPI;
using RoR2.Items;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;
using SwanSongExtended.Modules;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class VoidUtilityBelt : ItemBase<VoidUtilityBelt>
    {
        public static BuffDef boostBuff;
        public static float boostDuration = 2.5f;
        public static float boostPerSecond = 0.04f;
        public override string ItemName => "Slipstream";

        public override string ItemLangTokenName => "VOIDUTILITYBELT";

        public override string ItemPickupDesc => "Casting your Utility skill grants an additional burst of movement speed.";

        public override string ItemFullDescription => $"Activating your <style=cIsUtility>Utility skill</style> " +
            $"also increases your {UtilityColor("movement speed")} by {UtilityColor(boostPerSecond.AsPercent())} {StackText("+" + boostPerSecond.AsPercent())} per second of the skill's {UtilityColor("base cooldown")}. " +
            $"Lasts for {UtilityColor(boostDuration.ToString() + " seconds")}. {VoidColor("Corrupts all Utility Knives.")}";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.MobilityRelated };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            boostBuff = Content.CreateAndAddBuff("bdVoidUtilityBeltBoost", null, Color.magenta, false, false, isHidden: false);
            base.Init();
        }

        public override void Hooks()
        {
            GetStatCoefficients += BoostMovementSpeed;
        }

        private void BoostMovementSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.skillLocator == null)
                return;

            GenericSkill utility = sender.skillLocator.utility;
            if (utility == null)
                return;

            if (sender.HasBuff(boostBuff) == false)
                return;

            int stack = GetCount(sender);
            if (stack <= 0)
                return;


            float effectiveCooldown = utility.baseRechargeInterval / utility.rechargeStock;
            float boost = boostPerSecond * (1 + effectiveCooldown) * stack;
            args.moveSpeedMultAdd += boost;
        }

        public override void PostInit()
        {
            base.PostInit();
            AddVoidItemRelationship(itemToCorrupt: UtilityBelt.instance.ItemsDef);
        }

        public static void GiveUtilityBoost(CharacterBody body, GenericSkill skill)
        {
            if (skill != null)
                GiveUtilityBoost(body, skill.baseRechargeInterval, VoidUtilityBelt.instance.GetCount(body));
        }
        public static void GiveUtilityBoost(CharacterBody body, float skillBaseCooldown, int stack)
        {
            if (body.healthComponent && NetworkServer.active)
            {
                if (stack > 0f)
                {
                    body.AddTimedBuff(boostBuff, boostDuration);
                }
            }
        }
    }

    public class VoidUtilityKnifeBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => VoidUtilityBelt.instance.ItemsDef;

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
            if (skill.baseRechargeInterval > 0 && skill.rechargeStock > 0 && skill == body.skillLocator.utility)
            {
                float effectiveCooldown = skill.baseRechargeInterval;
                if (skill.rechargeStock > 1)
                    effectiveCooldown /= (float)skill.rechargeStock;

                VoidUtilityBelt.GiveUtilityBoost(body, effectiveCooldown, stack);
            }
        }
    }
}
