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
    class VoidIchorRed : ItemBase<VoidIchorRed>
    {
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Interactables.VoidHusk.GetIchorConfig();
        }
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        int critBase = 5;
        int critStack = 5;
        float attackSpeedBase = .075f;
        float attackSpeedStack = .075f;
        public override string ItemName => "Metamorphic Ichor (Red)";

        public override string ItemLangTokenName => "ICHORRED";

        public override string ItemPickupDesc => $"Increase attack speed and critical strike chance. .";

        public override string ItemFullDescription => $"Increase {DamageColor($"attack speed")} by " +
            $"{DamageColor(ConvertDecimal(attackSpeedBase))} {StackText($"+{ConvertDecimal(attackSpeedStack)}")}. " +
            $"Increase {DamageColor($"critical strike chance")} by {DamageColor($"{critBase}%")} " +
            $"{StackText($"+{critStack}%")}. {VoidColor("Corrupts all Replusion Armor Plates and Yellow Ichors")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlIchorR.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/voidichorred.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            base.Init();
        }

        public override void Hooks()
        {
            RecalculateStatsAPI.GetStatCoefficients += RedIchorStats;
        }
        public override void AddVoidRelationships()
        {
            base.AddVoidRelationships();
            AddVoidItemRelationship(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ArmorPlate.ArmorPlate_asset);
            AddVoidItemRelationship(VoidIchorYellow.instance.ItemsDef);
        }

        private void RedIchorStats(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            args.critAdd += critBase + critStack * (itemCount - 1);
            args.attackSpeedMultAdd += attackSpeedBase + attackSpeedStack * (itemCount - 1);
        }
    }
}
