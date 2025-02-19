using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using On.RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using R2API;

namespace BarrierRework
{
    public partial class BarrierReworkPlugin
    {
        public static float aegisBarrierFraction = 0.1f;
        public static float _aegisBarrierFlat = 60;
        public static BuffDef aegisDecayBuff;

        protected void ReworkAegis()
        {
            CreateBuff();
            Hooks();
            IL.RoR2.HealthComponent.Heal += RemoveAegisOverheal;

            LanguageAPI.Add("ITEM_BARRIERONOVERHEAL_PICKUP", "Gain barrier on any interaction. While out of danger, barrier stops decaying.");
            LanguageAPI.Add("ITEM_BARRIERONOVERHEAL_DESC", 
                $"Using any interactable grants a <style=cIsHealing>temporary barrier</style> " +
                $"for <style=cIsHealing>{AegisBarrierFlat.Value} health</style> <style=cStack>(+{AegisBarrierFlat.Value} per stack)</style>. " +
                $"While outside of danger, <style=cIsUtility>barrier will not decay</style>.");
        }

        private void RemoveAegisOverheal(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.barrierOnOverHeal)));
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }


        private void CreateBuff()
        {
            aegisDecayBuff = ScriptableObject.CreateInstance<BuffDef>();
            aegisDecayBuff.name = "AegisDecayFreeze";
            aegisDecayBuff.buffColor = new Color(0.95f, 0.85f, 0.08f);
            aegisDecayBuff.canStack = false;
            aegisDecayBuff.isDebuff = false;
            aegisDecayBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion();
            R2API.ContentAddition.AddBuffDef(aegisDecayBuff);
        }

        #region rework
        public void Hooks()
        {
            MultiShopCardUtils.OnMoneyPurchase += OnMoneyPurchase;
            MultiShopCardUtils.OnNonMoneyPurchase += OnNonMoneyPurchase; 
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetBarrierStats += AegisDecayFreeze;
        }

        private void AegisDecayFreeze(CharacterBody body, BarrierStats barrierStats)
        {
            if(body.HasBuff(aegisDecayBuff))
                barrierStats.barrierFreezeCount += 1;
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
                self.AddItemBehavior<AegisDecayBehavior>(self.inventory.GetItemCount(RoR2Content.Items.BarrierOnOverHeal));
            }
        }

        private void AegisBarrierGrant(CostTypeDef.PayCostContext context)
        {
            CharacterBody activator = context.activatorMaster?.GetBody();
            if (activator)
            {
                int aegisCount = activator.inventory.GetItemCount(RoR2Content.Items.BarrierOnOverHeal);
                HealthComponent hc = activator.healthComponent;
                if (aegisCount > 0 && hc != null)
                {
                    //float barrierGrant = aegisCount * aegisBarrierFraction * hc.fullCombinedHealth;
                    float barrierGrant = aegisCount * _aegisBarrierFlat;
                    hc.AddBarrierAuthority(barrierGrant);
                }
            }
        }
        #endregion
    }
    public class AegisDecayBehavior : CharacterBody.ItemBehavior
    {
        bool decayFrozen = false;

        public void FixedUpdate()
        {
            if (body.outOfDanger != decayFrozen)
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
            body.AddBuff(BarrierReworkPlugin.aegisDecayBuff);
            //body.barrierDecayRate = 0;
        }

        private void UnfreezeDecay()
        {
            decayFrozen = false;
            body.RemoveBuff(BarrierReworkPlugin.aegisDecayBuff);
            //body.barrierDecayRate = body.maxBarrier / 30f;
        }

        void OnDisable()
        {
            if (decayFrozen)
                UnfreezeDecay();
        }
    }
}
