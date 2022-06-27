using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class GreedyRing : ItemBase<GreedyRing>
    {
        public static BuffDef greedyRingBuff;
        float bonusGold = 0.1f;
        public static int discountedChests = 3;
        int discountAmountBase = 8;
        int discountAmountStack = 2;

        public override string ItemName => "Greedy Ring";

        public override string ItemLangTokenName => "BORBODISCOUNT";

        public override string ItemPickupDesc => $"Get a flat discount on the first {discountedChests} chests in every stage.";

        public override string ItemFullDescription => $"After entering a stage, receive a " +
            $"<style=cIsUtility>coupon code</style> that <style=cIsUtility>reduces the cost</style> of " +
            $"up to <style=cIsUtility>{discountedChests}</style> chests " +
            $"by  <style=cIsUtility>${discountAmountBase}</style> <style=cStack>(+{discountAmountStack} per stack)</style>. " +
            $"Scales over time.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override bool IsHidden => true;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    GreedyRingBehavior ringBehavior = self.AddItemBehavior<GreedyRingBehavior>(GetCount(self));
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

        private void CreateBuff()
        {
            greedyRingBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                greedyRingBuff.name = "GreedyRingBuff";
                greedyRingBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("texbuffelementalringsreadyicon");
                greedyRingBuff.buffColor = new Color(0.9f, 0.8f, 0.0f);
                greedyRingBuff.canStack = false;
                greedyRingBuff.isDebuff = false;
            };
            Assets.buffDefs.Add(greedyRingBuff);
        }
    }

    public class GreedyRingBehavior : CharacterBody.ItemBehavior
    {
        void Start()
        {
            for(int i = 0; i < GreedyRing.discountedChests; i++)
            {
                body.AddBuff(GreedyRing.greedyRingBuff);
            }
        }
    }
}
