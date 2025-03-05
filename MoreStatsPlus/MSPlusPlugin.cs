using BepInEx;
using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Security;
using System.Security.Permissions;
using static MoreStats.StatHooks;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MoreStatsPlus
{
    [BepInPlugin(guid, modName, version)]
    public class MSPlusPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "MoreStatsPlus";
        public const string version = "1.0.0";
        #endregion

        public static ConfigFile CustomConfigFile;
        public static ConfigEntry<bool> enablePearlChanges;
        public static ConfigEntry<bool> enableTonicChanges;
        public static ConfigEntry<bool> enableAfflictionChanges;

        void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\MoreStatsPlus.cfg", true);

            enablePearlChanges = CustomConfigFile.Bind<bool>(
                "MoreStatsPlus",
                "Enable Pearl Rework",
                true,
                "Set to true to use morestats pearl, set to false for vanilla pearl."
                );
            enableTonicChanges = CustomConfigFile.Bind<bool>(
                "MoreStatsPlus",
                "Enable Tonic Rework",
                true,
                "Set to true to use morestats tonic, set to false for vanilla tonic."
                );
            enableAfflictionChanges = CustomConfigFile.Bind<bool>(
                "MoreStatsPlus",
                "Enable Affliction Rework",
                true,
                "Set to true to use morestats affliction, set to false for vanilla affliction."
                );
            //GetMoreStatCoefficients += MoreStatsPlusStats;
            //GetMoreStatCoefficients += MorePearlStats;
            //GetMoreStatCoefficients += MoreTonicStats;
            GetMoreStatCoefficients += MoreAfflictionStats;

            LanguageAPI.Add("EQUIPMENT_TONIC_DESC", $"Drink the Tonic, gaining a boost for 20 seconds. " +
                $"Increases <style=cIsDamage>damage</style> by <style=cIsDamage>+100%</style>. " +
                $"Increases <style=cIsDamage>attack speed</style> by <style=cIsDamage>+70%</style>. " +
                $"Increases <style=cIsDamage>armor</style> by <style=cIsDamage>+20</style>. " +
                $"Increases <style=cIsHealing>maximum health</style> by <style=cIsHealing>+50%</style>. " +
                $"Increases <style=cIsHealing>passive health regeneration</style> by <style=cIsHealing>+300%</style>. " +
                $"Increases <style=cIsHealing>healing</style> by <style=cIsHealing>+70%</style>." +
                $"Increases <style=cIsUtility>movespeed</style> by <style=cIsUtility>+30%</style>." +
                $"Increases <style=cIsUtility>Luck</style> by <style=cIsUtility>+0.5</style>." +
                $"Increases <style=cIsUtility>jumps</style> by <style=cIsUtility>1</style>." +
                $"Reduces <style=cIsUtility>shield regeneration delay</style> by <style=cIsUtility>-2 seconds</style>." +
                $"Reduces <style=cIsUtility>barrier decay</style> by <style=cIsUtility>-50%</style>." +
                $"" +
                $"\n\nWhen the Tonic wears off, you have a <style=cIsHealth>20%</style> chance to gain a <style=cIsHealth>Tonic Affliction, reducing all of your stats</style> by <style=cIsHealth>-5%</style> <style=cStack>(-5% per stack)</style>.");
        }
        private void MoreStatsPlusStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv != null)
            {
                int irradiantPearlCount = inv.GetItemCount(RoR2Content.Items.ShinyPearl);
                if (irradiantPearlCount > 0 && enablePearlChanges.Value)
                {
                    float pow = MathF.Pow(1.1f, irradiantPearlCount);
                    args.luckAdd += 0.1f * irradiantPearlCount;
                    args.healingPercentIncreaseMult *= pow;
                    args.barrierDecayRatePercentDecreaseDiv *= pow;
                    args.shieldDelayPercentDecreaseDiv *= pow;
                }
                int afflictionCount = inv.GetItemCount(RoR2Content.Items.TonicAffliction);
                if (afflictionCount > 0 && enableAfflictionChanges.Value)
                {
                    float pow = MathF.Pow(0.95f, afflictionCount);
                    args.luckAdd -= 0.05f * afflictionCount;
                    args.healingPercentIncreaseMult *= pow;
                    args.barrierDecayRatePercentDecreaseDiv *= pow;
                    args.shieldDelayPercentDecreaseDiv *= pow;
                }
            }
            if (sender.HasBuff(RoR2Content.Buffs.TonicBuff) && enableTonicChanges.Value)
            {
                args.luckAdd += 0.5f;
                args.healingPercentIncreaseMult *= 1.7f;
                args.shieldDelaySecondsIncreaseAddPreMult -= 2f;
                args.barrierDecayRatePercentDecreaseDiv *= 2f;
                args.jumpCountAdd += 1;
            }
        }

        private void MoreAfflictionStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv != null)
            {
                int afflictionCount = inv.GetItemCount(RoR2Content.Items.TonicAffliction);
                if (afflictionCount > 0)
                {
                    float pow = MathF.Pow(0.95f, afflictionCount);
                    args.luckAdd -= 0.05f * afflictionCount;
                    args.healingPercentIncreaseMult *= pow;
                    args.barrierDecayRatePercentDecreaseDiv *= pow;
                    args.shieldDelayPercentDecreaseDiv *= pow;
                }
            }
        }

        private void MoreTonicStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.HasBuff(RoR2Content.Buffs.TonicBuff))
            {
                args.luckAdd += 0.5f;
                args.healingPercentIncreaseMult *= 1.7f;
                args.shieldDelaySecondsIncreaseAddPreMult -= 2f;
                args.barrierDecayRatePercentDecreaseDiv += 2f;
                args.jumpCountAdd += 1;
            }
        }

        private void MorePearlStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv != null)
            {
                int irradiantPearlCount = inv.GetItemCount(RoR2Content.Items.ShinyPearl);
                if (irradiantPearlCount > 0)
                {
                    float pow = MathF.Pow(1.1f, irradiantPearlCount);
                    args.luckAdd += 0.1f * irradiantPearlCount;
                    args.healingPercentIncreaseMult *= pow;
                    args.barrierDecayRatePercentDecreaseDiv *= pow;
                    args.shieldDelayPercentDecreaseDiv *= pow;
                }
            }
        }
    }
}
