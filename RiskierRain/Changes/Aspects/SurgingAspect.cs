using BepInEx.Configuration;
using R2API;
using RiskierRain.Equipment;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRain.CoreModules.EliteModule;

namespace RiskierRain.Changes.Aspects
{
    class SurgingAspect : EliteEquipmentBase<SurgingAspect>
    {
        public override string EliteAffixToken => "AFFIX_SURGE";

        public override string EliteModifier => "Surging";

        public override string EliteEquipmentName => "Tantalizing Feast";

        public override string EliteEquipmentPickupDesc => "Become an aspect of deluge.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteHaunted/texBuffAffixBlazing.png").WaitForCompletion();
        public override Color EliteBuffColor => Color.cyan;

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matLeeching.mat");
        //public override string EliteRampTextureName { get; set; } = "texRampLeeching";
        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier1;

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {

        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }
}
