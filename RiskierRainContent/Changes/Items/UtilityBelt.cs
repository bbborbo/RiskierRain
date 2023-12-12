using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Skills;

namespace RiskierRainContent.Items
{
    class UtilityBelt : ItemBase<UtilityBelt>
    {
        public static List<string> blacklistedSkillNameTokens = new List<string>(1) { "MAGE_UTILITY_ICE_NAME", "ENGI_SKILL_HARPOON_NAME", "CAPTAIN_UTILITY_NAME", "CAPTAIN_UTILITY_ALT_NAME" };
        public static BuffDef utilityBeltCooldown;
        static float minBaseCooldown = 2f;
        static float maxBaseCooldown = 20f;

        public static float castBarrierBase = 0.02f;
        public static float castBarrierStack = 0.005f;
        public override string ItemName => "Utility Knife";

        public override string ItemLangTokenName => "UTILITYBARRIER";

        public override string ItemPickupDesc => "Casting your Utility skill grants a temporary barrier.";

        public override string ItemFullDescription => $"Casting your Utility skill grants you <style=cIsHealing>a temporary barrier</style> " +
            $"for <style=cIsHealing>{Tools.ConvertDecimal(castBarrierBase)}</style> of your maximum health " +
            $"<style=cStack>(+{Tools.ConvertDecimal(castBarrierStack)} per stack)</style> " +
            $"per second of your Utility skill's <style=cIsUtility>base cooldown</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlUtilityBelt.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_BORBOBARRIERBELT.png");
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnSkillActivated += UtilityBeltBarrierGrant;
            //hard compat
            On.EntityStates.Mage.Weapon.PrepWall.OnExit += PrepWall_OnExit;
            On.EntityStates.Captain.Weapon.CallAirstrikeBase.OnEnter += CallAirstrikeBase_OnEnter;
            On.EntityStates.Engi.EngiMissilePainter.Fire.FireMissile += Fire_FireMissile;
        }

        private void UtilityBeltBarrierGrant(On.RoR2.CharacterBody.orig_OnSkillActivated orig, CharacterBody self, GenericSkill skill)
        {
            orig(self, skill);

            if (!blacklistedSkillNameTokens.Contains(skill.skillNameToken) && skill == self.skillLocator.utility)
            {
                UtilityBelt.GiveUtilityBarrier(self, skill.baseRechargeInterval);
            }
        }

        #region hard compat
        private void Fire_FireMissile(On.EntityStates.Engi.EngiMissilePainter.Fire.orig_FireMissile orig, EntityStates.Engi.EngiMissilePainter.Fire self, HurtBox target, Vector3 position)
        {
            orig(self, target, position);
            UtilityBelt.GiveUtilityBarrier(self.characterBody, self.activatorSkillSlot);
        }

        private void CallAirstrikeBase_OnEnter(On.EntityStates.Captain.Weapon.CallAirstrikeBase.orig_OnEnter orig, EntityStates.Captain.Weapon.CallAirstrikeBase self)
        {
            orig(self);
            SkillLocator skillLocator = self.skillLocator;
            if (skillLocator)
            {
                GiveUtilityBarrier(self.characterBody, skillLocator.utility.baseRechargeInterval / 3);
            }
        }

    private void PrepWall_OnExit(On.EntityStates.Mage.Weapon.PrepWall.orig_OnExit orig, EntityStates.Mage.Weapon.PrepWall self)
    {
        if (!self.outer.destroying)
        {
            if (self.goodPlacement)
            {
                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator)
                {
                    GiveUtilityBarrier(self.characterBody, skillLocator.utility);
                }
            }
        }
        orig(self);
    }
        #endregion

        #region grant barrier
        public static void GiveUtilityBarrier(CharacterBody body, GenericSkill skill)
        {
            if (skill != null)
                GiveUtilityBarrier(body, skill.baseRechargeInterval);
        }
        public static void GiveUtilityBarrier(CharacterBody body, float skillBaseCooldown)
        {
            if (body.healthComponent)
            {
                //body is nullchecked by getcount automatically
                float itemCount = UtilityBelt.instance.GetCount(body);

                if (itemCount > 0f)
                {
                    float barrierFraction = castBarrierBase + castBarrierStack * (itemCount - 1);
                    float scaledBarrierFraction = barrierFraction * Mathf.Clamp(skillBaseCooldown, minBaseCooldown, maxBaseCooldown);
                    // float barrier = castBarrierBase + castBarrierStack * (itemCount - 1);
                    // int adjustedBarrier = (int)(barrier * Mathf.Pow(baseCooldown / 2f, 0.75f));
                    body.healthComponent.AddBarrier(body.healthComponent.fullCombinedHealth * scaledBarrierFraction);
                }
            }
        }
        #endregion

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
