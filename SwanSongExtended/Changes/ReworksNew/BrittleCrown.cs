using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class BrittleCrown : ReworkBase<BrittleCrown>
    {
        public static BuffDef brittleCrownCursePurchase;
        public static int brittleCrownStealCountBase = 2;
        public static int brittleCrownStealCountStack = 1;
        public static float crownCommonStealSoulCost = 0.25f;
        public static float crownUncommonStealSoulCost = 0.5f;
        public static float crownRareStealSoulCost = 0.8f;
        private string common = Tools.ConvertDecimal(crownCommonStealSoulCost);
        private string uncommon = Tools.ConvertDecimal(crownUncommonStealSoulCost);
        private string rare = Tools.ConvertDecimal(crownRareStealSoulCost);

        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_GoldOnHit.GoldOnHit_asset;

        public override string ItemName => "Sunken Crown";

        public override string ItemPickupDesc => $"Steal from chests... {HealthColor("at the cost of health.")}";

        public override string ItemFullDesc => $"Allows interacting with chests without the ability to afford them, " +
                $"opening the chest {UtilityColor("without spending ANY money")}. " +
                $"Stealing from chests costs {HealthColor($"[ {common} / {uncommon} / {rare} ]")} " +
                $"of your {HealthColor("maximum health")}, depending on the size of the chest. " +
                $"Can steal up to {brittleCrownStealCountBase} {StackText($"+{brittleCrownStealCountStack}")} times per stage.";

        public override void Init()
        {
            brittleCrownCursePurchase = Content.CreateAndAddBuff(
                "bdBrittleCrownCursePurchase",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(),
                Color.cyan, true, false);
            base.Init();
        }
        public override void Hooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += RemoveCrownPenalty;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveCrownReward;
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += PurchaseInteraction_CanBeAffordedByInteractor;
        }

        private void RemoveCrownReward(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "GoldOnHit"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        private void RemoveCrownPenalty(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent.ItemCounts>(nameof(HealthComponent.ItemCounts.goldOnHit))
                );
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4, 0);
        }

        private void CTCInit(On.RoR2.CostTypeCatalog.orig_Init orig)
        {
            orig();
            CostTypeDef ctd = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.Money);
            var method = ctd.payCost.Method;
            ILHook hook = new ILHook(method, PatchMoneyCostForBrittleCrown);
        }

        private void PatchMoneyCostForBrittleCrown(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<CostTypeDef.PayCostContext>>((ctx) =>
            {
                CharacterBody activatorBody = ctx.activatorBody;
                if (activatorBody && ctx.activatorMaster && ctx.activatorMaster.money < ctx.cost)
                {
                    ctx.cost = 0;
                }
            });
        }

        private bool PurchaseInteraction_CanBeAffordedByInteractor(On.RoR2.PurchaseInteraction.orig_CanBeAffordedByInteractor orig, RoR2.PurchaseInteraction self, RoR2.Interactor activator)
        {
            bool canPurchase = orig.Invoke(self, activator);
            if (canPurchase)
                return canPurchase;

            CharacterBody activatorBody = null;

            if (self.costType == CostTypeIndex.Money && self.saleStarCompatible)
            {
                if (activator.gameObject.TryGetComponent(out activatorBody))
                {
                    if (activatorBody.HasBuff(brittleCrownCursePurchase) && activatorBody.master.money < self.cost)
                    {
                        int common = 1;
                        int uncommon = Run.instance.GetDifficultyScaledCost(45, Stage.instance.entryDifficultyCoefficient);
                        int rare = Run.instance.GetDifficultyScaledCost(245, Stage.instance.entryDifficultyCoefficient);
                        if (self.cost >= common && self.cost < uncommon)
                        {
                            CounterfeitCalculations(activatorBody, crownCommonStealSoulCost);
                        }
                        else if (self.cost >= uncommon && self.cost < rare)
                        {
                            CounterfeitCalculations(activatorBody, crownUncommonStealSoulCost);
                        }
                        else
                        {
                            CounterfeitCalculations(activatorBody, crownRareStealSoulCost);
                        }
                        //self.cost = 0;
                        canPurchase = true;
                    }
                }
            }

            return canPurchase;
        }
        public void CounterfeitCalculations(CharacterBody activator, float soulCost)
        {
            //for(int i = 0; i < buffCount; i++)
            //{
            //    activator.AddBuff(DLC2Content.Buffs.SoulCost);
            //}
            BetterSoulCost.SoulCostPlugin.AddSoulCostToBody(activator, soulCost);

            Util.PlaySound("sfx_lunarmoney_start", activator.gameObject);

            if (NetworkServer.active)
            {
                activator.AddBuff(DLC2Content.Buffs.FreeUnlocks);
                activator.RemoveBuff(brittleCrownCursePurchase);
            }
        }
    }
    public class BrittleCrownBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => RoR2Content.Items.GoldOnHit;
        public void Start()
        {
            body.SetBuffCount(BrittleCrown.brittleCrownCursePurchase.buffIndex,
                BrittleCrown.brittleCrownStealCountBase + BrittleCrown.brittleCrownStealCountStack * (this.stack - 1));
        }

        public void OnDisable()
        {
            body.SetBuffCount(BrittleCrown.brittleCrownCursePurchase.buffIndex, 0);
        }
    }
}
