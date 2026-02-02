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
        public const string version = "1.3.6";
        #endregion

        public const string noAttackSpeedKeywordName = "Exacting";
        public const string shelterKeywordToken = "2R4R_SHELTER_KEYWORD";
        public const string executeKeywordToken = "2R4R_EXECUTION_KEYWORD";
        /// <summary>
        /// Multiply the attack's total damage
        /// </summary>
        public const string noAttackSpeedMultiplicativeKeywordToken = "2R4R_NOATTACKSPEEDMULTIPLICATIVE_KEYWORD";
        /// <summary>
        /// Added to the attack's damage multiplier
        /// </summary>
        public const string noAttackSpeedAdditiveKeywordToken = "2R4R_NOATTACKSPEEDADDITIVE_KEYWORD";
        /// <summary>
        /// Simple form -- deprecated. Use Additive or Multiplicative versions instead.
        /// </summary>
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
                FormatExacting("", "<style=cIsDamage>increase</style> this attack's <style=cIsDamage>total damage</style>."
                ));
            LanguageAPI.Add(noAttackSpeedMultiplicativeKeywordToken,
                FormatExacting("Multiplicative", "<style=cIsDamage>multiply</style> this attack's <style=cIsDamage>total damage</style>."
                ));
            LanguageAPI.Add(noAttackSpeedAdditiveKeywordToken,
                FormatExacting("Additive", "are <style=cIsDamage>added to</style> this attack's <style=cIsDamage>damage multiplier</style>."
                ));

            string FormatExacting(string exactingType, string attackSpeedBonusesThen)
            {
                string keywordName = 
                    string.IsNullOrWhiteSpace(exactingType) ? noAttackSpeedKeywordName 
                    : $"{noAttackSpeedKeywordName} ({exactingType})";
                return $"<style=cKeywordName>{keywordName}</style>" +
                $"<style=cSub>This skill will always take the same amount of time to cast, " +
                $"and is <style=cIsHealth>unaffected by attack speed bonuses</style>. " +
                $"Instead, attack speed bonuses {attackSpeedBonusesThen}.</style>";
            }

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

        public static void DebugBreakpoint(string methodName, int breakpointNumber = -1)
        {
            string s = $"{modName}: {methodName} IL hook failed!";
            if (breakpointNumber >= 0)
                s += $" (breakpoint {breakpointNumber})";
            Debug.LogError(s);
        }

        //public void FixedUpdate()
        //{
        //    FrostUtilsModule.FixedUpdate();
        //}
    }
}
