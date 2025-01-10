using BepInEx.Configuration;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Items
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

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlEnergyCrystal.prefab");
        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_FLAMEORB.png");
        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        #endregion
        int critChance = 5;
        int barrierBase = 8;
        int barrierStack = 4;

        float speedBoost = 3f;
        int durationBase = 2;
        int durationStack = 1;

        public static BuffDef energyBuff;

        public override void Init()
        {
            energyBuff = Content.CreateAndAddBuff(
                "bdCrystalEnergy",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), //replace me
                new Color(1f, 0.9f, .7f),
                false, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            GetStatCoefficients += CrystalStats;
            On.RoR2.GlobalEventManager.OnCrit += EnergyCrystalCrit;
            On.RoR2.GlobalEventManager.OnHitAllProcess += EnergyCrystalHit;
        }

        private void EnergyCrystalHit(On.RoR2.GlobalEventManager.orig_OnHitAllProcess orig, GlobalEventManager self, DamageInfo damageInfo, GameObject hitObject)
        {
            orig(self, damageInfo, hitObject);
            if (!damageInfo.crit) return;

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
    }
}
