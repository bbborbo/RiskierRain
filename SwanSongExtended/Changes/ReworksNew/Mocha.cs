using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using System.Collections;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using MonoMod.Cil;
using Mono.Cecil.Cil;

using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class Mocha : ReworkBase<Mocha>
    {
        public static BuffDef mochaBuffActive;
        public static BuffDef mochaBuffInactive;
        public static Sprite mochaCustomSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();

        [AutoConfig("Seconds Of Boost On Stage Start", 80)]
        public static int mochaDurationOnEntry = 80;
        [AutoConfig("Seconds Of Boost On Item Pickup", 40)]
        public static int mochaDurationOnPickup = 40;
        [AutoConfig("Seconds Of Boost On Interactable Use", 20)]
        public static int mochaDurationOnPurchase = 20;

        [AutoConfig("Free Movement/Atk Speed Bonus", 0.05f)]
        public static float spdBoostFree = 0.05f;
        [AutoConfig("Buffed Movement/Atk Speed Bonus", 0.20f)]
        public static float spdBoostBuff = 0.20f;
        [AutoConfig("Free Cooldown Reduction Bonus", 0.00f)]
        public static float cdrBoostFree = 0.00f;
        [AutoConfig("Buffed Cooldown Reduction Bonus", 0.12f)]
        public static float cdrBoostBuff = 0.12f;

        public override bool IsEquipment => false;

        public override string ItemPath => RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_AttackSpeedAndMoveSpeed.AttackSpeedAndMoveSpeed_asset;

        public override string ItemName => "Morning Mocha";

        public override string ItemPickupDesc => "Gain a temporary speed boost after beginning a stage.";

        public override string ItemFullDesc => 
            $"For <style=cIsUtility>{mochaDurationOnEntry}</style> seconds after entering any stage, " +
            $"increase {DamageColor("attack speed")} and {DamageColor("movement speed")} " +
            $"by {DamageColor(Tools.ConvertDecimal(spdBoostBuff))} {StackText($"+{Tools.ConvertDecimal(spdBoostBuff)}")}, " +
            $"and reduce {UtilityColor("skill cooldowns")} by " +
            $"{UtilityColor($"-{Tools.ConvertDecimal(cdrBoostBuff)}")} {StackText($"-{Tools.ConvertDecimal(cdrBoostBuff)}")}. " +
            $"Using {UtilityColor("any interactable")} while this buff is active will extend the duration of the buff " +
            $"by {UtilityColor($"{mochaDurationOnPurchase} seconds")}.";

        public override void Init()
        {
            mochaBuffActive = Content.CreateAndAddBuff(
                "bdCoffeeActive",
                mochaCustomSprite,
                new Color(0.6f, 0.3f, 0.1f),
                true, false
                );
            mochaBuffInactive = Content.CreateAndAddBuff(
                "bdCoffeeInctive",
                mochaCustomSprite,
                new Color(0.1f, 0.1f, 0.2f),
                false, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnBuffFinalStackLost += MochaExpiredBuff;
            On.RoR2.Items.MultiShopCardUtils.OnPurchase += MochaExtend;
            TeleporterInteraction.onTeleporterBeginChargingGlobal += MochaExtendTP;
            GetStatCoefficients += MochaSpeed;
            IL.RoR2.CharacterBody.RecalculateStats += RemoveMochaStats;
        }

        public static void RemoveMochaStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", nameof(DLC1Content.Items.AttackSpeedAndMoveSpeed)),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(RemoveMochaStats));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        public static void MochaExtendTP(TeleporterInteraction obj)
        {
            if (NetworkServer.active)
                ExtendMochaBuff(obj.chargeActivatorServer.GetComponent<CharacterBody>());
        }

        public static void MochaExtend(On.RoR2.Items.MultiShopCardUtils.orig_OnPurchase orig, CostTypeDef.PayCostContext context, int moneyCost)
        {
            orig(context, moneyCost);
            if (NetworkServer.active)
                ExtendMochaBuff(context.activatorBody);
        }

        public static void ExtendMochaBuff(CharacterBody body)
        {
            if (!body)
                return;
            int buffCount = body.GetBuffCount(mochaBuffActive);
            if (buffCount <= 0)
                return;

            float newBuffCount = Mathf.Min(buffCount + mochaDurationOnPurchase, mochaDurationOnEntry - 1);
            for (int i = buffCount; i < newBuffCount; i++)
            {
                body.AddTimedBuffAuthority(mochaBuffActive.buffIndex, i + 1);
            }
        }


        public static void MochaExpiredBuff(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            if (buffDef == mochaBuffActive)
            {
                self.AddBuff(mochaBuffInactive);
            }
            orig(self, buffDef);
        }

        public static void MochaSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            //Debug.Log("dsfjhgbds");
            int mochaCount = instance.GetCount(sender);
            if (mochaCount <= 0)
                return;
            float spdBuff = spdBoostFree;
            float cdrBoost = Mathf.Pow(1 - cdrBoostFree, mochaCount);
            if (sender.HasBuff(mochaBuffActive))
            {
                spdBuff += spdBoostBuff;
                cdrBoost *= Mathf.Pow(1 - cdrBoostBuff, mochaCount);
            }
            args.moveSpeedMultAdd += spdBuff * mochaCount;
            args.attackSpeedMultAdd += spdBuff * mochaCount;

            args.allSkills.cooldownMultiplier *= cdrBoost;
        }
    }

    public class BorboMochaBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => DLC1Content.Items.AttackSpeedAndMoveSpeed;
        int cachedMochaCount = 0;
        bool addingBuffs = false;
        float durationPerBuff = 1; //in seconds

        private void Start()
        {
            OnInventoryRefresh();
        }
        private void OnDestroy()
        {
            stack = 0;
            OnInventoryRefresh();
        }

        public override void OnInventoryRefresh()
        {
            base.OnInventoryRefresh();

            if (stack == 0 /*&& !body.inventory.inventoryDisabled*/)
            {
                //body.ClearTimedBuffs(Mocha.mochaBuffActive);
                //body.RemoveBuff(Mocha.mochaBuffInactive);
                return;
            }

            if (cachedMochaCount < stack)
            {
                //if already had mochas and gained more, give duration from pickup
                if (cachedMochaCount != 0)
                    SetMochaTime(Mocha.mochaDurationOnPickup);
                //if had no mochas, give duration on entry, but only if mocha hasnt already expired
                else if (!body.HasBuff(Mocha.mochaBuffInactive))
                    SetMochaTime(Mocha.mochaDurationOnEntry);
            }
            cachedMochaCount = stack;
        }

        private void SetMochaTime(int targetCount)
        {
            int startingBuffCount = 0;
            if (!NetworkServer.active)
                return;

            if (body.HasBuff(Mocha.mochaBuffInactive))
                body.RemoveBuff(Mocha.mochaBuffInactive);
            else
                startingBuffCount = body.GetBuffCount(Mocha.mochaBuffActive);

            float endBuffCount = targetCount / durationPerBuff;
            if (endBuffCount > startingBuffCount)
            {
                for (int i = startingBuffCount; i < endBuffCount; i++)
                    AddMochaBuff((i + 1) * durationPerBuff);
            }
        }

        private void AddMochaBuff(float duration)
        {
            body.AddTimedBuff(Mocha.mochaBuffActive, duration);
        }
    }
}
