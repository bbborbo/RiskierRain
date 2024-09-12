using BepInEx.Configuration;
using On.RoR2.Items;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace RiskierRainContent.Items
{
    class Aegis : ItemBase<Aegis>
    {
        public static float aegisBarrierFraction = 0.1f;
        public static float aegisBarrierFlat = 60;
        public static BuffDef aegisDecayBuff;
        public override string ItemName => "Aegis";

        public override string ItemLangTokenName => "BORBOAEGIS";

        public override string ItemPickupDesc => "Gain barrier on any interaction. While out of danger, barrier stops decaying.";

        public override string ItemFullDescription => $"Using any interactable grants a <style=cIsHealing>temporary barrier</style> " +
            $"for <style=cIsHealing>{aegisBarrierFlat} health</style> <style=cStack>(+{aegisBarrierFlat} per stack)</style>. " +
            $"While outside of danger, <style=cIsUtility>barrier will not decay</style>.";

        public override string ItemLore => "Order: Artifact E-8EE572\r\nTracking Number: 490******\r\nEstimated Delivery: 08/10/2056\r\nShipping Method: Priority\r\nShipping Address: Titan Museum of History and Culture, Titan\r\n\r\nSorry about the delay, we've had a flood of orders come in from this site. But it was exactly where you said we should look - there was a sealed off room where you marked the excavation diagram. I finished translating the engraving too, so consider that a bonus for the time we took to get to it:\r\n\r\n\"I am the will to survive made manifest. To those who never lose hope, to they who try in the face of impossible odds, I offer not \r\nprotection, but the means to bring one's unconquerable spirit forth as the defender of their mortal lives.\"\r\n\r\nIt\u2019s so lightweight, we figure it must've been entirely decorative. That seems to line up with the text. In any case, I hope it makes a good exhibit! I'm a big fan of the museum, so it wouldn't hurt to give me a partner's discount next time I visit, right?\r\n";

        public override ItemTier Tier => ItemTier.Tier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing, ItemTag.InteractableRelated };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/BarrierOnOverHeal/PickupAegis.prefab").WaitForCompletion(); 

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/BarrierOnOverHeal/texAegisIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        private IEnumerator GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.BarrierOnOverHeal);
            yield break;
        }

        public override void Hooks()
        {
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
            RiskierRainContent.RetierItem(nameof(RoR2Content.Items.BarrierOnOverHeal));

            MultiShopCardUtils.OnMoneyPurchase += OnMoneyPurchase;
            MultiShopCardUtils.OnNonMoneyPurchase += OnNonMoneyPurchase;
            On.RoR2.CharacterBody.RecalculateStats += AegisBarrierDecay;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void OnNonMoneyPurchase(MultiShopCardUtils.orig_OnNonMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            AegisBarrierGrant(context);
            orig(context);
        }

        private void OnMoneyPurchase(MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            AegisBarrierGrant(context);
            orig(context);
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<AegisDecayBehavior>(GetCount(self));
            }
        }

        private void AegisBarrierDecay(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(aegisDecayBuff))
                self.barrierDecayRate = 0;
        }

        private void AegisBarrierGrant(CostTypeDef.PayCostContext context)
        {
            CharacterBody activator = context.activatorMaster?.GetBody();
            if (activator)
            {
                int aegisCount = GetCount(activator);
                HealthComponent hc = activator.healthComponent;
                if(aegisCount > 0 && hc != null)
                {
                    //float barrierGrant = aegisCount * aegisBarrierFraction * hc.fullCombinedHealth;
                    float barrierGrant = aegisCount * aegisBarrierFlat;
                    hc.AddBarrierAuthority(barrierGrant);
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
            aegisDecayBuff = ScriptableObject.CreateInstance<BuffDef>();
            aegisDecayBuff.name = "AegisDecayFreeze";
            aegisDecayBuff.buffColor = new Color(0.95f, 0.85f, 0.08f);
            aegisDecayBuff.canStack = false;
            aegisDecayBuff.isDebuff = false;
            aegisDecayBuff.iconSprite = CoreModules.Assets.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            CoreModules.Assets.buffDefs.Add(aegisDecayBuff);
        }
    }
    public class AegisDecayBehavior : CharacterBody.ItemBehavior
    {
        bool decayFrozen = false;

        public void FixedUpdate()
        {
            if(body.outOfDanger != decayFrozen)
            {
                if (!decayFrozen)
                {
                    FreezeDecay();
                }
                else
                {
                    UnfreezeDecay();
                }
            }
        }

        private void FreezeDecay()
        {
            decayFrozen = true;
            body.AddBuff(Aegis.aegisDecayBuff);
            body.barrierDecayRate = 0;
        }

        private void UnfreezeDecay()
        {
            decayFrozen = false;
            body.RemoveBuff(Aegis.aegisDecayBuff);
            body.barrierDecayRate = body.maxBarrier / 30f;
        }

        void OnDisable()
        {
            if (decayFrozen)
                UnfreezeDecay();
        }
    }
}
