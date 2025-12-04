using BepInEx;
using RoR2.ExpansionManagement;
using System;
using UnityEngine;
using SurvivorTweaks.Modules;
using MissileRework;
using System.Runtime.CompilerServices;
using RoR2;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using SurvivorTweaks.Skills;
using R2API.Utils;
using R2API;
using RoR2.Skills;
using System.Security.Permissions;
using System.Security;
using SurvivorTweaks.SurvivorTweaks;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace SurvivorTweaks
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.LoadoutAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DeployableAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Withor.AcridBiteLunge", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(RecalculateStatsAPI), nameof(DotAPI))]
    [BepInPlugin(guid, modName, version)]
    public class SurvivorTweaksPlugin : BaseUnityPlugin
    {

        public static SurvivorTweaksPlugin instance;
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "SurvivorTweaks";
        public const string version = "3.0.0";

        public const string DEVELOPER_PREFIX = "FRUIT";

        public static AssetBundle mainAssetBundle => CommonAssets.mainAssetBundle;

        public static ExpansionDef expansionDefSS2 = null;
        #region asset paths
        public const string iconsPath = "";
        #endregion

        #region mods loaded
        public static bool ModLoaded(string modGuid) { return modGuid != "" && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid); }
        public static bool iabMissilesLoaded => ModLoaded(MissileRework.MissileReworkPlugin.guid);
        public static bool isAELoaded => ModLoaded("com.Borbo.ArtificerExtended");
        public static bool is2R4RLoaded => ModLoaded("com.HouseOfFruits.RiskierRain");
        public static bool isHBULoaded => ModLoaded("com.Borbo.HuntressBuffULTIMATE");
        public static bool isScepterLoaded => ModLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded => ModLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool acridLungeLoaded => ModLoaded("Withor.AcridBiteLunge");
        public static bool ucrLoaded => ModLoaded("HIFU.UltimateCustomRun");

        public static bool IsMissileArtifactEnabled()
        {
            if (ModLoaded(MissileReworkPlugin.guid))
            {
                return GetMissileArtifactEnabled();
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool GetMissileArtifactEnabled()
        {
            return RunArtifactManager.instance.IsArtifactEnabled(MissileReworkPlugin.MissileArtifact);
        }
        #endregion

        void Awake()
        {
            instance = this;

            Modules.Config.Init();
            Log.Init(Logger);

            Modules.Language.Init();
            Modules.Hooks.Init();
            Modules.CommonAssets.Init();
            Modules.AllyCaps.Init();

            InitializeContent();

            // this has to be last
            new Modules.ContentPacks().Initialize();
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void SetExpansion()
        {
            expansionDefSS2 = SwanSongExtended.SwanSongPlugin.expansionDefSS2;
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
            BeginInitializing<SurvivorTweakBase>(allTypes, "SurvivorTweaks.txt");

            BeginInitializing<SkillBase>(allTypes, "SurvivorTweaksSkills.txt");
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

        public static bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
        {
            return ConfigManager.DualBindToConfig<bool>(packetTitle, Modules.Config.MyConfig, "Should This Content Be Enabled", defaultValue, desc);
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
        public static SkillDef CloneSkillDef(SkillDef oldDef)
        {
            SkillDef skillDef = ScriptableObject.CreateInstance<SkillDef>();
            skillDef.skillName = oldDef.skillName;
            skillDef.skillNameToken = oldDef.skillNameToken;
            skillDef.skillDescriptionToken = oldDef.skillDescriptionToken;
            skillDef.icon = oldDef.icon;
            skillDef.activationStateMachineName = oldDef.activationStateMachineName;
            skillDef.activationState = oldDef.activationState;
            skillDef.interruptPriority = oldDef.interruptPriority;
            skillDef.baseRechargeInterval = oldDef.baseRechargeInterval;
            skillDef.baseMaxStock = oldDef.baseMaxStock;
            skillDef.rechargeStock = oldDef.rechargeStock;
            skillDef.requiredStock = oldDef.requiredStock;
            skillDef.stockToConsume = oldDef.stockToConsume;
            skillDef.beginSkillCooldownOnSkillEnd = oldDef.beginSkillCooldownOnSkillEnd;
            skillDef.fullRestockOnAssign = oldDef.fullRestockOnAssign;
            skillDef.dontAllowPastMaxStocks = oldDef.dontAllowPastMaxStocks;
            skillDef.resetCooldownTimerOnUse = oldDef.resetCooldownTimerOnUse;
            skillDef.isCombatSkill = oldDef.isCombatSkill;
            skillDef.cancelSprintingOnActivation = oldDef.cancelSprintingOnActivation;
            skillDef.canceledFromSprinting = oldDef.canceledFromSprinting;
            skillDef.forceSprintDuringState = oldDef.forceSprintDuringState;
            skillDef.mustKeyPress = oldDef.mustKeyPress;
            skillDef.keywordTokens = oldDef.keywordTokens;
            return skillDef;
        }
        #region modify items and equips
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

        public static void RemoveEquipment(string equipName)
        {
            EquipmentDef equipDef = LoadEquipDef(equipName);
            equipDef.canDrop = false;
            equipDef.canBeRandomlyTriggered = false;
            equipDef.enigmaCompatible = false;
            equipDef.dropOnDeathChance = 0;
        }
        public static void ChangeEquipmentEnigma(string equipName, bool canEnigma)
        {
            EquipmentDef equipDef = LoadEquipDef(equipName);
            if (equipDef != null)
            {
                equipDef.enigmaCompatible = canEnigma;
            }
        }
        public static void ChangeBuffStacking(string buffName, bool canStack)
        {
            BuffDef buffDef = LoadBuffDef(buffName);
            if (buffDef != null)
            {
                buffDef.canStack = canStack;
            }
        }
        static ItemDef LoadItemDef(string name)
        {
            ItemDef itemDef = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/" + name);
            return itemDef;
        }
        static EquipmentDef LoadEquipDef(string name)
        {
            EquipmentDef equipDef = LegacyResourcesAPI.Load<EquipmentDef>("EquipmentDefs/" + name);
            return equipDef;
        }
        static BuffDef LoadBuffDef(string name)
        {
            BuffDef buffDef = LegacyResourcesAPI.Load<BuffDef>("BuffDefs/" + name);
            return buffDef;
        }
        #endregion
    }
}
