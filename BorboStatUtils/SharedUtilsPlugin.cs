using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RainrotSharedUtils.Frost;
using RainrotSharedUtils.Shelters;
using RoR2;
using System;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RainrotSharedUtils
{
    //[BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
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
        public const string version = "1.1.1";
        #endregion

        public const string shelterKeywordToken = "2R4R_SHELTER_KEYWORD";
        public const string executeKeywordToken = "2R4R_EXECUTION_KEYWORD";
        public const string noAttackSpeedKeywordToken = "2R4R_NOATTACKSPEED_KEYWORD";
        public const string sparkPickupKeywordToken = "2R4R_SPARKPICKUP_KEYWORD";
        public const float survivorExecuteThreshold = 0.15f;

        public void Awake()
        {
            Assets.Init();
            ShelterUtilsModule.Init();
            FrostUtilsModule.Init();
            Hooks.DoHooks();

            LanguageAPI.Add(executeKeywordToken,
                $"<style=cKeywordName>Finisher</style>" +
                $"<style=cSub>Enemies targeted by this skill can be " +
                $"<style=cIsHealth>instantly killed</style> if below " +
                $"<style=cIsHealth>{survivorExecuteThreshold * 100}% health</style>.</style>");
            LanguageAPI.Add(noAttackSpeedKeywordToken,
                $"<style=cKeywordName>Exacting</style>" +
                $"<style=cSub>This skill <style=cIsHealth>does not gain attack speed bonuses</style>. " +
                $"Instead, attack speed <style=cIsDamage>increases total damage</style>.</style>");
            LanguageAPI.Add(shelterKeywordToken,
                $"<style=cKeywordName>Shelter</style>" +
                $"<style=cSub>Protects from storms and fog.</style>");
            LanguageAPI.Add(sparkPickupKeywordToken,
                $"<style=cKeywordName>Energizing Sparks</style>" +
                $"<style=cSub>Creates <style=cIsDamage>spark pickups</style> that increase the " +
                $"<style=cIsDamage>attack speed</style> of all allies within <style=cIsDamage>{Assets.nebulaBoosterBuffRadius}m</style> " +
                $"by <style=cIsDamage>{Assets.sparkBoosterAspdBonus * 100}%</style> for {Assets.nebulaBoosterBuffDuration} seconds. " +
                $"Can stack up to {Assets.maxNebulaBoosterStackCount} times.</style>");
        }
        //public void FixedUpdate()
        //{
        //    FrostUtilsModule.FixedUpdate();
        //}
    }
}
