﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static SwanSongExtended.Modules.EliteModule;

namespace SwanSongExtended.Elites
{
    class DeuteriumAspect : EliteEquipmentBase<DeuteriumAspect>
    {
        #region
        public override string ConfigName => "Elites : Storm : " + EliteModifier;
        #endregion
        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        public override string EliteAffixToken => "AFFIX_DEUTERIUM";

        public override string EliteModifier => "Deuterium";

        public override string EliteEquipmentName => "Edge of Extinction";

        public override string EliteEquipmentPickupDesc => "Become an aspect of sorrow.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";
        public override float EliteHealthModifier => 9f;

        public override float EliteDamageModifier => 4.5f;

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.tif").WaitForCompletion();
        public override Color EliteBuffColor => Color.blue;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampLeeching";
        public override EliteTiers EliteTier { get; set; } = EliteTiers.StormT2;
        //public override CombatDirector.EliteTierDef[] CanAppearInEliteTiers => new CombatDirector.EliteTierDef[1] { RiskierRainContent.StormT2 };

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }
}
