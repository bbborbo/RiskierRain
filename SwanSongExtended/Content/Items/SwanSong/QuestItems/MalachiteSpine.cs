using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Items
{
    class MalachiteSpine : ItemBase<MalachiteSpine>
    {
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Malachite Spine";

        public override string ItemLangTokenName => "UNCHARGEDMALACHITESPINE";

        public override string ItemPickupDesc => "Poisons the holder when struck.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.Cleansable, ItemTag.AIBlacklist};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlSpine.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += MalachiteSpineTakeDamage;
        }

        private void MalachiteSpineTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig (self, damageInfo);
            int i = GetCount(self.body);
            if (i <= 0 || self.body.HasBuff(RoR2Content.Buffs.HealingDisabled)) return;
            self.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, i);//1 second per stack ig
        }
    }
}
