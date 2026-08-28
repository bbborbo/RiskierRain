using System;
using System.Collections.Generic;
using System.Text;

namespace MoreStats
{
    public static class BaseStats
    {
        /// <summary>
        /// more or less a version indicator to automatically deploy shield conversion changes when the hook is ready, if ever.
        /// please do not modify this externally unless youre doing so for compat and are willing to accept the consequences.
        /// and if you wonder why isnt this readonly the reason is i dont care what you do as long as you dont make it my problem
        /// </summary>
        public static bool ApplyShieldConversionHook = false;
        public static float PerfectedHealthBonus = 0.25f;
        public static float TranscendenceHealthBonusBase = 0.5f;
        public static float TranscendenceHealthBonusStack = 0.25f;
        public static float TranscendenceShieldConversionFractionBase = 1f;
        public static float TranscendenceShieldConversionFractionStack = 0.0f;
        public static float PerfectedShieldConversionFraction = 1;
        public static float OverloadingShieldConversionFraction = 0.5f;

        public static float BaseShieldDelaySeconds = 7f;
        public static float MinShieldDelaySeconds = 1f;

        public static float BarrierLowDecayFactor = 0.5f;
        public static float BarrierHighDecayFactor = 3f;
        public static float BarrierDecayStaticMaxHealthTime = 30;

        public static int FeatherJumpCountBase = 1;
        public static int FeatherJumpCountStack = 1;

        public static bool IncludeStrangeScrapInScrapTotal = true;
    }
}
