using System;
using System.Collections.Generic;
using System.Text;

namespace MoreStats
{
    public static class BaseStats
    {
        public static float BaseShieldDelaySeconds = 7f;
        public static float MinShieldDelaySeconds = 1f;

        public static float BarrierDecayDynamicHalfLife = 0;
        public static float BarrierDecayStaticMaxHealthTime = 30;
        public static float MinBarrierDecayWithDynamicRate = 1f;

        public static int FeatherJumpCountBase = 1;
        public static int FeatherJumpCountStack = 1;
    }
}
