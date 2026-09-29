using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using static BossDropRework.BossDropReworkPlugin;

namespace BossDropRework.Modules
{
    public static class Tools
    {
        internal static bool isLoaded(string modguid)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
            {
                string key = keyValuePair.Key;
                PluginInfo value = keyValuePair.Value;
                bool flag = key == modguid;
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }
    }
    public static class Assets
    {
        /// <summary>
        /// Loads an embedded asset bundle
        /// </summary>
        /// <param name="resourceBytes">The bytes returned by Properties.Resources.ASSETNAME</param>
        /// <returns>The loaded bundle</returns>
        internal static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        internal static AssetBundle LoadAssetBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName))
            {
                return loadedBundles[bundleName];
            }

            AssetBundle assetBundle = null;
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(BossDropReworkPlugin.PInfo.Location), bundleName));

            loadedBundles[bundleName] = assetBundle;

            return assetBundle;
        }
    }
    public static class Bindings
    {
        public static ConfigEntry<float> LesserDropChance { get; set; }
        public static ConfigEntry<float> EliteDropChance { get; set; }
        public static ConfigEntry<float> ChampionDropChance { get; set; }
        public static ConfigEntry<float> ChampionEliteDropChance { get; set; }
        public static ConfigEntry<float> SpecialBossDropChance { get; set; }
        public static ConfigEntry<bool> ForceDropsFromAurelionite { get; set; }
        public static ConfigEntry<bool> ReworkTricorn { get; set; }
        public static ConfigEntry<bool> ReworkVultures { get; set; }
        public static ConfigEntry<bool> ReworkHordeOfMany { get; set; }
        public static ConfigEntry<bool> ReworkPrinters { get; set; }
        public static ConfigEntry<float> TricornDamageCoefficient { get; set; }
        public static ConfigEntry<float> TricornProcCoefficient { get; set; }

        public static bool BindSection(string sectionName)
        {
            return CustomConfigFile.Bind<bool>("Fruity Boss Drop : Full Section Config",
                sectionName,
                true,
                "Vanilla is FALSE. Set to false if you wish to disable changes made to an entire item or group of items.").Value;
        }
        internal static ConfigFile CustomConfigFile { get; set; }
        public static void Init()
        {
            string section = "Fruity Boss Drop : ";

            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);
            CustomConfigFile.SaveOnConfigSet = false;

            LesserDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Non-Elite Lessers",
                0.5f,
                "Includes Horde of Many");
            EliteDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Elite Lessers",
                2,
                "Includes Horde of Many");
            ChampionDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Non-Elite Champions",
                6,
                "");
            ChampionEliteDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Elite Champions",
                10,
                "");
            SpecialBossDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Special Bosses",
                14,
                "Affects AWU");
            ForceDropsFromAurelionite = CustomConfigFile.Bind<bool>(
                "Trophy Drops",
                "Force Drops From Aurelionite",
                true,
                "Force Aurelionite to drop a Halcyon Seed on death, replacing the portal loot with 1 green for each player. Recommended to set to false for use with GildedCoastPlus"
                );

            TricornDamageCoefficient = CustomConfigFile.Bind<float>(
                "Tricorn",
                "Tricorn Damage Coefficient",
                70,
                "Multiply by 100 for % ie 70 is 7000%");
            TricornProcCoefficient = CustomConfigFile.Bind<float>(
                "Tricorn",
                "Tricorn Proc Coefficient",
                5,
                "Most attacks default to 1. Recommended to install ProcPatcher");
        }
        public static void Save()
        {
            CustomConfigFile.SaveOnConfigSet = true;
            CustomConfigFile.Save();
        }
    }
}
