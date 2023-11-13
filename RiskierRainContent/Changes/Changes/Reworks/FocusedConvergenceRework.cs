using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.HoldoutZoneController;

namespace RiskierRainContent
{
    public partial class RiskierRainContent
    {
        public static float foconMinRadius = 8f; //0
        public static float foconRadiusMultiplier = 0.5f; //0.5f
        public static float foconChargeBonus = 1f; //0.3f
        public static int foconMaxStack = 5; //3
        public void FocusedConvergenceChanges()
        {
            //IL.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRadius += FoconApplyRadius;
            On.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRadius += FoconNewRadius;
            IL.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRate += FoconApplyRate;
            IL.RoR2.HoldoutZoneController.FocusConvergenceController.FixedUpdate += FoconUpdate;

            LanguageAPI.Add("ITEM_FOCUSEDCONVERGENCE_PICKUP", $"Increase the speed Holdout Zones charge... <color=#FF7F7F>BUT reduce the size of the zone</color>. Max of {foconMaxStack}.");
            LanguageAPI.Add("ITEM_FOCUSEDCONVERGENCE_DESC",
                $"Holdout Zones charge <style=cIsUtility>{Tools.ConvertDecimal(foconChargeBonus)} " +
                $"<style=cStack>(+{Tools.ConvertDecimal(foconChargeBonus)} per stack)</style> faster</style>, " +
                $"but are <style=cIsHealth>{Tools.ConvertDecimal(1 - foconRadiusMultiplier)} smaller</style> " +
                $"<style=cStack>(-{Tools.ConvertDecimal(1 - foconRadiusMultiplier)} per stack)</style>. " +
                $"Max of {foconMaxStack}.");
        }

        private void FoconUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>("cap")
                );
            c.EmitDelegate<Func<int, int>>((cap) =>
            {
                return foconMaxStack;
            });
        }

        private void FoconNewRadius(On.RoR2.HoldoutZoneController.FocusConvergenceController.orig_ApplyRadius orig, MonoBehaviour self, ref float radius)
        {
            FocusConvergenceController controller = self as FocusConvergenceController;
            if (controller.currentFocusConvergenceCount > 0)
            {
                radius -= foconMinRadius;
                radius *= Mathf.Pow(foconRadiusMultiplier, (float)controller.currentFocusConvergenceCount);
                radius += foconMinRadius;
            }
            //orig(self, ref radius);
        }

        private void FoconApplyRadius(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>(nameof(HoldoutZoneController.FocusConvergenceController.convergenceRadiusDivisor))
                );
            c.Emit(OpCodes.Ldc_R4, foconMinRadius);
            c.Emit(OpCodes.Sub);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>(nameof(HoldoutZoneController.FocusConvergenceController.convergenceRadiusDivisor))
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, 2); //foconRadiusDivisor);

            c.GotoNext(MoveType.Before,
                x => x.MatchStindR4()
                );
            c.Emit(OpCodes.Ldc_R4, foconMinRadius);
            c.Emit(OpCodes.Add);
        }

        private void FoconApplyRate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>("convergenceChargeRateBonus")
                );
            c.EmitDelegate<Func<float, float>>((chargeBonus) =>
            {
                return foconChargeBonus;
            });
        }
    }
}
