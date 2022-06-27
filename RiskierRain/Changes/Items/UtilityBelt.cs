using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Items
{
    class UtilityBelt : ItemBase<UtilityBelt>
    {
        public static BuffDef utilityBeltCooldown;

        public static float castBarrierBase = 25;
        public static float castBarrierStack = 25;
        public override string ItemName => "Utility Belt";

        public override string ItemLangTokenName => "BORBOBARRIERBELT";

        public override string ItemPickupDesc => "Casting your Utility skill grants a temporary barrier.";

        public override string ItemFullDescription => $"Casting your Utility skill grants you <style=cIsHealing>a temporary barrier</style> " +
            $"for <style=cIsHealing>{castBarrierBase} health</style> " +
            $"<style=cStack>(+{castBarrierStack} per stack)</style>. " +
            $"<style=cIsUtility>Affected by Utility cooldown length</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfHealth;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Healing , ItemTag.Utility };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnSkillActivated += UtilityBeltBarrierGrant;
            On.RoR2.SkillLocator.ApplyAmmoPack += UtilityBeltBandolierSynergy;
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
                    float endCooldown = baseCooldown * skill.cooldownScale - skill.flatCooldownReduction;
                    if (endCooldown < 0.5f)
                    {
                        endCooldown = 0.5f;
                    }

                    float barrier = castBarrierBase + castBarrierStack * (itemCount - 1);
                    int adjustedBarrier = (int)(barrier * Mathf.Pow(baseCooldown / 2f, 0.75f));
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
                utilityBeltCooldown.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
            };

            Assets.buffDefs.Add(utilityBeltCooldown);
        }
    }
}
