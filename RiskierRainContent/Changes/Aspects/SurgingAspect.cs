using BepInEx.Configuration;
using R2API;
using RiskierRainContent.Equipment;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRainContent.CoreModules.EliteModule;

namespace RiskierRainContent.Changes.Aspects
{
    class SurgingAspect : T1EliteEquipmentBase<SurgingAspect>
    {
        //VERY important
        public override EliteTiers EliteTier { get; set; } = EliteTiers.StormT1;

        public override string EliteAffixToken => "AFFIX_SURGE";

        public override string EliteModifier => "Surging";

        public override string EliteEquipmentName => "Tantalizing Feast";

        public override string EliteEquipmentPickupDesc => "Become an aspect of deluge.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteFire/texBuffAffixRed.tif").WaitForCompletion();
        public override Color EliteBuffColor => Color.cyan;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampLeeching";
        //public override CombatDirector.EliteTierDef[] CanAppearInEliteTiers => new CombatDirector.EliteTierDef[1] { RiskierRainContent.StormT1 };

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateEliteEquipment();
            CreateLang();
            CreateElite();
            Hooks();
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }
}
