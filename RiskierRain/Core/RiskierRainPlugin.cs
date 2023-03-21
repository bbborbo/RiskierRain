using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RiskierRain.CoreModules;
using RiskierRain.Equipment;
using RiskierRain.Interactables;
using RiskierRain.Items;
using RiskierRain.Scavengers;
using RiskierRain.Skills;
using RiskierRain.SurvivorTweaks;
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
using static R2API.RecalculateStatsAPI;
//using RiskierRain.Changes.Reworks.NerfsReworks.SpawnlistChanges; //idk if this is a good way of doing

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
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.ArtifactGesture", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.TransRights.RealisticTransgendence", BepInDependency.DependencyFlags.HardDependency)]

    [BepInDependency("com.DestroyedClone.AncientScepter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("Withor.AcridBiteLunge", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.johnedwa.RTAutoSprintEx", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("HIFU.UltimateCustomRun", BepInDependency.DependencyFlags.SoftDependency)]

    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(PrefabAPI), 
        nameof(DirectorAPI), 
        nameof(ItemAPI), nameof(RecalculateStatsAPI), nameof(EliteAPI))]

    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "RiskierRain";
        public const string version = "1.0.0";

        public static PluginInfo PInfo { get; private set; }

        public static AssetBundle mainAssetBundle = Tools.LoadAssetBundle(RiskierRain.Properties.Resources.itmightbebad);
        public static AssetBundle placeholderAssetBundle = Tools.LoadAssetBundle(RiskierRain.Properties.Resources.borboitemicons);
        public static AssetBundle orangeAssetBundle = Tools.LoadAssetBundle(RiskierRain.Properties.Resources.orangecontent);
        public static string dropPrefabsPath = "Assets/Models/DropPrefabs";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static string eliteMaterialsPath = "Assets/Textures/Materials/Elite/";

        public static bool isAELoaded = Tools.isLoaded("com.Borbo.ArtificerExtended");
        public static bool isHBULoaded = Tools.isLoaded("com.Borbo.HuntressBuffULTIMATE");
        public static bool isScepterLoaded = Tools.isLoaded("com.DestroyedClone.AncientScepter");
        public static bool autosprintLoaded = Tools.isLoaded("com.johnedwa.RTAutoSprintEx");
        public static bool acridLungeLoaded = Tools.isLoaded("Withor.AcridBiteLunge");
        public static bool ucrLoaded = Tools.isLoaded("HIFU.UltimateCustomRun");

        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> EnableConfig { get; set; }
        public static ConfigEntry<bool> StateOfDefenseAndHealing { get; set; }
        public static ConfigEntry<bool> StateOfHealth { get; set; }
        public static ConfigEntry<bool> StateOfInteraction { get; set; }
        public static ConfigEntry<bool> StateOfDamage { get; set; }
        public static ConfigEntry<bool> StateOfEconomy { get; set; }
        public static ConfigEntry<bool> StateOfElites { get; set; }
        public static ConfigEntry<bool> StateOfDifficulty { get; set; }
        public static ConfigEntry<bool> StateOfSurvivors { get; set; }
        public static ConfigEntry<bool> StateOfCommencement { get; set; }

        public static ConfigEntry<bool>[] EnableConfigCategories = new ConfigEntry<bool>[(int)BalanceCategory.Count] 
        { StateOfDefenseAndHealing, StateOfHealth, StateOfInteraction, StateOfDamage, StateOfDifficulty, StateOfSurvivors, StateOfCommencement };

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

        bool IsCategoryEnabled(BalanceCategory category)
        {
            bool enabled = true;

            if (EnableConfig.Value && !EnableConfigCategories[(int)category].Value)
            {
                enabled = false;
            }

            return enabled;
        }

        void Awake()
        {
            PInfo = Info;

            InitializeCoreModules();

            InitializeConfig();
            InitializeItems();
            InitializeSkills();
            InitializeEquipment();
            InitializeEliteEquipment();
            InitializeScavengers();
            //InitializeEverything();
            Assets.SwapShadersFromMaterialsInBundle(orangeAssetBundle);

            #region rework pending / priority removal
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.GoldOnHurt)); //penny roll/roll of pennies
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.OutOfCombatArmor)); //weirdly shaped opal

            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp

            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoreMissile)); //pocket icbm
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PermanentDebuffOnHit)); //symbiotic scorpion
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.DroneWeapons)); //spare drone parts
            #endregion

            RetierItem(nameof(RoR2Content.Items.HeadHunter), ItemTier.Boss);

            RoR2Application.onLoad += InitializeEverything;

            //lol
            LanguageAPI.Add("ITEM_SHOCKNEARBY_PICKUP", "lol");
            LanguageAPI.Add("ITEM_AUTOCASTEQUIPMENT_PICKUP", "lol");
            LanguageAPI.Add("ITEM_EXECUTELOWHEALTHELITE_PICKUP", "lol");

            
            new ContentPacks().Initialize();
        }

        private void InitializeEverything()
        {
            IL.RoR2.Orbs.DevilOrb.OnArrival += BuffDevilOrb;

            ///summary
            ///- nerfs healing
            ///- nerfs mobility (jump rework)
            ///- nerfs non-EHP defense
            BalanceCategory currentCategory = BalanceCategory.StateOfDefenseAndHealing;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: beans, battery, berserker brew, morning mocha, star veil, cloud/fart in a bottle
                // EQUIPMENT: ninja gear
                // ENEMIES: Baba the Enlightened (speed scav)

                #region ESSENTIAL
                // healing
                MedkitNerf();
                MonsterToothNerf();
                ReworkWeepingFungus();

                // mobility
                JumpReworks();
                IL.EntityStates.GenericCharacterMain.ProcessJump += FeatherNerf;
                GoatHoofNerf();
                EnergyDrinkNerf();

                // defense
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


                
                #endregion
            }

            ///region
            ///- ehp
            currentCategory = BalanceCategory.StateOfHealth;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: borbos band, cobalt shield, frozen turtle shell, flower crown, utility belt
                // EQUIPMENT: tesla coil
                // ENEMIES: Bobo the Unbreakable (defense scav)

                #region ESSENTIAL
                //barrier
                this.BuffBarrier();
                TeddyChanges();
                #endregion

                #region PACKETS
                //infusion
                if (GetConfigBool(currentCategory, true, "Infusion"))
                {
                    this.FuckingFixInfusion();
                }

                // jade elephant
                if (GetConfigBool(currentCategory, true, "Jade Elephant"))
                {
                    JadeElephantChanges();
                }

                AdjustVanillaDefense();

                knurlFreeArmor = FreeArmorConfig("Knurl", knurlFreeArmor);
                bucklerFreeArmor = FreeArmorConfig("Rose Buckler", bucklerFreeArmor);
                rapFreeArmor = FreeArmorConfig("Repulsion Armor Plating", rapFreeArmor);
                int FreeArmorConfig(string name, int defaultValue)
                {
                    return CustomConfigFile.Bind<int>(
                        currentCategory.ToString() + " Packet", 
                        name + " Armor Packet", 
                        defaultValue, 
                        "Set how much additional armor this item gives. Vanilla 0."
                        ).Value;
                }


                //shock restores shield
                if (GetConfigBool(currentCategory, true, "Shock Buff"))
                {
                    ShockBuff();
                }
                #endregion
                //nerf engi turret max health?
            }

            ///summary
            ///- status effects (attack speed slow)
            ///- planula
            ///- enemy item blacklists
            ///- enigma blacklists
            ///most general "gameplay" category
            currentCategory = BalanceCategory.StateOfInteraction;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: magic quiver, slungus, wicked band, permafrost, fuse, void happiest mask/tragic facade
                // EQUIPMENT: old guillotine
                // ENEMIES: 

                #region ESSENTIAL
                // misc
                RiskierRainPlugin.RemoveEquipment(nameof(RoR2Content.Equipment.Gateway));
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
                    this.FixJellyNuke();
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

                this.BuffSlows();

                tarSlowAspdReduction = SlowAspdConfig("Tar", tarSlowAspdReduction); 
                kitSlowAspdReduction = SlowAspdConfig("Kit", kitSlowAspdReduction); 
                chronoSlowAspdReduction = SlowAspdConfig("Chronobauble", chronoSlowAspdReduction);
                chillSlowAspdReduction = SlowAspdConfig("Chill", chillSlowAspdReduction);
                float SlowAspdConfig(string name, float defaultValue)
                {
                    return CustomConfigFile.Bind<float>(
                        currentCategory.ToString() + " Packet",
                        name + " Slow Attack Speed Packet",
                        defaultValue,
                        "Set how much this debuff slows attack speed, expressed as a decimal. Vanilla 0."
                        ).Value;
                }

                //lepton daisy ADD CONFIG
                if (GetConfigBool(currentCategory, true, "Lepton Daisy"))
                {
                    BuffDaisy();
                }

                //fuel array
                if (GetConfigBool(currentCategory, true, "Fuel Array Activates Equipment Effects"))
                {
                    FuelArrayFunnyBuff();
                }

                //fuel array
                if (GetConfigBool(currentCategory, true, "Spawn Slot Minions (i.e. Xi Construct) Inherit Elite Affix"))
                {
                    MakeSpawnSlotSpawnsInheritEliteAffix();
                }
                #endregion
                //this.MakeMinionsInheritOnKillEffects();

                //scav could have royal cap? cunning
            }

            ///summary
            ///- autoplay
            ///- procs and crits
            ///- burn rework
            currentCategory = BalanceCategory.StateOfDamage;
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: atg mk3, chefs stache, new lopper, natures gift, Shard Vomitter
                // EQUIPMENT: Broken Zapinator 
                // ENEMIES: 

                #region ESSENTIAL
                // razorwire
                RazorwireReworks();
                On.RoR2.Orbs.LightningOrb.Begin += NerfRazorwireOrb;

                // damage
                this.NerfBands();
                this.StickyRework();
                BurnReworks();

                if (AtgMissileMk3.instance.ItemsDef != null)
                {
                    this.ReworkPlasmaShrimp();
                }
                #endregion

                #region PACKETS
                // crits
                if (GetConfigBool(currentCategory, true, "Critical Strike"))
                {
                    this.NerfCritGlasses();
                    OcularHudBuff();
                }
                if (GetConfigBool(currentCategory, true, "Laser Scope Rework (Combat Telescope)"))
                {
                    ReworkLaserScope();
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

                // voidsent flame
                if (GetConfigBool(currentCategory, true, "Voidsent Flame"))
                {
                    VoidsentNerfs();
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

                // polylute
                if (GetConfigBool(currentCategory, true, "Polylute Nerf"))
                {
                    this.ReworkPolylute();
                }

                // shuriken
                if (GetConfigBool(currentCategory, true, "Shuriken Rework"))
                {
                    this.ReworkShuriken();
                }
                #endregion

                //this.DoSadistScavenger();
            }

            ///summary
            ///- economy changes
            ///- enemy changes
            ///- boss item drops
            ///- difficulty changes
            ///- elites
            ///this is essentially DifficultyPlus
            currentCategory = BalanceCategory.StateOfDifficulty; 
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: scalpel, coin gun, greedy ring
                // EQUIPMENT: gold bomb? lol
                // ENEMIES: 

                //enemies use equipment
                MakeEnemiesuseEquipment();
                //spawnlists
                ChangeSpawnlists();

                #region difficulty dependent difficulty
                //ambient level
                if (GetConfigBool(currentCategory, true, "Difficulty: Difficulty Dependent Ambient Difficulty Boost"))
                {
                    AmbientLevelDifficulty();
                    FixMoneyAndExpRewards(); //related to ambient difficulty boost
                }

                //elite stats
                if (GetConfigBool(currentCategory, true, "Elite: Elite Stats and Ocurrences"))
                {
                    ChangeEliteStats();
                }

                //teleporter particle
                if (GetConfigBool(currentCategory, true, "Difficulty: Teleporter Particle Radius"))
                {
                    DifficultyDependentTeleParticles();
                }

                //monsoon stat boost
                if (GetConfigBool(currentCategory, true, "Difficulty: Monsoon Stat Booster"))
                {
                    MonsoonStatBoost();
                }

                //eclipse changes
                if (GetConfigBool(currentCategory, true, "Difficulty: Eclipse Changes"))
                {
                    EclipseChanges();
                }

                //eclipse level select
                if (GetConfigBool(currentCategory, true, "Difficulty: Eclipse Level Select"))
                {
                    EclipseLevelSelect();
                }
                #endregion

                #region packets
                //economy

                // boss item drop
                if (GetConfigBool(currentCategory, true, "Boss: Boss Item Drops"))
                {
                    BossesDropBossItems();
                    TricornRework();
                    DirectorAPI.InteractableActions += DeleteYellowPrinters;
                }

                // horde of many wake of vultures trophy
                if (GetConfigBool(currentCategory, true, "Boss: Wake Of Vultures Horde Of Many Trophy"))
                {
                    HordeOfManyDropsWakeOfVultures();
                }

                //overloading elite
                if (GetConfigBool(currentCategory, true, "Elite: Overloading Elite Rework"))
                {
                    OverloadingEliteChanges();
                }

                //Mending elite
                if (GetConfigBool(currentCategory, true, "Elite: Mending Elite Rework"))
                {
                    MendingEliteChanges();
                }

                //voidtouched elite
                if (GetConfigBool(currentCategory, true, "Elite: Voidtouched Elite Rework"))
                {
                    VoidtouchedEliteChanges();
                }

                //blazing elite
                //BlazingEliteChanges();

                //newt shrine
                if (GetConfigBool(currentCategory, true, "Lunar: Newt Shrine"))
                {
                    NerfBazaarStuff();
                }

                On.RoR2.Run.BeginStage += GetChestCostForStage;

                //gold gain and chest scaling
                if (GetConfigBool(currentCategory, true, "Economy: Gold Gain and Chest Scaling"))
                {
                    FixMoneyScaling();
                }

                //elite gold
                if (GetConfigBool(currentCategory, true, "Economy: Elite Gold Rewards"))
                {
                    EliteGoldReward();
                }

                //stage interactable credits
                if (GetConfigBool(currentCategory, true, "Economy: Stage Interactable Credits"))
                {
                    DirectorAPI.StageSettingsActions += IncreaseStageInteractableCredits;
                }

                //stage monster credits
                if (GetConfigBool(currentCategory, true, "Economy: Stage Monster Credits"))
                {
                    DirectorAPI.StageSettingsActions += IncreaseStageMonsterCredits;
                }

                //printer
                if (GetConfigBool(currentCategory, true, "Economy: Printer"))
                {
                    DirectorAPI.InteractableActions += PrinterOccurrenceHook;
                }

                //scrapper
                if (GetConfigBool(currentCategory, true, "Economy: Scrapper"))
                {
                    DirectorAPI.InteractableActions += ScrapperOccurrenceHook;
                }

                //equipment barrels and shops
                if (GetConfigBool(currentCategory, true, "Economy: Equipment Barrel/Shop"))
                {
                    DirectorAPI.InteractableActions += EquipBarrelOccurrenceHook;
                }

                //blood shrine
                if (GetConfigBool(currentCategory, true, "Economy: Blood Shrine"))
                {
                    BloodShrineRewardRework();
                }

                //void cradle
                if (GetConfigBool(currentCategory, true, "Economy: Void Cradle"))
                {
                    VoidCradleRework();
                }

                //void cradle
                if (GetConfigBool(currentCategory, true, "Economy: Crowdfunder Funny Money"))
                {
                    CrowdfunderFunny();
                }

                //wandering vagrant
                if (GetConfigBool(currentCategory, true, "Enemy: Wandering Vagrant"))
                {
                    VagrantChanges();
                }

                //blind pest
                if (GetConfigBool(currentCategory, true, "Enemy: Blind Pest"))
                {
                    PestChanges();
                }

                //beetle queen
                if (GetConfigBool(currentCategory, true, "Enemy: Beetle Queen"))
                {
                    QueenChanges();
                }

                //void reaver
                if (GetConfigBool(currentCategory, true, "Enemy: Void Reaver"))
                {
                    VoidReaverChanges();
                }

                //void barnacle
                if (GetConfigBool(currentCategory, true, "Enemy: Void Barnacle"))
                {
                    BarnacleChanges();
                }

                //gup
                if (GetConfigBool(currentCategory, true, "Enemy: Gup"))
                {
                    GupChanges();
                }
                #endregion

                LanguageAPI.Add("DIFFICULTY_EASY_DESCRIPTION", drizzleDesc + "</style>");
                // " + $"\n>Most Bosses have <style=cIsHealing>reduced skill sets</style>

                LanguageAPI.Add("DIFFICULTY_NORMAL_DESCRIPTION", rainstormDesc + "</style>");

                LanguageAPI.Add("DIFFICULTY_HARD_DESCRIPTION", monsoonDesc + "</style>");

                //this.DoSadistScavenger();
            }

            ///summary
            ///DuckSurvivorTweaks
            currentCategory = BalanceCategory.StateOfSurvivors; 
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: 
                // EQUIPMENT: 
                // ENEMIES: 

                #region dead
                RiskierRainPlugin.RemoveEquipment(nameof(RoR2Content.Equipment.Gateway));
                InitializeSurvivorTweaks();
                #endregion

                //this.DoSadistScavenger();
            }

            ///summary 
            ///- commencement changes
            ///- pillar items
            ///- mithrix changes
            ///essentially CommencementPerfected
            currentCategory = BalanceCategory.StateOfCommencement; 
            if (IsCategoryEnabled(currentCategory))
            {
                // CONTENT...
                // ITEMS: 
                // EQUIPMENT: 
                // ENEMIES: 

                #region dead
                MakePillarsFun();
                #endregion

                //this.DoSadistScavenger();
            }

            //interactables bc they need to load after items:
            InitializeInteractables();
        }

        /*[HarmonyPrefix, HarmonyPatch(typeof(RoR2.Items.ContagiousItemManager), nameof(RoR2.Items.ContagiousItemManager.Init))]
        private static void InitializeTransformations()
        {
            List<ItemDef.Pair> newTransformationTable = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].ToList();
            instance.tooLate = true;
            if (instance.entries.Count <= 0 && instance.modifications.Count <= 0)
            {
                instance.Logger.LogWarning("No void items in the entries and modifications list! Cancelling transformation.");
                return;
            }
            foreach (CustomVoidEntry entry in instance.entries)
            {
                if (entry.VoidItem.requiredExpansion == null)
                {
                    entry.VoidItem.requiredExpansion = ExpansionCatalog.expansionDefs.FirstOrDefault(x => x.nameToken == "DLC1_NAME");
                }
            }
            IEnumerable<CustomVoidEntry> defs = instance.entries.Split(x => x.transformType == CustomVoidEntry.TransformType.Def, out IEnumerable<CustomVoidEntry> names);

            foreach (CustomVoidEntry entry in defs)
            {
                instance.newTransformationTable.Add(new ItemDef.Pair() { itemDef1 = entry.TransformedItem, itemDef2 = entry.VoidItem });
                instance.Logger.LogMessage("Successfully created a transformation for " + entry.VoidItem.name + "!");
            }
            foreach (CustomVoidEntry entry in names)
            {
                ItemDef itemDef = ValidateItemString(entry.TransformedItemName);
                if (!ValidateItem(itemDef))
                {
                    instance.Logger.LogError("Failed to create a transformation for " + entry.VoidItem.name + " on transformation name " + entry.TransformedItemName + ".");
                    continue;
                }
                instance.newTransformationTable.Add(new ItemDef.Pair() { itemDef1 = itemDef, itemDef2 = entry.VoidItem });
                instance.Logger.LogMessage("Successfully created a transformation for " + entry.VoidItem.name + "!");
            }

            IEnumerable<VoidItemModification> modDefs = instance.modifications.Split(x => x.transformType == CustomVoidEntry.TransformType.Def, out IEnumerable<VoidItemModification> modNames);
            foreach (VoidItemModification mod in modDefs)
            {
                if (mod.modification == VoidItemModification.ModificationType.Modify)
                {
                    ChangeTransformation(mod);
                }
                else
                {
                    RemoveTransformation(mod);
                }
            }
            foreach (VoidItemModification mod in modNames)
            {
                ItemDef VoidItem = ValidateItemString(mod.VoidItemName);
                ItemDef CurrentTransformedItem = ValidateItemString(mod.CurrentTransformedItemName);
                ItemDef NewTransformation = null;
                if (mod.modification == VoidItemModification.ModificationType.Modify)
                    NewTransformation = ValidateItemString(mod.NewTransformationName);

                if (!ValidateItem(VoidItem) || !ValidateItem(CurrentTransformedItem))
                {
                    instance.Logger.LogError("Issue modifying transformation for " + mod.VoidItemName + ". Aborting this modification.");
                    continue;
                }
                if (mod.modification == VoidItemModification.ModificationType.Modify)
                {
                    ChangeTransformation(new VoidItemModification(VoidItem, CurrentTransformedItem, NewTransformation, mod.modification));
                }
                else
                {
                    RemoveTransformation(new VoidItemModification(VoidItem, CurrentTransformedItem, NewTransformation, mod.modification));
                }
            }

            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = newTransformationTable.ToArray();
        }*/

        #region modify items and equips
        static public ItemDef RetierItem(string itemName, ItemTier tier = ItemTier.NoTier)
        {
            ItemDef def = LoadItemDef(itemName);
            if (def != null)
            {
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
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);

            EnableConfig = CustomConfigFile.Bind<bool>("Allow Config Options", "Enable Config", false,
                "Set this to true to enable config options. Please keep in mind that it was not within my design intentions to play this way. " +
                "This is primarily meant for modpack users with tons of mods installed. " +
                "If you have any issues or feedback on my mod balance, please feel free to send in feedback with the contact info in the README or Thunderstore description.");

            for (int i = 0; i < EnableConfigCategories.Length; i++)
            {
                EnableConfigCategories[i] = AddConfigCategory((BalanceCategory)i);
            }
        }

        ConfigEntry<bool> AddConfigCategory(BalanceCategory category)
        {
            string categoryName = (category).ToString();

            ConfigEntry<bool> newCategoryConfig = CustomConfigFile.Bind<bool>(
                "Disable Balance Categories",
                categoryName,
                false,
                $"Set this to TRUE if you would like to ENABLE changes for the balance category: {categoryName}"
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
                if (!item.IsHidden)
                {
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
        }

        bool ValidateItem(ItemBase item, List<ItemBase> itemList)
        {
            BalanceCategory category = item.Category;

            var itemEnabled = item.Tier == ItemTier.NoTier;

            if (!itemEnabled)
            {
                string name = item.ItemName.Replace("'", "");
                if (category != BalanceCategory.None && category != BalanceCategory.Count && !itemEnabled)
                {
                    itemEnabled = IsCategoryEnabled(category) &&
                    CustomConfigFile.Bind<bool>(category.ToString() + " Content", $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
                }
                else
                {
                    itemEnabled = IsCategoryEnabled(category) &&
                    CustomConfigFile.Bind<bool>("Uncategorized Content", $"Enable Item: {name}", true, "Should this item appear in runs?").Value;
                    Debug.Log($"{name} item initializing into Balance Category: {category}!!");
                }
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

        void InitializeSurvivorTweaks()
            {
                var TweakTypes = Assembly.GetExecutingAssembly().GetTypes().Where(type => !type.IsAbstract && type.IsSubclassOf(typeof(SurvivorTweakModule)));

                foreach (var tweakType in TweakTypes)
                {
                    SurvivorTweakModule module = (SurvivorTweakModule)Activator.CreateInstance(tweakType);

                    string name = module.survivorName == "" ? module.bodyName : module.survivorName;
                    bool isEnabled = CustomConfigFile.Bind<bool>("Survivor Tweaks",
                        $"Enable Tweaks For: {module.survivorName}", true,
                        $"Should DuckSurvivorTweaks change {module.survivorName}?").Value;
                    if (isEnabled)
                    {
                        module.Init();
                    }
                    //TweakStatusDictionary.Add(module.ToString(), isEnabled);
                }
            }

        #region skills
        public static List<Type> entityStates = new List<Type>();
        public static List<SkillBase> Skills = new List<SkillBase>();
        public static List<ScepterSkillBase> ScepterSkills = new List<ScepterSkillBase>();
        public static Dictionary<SkillBase, bool> SkillStatusDictionary = new Dictionary<SkillBase, bool>();

        private void InitializeSkills()
        {
            if (!IsCategoryEnabled(BalanceCategory.StateOfSurvivors))
                return;
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

            if (forceUnlock)
            {
                Skills.Add(item);
            }
            SkillStatusDictionary.Add(item, forceUnlock);

            return forceUnlock;
        }

        bool ValidateScepterSkill(ScepterSkillBase item)
        {
            var forceUnlock = isScepterLoaded;

            if (forceUnlock)
            {
                ScepterSkills.Add(item);
            }

            return forceUnlock;
        }
        #endregion
    }
}
