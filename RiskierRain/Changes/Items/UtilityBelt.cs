using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Skills;

namespace RiskierRain.Items
{
    class UtilityBelt : ItemBase<UtilityBelt>
    {
        public static BuffDef utilityBeltCooldown;
        public static float idealBaseCooldown = 6f;

        public static float castBarrierBase = 0.15f;
        public static float castBarrierStack = 0.05f;
        public override string ItemName => "Utility Belt";

        public override string ItemLangTokenName => "BORBOBARRIERBELT";

        public override string ItemPickupDesc => "Casting your Utility skill grants a temporary barrier.";

        public override string ItemFullDescription => $"Casting your Utility skill grants you <style=cIsHealing>a temporary barrier</style> " +
            $"for <style=cIsHealing>{Tools.ConvertDecimal(castBarrierBase)} health</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(castBarrierStack)} per stack)</style>. " +
            $"<style=cIsUtility>Affected by Utility cooldown length</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlUtilityBelt.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BORBOBARRIERBELT.png");
        public override BalanceCategory Category => BalanceCategory.StateOfHealth;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing , ItemTag.Utility };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnSkillActivated += UtilityBeltBarrierGrant;
            On.RoR2.SkillLocator.ApplyAmmoPack += UtilityBeltBandolierSynergy;
            On.RoR2.GenericSkill.Reset += UtilityBeltCooldownReset;
        }

        private void UtilityBeltCooldownReset(On.RoR2.GenericSkill.orig_Reset orig, GenericSkill self)
        {
            orig(self);
            if(self.skillFamily == self.characterBody?.skillLocator?.utility?.skillFamily)
            {
                self.characterBody.ClearTimedBuffs(utilityBeltCooldown);
            }
        }

        private void UtilityBeltBandolierSynergy(On.RoR2.SkillLocator.orig_ApplyAmmoPack orig, SkillLocator self)
        {
            orig(self);

            if(self.utility)
            {
                CharacterBody body = self.utility.characterBody;
                if(body && body.HasBuff(utilityBeltCooldown))
                {
                    body.ClearTimedBuffs(utilityBeltCooldown);
                }
            }
        }

        private void UtilityBeltBarrierGrant(On.RoR2.CharacterBody.orig_OnSkillActivated orig, CharacterBody self, GenericSkill skill)
        {
            orig(self, skill);

            if (self.healthComponent && skill == self.skillLocator.utility)
            {
                float itemCount = GetCount(self);
                int currentCooldownCount = self.GetBuffCount(utilityBeltCooldown);

                if (itemCount > 0f && currentCooldownCount < skill.maxStock)
                {
                    float baseCooldown = skill.baseRechargeInterval;
                    float endCooldown = Mathf.Max(baseCooldown * skill.cooldownScale - skill.flatCooldownReduction, 0.5f);

                    float barrierFraction = castBarrierBase + castBarrierStack * (itemCount - 1);
                    float adjustedBarrier = (self.healthComponent.fullCombinedHealth * barrierFraction) * Mathf.Min(baseCooldown / idealBaseCooldown, 3);
                    // float barrier = castBarrierBase + castBarrierStack * (itemCount - 1);
                    // int adjustedBarrier = (int)(barrier * Mathf.Pow(baseCooldown / 2f, 0.75f));
                    self.healthComponent.AddBarrier(adjustedBarrier);

                    self.AddTimedBuffAuthority(utilityBeltCooldown.buffIndex, endCooldown * (currentCooldownCount + 1));
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        void CreateBuff()
        {
            utilityBeltCooldown = ScriptableObject.CreateInstance<BuffDef>();
            {
                utilityBeltCooldown.name = "UtilityBeltCooldown";
                utilityBeltCooldown.buffColor = Color.black;
                utilityBeltCooldown.canStack = true;
                utilityBeltCooldown.isDebuff = false;
                utilityBeltCooldown.isHidden = true;
                utilityBeltCooldown.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
            };

            Assets.buffDefs.Add(utilityBeltCooldown);
        }
    }
}
