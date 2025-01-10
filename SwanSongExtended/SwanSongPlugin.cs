using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using BepInEx;
using SwanSongExtended.Equipment;
using SwanSongExtended.Items;
using SwanSongExtended.Modules;
using SwanSongExtended.Skills;
using SwanSongExtended.Survivors;
using R2API;
using R2API.Utils;
using UnityEngine;
using RoR2.ExpansionManagement;
using System.Runtime.CompilerServices;
using RoR2;
using MissileRework;
using SwanSongExtended.Interactables;
using SwanSongExtended.Elites;
using SwanSongExtended.Artifacts;
using SwanSongExtended.Scavengers;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace SwanSongExtended
{
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(RecalculateStatsAPI), nameof(DotAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class SwanSongPlugin : BaseUnityPlugin
    {
        GameObject meatballNapalmPool => CommonAssets.meatballNapalmPool;


        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "SwanSongExtended";
        public const string version = "1.0.0";
        public const string expansionName = "Swan Song";
        public const string expansionToken = "EXPANSION2R4R";

        public const string DEVELOPER_PREFIX = "FRUIT";

        public static SwanSongPlugin instance;
        public static AssetBundle mainAssetBundle => CommonAssets.mainAssetBundle;
        public static AssetBundle orangeAssetBundle => CommonAssets.orangeAssetBundle;

        public static ExpansionDef expansionDef;

        #region asset paths
        public const string iconsPath = "";
        #endregion

        #region mods loaded
        public static bool ModLoaded(string modGuid) { return modGuid != "" && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid); }
        public static bool iabMissilesLoaded => ModLoaded("com.HouseOfFruits.IAmBecomeMissiles");
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

        [AutoConfig("Enable Debugging", "Enable debug outputs to the log for troubleshooting purposes. Enabling this will slow down the game.", false)]
        public static bool enableDebugging;

        void Awake()
        {
            instance = this;

            Log.Init(Logger);

            CreateExpansionDef();
            Modules.Config.Init();
            Modules.Language.Init();
            Modules.Hooks.Init();
            Modules.CommonAssets.Init();
            Modules.AllyCaps.Init();
            this.InitializeStorms();

            ConfigManager.HandleConfigAttributes(GetType(), "SwanSong", Modules.Config.MyConfig);
            
            InitializeContent();
            RoR2Application.onLoad += InitializeChanges;

            Modules.Materials.SwapShadersFromMaterialsInBundle(mainAssetBundle);
            Modules.Materials.SwapShadersFromMaterialsInBundle(orangeAssetBundle);

            // this has to be last
            new Modules.ContentPacks().Initialize();

            ////refer to guide on how to build and distribute your mod with the proper folders
        }


        private void CreateExpansionDef()
        {
            expansionDef = ScriptableObject.CreateInstance<ExpansionDef>();
            expansionDef.nameToken = expansionToken + "_NAME";
            expansionDef.descriptionToken = expansionToken + "_DESCRIPTION";
            LanguageAPI.Add(expansionToken + "_NAME", expansionName);
            LanguageAPI.Add(expansionToken + "_DESCRIPTION", $"Adds content from the '{expansionName}' expansion to the game.");

            Content.AddExpansionDef(expansionDef);
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
            BeginInitializing<ItemBase>(allTypes, "SwanSongItems.txt");

            BeginInitializing<InteractableBase>(allTypes, "SwanSongInteractables.txt");

            BeginInitializing<SkillBase>(allTypes, "SwanSongSkills.txt");

            BeginInitializing<EquipmentBase>(allTypes, "SwanSongEquipment.txt");

            BeginInitializing<EliteEquipmentBase>(allTypes, "SwanSongElites.txt");

            BeginInitializing<ArtifactBase>(allTypes, "SwanSongArtifacts.txt");

            BeginInitializing<TwistedScavengerBase>(allTypes, "SwanSongScavengers.txt");
        }
        private void InitializeChanges()
        {
            BurnReworks();
            if (GetConfigBool(true, "Reworks : Razorwire"))
            {
                RazorwireRework();
            }
            if (GetConfigBool(true, "Reworks : Laser Scope"))
            {
                ReworkLaserScope();
            }
            if (GetConfigBool(true, "Reworks : Happiest Mask"))
            {
                HappiestMaskRework();
            }
            if (GetConfigBool(true, "Reworks : Hunters Harpoon"))
            {
                HuntersHarpoonRework();
            }
            if (GetConfigBool(true, "Reworks : Focused Convergence"))
            {
                FocusedConvergenceChanges();
            }
            //squid polyp :3
            if (GetConfigBool(true, "Reworks : Squid Polyp"))
            {
                SquolypRework();
            }
            if(GetConfigBool(true, "Reworks : Executive Card"))
            {
                ExecutiveCardChanges();
            }
            if (GetConfigBool(true, "Reworks : Leeching Seed"))
            {
                ReworkLeechingSeed();
            }
            if (GetConfigBool(true, "Reworks : Bison Steak"))
            {
                ReworkFreshMeat();
            }
            if (GetConfigBool(true, "Reworks : Commencement"))
            {
                MakePillarsFun();
                LunarExplodersDuringBrother();
            }
            //interactables bc they need to load after items:
            //InitializeInteractables();
            //need to do this after interactablestuff
            //List<DirectorCard> directorCards = new List<DirectorCard>();
            //directorCards.Add(doubleChestDirectorCard);
            //Secrets.AddSecrets(directorCards);
            Secrets.AddSecrets();
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

        private bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
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
