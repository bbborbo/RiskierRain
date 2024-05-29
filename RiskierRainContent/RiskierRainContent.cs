using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RiskierRainContent.Artifacts;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Equipment;
using RiskierRainContent.Interactables;
using RiskierRainContent.Items;
using RiskierRainContent.Scavengers;
using RiskierRainContent.Skills;
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
using BorboStatUtils;
using ChillRework;
using MissileRework;
using RoR2.ExpansionManagement;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RiskierRainContent
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.PrefabAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ItemAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.RecalculateStatsAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.EliteAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    //[BepInDependency("com.Borbo.ArtificerExtended", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(ChillRework.ChillRework.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BorboStatUtils.BorboStatUtils.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(NegativeRegenFix.NegativeRegenFix.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.ArtifactGesture", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Withor.AcridBiteLunge", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("HIFU.UltimateCustomRun", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Skell.DeathMarkChange", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI),
        nameof(DirectorAPI),
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "RiskierRainContent";
        public const string version = "1.0.0";
        public const string expansionName = "Swan Song";
        public const string expansionToken = "EXPANSION2R4R";
        public static PluginInfo PInfo { get; private set; }

        public static ExpansionDef expansionDef;
        public static AssetBundle mainAssetBundle => Assets.mainAssetBundle;
        public static AssetBundle orangeAssetBundle => Assets.orangeAssetBundle;
        public static string dropPrefabsPath => Assets.dropPrefabsPath;
        public static string iconsPath => Assets.iconsPath;
        public static string eliteMaterialsPath => Assets.eliteMaterialsPath;

        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> EnableConfig { get; set; }

        public static bool isAELoaded => Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool is2R4RLoaded => Tools.isLoaded("com.HouseOfFruits.RiskierRain");
        public static bool isHBULoaded => Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");
        public static bool IsMissileArtifactEnabled()
        {
            if (Tools.isLoaded(MissileReworkPlugin.guid))
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

        public static bool isScepterLoaded => Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded => Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool acridLungeLoaded => Tools.isLoaded("Withor.AcridBiteLunge");
        public static bool ucrLoaded => Tools.isLoaded("HIFU.UltimateCustomRun");

        public void Awake()
        {
            PInfo = Info;

            CreateExpansionDef();
            InitializeCoreModules();
            InitializeStorms();

            InitializeConfig();
            InitializeItems();
            InitializeSkills();
            InitializeEquipment();
            InitializeEliteEquipment();
            InitializeArtifacts();
            InitializeScavengers();
            //InitializeEverything();
            Assets.SwapShadersFromMaterialsInBundle(orangeAssetBundle);

            RoR2Application.onLoad += InitializeEverything;

            new ContentPacks().Initialize();
        }

        private void CreateExpansionDef()
        {
            expansionDef = ScriptableObject.CreateInstance<ExpansionDef>();
            expansionDef.nameToken = expansionToken + "_NAME";
            expansionDef.descriptionToken = expansionToken + "_DESCRIPTION";
            LanguageAPI.Add(expansionToken + "_NAME", expansionName);
            LanguageAPI.Add(expansionToken + "_DESCRIPTION", $"Adds content from the '{expansionName}' expansion to the game.");
            Assets.expansionDefs.Add(expansionDef);
        }

        private void InitializeEverything()
        {
            JumpReworks();
            BurnReworks();
            MakePillarsFun();
            if (GetConfigBool(true, "Core: Laser Scope Rework (Combat Telescope)"))
            {
                ReworkLaserScope();
            }
            // boss item drop
            if (GetConfigBool(true, "Core: Boss Item Drops"))
            {
                BossesDropBossItems();
                TricornRework();
                DirectorAPI.InteractableActions += DeleteYellowPrinters;
            }
            //happiest mask
            if (GetConfigBool(true, "Core: Happiest Mask"))
            {
                HappiestMaskRework();
            }
            //happiest mask
            if (GetConfigBool(true, "Core: Hunters Harpoon"))
            {
                HuntersHarpoonRework();
            }
            //focused convergence, focon
            if (GetConfigBool(true, "Core: Focused Convergence"))
            {
                FocusedConvergenceChanges();
            }
            //squid polyp :3
            if (GetConfigBool(true, "Core: Squid Polyp"))
            {
                SquolypRework();
            }
            //interactables bc they need to load after items:
            InitializeInteractables();
            //need to do this after interactablestuff
            //List<DirectorCard> directorCards = new List<DirectorCard>();
            //directorCards.Add(doubleChestDirectorCard);
            //Secrets.AddSecrets(directorCards);
            Secrets.AddSecrets();
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

        GameObject meatballNapalmPool;
        private void CreateMeatballNapalm()
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

            Assets.projectilePrefabs.Add(meatballNapalmPool);
        }

        private bool GetConfigBool(bool defaultValue, string packetTitle, string desc = "")
        {
            if (desc != "")
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

            EnableConfig = CustomConfigFile.Bind<bool>("Allow Config Options", "Enable Config", false,
                "Set this to true to enable config options. Please keep in mind that it was not within my design intentions to play this way. " +
                "This is primarily meant for modpack users with tons of mods installed. " +
                "If you have any issues or feedback on my mod balance, please feel free to send in feedback with the contact info in the README or Thunderstore description.");
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

        #region twisted scavs

        public List<TwistedScavengerBase> Scavs = new List<TwistedScavengerBase>();
        public static Dictionary<TwistedScavengerBase, bool> ScavStatusDictionary = new Dictionary<TwistedScavengerBase, bool>();
        private void InitializeScavengers()
        {
            var ScavTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(TwistedScavengerBase)));

            foreach (var scavType in ScavTypes)
            {
                TwistedScavengerBase scav = (TwistedScavengerBase)System.Activator.CreateInstance(scavType);

                if (ValidateScav(scav, Scavs))
                {
                    scav.PopulateItemInfos(CustomConfigFile);
                    scav.Init(CustomConfigFile);
                }
                else
                {
                    Debug.Log("Scavenger: " + scav.ScavLangTokenName + " did not initialize!");
                }
            }
        }

        bool ValidateScav(TwistedScavengerBase scav, List<TwistedScavengerBase> scavList)
        {
            bool enabled = true;

            string name = scav.ScavName.Replace("'", "");
            enabled = CustomConfigFile.Bind<bool>(modName + " Scavengers", $"Enable Twisted Scavenger: {name} the {scav.ScavTitle}", true, "Should this scavenger appear in A Moment, Whole?").Value;

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (enabled)
            {
                scavList.Add(scav);
            }
            return enabled;
        }
        #endregion

        #region items

        public List<ItemBase> Items = new List<ItemBase>();
        public static Dictionary<ItemBase, bool> ItemStatusDictionary = new Dictionary<ItemBase, bool>();

        void InitializeItems()
        {
            var ItemTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ItemBase)));

            foreach (var itemType in ItemTypes)
            {
                ItemBase item = (ItemBase)System.Activator.CreateInstance(itemType);
                if (!item.IsDisabled)
                {
                    if (item.IsHidden || ValidateItem(item, Items))
                    {
                        item.Init(CustomConfigFile);
                    }
                    else
                    {
                        Debug.Log("Item: " + item.ItemName + " Did not initialize!");
                    }
                }
            }
        }

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            var itemEnabled = item.Tier == ItemTier.NoTier;

            if (!itemEnabled)
            {
                string name = item.ItemName.Replace("'", "");
                itemEnabled = CustomConfigFile.Bind<bool>(modName + " Items: " + item.Tier.ToString(), $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
            }

            //ItemStatusDictionary.Add(item, itemEnabled);

            if (itemEnabled)
            {
                itemList.Add(item);
            }
            return itemEnabled;
        }
        #endregion

        #region equips

        public List<EquipmentBase> Equipments = new List<EquipmentBase>();
        public static Dictionary<EquipmentBase, bool> EquipmentStatusDictionary = new Dictionary<EquipmentBase, bool>();
        void InitializeEquipment()
        {
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EquipmentBase)));

            foreach (var equipmentType in EquipmentTypes)
            {
                EquipmentBase equipment = (EquipmentBase)System.Activator.CreateInstance(equipmentType);
                if (equipment.IsHidden)
                    return;

                if (equipment.ForceEnable || ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);
                }
            }
        }
        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            var itemEnabled = true;
            itemEnabled =  CustomConfigFile.Bind<bool>(modName + " Equipment", "Enable Equipment: " + equipment.EquipmentName, true, "Should this equipment appear in runs?").Value;

            EquipmentStatusDictionary.Add(equipment, itemEnabled);

            if (itemEnabled)
            {
                equipmentList.Add(equipment);
            }
            return itemEnabled;
        }

        public static List<EquipmentDef> EliteEquipments = new List<EquipmentDef>();
        public static Dictionary<EliteEquipmentBase, bool> EliteEquipmentStatusDictionary = new Dictionary<EliteEquipmentBase, bool>();
        void InitializeEliteEquipment()
        {
            var EquipmentTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(EliteEquipmentBase)));

            foreach (var equipmentType in EquipmentTypes)
            {
                EliteEquipmentBase equipment = (EliteEquipmentBase)System.Activator.CreateInstance(equipmentType);

                if (ValidateEliteEquipment(equipment))
                {
                    equipment.Init(Config);
                    EliteEquipments.Add(equipment.EliteEquipmentDef);
                }
            }
        }
        public bool ValidateEliteEquipment(EliteEquipmentBase equipment)
        {
            var itemEnabled = true;

            itemEnabled = CustomConfigFile.Bind<bool>(modName + " Elites", $"Enable Aspect: {equipment.EliteEquipmentName} ({equipment.EliteModifier} Elite)", true, "Should these elites appear in runs?").Value;

            EliteEquipmentStatusDictionary.Add(equipment, itemEnabled);
            return itemEnabled;
        }
        #endregion

        #region interactables

        public List<InteractableBase> Interactables = new List<InteractableBase>();
        public static Dictionary<InteractableBase, bool> InteractableStatusDictionary = new Dictionary<InteractableBase, bool>();

        void InitializeInteractables()
        {
            var interactableTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(InteractableBase)));

            //test!!!
            //DirectorAPI.Helpers.AddNewInteractableToStage(DirectorAPI.Helpers.InteractableNames.TripleShopEquipment., DirectorAPI.InteractableCategory.Chests, DirectorAPI.Stage.TitanicPlains);

            foreach (var interactableType in interactableTypes)
            {
                InteractableBase interactable = (InteractableBase)System.Activator.CreateInstance(interactableType);
                //if (!interactable.IsHidden)
                {
                    //if (ValidateInteractable(interactable, Interactables))
                    {
                        interactable.Init(CustomConfigFile);
                    }
                    //else
                    //{
                    //    Debug.Log("Interactable: " + interactable.interactableName + " Did not initialize!");
                    //}
                }
            }
        }

        //bool ValidateInteractable(InteractableBase interactable, List<InteractableBase> itemList)
        //{
        //    BalanceCategory category = interactable.Category;
        //
        //    var itemEnabled = interactable.Tier == ItemTier.NoTier;
        //
        //    if (!itemEnabled)
        //    {
        //        string name = interactable.InteractableName.Replace("'", "");
        //        if (category != BalanceCategory.None && category != BalanceCategory.Count && !itemEnabled)
        //        {
        //            itemEnabled = IsCategoryEnabled(category) &&
        //            CustomConfigFile.Bind<bool>(category.ToString() + " Content", $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
        //        }
        //        else
        //        {
        //            itemEnabled = IsCategoryEnabled(category) &&
        //            CustomConfigFile.Bind<bool>("Uncategorized Content", $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
        //            Debug.Log($"{name} item initializing into Balance Category: {category}!!");
        //        }
        //    }
        //
        //    ItemStatusDictionary.Add(item, itemEnabled);
        //
        //    if (itemEnabled)
        //    {
        //        itemList.Add(interactable);
        //    }
        //    return true;
        //}
        #endregion

        #region skills
        public static List<Type> entityStates = new List<Type>();
        public static List<SkillBase> Skills = new List<SkillBase>();
        public static List<ScepterSkillBase> ScepterSkills = new List<ScepterSkillBase>();
        public static Dictionary<SkillBase, bool> SkillStatusDictionary = new Dictionary<SkillBase, bool>();

        private void InitializeSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SkillBase)));

            foreach (var skillType in SkillTypes)
            {
                SkillBase skill = (SkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateSkill(skill))
                {
                    skill.Init(CustomConfigFile);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private void InitializeScepterSkills()
        {
            var SkillTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ScepterSkillBase)));

            foreach (var skillType in SkillTypes)
            {
                ScepterSkillBase skill = (ScepterSkillBase)System.Activator.CreateInstance(skillType);

                if (ValidateScepterSkill(skill))
                {
                    skill.Init(CustomConfigFile);
                }
            }
        }

        bool ValidateSkill(SkillBase item)
        {
            var forceUnlock = true;
            var enabled = false;// item.Tier == ItemTier.NoTier;

            if (forceUnlock && !enabled)
            {
                enabled = CustomConfigFile.Bind<bool>((modName + " Scepter Skills: " + item.CharacterName).Trim(), 
                    ($"Enable Skill: {item.SkillName}").Trim(), true, "Should this skill be available?").Value;
            }

            if (enabled)
                Skills.Add(item);

            return enabled;
        }

        bool ValidateScepterSkill(ScepterSkillBase item)
        {
            var enabled = false;// item.Tier == ItemTier.NoTier;

            if (isScepterLoaded && !enabled)
            {
                string name = item.SkillName;
                //enabled = CustomConfigFile.Bind<bool>(modName + " Scepter Skills: " + item.TargetBodyName, $"Enable Skill: {item.SkillName}", true, "Should this skill be available?").Value;
                enabled = true;
            }

            if(isScepterLoaded && enabled)
                ScepterSkills.Add(item);

            return isScepterLoaded && enabled;
        }
        #endregion

        #region artifacts
        public List<ArtifactBase> Artifacts = new List<ArtifactBase>();
        public static Dictionary<ArtifactBase, bool> ArtifactStatusDictionary = new Dictionary<ArtifactBase, bool>();

        void InitializeArtifacts()
        {
            var ArtifactTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(ArtifactBase)));

            foreach (var itemType in ArtifactTypes)
            {
                ArtifactBase artifact = (ArtifactBase)System.Activator.CreateInstance(itemType);
                if (ValidateArtifact(artifact, Artifacts))
                {
                    artifact.Init(CustomConfigFile);
                }
                else
                {
                    Debug.Log("Artifact of " + artifact.ArtifactName + " Did not initialize!");
                }
            }
        }
        bool ValidateArtifact(ArtifactBase artifact, List<ArtifactBase> itemList)
        {
            var enabled = false;// item.Tier == ItemTier.NoTier;

            if (!enabled)
            {
                string name = artifact.ArtifactName.Replace("'", "");
                enabled = CustomConfigFile.Bind<bool>(modName + " Artifacts", $"Enable Artifact of {name}", true, "Should this artifact be available?").Value;
            }

            if (enabled)
            {
                itemList.Add(artifact);
            }
            return enabled;
        }
        #endregion
        internal static void AIBlacklistSingleItem(string name)
        {
            ItemDef itemDef = LoadItemDef(name);
            List<ItemTag> itemTags = new List<ItemTag>(itemDef.tags);
            itemTags.Add(ItemTag.AIBlacklist);

            itemDef.tags = itemTags.ToArray();
        }
    }
}
