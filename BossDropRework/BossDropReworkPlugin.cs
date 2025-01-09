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
        public const string version = "1.1.0";
        #endregion

        public void Awake()
        {
            bossHunterDebuff = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuff.canStack = false;
            bossHunterDebuff.isDebuff = true;
            bossHunterDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            bossHunterDebuff.name = "TrophyHunterDebuff";
            bossHunterDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();

            R2API.ContentAddition.AddBuffDef(bossHunterDebuff);

            WakeOfVulturesRework();
            BossesDropBossItems();
            TricornRework();
            DirectorAPI.InteractableActions += DeleteYellowPrinters;
        }

        void ReworkBossItemDrops()
        {
        }
        public static GameObject overgrownPrinterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorWild");
        public static bool affectAurelionite = true;

        public static float baseDropChance = 6;
        public static float specialDropChance = 9;
        public static float eliteBonusDropChance = 4;

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

        public void BossesDropTrophies(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);

            if (damageReport.victimTeamIndex == TeamIndex.Player)
                return;

            CharacterBody attackerBody = damageReport.attackerBody;
            CharacterBody enemyBody = damageReport.victimBody;

            if (attackerBody == null || enemyBody == null)
                return;

            BodyIndex enemyBodyIndex = enemyBody.bodyIndex;

            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody") &&
                !affectAurelionite)
            {
                return;
            }

            if (enemyBody.healthComponent.alive)
            {
                return;
            }

            CharacterMaster killerMaster = damageReport.attackerMaster;


            ItemDef itemToDrop = null;

            int players = Run.instance.participatingPlayerCount;

            PickupDropTable dropTable;
            float dropChance = GetBaseBossItemDropChanceFromBody(enemyBody, out dropTable);
            if(dropChance > 0)
            {
                if(InvokeModifyBossItemDropChance(enemyBody, attackerBody, ref dropChance) > 0)
                {
                    PickupIndex drop = dropTable.GenerateDrop(attackerBody.equipmentSlot.rng);

                    if (Util.CheckRoll(dropChance, killerMaster) && drop != PickupCatalog.FindPickupIndex("VoidCoin"))
                    {
                        Vector3 vector = enemyBody ? enemyBody.corePosition : Vector3.zero;
                        Vector3 normalized = (vector - attackerBody.corePosition).normalized;

                        PickupDropletController.CreatePickupDroplet(
                            drop, vector, normalized * 15f);
                    }
                }
            }
        }

        public static float GetBaseBossItemDropChanceFromBody(CharacterBody body, out PickupDropTable dropTable)
        {
            DeathRewards deathRewards = GetDeathRewardsFromTarget(body);
            if (deathRewards == null || deathRewards.bossDropTable == null)
            {
                dropTable = null;
                return 0;
            }

            dropTable = deathRewards.bossDropTable;
            float dropChance = 0;

            BodyIndex enemyBodyIndex = body.bodyIndex;
            if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
            {
                return affectAurelionite ? 100 : 0;
            }
            else if (enemyBodyIndex == BodyCatalog.FindBodyIndex("SuperRoboBallBossBody"))
            {
                dropChance = specialDropChance;
            }
            else
            {
                dropChance = baseDropChance;
            }

            if (body.isElite || enemyBodyIndex == BodyCatalog.FindBodyIndex("ElectricWormBody"))
            {
                dropChance += eliteBonusDropChance;
            }

            return dropChance;
        }


        public static DeathRewards GetDeathRewardsFromTarget(HurtBox hurtBox)
        {
            DeathRewards deathRewards = null;
            if (hurtBox != null)
            {
                HealthComponent healthComponent = hurtBox.healthComponent;
                if (healthComponent != null)
                {
                    deathRewards = GetDeathRewardsFromTarget(healthComponent.body);
                }
            }
            return deathRewards;
        }
        public static DeathRewards GetDeathRewardsFromTarget(CharacterBody enemyBody)
        {
            DeathRewards deathRewards = null;

            if (enemyBody != null)
            {
                GameObject gameObject = enemyBody.gameObject;
                deathRewards = ((gameObject != null) ? gameObject.GetComponent<DeathRewards>() : null);
            }

            return deathRewards;
        }
    }
}
