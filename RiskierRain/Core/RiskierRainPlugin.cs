using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RiskierRain.CoreModules;
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
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using BorboStatUtils;
using ChillRework;
using MonoMod.RuntimeDetour;
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
    [BepInDependency(ChillRework.ChillRework.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(BorboStatUtils.BorboStatUtils.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(NegativeRegenFix.NegativeRegenFix.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(RiskierRainContent.RiskierRainContent.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.GreenAlienHead", BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency("com.Borbo.ArtifactGesture", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.Borbo.HuntressBuffULTIMATE", BepInDependency.DependencyFlags.HardDependency)]

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

    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "RiskierRain";
        public const string version = "1.0.0";

        public static PluginInfo PInfo { get; private set; }

        public static string dropPrefabsPath => Assets.dropPrefabsPath;
        public static string iconsPath => Assets.iconsPath;
        public static string eliteMaterialsPath => Assets.eliteMaterialsPath;

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

            #region rework pending / priority removal
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.StunChanceOnHit)); //stun grenade
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.GoldOnHurt)); //penny roll/roll of pennies

            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.BonusGoldPackOnKill)); //squid polyp
            ItemDef tome = Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/TreasureCache/TreasureCache.asset").WaitForCompletion();
            RiskierRainPlugin.RetierItem(tome); //ghor's tome

            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Talisman)); //soulbound
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoreMissile)); //pocket icbm
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PermanentDebuffOnHit)); //symbiotic scorpion
            RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.DroneWeapons)); //spare drone parts
            #endregion

            RoR2Application.onLoad += InitializeEverything;

            
            new ContentPacks().Initialize();
        }

        private void InitializeEverything()
        {
            IL.RoR2.Orbs.DevilOrb.OnArrival += BuffDevilOrb;

            Hook ospHook = new Hook(
              typeof(CharacterBody).GetMethod("get_hasOneShotProtection", (BindingFlags)(-1)),
              typeof(RiskierRainPlugin).GetMethod(nameof(FuckOsp), (BindingFlags)(-1))
            );

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

            // healing
            MedkitNerf();
            MonsterToothNerf();
            ReworkWeepingFungus();

            // mobility
            GoatHoofNerf();
            IL.EntityStates.GenericCharacterMain.ProcessJump += FeatherNerf;
            EnergyDrinkNerf();

            // defense
            this.BuffBarrier();
            TeddyChanges();

            // scythe
            if (GetConfigBool(true, "Harvesters Scythe"))
            {
                ScytheNerf();
            }

            // dynamic jump
            if (GetConfigBool(true, "Dynamic Jump"))
            {
                IL.RoR2.CharacterMotor.PreMove += DynamicJump;
            }

            // nkuhana D+H
            if (GetConfigBool(true, "(D+H) NKuhanas Opinion"))
            {
                this.BuffNkuhana();
            }

            // droplet general
            if (GetConfigBool(true, "Droplet General"))
            {
                this.FixPickupStats();
            }

            // monster tooth
            if (GetConfigBool(true, "Monster Tooth"))
            {
                MonsterToothDurationBuff();
            }
            //infusion
            if (GetConfigBool(true, "Infusion"))
            {
                this.FuckingFixInfusion();
            }

            // jade elephant
            if (GetConfigBool(true, "Jade Elephant"))
            {
                JadeElephantChanges();
            }

            AdjustVanillaDefense();

            knurlFreeArmor = FreeArmorConfig("Knurl", knurlFreeArmor);
            bucklerFreeArmor = FreeArmorConfig("Rose Buckler", bucklerFreeArmor);
            rapFreeArmor = FreeArmorConfig("Repulsion Armor Plating", rapFreeArmor);
            int FreeArmorConfig(string name, int defaultValue)
            {
                return CustomConfigFile.Bind<int>("Packet",
                    name + " Armor Packet",
                    defaultValue,
                    "Set how much additional armor this item gives. Vanilla 0."
                    ).Value;
            }


            //shock restores shield
            if (GetConfigBool(true, "Shock Buff"))
            {
                ShockBuff();
            }
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

            // misc
            RiskierRainPlugin.RemoveEquipment(nameof(RoR2Content.Equipment.Gateway));

            // shattering justics
            if (GetConfigBool(true, "Shattering Justice"))
            {
                this.BuffJustice();
            }

            // resonance disc
            if (GetConfigBool(true, "Resonance Disc"))
            {
                ResonanceDiscNerfs();
                //this.NerfResDisc();
                EntityStates.LaserTurbine.FireMainBeamState.mainBeamProcCoefficient = 0.5f;
            }

            // jellynuke
            if (GetConfigBool(true, "Jellynuke"))
            {
                this.FixJellyNuke();
            }

            // planula
            if (GetConfigBool(true, "Planula"))
            {
                this.ReworkPlanula();
            }

            // shatterspleen, INT
            if (GetConfigBool(true, "Shatterspleen"))
            {
                this.ReworkShatterspleen();
            }

            // enemy blacklist
            if (GetConfigBool(true, "Enemy Blacklist"))
            {
                this.ChangeEquipmentBlacklists();
                this.HealingItemBlacklist();
            }

            // enigma artifact
            if (GetConfigBool(true, "Enigma Artifact"))
            {
                this.ChangeEnigmaBlacklists();
            }

            // stuns
            if (GetConfigBool(true, "Stun"))
            {
                this.StunChanges();
            }

            // the backup
            if (GetConfigBool(true, "The Backup Equipment"))
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
                return CustomConfigFile.Bind<float>("Packet",
                    name + " Slow Attack Speed Packet",
                    defaultValue,
                    "Set how much this debuff slows attack speed, expressed as a decimal. Vanilla 0."
                    ).Value;
            }

            //lepton daisy ADD CONFIG
            if (GetConfigBool(true, "Lepton Daisy"))
            {
                BuffDaisy();
            }

            //fuel array
            if (GetConfigBool(true, "Fuel Array Activates Equipment Effects"))
            {
                FuelArrayFunnyBuff();
            }

            //fuel array
            if (GetConfigBool(true, "Spawn Slot Minions (i.e. Xi Construct) Inherit Elite Affix"))
            {
                MakeSpawnSlotSpawnsInheritEliteAffix();
            }

            //goobo jr
            if (GetConfigBool(true, "Goobo Jr."))
            {
                GooboJrChanges();
            }
            //this.MakeMinionsInheritOnKillEffects();

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

            // razorwire
            RazorwireReworks();
            On.RoR2.Orbs.LightningOrb.Begin += NerfRazorwireOrb;

            // damage
            this.NerfBands();

            // crits
            if (GetConfigBool(true, "Critical Strike"))
            {
                this.NerfCritGlasses();
                OcularHudBuff();
            }

            // death mark fix :)
            if (GetConfigBool(true, "Death Mark Fix"))
            {
                DeathMarkFix();
            }

            // molten perforator
            if (GetConfigBool(true, "Molten Perforator"))
            {
                CreateMeatballNapalm();
                ProjectileImpactExplosion meatballPIE = meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>();
                this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().blastProcCoefficient = 0f; //0.7
                this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().childrenCount = 1; //0
                this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().childrenProjectilePrefab = meatballNapalmPool; //null
                this.meatballProjectilePrefab.GetComponent<ProjectileImpactExplosion>().fireChildren = true; //false
            }

            // charged perforator
            if (GetConfigBool(true, "Charged Perforator"))
            {
                On.RoR2.Orbs.SimpleLightningStrikeOrb.Begin += NerfChargedPerforatorOrb;
            }

            // shatterspleen, dmg
            if (GetConfigBool(true, "(DMG) Shatterspleen"))
            {
                this.spleenPrefab.GetComponent<RoR2.DelayBlast>().procCoefficient = 0f;
            }

            // fireworks
            if (GetConfigBool(true, "Fireworks"))
            {
                this.fireworkProjectilePrefab.GetComponent<ProjectileController>().procCoefficient = 0; //0.33f
            }

            // ceremonial dagger
            if (GetConfigBool(true, "Ceremonial Dagger"))
            {
                CeremonialDaggerNerfs();
            }

            // willowisp
            if (GetConfigBool(true, "Will-o-the-Wisp"))
            {
                WillowispNerfs();
            }

            // voidsent flame
            if (GetConfigBool(true, "Voidsent Flame"))
            {
                VoidsentNerfs();
            }

            // gasoline
            if (GetConfigBool(true, "Gasoline"))
            {
                GasolineChanges();
            }

            // meteorite
            if (GetConfigBool(true, "Glowing Meteorite"))
            {
                this.FixMeteorFalloff();
            }

            // warcry
            if (GetConfigBool(true, "Warcry Buff"))
            {
                this.EditWarCry();
            }


            On.RoR2.Orbs.DevilOrb.Begin += NerfDevilOrb;

            // nkuhanas opinion, DMG
            if (GetConfigBool(true, "(DMG) Nukuhanas Opinion"))
            {
                opinionDevilorbProc = 0;
            }

            // little disciple
            if (GetConfigBool(true, "Little Disciple"))
            {
                discipleDevilorbProc = 0;
            }

            // polylute
            if (GetConfigBool(true, "Polylute Nerf"))
            {
                this.ReworkPolylute();
            }

            // shuriken
            if (GetConfigBool(true, "Shuriken Rework"))
            {
                this.ReworkShuriken();
            }

            // lost seers lenses
            if (GetConfigBool(true, "Lost Seers Lenses Fix"))
            {
                LostSeersFix();
            }
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

            //enemies use equipment
            MakeEnemiesuseEquipment();
            //spawnlists
            ChangeSpawnlists();
            //double chest
            DoubleChestHook();

            //ambient level
            if (GetConfigBool(true, "Difficulty: Difficulty Dependent Ambient Difficulty Boost"))
            {
                AmbientLevelDifficulty();
                FixMoneyAndExpRewards(); //related to ambient difficulty boost
                VoidFieldsStageType(); //related to ambient difficulty boost
            }
            //void fields time cost
            if (GetConfigBool(true, "Difficulty: Void Fields Time Cost"))
            {
                VoidFieldsTimeCost();
            }

            //elite stats
            if (GetConfigBool(true, "Elite: Elite Stats and Ocurrences"))
            {
                ChangeEliteStats();
            }

            //teleporter particle
            if (GetConfigBool(true, "Difficulty: Teleporter Particle Radius"))
            {
                DifficultyDependentTeleParticles();
            }

            //monsoon stat boost
            if (GetConfigBool(true, "Difficulty: Monsoon Stat Booster"))
            {
                MonsoonStatBoost();
            }

            //eclipse changes
            if (GetConfigBool(true, "Difficulty: Eclipse Changes"))
            {
                EclipseChanges();
            }

            //eclipse level select
            if (GetConfigBool(true, "Difficulty: Eclipse Level Select"))
            {
                EclipseLevelSelect();
            }

            //overloading elite
            if (GetConfigBool(true, "Elite: Overloading Elite Rework"))
            {
                OverloadingEliteChanges();
            }

            //Mending elite
            if (GetConfigBool(true, "Elite: Mending Elite Rework"))
            {
                MendingEliteChanges();
            }

            //voidtouched elite
            if (GetConfigBool(true, "Elite: Voidtouched Elite Rework"))
            {
                VoidtouchedEliteChanges();
            }

            //blazing elite
            //BlazingEliteChanges();

            //newt shrine
            if (GetConfigBool(true, "Lunar: Newt Shrine"))
            {
                NerfBazaarStuff();
            }

            //gold gain and chest scaling
            if (GetConfigBool(true, "Economy: Gold Gain and Chest Scaling"))
            {
                FixMoneyScaling();
            }

            //elite gold
            if (GetConfigBool(true, "Economy: Elite Gold Rewards"))
            {
                EliteGoldReward();
            }

            //stage interactable credits
            if (GetConfigBool(true, "Economy: Stage Interactable Credits"))
            {
                DirectorAPI.StageSettingsActions += IncreaseStageInteractableCredits;
            }

            //stage monster credits
            if (GetConfigBool(true, "Economy: Stage Monster Credits"))
            {
                DirectorAPI.StageSettingsActions += IncreaseStageMonsterCredits;
            }

            //printer
            if (GetConfigBool(true, "Economy: Printer"))
            {
                DirectorAPI.InteractableActions += PrinterOccurrenceHook;
            }

            //scrapper
            if (GetConfigBool(true, "Economy: Scrapper"))
            {
                DirectorAPI.InteractableActions += ScrapperOccurrenceHook;
            }

            //equipment barrels and shops
            if (GetConfigBool(true, "Economy: Equipment Barrel/Shop"))
            {
                DirectorAPI.InteractableActions += EquipBarrelOccurrenceHook;
            }

            //equipment barrels and shops
            if (GetConfigBool(true, "Economy: Lunar Pod"))
            {
                DirectorAPI.InteractableActions += LunarPodOccurrenceHook;
            }

            //misc orange stuff i fucking guess
            if (GetConfigBool(true, "Economy: Gold Shrine"))
            {
                GoldShrineRework();
            }

            //blood shrine
            if (GetConfigBool(true, "Economy: Blood Shrine"))
            {
                BloodShrineRewardRework();
            }

            //void cradle, cradle curse
            if (GetConfigBool(true, "Economy: Void Cradle"))
            {
                VoidCradleRework();
            }

            //void cradle
            if (GetConfigBool(true, "Economy: Crowdfunder Funny Money"))
            {
                CrowdfunderFunny();
            }

            //void cradle
            if (GetConfigBool(true, "Economy: Gold/Legendary Chest Hacking Blacklist"))
            {
                ChangeHackingCriteria();
            }

            //wandering vagrant
            if (GetConfigBool(true, "Enemy: Wandering Vagrant"))
            {
                VagrantChanges();
            }

            //blind pest
            if (GetConfigBool(true, "Enemy: Blind Pest"))
            {
                PestChanges();
            }

            //beetle queen
            if (GetConfigBool(true, "Enemy: Beetle Queen"))
            {
                QueenChanges();
            }

            //void reaver
            if (GetConfigBool(true, "Enemy: Void Reaver"))
            {
                VoidReaverChanges();
            }

            //void barnacle
            if (GetConfigBool(true, "Enemy: Void Barnacle"))
            {
                BarnacleChanges();
            }

            //xi construct
            if (GetConfigBool(true, "Enemy: Xi Construct"))
            {
                XiAIFix();
            }

            //gup
            if (GetConfigBool(true, "Enemy: Gup"))
            {
                GupChanges();
            }

            LanguageAPI.Add("DIFFICULTY_EASY_DESCRIPTION", drizzleDesc + "</style>");
            // " + $"\n>Most Bosses have <style=cIsHealing>reduced skill sets</style>

            LanguageAPI.Add("DIFFICULTY_NORMAL_DESCRIPTION", rainstormDesc + "</style>");

            LanguageAPI.Add("DIFFICULTY_HARD_DESCRIPTION", monsoonDesc + "</style>");

            //this.DoSadistScavenger();
            #endregion

            ///summary
            ///DuckSurvivorTweaks
            #region survivor tweaks
            // CONTENT...
            // ITEMS: 
            // EQUIPMENT: 
            // ENEMIES: 

            RiskierRainPlugin.RemoveEquipment(nameof(RoR2Content.Equipment.Gateway));
            InitializeSurvivorTweaks();
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
        }

        public static bool FuckOsp(orig_getHasOneShotProtection orig, CharacterBody self)
        {
            return false;
        }
        public delegate bool orig_getHasOneShotProtection(CharacterBody self);

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
    }
}
