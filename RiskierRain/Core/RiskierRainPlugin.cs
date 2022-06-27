using System;
using System.Security;
using System.Security.Permissions;
using RoR2;
using RoR2.Skills;
using UnityEngine;
using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using RiskierRain.CoreModules;
using RiskierRain.Equipment;
using RiskierRain.Items;
using RiskierRain.Scavengers;
using static R2API.RecalculateStatsAPI;
using RoR2.Projectile;
using ThreeEyedGames;
using On.RoR2.ContentManagement;
using UnityEngine.AddressableAssets;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RiskierRain
{
    [BepInDependency("com.bepis.r2api", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Skell.DeathMarkChange", BepInDependency.DependencyFlags.SoftDependency)]

    [BepInDependency("com.Borbo.ArtificerExtended", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.DuckSurvivorTweaks", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.DifficultyPLUS", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.ArtifactGesture", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin("com.HouseOfFruits.RiskierRain", "RiskierRain", "1.0.0")]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), 
        nameof(DirectorAPI), 
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]

    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static AssetBundle assetBundle = Tools.LoadAssetBundle(RiskierRain.Properties.Resources.borboitemicons);
        public static AssetBundle assetBundle2 = Tools.LoadAssetBundle(RiskierRain.Properties.Resources.borbobundle);
        public static string assetsPath = "Assets/BorboItemIcons/";
        public static string modelsPath = "Assets/Models/Prefabs/";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static bool isAELoaded = Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool isDSTLoaded = Tools.isLoaded("com.Borbo.DuckSurvivorTweaks");
        public static bool isHBULoaded = Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");

        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> EnableConfig { get; set; }
        public static ConfigEntry<bool> StateOfDefenseAndHealing { get; set; }
        public static ConfigEntry<bool> StateOfHealth { get; set; }
        public static ConfigEntry<bool> StateOfInteraction { get; set; }
        public static ConfigEntry<bool> StateOfDamage { get; set; }
        public static ConfigEntry<bool> StateOfEconomy { get; set; }
        public static ConfigEntry<bool> StateOfElites { get; set; }
        public static ConfigEntry<bool> StateOfDifficulty { get; set; }

        public static ConfigEntry<bool>[] DisableConfigCategories = new ConfigEntry<bool>[(int)BalanceCategory.Count] 
        { StateOfDefenseAndHealing, StateOfHealth, StateOfInteraction, StateOfDamage };
        bool IsCategoryEnabled(BalanceCategory category)
        {
            bool enabled = true;

            if (EnableConfig.Value && DisableConfigCategories[(int)category].Value)
            {
                enabled = false;
            }

            return enabled;
        }

        void Awake()
        {
            InitializeConfig();
            InitializeItems();
            //InitializeEquipment();
            //InitializeEliteEquipment();
            //InitializeScavengers();
            InitializeEverything();

            if (isAELoaded)
            {
                if (IsCategoryEnabled(BalanceCategory.StateOfInteraction))
                {
                    LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                        $"<style=cSub>Has a chance to temporarily reduce <style=cIsUtility>movement speed and attack speed</style> by <style=cIsDamage>80%.</style></style>");
                }
                else
                {
                    LanguageAPI.Add("ARTIFICEREXTENDED_KEYWORD_CHILL", "<style=cKeywordName>Chilling</style>" +
                        $"<style=cSub>Has a chance to temporarily reduce <style=cIsUtility>movement speed</style> by <style=cIsDamage>80%.</style></style>");
                }
            }

            //lol
            LanguageAPI.Add("ITEM_SHOCKNEARBY_PICKUP", "lol");
            LanguageAPI.Add("ITEM_AUTOCASTEQUIPMENT_PICKUP", "lol");
            LanguageAPI.Add("ITEM_EXECUTELOWHEALTHELITE_PICKUP", "lol");

            InitializeCoreModules();
            new ContentPacks().Initialize();
        }

        private void InitializeEverything()
        {
            IL.RoR2.Orbs.DevilOrb.OnArrival += BuffDevilOrb;

            BalanceCategory currentCategory = BalanceCategory.StateOfDefenseAndHealing;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS:
                // EQUIPMENT: ninja gear
                // ENEMIES: Baba the Enlightened (speed scav)

                #region ESSENTIAL
                // healing
                MedkitNerf();
                MonsterToothNerf();

                // mobility
                IL.EntityStates.GenericCharacterMain.ProcessJump += FeatherNerf;
                GoatHoofNerf();
                EnergyDrinkNerf();

                // defense
                TeddyChanges();
                #endregion

                #region PACKETS
                // scythe
                if (GetConfigBool(currentCategory, true, "Harvesters Scythe"))
                {
                    ScytheNerf();
                }

                // dynamic jump
                if (GetConfigBool(currentCategory, true, "Dynamic Jump"))
                {
                    IL.RoR2.CharacterMotor.PreMove += DynamicJump;
                }

                // jade elephant
                if (GetConfigBool(currentCategory, true, "Jade Elephant"))
                {
                    JadeElephantChanges();
                }

                // steak
                if (GetConfigBool(currentCategory, true, "Bison Steak"))
                {
                    GetStatCoefficients += MeatReduceHealth;
                    FreshMeatStackingFix();
                    MeatBuff();
                }

                // nkuhana D+H
                if (GetConfigBool(currentCategory, true, "(D+H) NKuhanas Opinion"))
                {
                    this.BuffNkuhana();
                }

                // droplet general
                if (GetConfigBool(currentCategory, true, "Droplet General"))
                {
                    this.FixPickupStats();
                }

                // monster tooth
                if (GetConfigBool(currentCategory, true, "Monster Tooth"))
                {
                    MonsterToothDurationBuff();
                }


                string armorChangesTitle = " Armor Packet";
                string armorChangesDesc = "Set how much additional armor this item gives. Vanilla 0.";
                AdjustVanillaDefense();

                // knurl
                knurlFreeArmor = CustomConfigFile.Bind<int>(currentCategory.ToString() + " Packet", "Knurl" + armorChangesTitle, knurlFreeArmor, armorChangesDesc).Value;

                // buckler
                bucklerFreeArmor = CustomConfigFile.Bind<int>(currentCategory.ToString() + " Packet", "Rose Buckler" + armorChangesTitle, bucklerFreeArmor, armorChangesDesc).Value;

                // rap
                rapFreeArmor = CustomConfigFile.Bind<int>(currentCategory.ToString() + " Packet", "Repulsion Armor Plating" + armorChangesTitle, rapFreeArmor, armorChangesDesc).Value;
                #endregion
            }

            currentCategory = BalanceCategory.StateOfHealth;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: borbos band, frozen turtle shell, flower crown, utility belt
                // EQUIPMENT: tesla coil
                // ENEMIES: Bobo the Unbreakable (defense scav)

                #region ESSENTIAL
                //barrier
                this.BuffBarrier();
                #endregion

                #region PACKETS
                //infusion
                if (GetConfigBool(currentCategory, true, "Infusion"))
                {
                    this.FuckingFixInfusion();
                }
                #endregion
                //nerf engi turret max health?
            }

            currentCategory = BalanceCategory.StateOfInteraction;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: atg mk3, magic quiver, wicked band, permafrost
                // EQUIPMENT:
                // ENEMIES: 

                #region ESSENTIAL
                // misc
                #endregion

                #region PACKETS
                // shattering justics
                if (GetConfigBool(currentCategory, true, "Shattering Justice"))
                {
                    this.BuffJustice();
                }

                // resonance disc
                if (GetConfigBool(currentCategory, true, "Resonance Disc"))
                {
                    ResonanceDiscNerfs();
                    //this.NerfResDisc();
                    EntityStates.LaserTurbine.FireMainBeamState.mainBeamProcCoefficient = 0.5f;
                }

                // jellynuke
                if (GetConfigBool(currentCategory, true, "Jellynuke"))
                {
                    this.FixVagrantNova();
                }

                // planula
                if (GetConfigBool(currentCategory, true, "Planula"))
                {
                    this.ReworkPlanula();
                }

                // shatterspleen, INT
                if (GetConfigBool(currentCategory, true, "Shatterspleen"))
                {
                    this.ReworkShatterspleen();
                }

                // enemy blacklist
                if (GetConfigBool(currentCategory, true, "Enemy Blacklist"))
                {
                    this.ChangeEquipmentBlacklists();
                    this.HealingItemBlacklist();
                }

                // enigma artifact
                if (GetConfigBool(currentCategory, true, "Enigma Artifact"))
                {
                    this.ChangeEnigmaBlacklists();
                }

                // stuns
                if (GetConfigBool(currentCategory, true, "Stun"))
                {
                    this.StunChanges();
                }

                // the backup
                if (GetConfigBool(currentCategory, true, "The Backup Equipment"))
                {
                    LoadEquipDef(nameof(RoR2Content.Equipment.DroneBackup)).cooldown = 60;
                }

                string slowChangesTitle = " Slow Attack Speed Packet";
                string slowChangesDesc = "Set how much this debuff slows attack speed, expressed as a decimal. Vanilla 0.";
                this.BuffSlows();

                // tar slow
                tarSlowAspdReduction = CustomConfigFile.Bind<float>(currentCategory.ToString() + " Packet", "Tar" + slowChangesTitle, tarSlowAspdReduction, slowChangesDesc).Value;

                // kit slow
                kitSlowAspdReduction = CustomConfigFile.Bind<float>(currentCategory.ToString() + " Packet", "Kit" + slowChangesTitle, kitSlowAspdReduction, slowChangesDesc).Value;

                // chronobauble
                chronoSlowAspdReduction = CustomConfigFile.Bind<float>(currentCategory.ToString() + " Packet", "Chronobauble" + slowChangesTitle, chronoSlowAspdReduction, slowChangesDesc).Value;
                #endregion




                //this.MakeMinionsInheritOnKillEffects();

                //scav could have royal cap? cunning
            }

            currentCategory = BalanceCategory.StateOfDamage;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: chefs stache, malware stick, new lopper, whetstone
                // EQUIPMENT: old guillotine
                // ENEMIES: Chipchip the Wicked (debuff)

                #region ESSENTIAL
                // razorwire
                RazorwireReworks();
                On.RoR2.Orbs.LightningOrb.Begin += NerfRazorwireOrb;

                // damage
                this.NerfBands();
                this.StickyRework();
                #endregion

                #region PACKETS
                // crits
                if (GetConfigBool(currentCategory, true, "Critical Strike"))
                {
                    this.NerfCritGlasses();
                    OcularHudBuff();
                }

                // death mark fix :)
                if (GetConfigBool(currentCategory, true, "Death Mark Fix"))
                {
                    DeathMarkFix();
                }

                // molten perforator
                if (GetConfigBool(currentCategory, true, "Molten Perforator"))
                {
                    CreateMeatballNapalm();
                    ProjectileImpactExplosion meatballPIE = meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
                    this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().blastProcCoefficient = 0f; //0.7
                    this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().childrenCount = 1; //0
                    this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().childrenProjectilePrefab = meatballNapalmPool; //null
                    this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().fireChildren = true; //false
                }

                // charged perforator
                if (GetConfigBool(currentCategory, true, "Charged Perforator"))
                {
                    On.RoR2.Orbs.SimpleLightningStrikeOrb.Begin += NerfChargedPerforatorOrb;
                }

                // shatterspleen, dmg
                if (GetConfigBool(currentCategory, true, "(DMG) Shatterspleen"))
                {
                    this.spleenPrefab.GetComponent<RoR2.DelayBlast>().procCoefficient = 0f;
                }

                // fireworks
                if (GetConfigBool(currentCategory, true, "Fireworks"))
                {
                    this.fireworkProjectilePrefab.GetComponent<ProjectileController>().procCoefficient = 0; //0.33f
                }

                // ceremonial dagger
                if (GetConfigBool(currentCategory, true, "Ceremonial Dagger"))
                {
                    CeremonialDaggerNerfs();
                }

                // willowisp
                if (GetConfigBool(currentCategory, true, "Will-o-the-Wisp"))
                {
                    WillowispNerfs();
                }

                // gasoline
                if (GetConfigBool(currentCategory, true, "Gasoline"))
                {
                    GasolineChanges();
                }

                // meteorite
                if (GetConfigBool(currentCategory, true, "Glowing Meteorite"))
                {
                    this.FixMeteorFalloff();
                }

                // warcry
                if (GetConfigBool(currentCategory, true, "Warcry Buff"))
                {
                    this.EditWarCry();
                }


                On.RoR2.Orbs.DevilOrb.Begin += NerfDevilOrb;

                // nkuhanas opinion, DMG
                if (GetConfigBool(currentCategory, true, "(DMG) Nukuhanas Opinion"))
                {
                    opinionDevilorbProc = 0;
                }

                // little disciple
                if (GetConfigBool(currentCategory, true, "Little Disciple"))
                {
                    discipleDevilorbProc = 0;
                }
                #endregion

                //this.DoSadistScavenger();
            }
        }

        #region modify items and equips
        static public void RetierItem(string itemName, ItemTier tier = ItemTier.NoTier)
        {
            ItemDef def = LoadItemDef(itemName);
            if (def != null)
            {
                def.tier = tier;
                def.deprecatedTier = tier;
            }
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
            if(buffDef != null)
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

        private bool GetConfigBool(BalanceCategory currentCategory, bool defaultValue, string packetTitle, string desc = "")
        {
            if(desc != "")
            {
                return CustomConfigFile.Bind<bool>(currentCategory + " Packets - See README For Details.", 
                    packetTitle + " Packet", defaultValue, 
                    $"The changes in this Packet will be enabled if set to true.").Value;
            }
            return CustomConfigFile.Bind<bool>(currentCategory + " Packets", 
                packetTitle + " Packet", defaultValue, 
                "(The following changes will be enabled if set to true) " + desc).Value;
        }

        #region config
        private void InitializeConfig()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\RiskierRain.cfg", true);

            EnableConfig = CustomConfigFile.Bind<bool>("Allow Config Options", "Enable Config", false,
                "Set this to true to enable config options. Please keep in mind that it was not within my design intentions to play this way. " +
                "This is primarily meant for modpack users with tons of mods installed. " +
                "If you have any issues or feedback on my mod balance, please feel free to send in feedback with the contact info in the README or Thunderstore description.");

            for (int i = 0; i < DisableConfigCategories.Length; i++)
            {
                DisableConfigCategories[i] = AddConfigCategory((BalanceCategory)i);
            }
        }

        ConfigEntry<bool> AddConfigCategory(BalanceCategory category)
        {
            string categoryName = (category).ToString();

            ConfigEntry<bool> newCategoryConfig = CustomConfigFile.Bind<bool>(
                "Disable Balance Categories",
                categoryName,
                false,
                $"Set this to TRUE if you would like to DISABLE changes for the balance category: {categoryName}"
                );

            return newCategoryConfig;
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
            BalanceCategory category = scav.Category;

            bool enabled = true;

            if (category != BalanceCategory.None && category != BalanceCategory.Count)
            {
                string name = scav.ScavName.Replace("'", "");
                enabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString() + " Content", $"Enable Twisted Scavenger: {name} the {scav.ScavTitle}", true, "Should this scavenger appear in A Moment, Whole?").Value;
            }
            else
            {
                Debug.Log($"{scav.ScavLangTokenName} initializing into Balance Category: {category}!!");
            }

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
                if (item.IsHidden)
                    return;

                if (ValidateItem(item, Items))
                {
                    item.Init(CustomConfigFile);
                }
                else
                {
                    Debug.Log("Item: " + item.ItemName + " Did not initialize!");
                }
            }
        }

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            BalanceCategory category = item.Category;

            var itemEnabled = item.Tier == ItemTier.NoTier;

            if (category != BalanceCategory.None && category != BalanceCategory.Count && !itemEnabled)
            {
                string name = item.ItemName.Replace("'", "");
                itemEnabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString() + " Content", $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
            }
            else
            {
                Debug.Log($"{item.ItemName} item initializing into Balance Category: {category}!!");
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

                if (ValidateEquipment(equipment, Equipments))
                {
                    equipment.Init(Config);
                }
            }
        }
        public bool ValidateEquipment(EquipmentBase equipment, List<EquipmentBase> equipmentList)
        {
            BalanceCategory category = equipment.Category;

            var itemEnabled = true;

            if (category != BalanceCategory.None && category != BalanceCategory.Count)
            {
                itemEnabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString() + " Content", "Enable Equipment: " + equipment.EquipmentName, true, "Should this item appear in runs?").Value;
            }
            else
            {
                Debug.Log($"{equipment.EquipmentName} equipment initializing into Balance Category: {category}!!");
            }

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
            BalanceCategory category = equipment.Category;

            var itemEnabled = true;

            if (category != BalanceCategory.None && category != BalanceCategory.Count)
            {
                itemEnabled = IsCategoryEnabled(category) &&
                CustomConfigFile.Bind<bool>(category.ToString() + " Content", $"Enable Aspect: {equipment.EliteEquipmentName} ({equipment.EliteModifier} Elite)", true, "Should these elites appear in runs?").Value;
            }
            else
            {
                Debug.Log($"{equipment.EliteEquipmentName} equipment initializing into Balance Category: {category}!!");
            }

            EliteEquipmentStatusDictionary.Add(equipment, itemEnabled);
            return itemEnabled;
        }
        #endregion
    }
}
