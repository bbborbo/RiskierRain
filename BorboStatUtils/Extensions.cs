using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils
{
    public static partial class Extensions
    {
        public static float GetStageStopwatch(this Run instance, out bool isFirstStage)
        {
            isFirstStage = true;
            if (instance == null)
                return 0;
            isFirstStage = instance.stageClearCount == 0;
            float stopwatch = instance.GetRunStopwatch();
            float entryStopwatchValue = 0;
            if (Stage.instance)
                entryStopwatchValue = Mathf.Floor(Stage.instance.entryStopwatchValue);
            return stopwatch - entryStopwatchValue;
        }
        public static bool IsInvincible(this CharacterBody self)
        {
            return
                self.HasBuff(RoR2Content.Buffs.HiddenInvincibility)
                || self.HasBuff(RoR2Content.Buffs.Immune)
                || self.HasBuff(RoR2Content.Buffs.Intangible)
                || self.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage)
                ;
        }
        public static bool IsInvincibleOrInvisible(this CharacterBody self)
        {
            return self.IsInvincible()
                || self.IsInvisible()
                ;
        }
        public static bool IsInvisible(this CharacterBody self)
        {
            return self.HasBuff(RoR2Content.Buffs.Cloak)
                || (self.TryGetComponent(out CharacterModel model) == true && model.invisibilityCount > 0)
                ;
        }
    }
}
