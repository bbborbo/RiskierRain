using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        BuffDef brittleCrownBuff => Modules.CommonAssets.brittleCrownCursePurchase;
        public static int brittleCrownStealCountBase = 3;
        public static int brittleCrownStealCountStack = 2;
        public void BrittleCrownChanges()
        {
            LanguageAPI.Add("ITEM_GOLDONHIT_NAME", "Sunken Crown");
            LanguageAPI.Add("ITEM_GOLDONHIT_PICKUP", $"Steal from chests... {HealthColor("at the cost of health.")}");
            LanguageAPI.Add("ITEM_GOLDONHIT_DESC", $"Allows interacting with chests without the ability to afford them, " +
                $"opening the chest {UtilityColor("without spending ANY money")}. " +
                $"Stealing from chests costs {HealthColor("[ 17% / 33% / 80% ]")} " +
                $"of your {HealthColor("maximum health")}, depending on the size of the chest. " +
                $"Can steal up to {brittleCrownStealCountBase} {StackText($"+{brittleCrownStealCountStack}")} times per stage.");

            IL.RoR2.HealthComponent.TakeDamageProcess += RemoveCrownPenalty;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += RemoveCrownReward;
            On.RoR2.PurchaseInteraction.CanBeAffordedByInteractor += PurchaseInteraction_CanBeAffordedByInteractor;
            //On.RoR2.PurchaseInteraction.OnInteractionBegin += PurchaseInteraction_OnInteractionBegin;
            //On.RoR2.CostTypeCatalog.Init += CTCInit;
            On.RoR2.CharacterBody.OnInventoryChanged += AddBrittleCrownBehavior;
        }

        private void AddBrittleCrownBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<BrittleCrownBehavior>(self.inventory.GetItemCount(RoR2Content.Items.GoldOnHit));
            }
        }

        private void RemoveCrownReward(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "GoldOnHit"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
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

            if (activator.gameObject.TryGetComponent(out activatorBody))
            {
                if (activatorBody.HasBuff(brittleCrownBuff))
                {
                    if (self.costType == CostTypeIndex.Money && self.saleStarCompatible && activatorBody.master.money < self.cost)
                    {
                        int common = Run.instance.GetDifficultyScaledCost(1, Stage.instance.entryDifficultyCoefficient);
                        int uncommon = Run.instance.GetDifficultyScaledCost(45, Stage.instance.entryDifficultyCoefficient);
                        int rare = Run.instance.GetDifficultyScaledCost(245, Stage.instance.entryDifficultyCoefficient);
                        if (self.cost >= common && self.cost < uncommon)
                        {
                            CounterfeitCalculations(activatorBody, 2);
                        }
                        else if (self.cost >= uncommon && self.cost < rare)
                        {
                            CounterfeitCalculations(activatorBody, 5);
                        }
                        else
                        {
                            CounterfeitCalculations(activatorBody, 40);
                        }
                        //self.cost = 0;
                        canPurchase = true;
                    }
                }
            }

            return canPurchase;
        }
        public void CounterfeitCalculations(CharacterBody activator, int buffCount)
        {
            for(int i = 0; i < buffCount; i++)
            {
                activator.AddBuff(DLC2Content.Buffs.SoulCost);
            }
            activator.AddBuff(DLC2Content.Buffs.FreeUnlocks);

            Util.PlaySound("sfx_lunarmoney_start", activator.gameObject);

            if (NetworkServer.active)
            {
                activator.RemoveBuff(brittleCrownBuff);
            }
        }
    }

    public class BrittleCrownBehavior : CharacterBody.ItemBehavior
    {
        public void Start()
        {
            if (NetworkServer.active)
            {
                body.SetBuffCount(Modules.CommonAssets.brittleCrownCursePurchase.buffIndex, 
                    SwanSongPlugin.brittleCrownStealCountBase + SwanSongPlugin.brittleCrownStealCountStack * (this.stack - 1));
            }
        }

        public void OnDisable()
        {
            if (NetworkServer.active)
            {
                body.SetBuffCount(Modules.CommonAssets.brittleCrownCursePurchase.buffIndex, 0);
            }
        }
    }
}
