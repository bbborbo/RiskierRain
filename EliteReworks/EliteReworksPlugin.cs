using BepInEx;
using BepInEx.Configuration;
using EliteReworks.Components;
using EliteReworks.EliteReworks;
using EliteReworks.Equipment;
using EliteReworks.Modules;
using HG;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.ContentManagement;
using RoR2.ExpansionManagement;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace EliteReworks
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(RecalculateStatsAPI), nameof(DotAPI))]
    [BepInPlugin(guid, modName, version)]
    public class EliteReworksPlugin : BaseUnityPlugin
    {
        public static EliteReworksPlugin instance;
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityAspectGaming";
        public const string version = "1.0.0";

        public const string DEVELOPER_PREFIX = "FRUIT";
        public static AssetBundle mainAssetBundle => CommonAssets.mainAssetBundle;

        public static ExpansionDef expansionDefSS2 = null;

        float softEliteHealthBoostCoefficient = 2f; //3
        float rareEliteHealthBoostCoefficient = 4f; //5
        float baseEliteHealthBoostCoefficient = 3f; //4
        float T2EliteHealthBoostCoefficient = 9; //18
        float rareEliteDamageBoostCoefficient = 2f; //2.5f
        float baseEliteDamageBoostCoefficient = 1.5f; //2
        float T2EliteDamageBoostCoefficient = 4f; //6

        public static ConfigFile CustomConfigFile;


        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void SetExpansion()
        {
            expansionDefSS2 = SwanSongExtended.SwanSongPlugin.expansionDefSS2;
        }
        void Awake()
        {
            instance = this;

            Modules.Config.Init();
            Log.Init(Logger);

            Modules.Language.Init();
            Modules.Hooks.Init();
            Modules.CommonAssets.Init();
            InitializeContent();

            if(Bind("Change Elite Stats"))
            {
                RoR2Application.onLoad += ChangeEliteTierStats;
            }
            if(Bind("Add Periodical OnHitAll To BeetleGuard Sunder (Affects Overloading Orbs)"))
            {
                //BuffSunder(Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Beetle/Sunder.prefab").WaitForCompletion());
                AssetReferenceT<GameObject> ref1 = new AssetReferenceT<GameObject>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_BeetleGuard.Sunder_prefab);
                AssetAsyncReferenceManager<GameObject>.LoadAsset(ref1).Completed += (ctx) => BuffSunder(ctx.Result);
            }

            bool Bind(string configName, string configDesc = "")
            {
                return GetConfigBool(true, configName, configDesc);// CustomConfigFile.Bind<bool>("Elite Reworks", configName, true, configDesc).Value;
            }
        }

        private void InitializeContent()
        {
            Type[] allTypes = Assembly.GetExecutingAssembly().GetTypes();

            //BeginInitializing<SurvivorBase>(allTypes, "SwanSongSurvivors.txt");

            ///items
            ///interactables
            ///skills
            ///equipment
            ///elites
            ///artifacts
            ///scavengers
            BeginInitializing<EliteReworkBase>(allTypes, "EliteReworks.txt");

            BeginInitializing<EquipmentBase>(allTypes, "EliteReworksEquipment.txt");
        }
        #region content initialization
        private void BeginInitializing<T>(Type[] allTypes, string fileName = "") where T : SharedBase
        {
            Type baseType = typeof(T);
            //base types must be a base and not abstract
            if (!baseType.IsAbstract)
            {
                Log.Error(Log.Combine() + "Incorrect BaseType: " + baseType.Name);
                return;
            }


            IEnumerable<Type> objTypesOfBaseType = allTypes.Where(type => !type.IsAbstract && type.IsSubclassOf(baseType));

            if (objTypesOfBaseType.Count() <= 0)
                return;

            Log.Debug(Log.Combine(baseType.Name) + "Initializing");

            foreach (var objType in objTypesOfBaseType)
            {
                string s = Log.Combine(baseType.Name, objType.Name);
                Log.Debug(s);
                T obj = (T)System.Activator.CreateInstance(objType);
                if (ValidateBaseType(obj as SharedBase))
                {
                    Log.Debug(s + "Validated");
                    InitializeBaseType(obj as SharedBase);
                    Log.Debug(s + "Initialized");
                }
            }

            if (!string.IsNullOrEmpty(fileName))
                Modules.Language.TryPrintOutput(fileName);
        }

        bool ValidateBaseType(SharedBase obj)
        {
            bool enabled = obj.isEnabled;
            if (obj.lockEnabled)
                return enabled;
            return obj.Bind(enabled, "Should This Content Be Enabled");
        }
        void InitializeBaseType(SharedBase obj)
        {
            obj.Init();
        }
        #endregion


        private void BuffSunder(GameObject sunderPrefab)
        {
            ProjectileController pc = sunderPrefab.GetComponent<ProjectileController>();
            ProjectileDamage pd = sunderPrefab.GetComponent<ProjectileDamage>();
            OnHitAllInterval ohai = sunderPrefab.AddComponent<OnHitAllInterval>();
            ohai.pc = pc;
            ohai.pd = pd;
            ohai.interval = 0.25f;
        }

        private void ChangeEliteTierStats()
        {
            RoR2Content.Elites.Fire.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Fire.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.FireHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.FireHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            RoR2Content.Elites.Ice.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Ice.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.IceHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.IceHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            RoR2Content.Elites.Lightning.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.Lightning.healthBoostCoefficient = baseEliteHealthBoostCoefficient;
            RoR2Content.Elites.LightningHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            RoR2Content.Elites.LightningHonor.healthBoostCoefficient = baseEliteHealthBoostCoefficient / 2;

            RoR2Content.Elites.Poison.damageBoostCoefficient = T2EliteDamageBoostCoefficient;
            RoR2Content.Elites.Poison.healthBoostCoefficient = T2EliteHealthBoostCoefficient;

            RoR2Content.Elites.Haunted.damageBoostCoefficient = T2EliteDamageBoostCoefficient;
            RoR2Content.Elites.Haunted.healthBoostCoefficient = T2EliteHealthBoostCoefficient;

            DLC1Content.Elites.Earth.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC1Content.Elites.Earth.healthBoostCoefficient = softEliteHealthBoostCoefficient;
            DLC1Content.Elites.EarthHonor.damageBoostCoefficient = baseEliteDamageBoostCoefficient;
            DLC1Content.Elites.EarthHonor.healthBoostCoefficient = softEliteHealthBoostCoefficient / 2;

            DLC2Content.Elites.Aurelionite.damageBoostCoefficient = rareEliteDamageBoostCoefficient;
            DLC2Content.Elites.Aurelionite.healthBoostCoefficient = rareEliteHealthBoostCoefficient;
            DLC2Content.Elites.AurelioniteHonor.damageBoostCoefficient = rareEliteDamageBoostCoefficient;
            DLC2Content.Elites.AurelioniteHonor.healthBoostCoefficient = rareEliteHealthBoostCoefficient / 2;

            DLC2Content.Elites.Bead.damageBoostCoefficient = T2EliteDamageBoostCoefficient;
            DLC2Content.Elites.Bead.healthBoostCoefficient = T2EliteHealthBoostCoefficient;
        }
        public static bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
        {
            return ConfigManager.DualBindToConfig<bool>("Elite Reworks", Modules.Config.MyConfig, packetTitle, defaultValue, desc);
            //if (desc != "")
            //{
            //    return CustomConfigFile.Bind<bool>("Packets - See README For Details.",
            //        packetTitle + " Packet", defaultValue,
            //        $"The changes in this Packet will be enabled if set to true.").Value;
            //}
            //return CustomConfigFile.Bind<bool>("Packets",
            //    packetTitle + " Packet", defaultValue,
            //    "(The following changes will be enabled if set to true) " + desc).Value;
        }

        #region modify items and equipments
        static public ItemDef RetierItem(string itemName, ItemTier tier = ItemTier.NoTier)
        {
            ItemDef def = LoadItemDef(itemName);
            def = RetierItem(def, tier);
            return def;
        }

        static public ItemDef RetierItem(ItemDef def, ItemTier tier = ItemTier.NoTier)
        {
            if (def != null)
            {
                //def._itemTierDef = ItemTierCatalog.GetItemTierDef(tier);
                def.tier = tier;
                def.deprecatedTier = tier;
            }
            return def;
        }
        static ItemDef LoadItemDef(string name)
        {
            ItemDef itemDef = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/" + name);
            return itemDef;
        }
        #endregion
    }
}
