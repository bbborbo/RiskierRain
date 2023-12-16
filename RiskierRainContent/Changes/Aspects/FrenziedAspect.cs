using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.CombatDirector;
using static R2API.RecalculateStatsAPI;
using static RiskierRainContent.CoreModules.EliteModule;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Equipment
{
    class FrenziedAspect : EliteEquipmentBase<FrenziedAspect>
    {
        public override string EliteEquipmentName => "Chir\u2019s Tempo"; //momentum, tempo, alacrity, velocity

        public override string EliteEquipmentPickupDesc => "Become an aspect of velocity.";

        public override string EliteAffixToken => "AFFIX_SPEED";

        public override string EliteModifier => "Frenzied";

        public override string EliteEquipmentFullDescription => "Increase movement, attack, and ability recharge speed.";

        public override string EliteEquipmentLore => "";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matFrenzied.mat");
        public override string EliteRampTextureName { get; set; } = "texRampFrenzied";
        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier1;

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;

        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteLightning/texBuffAffixBlue.png").WaitForCompletion();
        public override Color EliteBuffColor => new Color(1.0f, 0.7f, 0.0f, 1.0f);

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            GetStatCoefficients += FrenziedStatBuff;
            On.RoR2.CharacterBody.RecalculateStats += FrenziedCooldownBuff;
        }

        private void FrenziedCooldownBuff(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);

            if (IsElite(self, EliteBuffDef))
            {
                float scale = 0.7f;
                if (self.skillLocator.primary)
                {
                    self.skillLocator.primary.cooldownScale *= scale;
                }
                if (self.skillLocator.secondary)
                {
                    self.skillLocator.secondary.cooldownScale *= scale;
                }
                if (self.skillLocator.utility)
                {
                    self.skillLocator.utility.cooldownScale *= scale;
                }
                if (self.skillLocator.special)
                {
                    self.skillLocator.special.cooldownScale *= scale;
                }
            }
        }

        private void FrenziedStatBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (IsElite(sender, EliteBuffDef))
            {
                args.moveSpeedMultAdd += 0.8f;
                args.baseAttackSpeedAdd += 1.2f;
            }
        }

        public override void Init(ConfigFile config)
        {
            /*Material mat = LegacyResourcesAPI.Load<Material>("materials/matElitePoisonOverlay");
            mat.color = Color.yellow;
            EliteMaterial = mat;*/

            //CanAppearInEliteTiers = VanillaTier1();

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
