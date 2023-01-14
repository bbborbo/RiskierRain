using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using On.RoR2.Items;
using RoR2.Orbs;

namespace RiskierRain.Items
{
    class GreedyRing : ItemBase<GreedyRing>
    {
        public static BuffDef greedyRingBuff;
        public static int bonusMoney = 10;
        public static int discountedChests = 3;
        int discountAmountBase = 8;
        int discountAmountStack = 3;

        public override string ItemName => "Greedy Ring";

        public override string ItemLangTokenName => "BORBODISCOUNT";

        public override string ItemPickupDesc => $"Get a flat discount on the first {discountedChests} chests in every stage.";

        public override string ItemFullDescription => $"At the beginning of each stage, " +
            $"receive {bonusMoney} gold " +
            $"and a <style=cIsUtility>coupon code</style> that <style=cIsUtility>reduces the cost</style> of " +
            $"up to <style=cIsUtility>{discountedChests}</style> chests " +
            $"by  <style=cIsUtility>${discountAmountBase}</style> <style=cStack>(+{discountAmountStack} per stack)</style>. " +
            $"Scales over time.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags { get; set; } = new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            MultiShopCardUtils.OnMoneyPurchase += GreedyRingRefund;
        }

        private void GreedyRingRefund(MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            orig(context);
            CharacterMaster activatorMaster = context.activatorMaster;
            if (activatorMaster && activatorMaster.hasBody)
            {
                CharacterBody body = activatorMaster.GetBody();
                int stack = GetCount(body);
                if (stack > 0 && body.GetBuffCount(greedyRingBuff) > 0)
                {
                    body.RemoveBuff(greedyRingBuff);

                    GoldOrb goldOrb = new GoldOrb();
                    GameObject purchasedObject = context.purchasedObject;
                    Vector3? vector;
                    if (purchasedObject == null)
                    {
                        vector = null;
                    }
                    else
                    {
                        Transform transform = purchasedObject.transform;
                        vector = ((transform != null) ? new Vector3?(transform.position) : null);
                    }
                    goldOrb.origin = (vector ?? body.corePosition);
                    goldOrb.target = body.mainHurtBox;
                    goldOrb.goldAmount = GetGreedyRefundAmt(stack, context.cost);
                    OrbManager.instance.AddOrb(goldOrb);
                }
            }
        }

        private uint GetGreedyRefundAmt(int stack, int moneyCost)
        {
            int greedyMaxRefund = discountAmountBase + discountAmountStack * (stack - 1);
            int endRefund = Mathf.Min(greedyMaxRefund, moneyCost - 1);
            return (uint)endRefund;
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
                greedyRingBuff.name = "GreedyCouponBuff";
                greedyRingBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("texbuffelementalringsreadyicon");
                greedyRingBuff.buffColor = new Color(0.9f, 0.8f, 0.0f);
                greedyRingBuff.canStack = true;
                greedyRingBuff.isDebuff = false;
            };
            Assets.buffDefs.Add(greedyRingBuff);
        }
    }

    public class GreedyRingBehavior : CharacterBody.ItemBehavior
    {
        void Start()
        {
            body.master.GiveMoney((uint)Run.instance.GetDifficultyScaledCost(GreedyRing.bonusMoney));

            for(int i = 0; i < GreedyRing.discountedChests; i++)
            {
                body.AddBuff(GreedyRing.greedyRingBuff);
            }
        }
    }
}
