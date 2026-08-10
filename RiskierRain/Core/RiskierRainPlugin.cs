using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RiskierRain.CoreModules;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Security;
using System.Security.Permissions;
using ThreeEyedGames;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using RainrotSharedUtils;
using MonoMod.RuntimeDetour;
using UnityEngine.Networking;
using MonoMod.Cil;
using RoR2BepInExPack.GameAssetPathsBetter;
using RoR2.ContentManagement;
using RainrotSharedUtils.Difficulties;
using static MoreStats.StatHooks;
using MoreStats;
using UnityEngine.ResourceManagement.AsyncOperations;
using RiskierRain.Changes;
//using RiskierRain.Changes.Reworks.NerfsReworks.SpawnlistChanges; //idk if this is a good way of doing

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RiskierRain
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    //[BepInDependency("com.Borbo.ArtificerExtended", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RainrotSharedUtils.SharedUtilsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(NegativeRegenFix.NegativeRegenFix.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(FabricatorStandalone.FabricatorPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(SwanSongExtended.SwanSongPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(SurvivorTweaks.SurvivorTweaksPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(FruityElites.EliteReworksPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Wolfo.WolfFixes", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("HIFU.UltimateCustomRun", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Skell.DeathMarkChange", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), 
        nameof(DirectorAPI), 
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]

    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "RiskierRain";
        public const string version = "1.3.46";

        public static PluginInfo PInfo { get; private set; }
        public static string dropPrefabsPath => CoreModules.Assets.dropPrefabsPath;
        public static string iconsPath => CoreModules.Assets.iconsPath;
        public static string eliteMaterialsPath => CoreModules.Assets.eliteMaterialsPath;

        public static bool isAELoaded = Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool isHBULoaded = Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");
        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool acridLungeLoaded = Tools.isLoaded("Withor.AcridBiteLunge");
        public static bool ucrLoaded = Tools.isLoaded("HIFU.UltimateCustomRun");

        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> EnableConfig { get; set; }

        public static string drizzleDesc = $"Simplifies difficulty for players new to the game. Weeping and gnashing is replaced by laughter and tickles." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealing>+50%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealing>-50%</style> " +
                $"\n>Player Damage Reduction: <style=cIsHealing>+38%</style>";
        public static string rainstormDesc = $"This is the way the game is meant to be played! Test your abilities and skills against formidable foes." +
                $"<style=cStack>\n\n>Player Health Regeneration: +0% " +
                $"\n>Difficulty Scaling: +0% ";
        public static string monsoonDesc = $"For hardcore players. Every bend introduces pain and horrors of the planet. You will die." +
                $"<style=cStack>\n\n>Player Health Regeneration: <style=cIsHealth>-40%</style> " +
                $"\n>Difficulty Scaling: <style=cIsHealth>+50%</style>";

        bool IsCategoryEnabled(bool category)
        {
            return category;
        }

        void Awake()
        {
            PInfo = Info;

            InitializeCoreModules();

            InitializeConfig();
            InitializeEverything();

            On.RoR2.CharacterBody.RemoveBuff_BuffDef += Gah;

            //RoR2Application.onLoad += InitializeEverything;

            SaveConfig();

            new ContentPacks().Initialize();
        }

        public static void DebugBreakpoint(string methodName, int breakpointNumber = -1)
        {
            string s = $"{modName}: {methodName} IL hook failed!";
            if (breakpointNumber >= 0)
                s += $" (breakpoint {breakpointNumber})";
            Debug.LogError(s);
        }

        private void Gah(On.RoR2.CharacterBody.orig_RemoveBuff_BuffDef orig, CharacterBody self, BuffDef buffType)
        {
            if (!NetworkServer.active)
            {
                Debug.Log(buffType.name);
            }
            orig(self, buffType);
        }

        private void InitializeEverything()
        {
            ItemChanges.Initialize();
            EquipmentChanges.Initialize();
            DifficultyChanges.Initialize();
            EnemyChanges.Initialize();
            InteractableChanges.Initialize();
            DoSacrificeDropLimit();


            LanguageAPI.Add("DIFFICULTY_EASY_DESCRIPTION", drizzleDesc + "</style>");
            // " + $"\n>Most Bosses have <style=cIsHealing>reduced skill sets</style>

            LanguageAPI.Add("DIFFICULTY_NORMAL_DESCRIPTION", rainstormDesc + "</style>");

            LanguageAPI.Add("DIFFICULTY_HARD_DESCRIPTION", monsoonDesc + "</style>");

            #region old stuff :]
            #region enemies
            #endregion

            ///summary
            ///- nerfs healing
            ///- nerfs mobility
            ///- nerfs EHP
            #region defense and health
            // CONTENT...
            // ITEMS: beans, battery, berserker brew, morning mocha, star veil, cloud/fart in a bottle
            // EQUIPMENT: ninja gear
            // ENEMIES: Baba the Enlightened (speed scav)

            // CONTENT...
            // ITEMS: borbos band, cobalt shield, frozen turtle shell, flower crown, utility belt
            // EQUIPMENT: tesla coil
            // ENEMIES: Bobo the Unbreakable (defense scav)


            #endregion

            ///summary
            ///- status effects (attack speed slow)
            ///- planula
            ///- enemy item blacklists
            ///- enigma blacklists
            ///most general "gameplay" category
            #region interaction
            // CONTENT...
            // ITEMS: magic quiver, slungus, wicked band, permafrost, fuse, void happiest mask/tragic facade
            // EQUIPMENT: old guillotine
            // ENEMIES: 


            //scav could have royal cap? cunning
            #endregion

            ///summary
            ///- autoplay
            ///- procs and crits
            ///- burn rework
            #region damage
            // CONTENT...
            // ITEMS: atg mk3, chefs stache, new lopper, natures gift, Shard Vomitter
            // EQUIPMENT: Broken Zapinator 
            // ENEMIES: 

            //this.DoSadistScavenger();
            #endregion

            ///summary
            ///- economy changes
            ///- enemy changes
            ///- boss item drops
            ///- difficulty changes
            ///- elites
            ///this is essentially DifficultyPlus
            #region difficulty
            // CONTENT...
            // ITEMS: scalpel, coin gun, greedy ring
            // EQUIPMENT: gold bomb? lol
            // ENEMIES: 

            //this.DoSadistScavenger();
            #endregion

            ///summary
            ///DuckSurvivorTweaks
            #region survivor tweaks
            // CONTENT...
            // ITEMS: 
            // EQUIPMENT: 
            // ENEMIES: 

            #endregion

            ///summary 
            ///- commencement changes
            ///- pillar items
            ///- mithrix changes
            ///essentially CommencementPerfected
            #region commencement
            // CONTENT...
            // ITEMS: 
            // EQUIPMENT: 
            // ENEMIES: 

            #endregion
            #endregion
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
            if(buffDef != null)
            {
                buffDef.canStack = canStack;
            }
        }

        public static ItemDef LoadItemDef(string name)
        {
            ItemDef itemDef = LegacyResourcesAPI.Load<ItemDef>("ItemDefs/" + name);
            return itemDef;
        }
        public static EquipmentDef LoadEquipDef(string name)
        {
            EquipmentDef equipDef = LegacyResourcesAPI.Load<EquipmentDef>("EquipmentDefs/" + name);
            return equipDef;
        }
        public static BuffDef LoadBuffDef(string name)
        {
            BuffDef buffDef = LegacyResourcesAPI.Load<BuffDef>("BuffDefs/" + name);
            return buffDef;
        }
        #endregion

        public static GameObject meatballNapalmPool;
        public static void CreateMeatballNapalm()
        {
            meatballNapalmPool = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenacid").InstantiateClone("NapalmFire", true);

            Color napalmColor = new Color32(255, 120, 0, 255);


            Transform pDotObjDecal = meatballNapalmPool.transform.Find("FX/Decal");
            Material napalmDecalMaterial = new Material(pDotObjDecal.GetComponent<Decal>().Material);
            napalmDecalMaterial.SetColor("_Color", napalmColor);
            pDotObjDecal.GetComponent<Decal>().Material = napalmDecalMaterial;

            ProjectileDotZone pdz = meatballNapalmPool.GetComponent<ProjectileDotZone>();
            pdz.lifetime = 5f;
            pdz.fireFrequency = 2f;
            pdz.damageCoefficient = 0.5f;
            pdz.overlapProcCoefficient = 0.5f;
            pdz.attackerFiltering = AttackerFiltering.Default;
            meatballNapalmPool.GetComponent<ProjectileDamage>().damageType = DamageType.IgniteOnHit;
            meatballNapalmPool.GetComponent<ProjectileController>().procCoefficient = 1f;

            float decalScale = 2.5f;
            meatballNapalmPool.GetComponent<Transform>().localScale = new Vector3(decalScale, decalScale, decalScale);

            Transform transform = meatballNapalmPool.transform.Find("FX");
            transform.Find("Spittle").gameObject.SetActive(false);

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(
                LegacyResourcesAPI.Load<GameObject>("prefabs/FireTrail").GetComponent<DamageTrail>().segmentPrefab, transform.transform);
            ParticleSystem.MainModule main = gameObject.GetComponent<ParticleSystem>().main;
            main.duration = 8f;
            main.gravityModifier = -0.075f;
            ParticleSystem.MinMaxCurve startSizeX = main.startSizeX;
            startSizeX.constantMin *= 0.6f;
            startSizeX.constantMax *= 0.8f;
            ParticleSystem.MinMaxCurve startSizeY = main.startSizeY;
            startSizeY.constantMin *= 0.8f;
            startSizeY.constantMax *= 1f;
            ParticleSystem.MinMaxCurve startSizeZ = main.startSizeZ;
            startSizeZ.constantMin *= 0.6f;
            startSizeZ.constantMax *= 0.8f;
            ParticleSystem.MinMaxCurve startLifetime = main.startLifetime;
            startLifetime.constantMin = 0.9f;
            startLifetime.constantMax = 1.1f;
            gameObject.GetComponent<DestroyOnTimer>().enabled = false;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            ParticleSystem.ShapeModule shape = gameObject.GetComponent<ParticleSystem>().shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.scale = Vector3.one * 0.5f;

            GameObject gameObject2 = transform.Find("Point Light").gameObject;
            Light component2 = gameObject2.GetComponent<Light>();
            component2.color = new Color(1f, 0.5f, 0f);
            component2.intensity = 6f;
            component2.range = 12f;

            CoreModules.Assets.projectilePrefabs.Add(meatballNapalmPool);
        }

        private bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
        {
            if(desc != "")
            {
                return CustomConfigFile.Bind<bool>("Packets - See README For Details.", 
                    packetTitle + " Packet", defaultValue, 
                    $"The changes in this Packet will be enabled if set to true.").Value;
            }
            return CustomConfigFile.Bind<bool>("Packets", 
                packetTitle + " Packet", defaultValue, 
                "(The following changes will be enabled if set to true) " + desc).Value;
        }

        #region config
        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);
            CustomConfigFile.SaveOnConfigSet = false;

            EnableConfig = CustomConfigFile.Bind<bool>("Allow Config Options", "Enable Config", false,
                "Set this to true to enable config options. Please keep in mind that it was not within my design intentions to play this way. " +
                "This is primarily meant for modpack users with tons of mods installed. " +
                "If you have any issues or feedback on my mod balance, please feel free to send in feedback with the contact info in the README or Thunderstore description.");

            Debug.Log("Config initialized!");
        }
        private void SaveConfig()
        {
            CustomConfigFile.SaveOnConfigSet = true;
            CustomConfigFile.Save();
        }

        void InitializeCoreModules()
        {
            var CoreModuleTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(CoreModule)));

            foreach (var coreModuleType in CoreModuleTypes)
            {
                CoreModule coreModule = (CoreModule)Activator.CreateInstance(coreModuleType);

                coreModule.Init();

                Debug.Log("Core Module: " + coreModule + " Initialized!");
            }
        }
        #endregion
    }
}
