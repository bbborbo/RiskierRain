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
        public const string version = "1.3.9";

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
            #region rework pending / priority removal
            //RiskierRainPlugin.RetierItemAsync(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_BarrierOnCooldown.BarrierOnCooldown_asset);//eclipse lite
            RiskierRainPlugin.RetierItemAsync(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_CritAtLowerElevation.CritAtLowerElevation_asset);//hikers boots

            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.PrimarySkillShuriken)); //shuriken
            //RiskierRainPlugin.RetierItem(nameof(DLC1Content.Items.MoveSpeedOnKill)); //hunter's harpoon
           //RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.Squid)); //squid polyp HAS BEEN REWORKED AND IS AWESOME NOW
            RiskierRainPlugin.RetierItemAsync(RoR2_Base_BonusGoldPackOnKill.BonusGoldPackOnKill_asset); //ghors
            RiskierRainPlugin.RetierItemAsync(RoR2_Base_DeathMark.DeathMark_asset); //guess faggot
            RiskierRainPlugin.RetierItemAsync(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset);//kinetic dampener

            RiskierRainPlugin.RetierItemAsync(RoR2_Base_Talisman.Talisman_asset); //soulbound
            //RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_MoreMissile.MoreMissile_asset); //icbm
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_PermanentDebuffOnHit.PermanentDebuffOnHit_asset); //scorpion
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_DroneWeapons.DroneWeapons_asset); //sdp
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BarrageOnBoss.BarrageOnBoss_asset); //war bonds
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC2_Items_BoostAllStats.BoostAllStats_asset); //growth nectar


            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfSpeedDoubleHealth.HalfSpeedDoubleHealth_asset); //
            RiskierRainPlugin.RetierItemAsync(RoR2_DLC1_HalfAttackSpeedHalfCooldowns.HalfAttackSpeedHalfCooldowns_asset); //

            //RiskierRainPlugin.RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/AutoCastEquipment/AutoCastEquipment.asset").WaitForCompletion());
            #endregion

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
            IL.RoR2.Orbs.DevilOrb.OnArrival += BuffDevilOrb;

            // removes one-shot protection (OSP)
            Hook hookTuah = new Hook(
              typeof(CharacterBody).GetMethod("get_hasOneShotProtection", (BindingFlags)(-1)),
              typeof(RiskierRainPlugin).GetMethod(nameof(ReflectOnThatThang), (BindingFlags)(-1))
            );

            IL.RoR2.GenericSkill.RunRecharge += FuckAspdScalingOnCooldowns;
            IL.RoR2.Skills.SkillDef.GetRechargeInterval += FuckAspdScalingOnCooldowns;

            void FuckAspdScalingOnCooldowns(ILContext il)
            {
                ILCursor c = new ILCursor(il);

                bool ilFound = c.TryGotoNext(MoveType.After,
                    x => x.MatchLdfld<RoR2.Skills.SkillDef>(nameof(RoR2.Skills.SkillDef.attackSpeedBuffsRestockSpeed))
                    );
                if (!ilFound)
                {
                    DebugBreakpoint(nameof(FuckAspdScalingOnCooldowns));
                    return;
                }
                c.Emit(Mono.Cecil.Cil.OpCodes.Pop);
                c.Emit(Mono.Cecil.Cil.OpCodes.Ldc_I4_0);
            }

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
            BuffBungus();

            // mobility
            GoatHoofNerf();
            EnergyDrinkNerf();
            FaradayNerf();

            // defense
            TeddyChanges();

            // barrier
            if (GetConfigBool(true, "Barrier Decay Rate"))
            {
                BaseStats.BarrierDecayStaticMaxHealthTime = 30f;// BarrierDecayRateStatic.Value; //30f
                BaseStats.BarrierHighDecayFactor = 5f;// BarrierDecayHighFactor.Value; //3f
                BaseStats.BarrierLowDecayFactor = 0.33f;// BarrierDecayLowFactor.Value; //0.5f
            }
            // scythe
            if (GetConfigBool(true, "Harvesters Scythe"))
            {
                ScytheNerf();
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
            if (false)//GetConfigBool(true, "Infusion"))
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
            RiskierRainPlugin.RemoveEquipmentAsync(RoR2_Base_Gateway.Gateway_asset);

            // shattering justics
            if (GetConfigBool(true, "Shattering Justice"))
            {
                this.BuffJustice();
            }

            // jellynuke
            if (GetConfigBool(true, "Jellynuke"))
            {
                this.FixJellyNuke();
            }

            // shatterspleen, INT
            if (GetConfigBool(true, "Shatterspleen"))
            {
                //this.ReworkShatterspleen();
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

            //fuel cell
            if (GetConfigBool(true, "Fuel Cell"))
            {
                ReworkFuelCell();
            }

            //bottled chaos
            if (GetConfigBool(true, "Bottled Chaos"))
            {
                BuffBottledChaos();
            }
            if (GetConfigBool(true, "Sale Star"))
            {
                SaleStarChanges();
            }
            if (GetConfigBool(true, "Chance Doll"))
            {
                ChanceDollChanges();
            }
            if (GetConfigBool(true, "Warped Echo"))
            {
                WarpedEchoChanges();
            }
            if (GetConfigBool(true, "Elusive Antlers"))
            {
                ElusiveAntlersChanges();
            }
            if (GetConfigBool(true, "Luminous Shot"))
            {
                LuminousShotBuff();
            }
            if (GetConfigBool(true, "Eclipse Lite"))
            {
                EclipseLiteChanges();
            }
            if (GetConfigBool(true, "Topaz Brooch"))
            {
                TopazBroochBuff();
            }
            if(GetConfigBool(true, "Artifact of Sacrifice/Sonorous Whispers"))
            {
                DoSacrificeDropLimit();
            }
            if (GetConfigBool(true, "Box of Dynamite"))
            {
                BoxOfDynamiteBuff();
            }
            if (GetConfigBool(true, "Warbanner"))
            {
                WarbannerBuff();
            }

            if (GetConfigBool(true, "Command/Potential Armor"))
            {
                On.RoR2.UI.PickupPickerPanel.Awake += CommandOrPotentialArmor;
                void CommandOrPotentialArmor(On.RoR2.UI.PickupPickerPanel.orig_Awake orig, RoR2.UI.PickupPickerPanel self)
                {
                    RoR2.LocalUser user = RoR2.LocalUserManager.GetFirstLocalUser();
                    RoR2.CharacterBody body = user.cachedBody;
                    body.AddTimedBuffAuthority(RoR2.RoR2Content.Buffs.HiddenInvincibility.buffIndex, 4);
                    orig(self);
                };
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

            // sticky bomb
            if (GetConfigBool(true, "Sticky Bomb Rework"))
            {
                //ReworkStickyBomb();
            }

            //soul shrine
            if (GetConfigBool(true, "Shrine of Shaping"))
            {
                ReworkSoulShrine();
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
            if (GetConfigBool(true, "Difficulty: Difficulty Scaling Changes"))
            {
                DifficultyUtilsModule.EnableAll();
                ChangeDifficultyCoefficientCalculation();
                FreezeTimeScalingOnFinalLevels();
                //VoidFieldsStageType(); //related to ambient difficulty boost
            }
            //void fields time cost
            if (GetConfigBool(true, "Difficulty: Void Fields Time Cost"))
            {
                //VoidFieldsTimeCost();
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

            //eclipse changes
            if (GetConfigBool(true, "Difficulty: Director Changes"))
            {
                ChangeDirectorStats();
            }

            //pity charge
            if (GetConfigBool(true, "Difficulty: pity charge"))
            {
                AddPityCharge();
            }

            //newt shrine
            if (GetConfigBool(true, "Lunar: Newt Shrine"))
            {
                //NerfBazaarStuff();
            }

            //gold gain and chest scaling
            if (GetConfigBool(true, "Economy: Gold Gain and Chest Scaling"))
            {
                FixMoneyScaling();
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
            if (GetConfigBool(true, "Economy: Printers and Scrappers"))
            {
                //DirectorAPI.InteractableActions += PrinterOccurrenceHook;
                //DirectorAPI.InteractableActions += ScrapperOccurrenceHook;
                DirectorAPI.InteractableActions += PrinterScrapperOccurrenceHook;
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

            //templar
            if (GetConfigBool(true, "Enemy: Templar"))
            {
                NerfTemplar();
            }

            //chimera wisp
            if (GetConfigBool(true, "Enemy: Chimera Wisp"))
            {
                NerfChimeraWisp();
            }

            //gup
            if (GetConfigBool(true, "Enemy: Gup"))
            {
                GupChanges();
            }

            //solus scorcher
            if (GetConfigBool(true, "Enemy: Solus Scorcher"))
            {
                ChangeSolusScorcher();
            }

            //solus prospector
            if (GetConfigBool(true, "Enemy: Solus Prospector"))
            {
                ChangeSolusProspector();
            }

            //halcyonite shrine
            if (GetConfigBool(true, "Economy: Halcyonite Shrine"))
            {
                ChangeHalcyoniteShrineGoldRequirements();
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

        public static bool ReflectOnThatThang(orig_getHasOneShotProtection orig, CharacterBody self)
        {
            return false;
        }
        public delegate bool orig_getHasOneShotProtection(CharacterBody self);

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
