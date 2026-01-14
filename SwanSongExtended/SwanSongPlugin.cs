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
using UnityEngine.AddressableAssets;
using RoR2.ContentManagement;
using UnityEngine.ResourceManagement.AsyncOperations;
using SwanSongExtended.Changes;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace SwanSongExtended
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(NegativeRegenFix.NegativeRegenFix.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BetterSoulCost.SoulCostPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(JumpRework.JumpReworkPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BossDropRework.BossDropReworkPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency(BarrierRework.BarrierReworkPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.ArtificerExtended", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Withor.AcridBiteLunge", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("HIFU.UltimateCustomRun", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Skell.DeathMarkChange", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), nameof(RecalculateStatsAPI), nameof(DotAPI), nameof(DifficultyAPI))]
    [BepInPlugin(guid, modName, version)]
    public partial class SwanSongPlugin : BaseUnityPlugin
    {
        GameObject meatballNapalmPool => CommonAssets.meatballNapalmPool;


        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "SwanSongExtended";
        public const string version = "0.1.0";
        public const string expansionName = "Swan Song";
        public const string expansionName2 = "Secrets of the Scug";
        public const string expansionToken = "EXPANSION2R4R";
        public const string expansionToken2 = "EXPANSIONSOTS";

        public const string DEVELOPER_PREFIX = "FRUIT";

        public static SwanSongPlugin instance;
        public static AssetBundle mainAssetBundle => CommonAssets.mainAssetBundle;
        public static AssetBundle orangeAssetBundle => CommonAssets.orangeAssetBundle;
        public static AssetBundle retierAssetBundle => CommonAssets.retierAssetBundle;

        public static ExpansionDef expansionDefSS2;
        public static ExpansionDef expansionDefSOTS;

        internal static event Action onSwanSongLoaded;

        #region asset paths
        public const string iconsPath = "";
        #endregion

        #region mods loaded
        public static bool ModLoaded(string modGuid) { return modGuid != "" && BepInEx.Bootstrap.Chainloader.PluginInfos.ContainsKey(modGuid); }
        public static bool iabMissilesLoaded => ModLoaded("com.HouseOfFruits.IAmBecomeMissiles");
        public static bool isAELoaded => ModLoaded("com.Borbo.ArtificerExtended");
        public static bool is2R4RLoaded => ModLoaded("com.RiskOfBrainrot.RiskierRain");
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

            CreateExpansionDef();
            CreateDifficultyDef();
            RainrotSharedUtils.Status.ShockUtilsModule.UseShockSparks = true;
            Modules.Language.Init();
            Modules.Hooks.Init();
            Modules.CommonAssets.Init();
            Modules.EliteModule.Init();
            Modules.AllyCaps.Init();
            Modules.Spawnlists.Init();
            Storms.StormsCore.Init();

            ConfigManager.HandleConfigAttributes(GetType(), "SwanSong", Modules.Config.MyConfig);

            InitializeChangesPreContent();
            InitializeContent();
            InitializeChanges();
            //RoR2Application.onLoad += InitializeChanges;

            Modules.Materials.SwapShadersFromMaterialsInBundle(mainAssetBundle);
            Modules.Materials.SwapShadersFromMaterialsInBundle(orangeAssetBundle);

            onSwanSongLoaded.Invoke();

            Modules.Config.Save();

            // this has to be last
            new Modules.ContentPacks().Initialize();

            ////refer to guide on how to build and distribute your mod with the proper folders
        }
        public static void DebugBreakpoint(string methodName, int breakpointNumber = -1)
        {
            string s = $"{modName}: {methodName} IL hook failed!";
            if (breakpointNumber >= 0)
                s += $" (breakpoint {breakpointNumber})";
            Debug.LogError(s);
        }

        private void CreateExpansionDef()
        {
            expansionDefSS2 = ScriptableObject.CreateInstance<ExpansionDef>();
            expansionDefSS2.nameToken = expansionToken + "_NAME";
            expansionDefSS2.descriptionToken = expansionToken + "_DESCRIPTION";
            expansionDefSS2.iconSprite = null;
            expansionDefSS2.disabledIconSprite = null;
            LanguageAPI.Add(expansionToken + "_NAME", expansionName);
            LanguageAPI.Add(expansionToken + "_DESCRIPTION", $"Adds content from the '{expansionName}' expansion to the game.");
            Content.AddExpansionDef(expansionDefSS2);

            expansionDefSOTS = ScriptableObject.CreateInstance<ExpansionDef>();
            expansionDefSOTS.nameToken = expansionToken2 + "_NAME";
            expansionDefSOTS.descriptionToken = expansionToken2 + "_DESCRIPTION";
            expansionDefSOTS.iconSprite = null;
            expansionDefSOTS.disabledIconSprite = null;
            LanguageAPI.Add(expansionToken2 + "_NAME", expansionName2);
            LanguageAPI.Add(expansionToken2 + "_DESCRIPTION", $"Adds content from the '{expansionName2}' expansion to the game.");

            Content.AddExpansionDef(expansionDefSOTS);
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
            BeginInitializing<ReworkBase>(allTypes, "SwanSongReworks.txt");

            BeginInitializing<ItemBase>(allTypes, "SwanSongItems.txt");

            BeginInitializing<EquipmentBase>(allTypes, "SwanSongEquipment.txt");

            BeginInitializing<EliteEquipmentBase>(allTypes, "SwanSongElites.txt");

            BeginInitializing<InteractableBase>(allTypes, "SwanSongInteractables.txt");

            BeginInitializing<ArtifactBase>(allTypes, "SwanSongArtifacts.txt");

            BeginInitializing<SkillBase>(allTypes, "SwanSongSkills.txt");

            BeginInitializing<TwistedScavengerBase>(allTypes, "SwanSongScavengers.txt");
        }

        private void InitializeChangesPreContent()
        {
            if (GetConfigBool(true, "Reworks : Commencement"))
            {
                MakePillarsFun();
                LunarExplodersDuringBrother();
            }
        }
        private void InitializeChanges()
        {
            /*BurnReworks();
            if (GetConfigBool(true, "Reworks : Executive Card"))
            {
                ExecutiveCardChanges();
            }
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
            if (GetConfigBool(true, "Reworks : Leeching Seed"))
            {
                ReworkLeechingSeed();
            }
            if (GetConfigBool(true, "Reworks : Bison Steak"))
            {
                ReworkFreshMeat();
            }
            if (GetConfigBool(true, "Reworks : Gesture of the Drowned"))
            {
                GestureChanges();
            }
            if (GetConfigBool(true, "Reworks : Brittle Crown"))
            {
                BrittleCrownChanges();
            }
            if (GetConfigBool(true, "Reworks : Planula"))
            {
                PlanulaChanges();
            }
            if (GetConfigBool(true, "Reworks : Resonance Disc"))
            {
                DeworkResonanceDisc();
            }
            if (GetConfigBool(true, "Reworks : Prayer Beads"))
            {
                PrayerBeadsRework();
            }*/
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

            IEnumerable<SharedBase> objsOfBaseType = 
                objTypesOfBaseType
                    .Select((objType) => (T)System.Activator.CreateInstance(objType))
                    .OrderBy((sharedBase) => sharedBase.loadOrder);

            Log.Debug(Log.Combine(baseType.Name) + "Initializing");

            foreach (SharedBase obj in objsOfBaseType)
            {
                string s = Log.Combine(baseType.Name, obj.ConfigName);
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
            if (obj.forcePrerequisites)
                return enabled && obj.GetPrerequisites();

            return obj.Bind(enabled, "Should This Content Be Enabled") && obj.GetPrerequisites();
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
        #region modify items and equips
        public static AssetReferenceT<T> LoadAsync<T>(string guid, Action<T> callback) where T : UnityEngine.Object
        {
            void onCompleted(AsyncOperationHandle<T> handle)
            {
                if (!(handle.Result is T) || handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError($"Failed to load asset [{handle.DebugName}] : {handle.OperationException}");
                    return;
                }

                callback(handle.Result);
            }

            AssetReferenceT<T> ref1 = new AssetReferenceT<T>(guid);
            AsyncOperationHandle<T> handle = AssetAsyncReferenceManager<T>.LoadAsset(ref1);

            if (callback == null)
            {
                return ref1;
            }

            if (handle.IsDone)
            {
                onCompleted(handle);
                return ref1;
            }

            handle.Completed += onCompleted;
            return ref1;
        }
        public static void RetierItemAsync(string itemGuid, ItemTier tier = ItemTier.NoTier, Action<ItemDef> callback = null)
        {
            AssetReferenceT<ItemDef> ref1 = new AssetReferenceT<ItemDef>(itemGuid);
            AssetAsyncReferenceManager<ItemDef>.LoadAsset(ref1).Completed += (ctx) =>
            {
                ItemDef itemDef = ctx.Result;
                itemDef.tier = tier;
                itemDef.deprecatedTier = tier;

                if (callback != null)
                    callback.Invoke(itemDef);
            };
        }
        public static void RemoveEquipmentAsync(string equipmentGuid, Action<EquipmentDef> callback = null)
        {
            AssetReferenceT<EquipmentDef> ref1 = new AssetReferenceT<EquipmentDef>(equipmentGuid);
            AssetAsyncReferenceManager<EquipmentDef>.LoadAsset(ref1).Completed += (ctx) =>
            {
                EquipmentDef equipDef = ctx.Result;
                equipDef.canDrop = false;
                equipDef.canBeRandomlyTriggered = false;
                equipDef.enigmaCompatible = false;
                equipDef.dropOnDeathChance = 0;

                if (callback != null)
                    callback.Invoke(equipDef);
            };
        }

        internal static void BlacklistSingleItem(ItemDef itemDef, ItemTag itemTag = ItemTag.AIBlacklist)
        {
            if (itemDef != null)
            {
                List<ItemTag> itemTags = new List<ItemTag>(itemDef.tags);
                itemTags.Add(itemTag);

                itemDef.tags = itemTags.ToArray();
            }
            else
            {
                Log.Error($"ItemDef null - unable to blacklist");
            }
        }
        internal static void BlacklistSingleItem(string name, ItemTag itemTag = ItemTag.AIBlacklist)
        {
            ItemDef itemDef = LoadItemDef(name);
            BlacklistSingleItem(itemDef, itemTag);
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
