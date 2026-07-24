using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Artifacts
{
    class SpiritArtifact : ArtifactBase<SpiritArtifact>
    {
        public override bool isEnabled => false;
        private static bool statsRecalculating = false;
        public static bool SpiritArtifactActive = false;
        public static float speedBoostMax = 1f;
        public override string ArtifactName => "Spirit";

        public override string ArtifactDescription => "Characters run and attack faster at lower health.";

        public override string ArtifactLangTokenName => "RISKIERRAINSPIRIT";

        public override Sprite ArtifactSelectedIcon => LoadArtifactIcon(fallBackOnWrench: true);

        public override Sprite ArtifactDeselectedIcon => LoadArtifactIcon(fallBackOnWrench: true);

        static ILHook aspdHook;
        static ILHook mspdHook;
        public override void Hooks()
        {
            var hookConfig = new ILHookConfig() { ManualApply = true };
            aspdHook = new ILHook(typeof(CharacterBody).GetMethod("get_attackSpeed"), SpiritStatModifier, ref hookConfig);
            mspdHook = new ILHook(typeof(CharacterBody).GetMethod("get_moveSpeed"), SpiritStatModifier, ref hookConfig);
            On.RoR2.CharacterBody.RecalculateStats += CharacterBody_RecalculateStats;
        }

        private void CharacterBody_RecalculateStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            statsRecalculating = true;
            orig(self);
            statsRecalculating = false;
        }

        public static void SpiritStatModifier(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.Goto(il.Instrs.Last());
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((valueIn, body) =>
            {
                if (statsRecalculating || !SpiritArtifactActive)
                    return valueIn;

                float inverted = 1 - body.healthComponent.combinedHealthFraction;
                return valueIn * (1 + speedBoostMax * inverted);
            });
        }

        public override void OnArtifactDisabledServer()
        {
            SpiritArtifactActive = false;
            aspdHook.Undo();
            mspdHook.Undo();
        }

        public override void OnArtifactEnabledServer()
        {
            SpiritArtifactActive = true;
            aspdHook.Apply();
            mspdHook.Apply();
        }
    }
}
