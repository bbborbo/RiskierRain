﻿using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class ChargedSpine : ItemBase<ChargedSpine>
    {

        public static float baseShield = 50;
        public static float stackShield = 50;
        public static float baseDuration = 5;
        public static float stackDuration = 5;
        public static float baseArmor = 200;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Charged Malachite Spine";

        public override string ItemLangTokenName => "CHARGEDMALACHITESPINE";

        public override string ItemPickupDesc => "Poison yourself when shields are broken, gaining great damage resistence.";

        public override string ItemFullDescription => $"Gain {HealingColor($"{baseShield} shield")} {StackText($"+{stackShield}")}. " +
            $"While poisoned, gain {HealingColor($"{baseArmor} armor")}. " +
            $"{RedText($"Poison is inflicted for {baseDuration} seconds on shield break")} {StackText($"+{stackDuration} seconds")}.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable };

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlChargedSpine.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CHARGED_MALACHITE_SPINE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ChargedSpineTakeDamage;
            GetStatCoefficients += ChargedSpineStats;
        }

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
    }
}
