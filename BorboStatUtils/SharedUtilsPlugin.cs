using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RainrotSharedUtils
{
    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public class SharedUtilsPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "RainrotSharedUtils";
        public const string version = "1.0.2";
        #endregion

        public const string executeKeywordToken = "2R4R_EXECUTION_KEYWORD";
        public const float survivorExecuteThreshold = 0.15f;

        public void Awake()
        {
            Hooks.DoHooks();

            LanguageAPI.Add(executeKeywordToken,
                $"<style=cKeywordName>Finisher</style>" +
                $"<style=cSub>Enemies targeted by this skill can be " +
                $"<style=cIsHealth>instantly killed</style> if below " +
                $"<style=cIsHealth>{survivorExecuteThreshold  * 100}% health</style>.</style>");
        }
    }
}
