using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RoR2.HoldoutZoneController;

namespace SwanSongExtended.Changes.ReworksNew
{
    public class FocusedConvergence : ReworkBase<FocusedConvergence>
    {
        public static float foconMinRadius = 8f; //0
        public static float foconRadiusMultiplier = 0.5f; //0.5f
        public static float foconChargeBonus = 1f; //0.3f
        public static int foconMaxStack = 5; //3
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_FocusConvergence.FocusConvergence_asset;

        public override string ItemName => "Focused Convergence";

        public override string ItemPickupDesc => null;

        public override string ItemFullDesc => 
            $"Holdout Zones charge <style=cIsUtility>{Tools.ConvertDecimal(foconChargeBonus)} " +
                $"<style=cStack>(+{Tools.ConvertDecimal(foconChargeBonus)} per stack)</style> faster</style>, " +
                $"but are <style=cIsHealth>{Tools.ConvertDecimal(1 - foconRadiusMultiplier)} smaller</style> " +
                $"<style=cStack>(-{Tools.ConvertDecimal(1 - foconRadiusMultiplier)} per stack)</style>. " +
                $"Max of {foconMaxStack}.";

        public override void Hooks()
        {
            //IL.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRadius += FoconApplyRadius;
            On.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRadius += FoconNewRadius;
            IL.RoR2.HoldoutZoneController.FocusConvergenceController.ApplyRate += FoconApplyRate;
            IL.RoR2.HoldoutZoneController.FocusConvergenceController.DoUpdate += FoconUpdate;
        }

        private void FoconUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>("cap")
                );
            if (b1 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FoconUpdate));
                return;
            }
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

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>(nameof(HoldoutZoneController.FocusConvergenceController.convergenceRadiusDivisor))
                );
            if (b1 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FoconApplyRadius), 1);
                return;
            }
            c.Emit(OpCodes.Ldc_R4, foconMinRadius);
            c.Emit(OpCodes.Sub);

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>(nameof(HoldoutZoneController.FocusConvergenceController.convergenceRadiusDivisor))
                );
            if (b2 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FoconApplyRadius), 2);
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, 2); //foconRadiusDivisor);

            bool b3 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStindR4()
                );
            if (b3 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FoconApplyRadius), 3);
                return;
            }
            c.Emit(OpCodes.Ldc_R4, foconMinRadius);
            c.Emit(OpCodes.Add);
        }

        private void FoconApplyRate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<HoldoutZoneController.FocusConvergenceController>("convergenceChargeRateBonus")
                );
            if (b1 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FoconApplyRate));
                return;
            }
            c.EmitDelegate<Func<float, float>>((chargeBonus) =>
            {
                return foconChargeBonus;
            });
        }
    }
}
