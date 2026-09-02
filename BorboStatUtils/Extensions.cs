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
    }
}
