using BepInEx.Configuration;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.Items
{
    class MalachiteSpine : ItemBase<MalachiteSpine>
    {
        public override string ItemName => "Malachite Spine";

        public override string ItemLangTokenName => "UNCHARGEDMALACHITESPINE";

        public override string ItemPickupDesc => "Poisons the holder when struck.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.Cleansable, ItemTag.AIBlacklist};

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlSpine.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texEggIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += MalachiteSpineTakeDamage;
        }

        private void MalachiteSpineTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            orig (self, damageInfo);
            int i = GetCount(self.body);
            if (i <= 0 || self.body.HasBuff(RoR2Content.Buffs.HealingDisabled)) return;
            self.body.AddTimedBuff(RoR2Content.Buffs.HealingDisabled, i);//1 second per stack ig
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
