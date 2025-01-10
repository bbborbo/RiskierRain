using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRainContent.Items
{
    class ChargedSpine : ItemBase<ChargedSpine>
    {
        public override string ItemName => "Charged Malachite Spine";

        public override string ItemLangTokenName => "CHARGEDMALACHITESPINE";

        public override string ItemPickupDesc => "Poison yourself when shields are broken, gaining great damage resistence.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override GameObject ItemModel => CoreModules.Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChargedSpine.prefab");

        public override Sprite ItemIcon => CoreModules.Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHARGED_MALACHITE_SPINE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ChargedSpineTakeDamage;
            GetStatCoefficients += ChargedSpineStats;
        }

        public static float baseShield = 25;
        public static float baseDuration = 5;
        public static float stackDuration = 5;
        public static float baseArmor = 100;

        private void ChargedSpineStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                args.baseShieldAdd += baseShield * itemCount;
                //gives armor when poisoned
                if (sender.HasBuff(RoR2Content.Buffs.HealingDisabled))
                {
                    args.armorAdd += baseArmor;
                }
            }
        }

        private void ChargedSpineTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            bool hadShieldBefore = HasShield(self);
            CharacterBody body = self.body;
            int spineItemCount = GetCount(body);

            orig(self, damageInfo);

            if (hadShieldBefore && !HasShield(self) && self.alive)
            {
                if (spineItemCount > 0 && !body.HasBuff(RoR2Content.Buffs.HealingDisabled))
                {
                    self.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, baseDuration + stackDuration * (spineItemCount - 1));
                }
            }
        }
        public static bool HasShield(HealthComponent hc)
        {
            return hc.shield > 1;
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
