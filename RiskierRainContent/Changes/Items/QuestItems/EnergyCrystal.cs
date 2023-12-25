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
    class EnergyCrystal : ItemBase<EnergyCrystal>
    {
        #region abstract
        public override string ItemName => "Energy Crystal";

        public override string ItemLangTokenName => "ENERGY_CRYSTAL";

        public override string ItemPickupDesc => "Critical hits grant barrier, BUT energize enemies.";

        public override string ItemFullDescription => "";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.Cleansable, ItemTag.Healing, ItemTag.Damage };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlEnergyCrystal.prefab");
        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_FLAMEORB.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        #endregion
        int critChance = 5;
        int barrierBase = 8;
        int barrierStack = 4;

        float speedBoost = .5f;
        int durationBase = 2;
        int durationStack = 1;


        public override void Hooks()
        {
            GetStatCoefficients += CrystalCritAdd;
            On.RoR2.GlobalEventManager.OnCrit += EnergyCrystalCrit;
        }

        private void CrystalCritAdd(CharacterBody sender, StatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.critAdd += critChance;
            }
        }

        private void EnergyCrystalCrit(On.RoR2.GlobalEventManager.orig_OnCrit orig, RoR2.GlobalEventManager self, RoR2.CharacterBody body, RoR2.DamageInfo damageInfo, RoR2.CharacterMaster master, float procCoefficient, RoR2.ProcChainMask procChainMask)
        {
            orig(self, body, damageInfo, master, procCoefficient, procChainMask);
            int itemCount = GetCount(body);
            if (itemCount <= 0) return;
            int barrierToAdd = barrierBase + (barrierStack * (itemCount - 1));
            body.healthComponent.AddBarrierAuthority(barrierToAdd);          
            
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            Hooks();
        }
    }
}
