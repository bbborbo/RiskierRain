using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using System.Linq;
using static R2API.DamageAPI;
using BossDropRework.Modules;

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

        private static PickupDef voidMarkerPickup;
        public void Awake()
        {
            Bindings.Init();
            CommonAssets.Init();

            BossesDropBossItems();

            if (Bindings.BindSection("Trophy Hunters Tricorn"))
                TricornRework();
            if (Bindings.BindSection("Wake of Vultures (HOM Boss Item)"))
                WakeOfVulturesRework();
            if (Bindings.BindSection("Horde of Many (Forced Elite)"))
                HordeOfManyRework();
            if (Bindings.BindSection("Overgrown Printers"))
                DirectorAPI.InteractableActions += DeleteYellowPrinters;

            Bindings.Save();
        }

        private void DeleteYellowPrinters(DccsPool pool, DirectorAPI.StageInfo currentStage)
        {
            DirectorAPI.Helpers.RemoveExistingInteractable(DirectorAPI.Helpers.InteractableNames.PrinterOvergrown3D);
        }



        public delegate void BossDropChanceHandler(CharacterBody victim, CharacterBody attacker, ref float dropChance);
        public static event BossDropChanceHandler ModifyBossItemDropChance;
        public static float InvokeModifyBossItemDropChance(CharacterBody victim, CharacterBody attacker, ref float dropChance)
        {
            ModifyBossItemDropChance?.Invoke(victim, attacker, ref dropChance);
            return dropChance;
        }
        public static float GetBaseBossItemDropChanceFromBody(CharacterBody body, out PickupDropTable dropTable)
        {
            //if no drop table, no drops
            DeathRewards deathRewards = GetDeathRewardsFromTarget(body);
            if (deathRewards == null || DropTableValid(deathRewards.bossDropTable) == false)
            {
                dropTable = null;
                return 0;
            }
            dropTable = deathRewards.bossDropTable;

            //if aurelionite, use drops determined by config
            BodyIndex enemyBodyIndex = body.bodyIndex;
            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
            {
                return Bindings.ForceDropsFromAurelionite.Value == true ? 100 : 0;
            }

            //if enemy has no rewards, no drops
            if (deathRewards.goldReward <= 0)
            {
                Debug.Log("FruityBossDrop: Enemy will not drop rewards due to dropping no gold. Is this an error?");
                return 0;
            }

            //if enemy is a rare boss
            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("SuperRoboBallBossBody") || enemyBodyIndex == BodyCatalog.FindBodyIndex("VultureHunterBody"))
            {
                return Bindings.SpecialBossDropChance.Value;
            }

            bool boss = body.isChampion;// || body.isBoss;
            bool elite = body.isElite || enemyBodyIndex == BodyCatalog.FindBodyIndex("ElectricWormBody");

            if (boss)
            {
                if (elite)
                    return Bindings.ChampionEliteDropChance.Value;
                return Bindings.ChampionDropChance.Value;
            }

            if (elite)
                return Bindings.EliteDropChance.Value;
            return Bindings.LesserDropChance.Value;
        }

        #region other api stuff
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
        public static bool GetBossDropFilter(CharacterBody victimBody)
        {
            if (victimBody == null)
                return false;

            if (victimBody.HasBuff(CommonAssets.NoBossDropsBuff))
                return false;

            BodyIndex enemyBodyIndex = victimBody.bodyIndex;

            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody") && Bindings.ForceDropsFromAurelionite.Value == false)
                return false;

            return true;
        }

        public static bool DropTableValid(PickupDropTable dropTable)
        {
            if (dropTable == null || dropTable.GetPickupCount() <= 0)
                return false;
            if ((dropTable is ExplicitPickupDropTable explicitDropTable))
            {
                if (explicitDropTable.weightedSelection.Count == 0)
                    explicitDropTable.GenerateWeightedSelection();

                return explicitDropTable.weightedSelection
                        .choices.Any(x =>
                            x.value.pickupIndex != PickupCatalog.FindPickupIndex("MiscPickupIndex.VoidCoin")
                            && x.value.pickupIndex != PickupIndex.none
                        );
            }

            return true;
        }
        #endregion
    }
}
