using BepInEx.Configuration;
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
    class WhirlwindAspect : T1EliteEquipmentBase<WhirlwindAspect>
    {
        #region
        public override string ConfigName => "Elites : Storm : " + EliteModifier;
        #endregion
        public override AssetBundle assetBundle => SwanSongPlugin.mainAssetBundle;

        //VERY important
        public override EliteTiers EliteTier { get; set; } = EliteTiers.StormT1;

        public override string EliteAffixToken => "AFFIX_WHIRLWIND";

        public override string EliteModifier => "Howling"; //churning, gyrating, swirling, winding

        public override string EliteEquipmentName => "Twisted Stare";

        public override string EliteEquipmentPickupDesc => "Become an aspect of sundering wind.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteHaunted/texBuffAffixHaunted.tif").WaitForCompletion();
        public override Color EliteBuffColor => Color.gray;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampLeeching";
        //public override CombatDirector.EliteTierDef[] CanAppearInEliteTiers => new CombatDirector.EliteTierDef[1] { RiskierRainContent.StormT1 };

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;

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
