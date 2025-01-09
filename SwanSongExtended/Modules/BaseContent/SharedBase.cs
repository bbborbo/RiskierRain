using BepInEx.Configuration;
using BepInEx.Logging;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SwanSongExtended
{
    public abstract class SharedBase
    {
        public virtual bool lockEnabled { get; } = false;
        public abstract string ConfigName { get; }
        public virtual bool isEnabled { get; } = true;
        public static ManualLogSource Logger => Log._logSource;
        public abstract AssetBundle assetBundle { get; }

        public abstract void Hooks();
        public abstract void Lang();

        public virtual void Init()
        {
            ConfigManager.HandleConfigAttributes(GetType(), ConfigName, Config.MyConfig);
            Hooks();
            Lang();
        }

        public T Bind<T>(T defaultValue, string configName, string configDesc = "")
        {
            return ConfigManager.DualBindToConfig<T>(ConfigName, Config.MyConfig, configName, defaultValue, configDesc);
        }

        public static float GetHyperbolic(float firstStack, float cap, float chance) // Util.ConvertAmplificationPercentageIntoReductionPercentage but Better :zanysoup:
        {
            if (firstStack >= cap) return cap * (chance / firstStack); // should not happen, but failsafe
            float count = chance / firstStack;
            float coeff = 100 * firstStack / (cap - firstStack); // should be good
            return cap * (1 - (100 / ((count * coeff) + 100)));
        }
    }
}
