using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.ExpansionManagement;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class VoidIchorYellow : ItemBase<VoidIchorYellow>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Interactables.VoidHusk.GetIchorConfig();
        }
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        float regenBase = 0.8f;
        float regenStack = 0.8f;
        public override string ItemName => "Metamorphic Ichor (Yellow)";

        public override string ItemLangTokenName => "ICHORYELLOW";

        public override string ItemPickupDesc => $"Gain health regeneration. {VoidColor("Corrupts all Soldier's Syringes and Violet Ichors")}.";

        public override string ItemFullDescription => $"Increase {HealingColor("base health regeneration")} by " +
            $"{HealingColor($"{regenBase} hp/s")} {StackText($"+{regenStack} hp/s")}. " +
            $"{VoidColor("Corrupts all Soldier's Syringes and Violet Ichors")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorY.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/voidichoryellow.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            base.Init();
        }
        public override void AddVoidRelationships()
        {
            base.AddVoidRelationships();
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Syringe.Syringe_asset);
            AddVoidItemRelationship(ItemsDef, VoidIchorRed.instance.ItemsDef);
        }
        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += IchorRegenBoost;
            SwanSongPlugin.onSwanSongLoaded += () => AddVoidItemRelationship(VoidIchorViolet.instance.ItemsDef);
        }

        private void IchorRegenBoost(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            args.baseRegenAdd += regenBase + (regenStack * (itemCount - 1)) * (1 + sender.level * 0.2f);
        }
    }
}
