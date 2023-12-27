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

        public static BuffDef energyBuff;

        public override void Hooks()
        {
            GetStatCoefficients += CrystalStats;
            On.RoR2.GlobalEventManager.OnCrit += EnergyCrystalCrit;
            On.RoR2.GlobalEventManager.OnHitAll += EnergyCrystalHit;
        }

        private void EnergyCrystalHit(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (!damageInfo.crit) return;

            Debug.Log("thatsacrit baby");
            CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
            if (attackerBody == null) return;

            int itemCount = GetCount(attackerBody);
            if (itemCount <= 0) return;

            CharacterBody victimBody = hitObject.GetComponent<CharacterBody>();
            if (victimBody == null) return;

            int duration = durationBase + (durationStack * (itemCount - 1));
            victimBody.AddTimedBuffAuthority(energyBuff.buffIndex, duration);
        }

        private void CrystalStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (GetCount(sender) > 0)
            {
                args.critAdd += critChance;
            }
            if (sender.HasBuff(energyBuff))
            {
                args.moveSpeedMultAdd += speedBoost;
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
        void CreateBuff()
        {
            energyBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                energyBuff.name = "energyBuff";
                energyBuff.buffColor = new Color(1f, 0.9f, .7f);
                energyBuff.canStack = false;
                energyBuff.isDebuff = false;
                energyBuff.iconSprite = Assets.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(energyBuff);
        }
        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }
    }
}
