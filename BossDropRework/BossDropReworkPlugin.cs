using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace BossDropRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DirectorAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DirectorAPI))]
    public partial class BossDropReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityBossDrops";
        public const string version = "1.2.2";
        #endregion

        #region config
        internal static ConfigFile CustomConfigFile { get; private set; }
        public static ConfigEntry<float> LesserDropChance { get; set; }
        public static ConfigEntry<float> EliteDropChance { get; set; }
        public static ConfigEntry<float> ChampionDropChance { get; set; }
        public static ConfigEntry<float> ChampionEliteDropChance { get; set; }
        public static ConfigEntry<float> SpecialBossDropChance { get; set; }
        public static ConfigEntry<bool> ForceDropsFromAurelionite { get; set; }
        public static ConfigEntry<bool> ReworkTricorn { get; set; }
        public static ConfigEntry<bool> ReworkVultures { get; set; }
        public static ConfigEntry<bool> ReworkPrinters { get; set; }
        public static ConfigEntry<float> TricornDamageCoefficient { get; set; }
        public static ConfigEntry<float> TricornProcCoefficient { get; set; }
        #endregion

        public void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\FruityBossDrops.cfg", true);

            LesserDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Non-Elite Lessers",
                0.5f,
                "Includes Horde of Many");
            EliteDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Elite Lessers",
                2,
                "Includes Horde of Many");
            ChampionDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Non-Elite Champions",
                6,
                "");
            ChampionEliteDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Elite Champions",
                10,
                "");
            SpecialBossDropChance = CustomConfigFile.Bind<float>(
                "Trophy Drops",
                "Droprate for Special Bosses",
                14,
                "Affects AWU");

            ForceDropsFromAurelionite = CustomConfigFile.Bind<bool>(
                "Trophy Drops",
                "Force Drops From Aurelionite",
                true,
                "Force Aurelionite to drop a Halcyon Seed on death, replacing the portal loot with 1 green for each player. Recommended to set to false for use with GildedCoastPlus"
                );

            ReworkTricorn = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Tricorn",
                true,
                ""
                );
            ReworkVultures = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Wake of Vultures",
                true,
                "Turns it into a Horde of Many trophy"
                );
            ReworkPrinters = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Overgrown Printers",
                true,
                "Removes Overgrown Printers from the game"
                );

            TricornDamageCoefficient = CustomConfigFile.Bind<float>(
                "Tricorn",
                "Tricorn Damage Coefficient",
                70,
                "Multiply by 100 for % ie 70 is 7000%");
            TricornProcCoefficient = CustomConfigFile.Bind<float>(
                "Tricorn",
                "Tricorn Proc Coefficient",
                5,
                "Most attacks default to 1. Recommended to install ProcPatcher");

            NoBossDropsBuff = ScriptableObject.CreateInstance<BuffDef>();

            NoBossDropsBuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            NoBossDropsBuff.canStack = false;
            NoBossDropsBuff.isDebuff = false;
            NoBossDropsBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            NoBossDropsBuff.name = "TrophyHunterDebuff";
            NoBossDropsBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();
            NoBossDropsBuff.isHidden = true;

            R2API.ContentAddition.AddBuffDef(NoBossDropsBuff);
            YesBossDropsBuff = ScriptableObject.CreateInstance<BuffDef>();

            YesBossDropsBuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            YesBossDropsBuff.canStack = false;
            YesBossDropsBuff.isDebuff = false;
            YesBossDropsBuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            YesBossDropsBuff.name = "TrophyHunterDebuff";
            YesBossDropsBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();
            YesBossDropsBuff.isHidden = true;

            R2API.ContentAddition.AddBuffDef(YesBossDropsBuff);

            BossesDropBossItems();

            if (ReworkTricorn.Value)
                TricornRework();
            if (ReworkVultures.Value)
                WakeOfVulturesRework();
            if(ReworkPrinters.Value)
                DirectorAPI.InteractableActions += DeleteYellowPrinters;
        }

        public static GameObject overgrownPrinterPrefab => LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorWild");

        void BossesDropBossItems()
        {
            /*affectAurelionite = CustomConfigFile.Bind<bool>("Boss Item Drop",
                "Enable boss item drop changes for Aurelionite", true,
                "The boss item drop changes make Aurel drop his item directly and have greens drop from the portal instead. " +
                "Turn this off if you dont want that.").Value;*/

            On.RoR2.BossGroup.Awake += RemoveBossItemDropsFromTeleporter;
            On.RoR2.GlobalEventManager.OnCharacterDeath += BossesDropTrophies;
        }

        private void DeleteYellowPrinters(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            DirectorAPI.Helpers.RemoveExistingInteractable(DirectorAPI.Helpers.InteractableNames.PrinterOvergrown3D);
        }

        private void RemoveBossItemDropsFromTeleporter(On.RoR2.BossGroup.orig_Awake orig, BossGroup self)
        {
            orig(self);
            self.bossDropChance = 0;
        }
        public delegate void BossDropChanceHandler(CharacterBody victim, CharacterBody attacker, ref float dropChance);
        public static event BossDropChanceHandler ModifyBossItemDropChance;
        public static float InvokeModifyBossItemDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
            ModifyBossItemDropChance?.Invoke(victim, attacker, ref dropChance);
            return dropChance;
        }

        public static bool GetBossDropFilter(CharacterBody victimBody)
        {
            if (victimBody == null)
                return false;

            if (victimBody.HasBuff(NoBossDropsBuff))
                return false;

            BodyIndex enemyBodyIndex = victimBody.bodyIndex;

            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody") && !ForceDropsFromAurelionite.Value)
                return false;

            return true;
        }

        public void BossesDropTrophies(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);

            if (damageReport.victimTeamIndex == TeamIndex.Player)
                return;

            CharacterBody attackerBody = damageReport.attackerBody;
            CharacterBody enemyBody = damageReport.victimBody;

            if (attackerBody == null)
                return;
            if (GetBossDropFilter(enemyBody) == false)
                return;

            if (enemyBody.healthComponent.alive)
                return;
            //bool bossOrChampion = enemyBody.isBoss || enemyBody.isChampion;
            //if (!bossOrChampion)
            //    return;

            CharacterMaster killerMaster = damageReport.attackerMaster;


            ItemDef itemToDrop = null;

            int players = Run.instance.participatingPlayerCount;
            DropItem(attackerBody, enemyBody, killerMaster);
        }

        internal static void DropItem(CharacterBody attackerBody, CharacterBody enemyBody, CharacterMaster killerMaster, float dropChanceOverride = -1)
        {
            PickupDropTable dropTable;
            float dropChance = GetBaseBossItemDropChanceFromBody(enemyBody, out dropTable);
            if (dropChanceOverride != 0)
                dropChance = dropChanceOverride;
            if (dropChance > 0 && dropTable != null)
            {
                if (InvokeModifyBossItemDropChance(enemyBody, attackerBody, ref dropChance) > 0)
                {

                    if (Util.CheckRoll(dropChance, killerMaster)) //&& drop != PickupCatalog.FindPickupIndex("VoidCoin"))
                    {
                        UniquePickup drop = dropTable.GeneratePickup(Run.instance.bossRewardRng);
                        Vector3 vector = enemyBody ? enemyBody.corePosition : Vector3.zero;
                        Vector3 normalized = (vector - attackerBody.corePosition).normalized;

                        PickupDropletController.CreatePickupDroplet(
                            drop, vector, normalized * 15f, false);
                    }
                }
            }
        }

        public static float GetBaseBossItemDropChanceFromBody(CharacterBody body, out PickupDropTable dropTable)
        {
            //if no drop table, no drops
            DeathRewards deathRewards = GetDeathRewardsFromTarget(body);
            if (deathRewards == null || deathRewards.bossDropTable == null)
            {
                dropTable = null;
                return 0;
            }
            dropTable = deathRewards.bossDropTable;

            //if aurelionite, use drops determined by config
            BodyIndex enemyBodyIndex = body.bodyIndex;
            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
            {
                return ForceDropsFromAurelionite.Value ? 100 : 0;
            }

            //if enemy has no rewards, no drops
            if (deathRewards.goldReward <= 0)
                return 0;

            //if enemy is a rare boss
            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("SuperRoboBallBossBody") || enemyBodyIndex == BodyCatalog.FindBodyIndex("VultureHunterBody"))
            {
                return SpecialBossDropChance.Value;
            }

            bool boss = body.isChampion;// || body.isBoss;
            bool elite = body.isElite || enemyBodyIndex == BodyCatalog.FindBodyIndex("ElectricWormBody");

            if (boss)
            {
                if (elite)
                    return ChampionEliteDropChance.Value;
                return ChampionDropChance.Value;
            }

            if (elite)
                return EliteDropChance.Value;
            return LesserDropChance.Value;
        }


        public static DeathRewards GetDeathRewardsFromTarget(HurtBox hurtBox)
        {
            if (hurtBox == null)
                return null;

            HealthComponent healthComponent = hurtBox.healthComponent;
            if (healthComponent == null)
                return null;

            return GetDeathRewardsFromTarget(healthComponent.body);
        }
        public static DeathRewards GetDeathRewardsFromTarget(CharacterBody enemyBody)
        {
            if (enemyBody == null)
                return null;
            return enemyBody.GetComponent<DeathRewards>();
        }
    }
}
