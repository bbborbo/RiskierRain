using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using R2API;
using static MoreStats.StatHooks;
using RoR2.Items;
using SwanSongExtended.Modules;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class Aegis : ReworkBase<Aegis>
    {
        public static bool GetOverhealReworkConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Reworks : Overheal Suite", "Reworks Aegis, Rejuvenation Rack, and Corpsebloom");
        }
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return Aegis.GetOverhealReworkConfig();
        }

        public static BuffDef aegisDecayBuff;
        [AutoConfig("Flat Barrier On Interaction", 40)]
        public static float aegisBarrierFlat = 40;
        [AutoConfig("Percent Barrier On Interaction", 10)]
        public static float aegisBarrierPercent = 10f;
        public override bool IsEquipment => false;

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BarrierOnOverHeal.BarrierOnOverHeal_asset;

        public override string ItemName => "Aegis";

        public override string ItemPickupDesc => 
            "Gain barrier on any interaction. While out of danger, barrier stops decaying.";

        public override string ItemFullDesc => 
            $"Using any interactable grants a <style=cIsHealing>temporary barrier</style> " +
            $"for <style=cIsHealing>{aegisBarrierFlat} health</style> <style=cStack>(+{aegisBarrierFlat} per stack)</style> " +
            $"plus an additional " +
            $"<style=cIsHealing>{aegisBarrierPercent}%</style> " +
            $"of <style=cIsHealing>maximum health</style>. " +
            $"While outside of danger, <style=cIsUtility>barrier will not decay</style>.";

        public override void Init()
        {
            aegisDecayBuff = Content.CreateAndAddBuff(
                "bdAegisFreeze",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                new Color(0.95f, 0.85f, 0.08f),
                false, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            IL.RoR2.HealthComponent.Heal += RemoveAegisOverheal;
            On.RoR2.Items.MultiShopCardUtils.OnMoneyPurchase += OnMoneyPurchase;
            On.RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase += OnNonMoneyPurchase;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += OnTeleporterInteraction;
            GetMoreStatCoefficients += AegisDecayFreeze;
        }


        public static void RemoveAegisOverheal(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.barrierOnOverHeal)));
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(RemoveAegisOverheal));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        public static void AegisDecayFreeze(CharacterBody body, MoreStatHookEventArgs args)
        {
            if (body.HasBuff(aegisDecayBuff))
                args.barrierFreezeCount += 1;
        }

        private void OnTeleporterInteraction(TeleporterInteraction tp)
        {
            AegisBarrierGrant(tp.chargeActivatorServer.GetComponent<CharacterBody>());
        }
        public static void OnNonMoneyPurchase(On.RoR2.Items.MultiShopCardUtils.orig_OnNonMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            AegisBarrierGrant(context.activatorBody);
            orig(context);
        }

        public static void OnMoneyPurchase(On.RoR2.Items.MultiShopCardUtils.orig_OnMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            AegisBarrierGrant(context.activatorBody);
            orig(context);
        }

        public static void AegisBarrierGrant(CharacterBody activatorBody)
        {
            if (activatorBody && NetworkServer.active)
            {
                int aegisCount = activatorBody.inventory.GetItemCountEffective(RoR2Content.Items.BarrierOnOverHeal);
                HealthComponent hc = activatorBody.healthComponent;
                if (aegisCount > 0 && hc != null)
                {
                    float barrierPercent = Util.ConvertAmplificationPercentageIntoReductionNormalized(aegisBarrierPercent * 0.01f) * hc.fullCombinedHealth;
                    float barrierFlat = aegisCount * aegisBarrierFlat;
                    hc.AddBarrierAuthority(barrierPercent + barrierFlat);
                }
            }
        }
    }

    public class AegisDecayBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => RoR2Content.Items.BarrierOnOverHeal;
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
            body.AddBuff(Aegis.aegisDecayBuff);
            //body.barrierDecayRate = 0;
        }

        private void UnfreezeDecay()
        {
            decayFrozen = false;
            body.RemoveBuff(Aegis.aegisDecayBuff);
            //body.barrierDecayRate = body.maxBarrier / 30f;
        }

        void OnDisable()
        {
            if (decayFrozen)
                UnfreezeDecay();
        }
    }
}
