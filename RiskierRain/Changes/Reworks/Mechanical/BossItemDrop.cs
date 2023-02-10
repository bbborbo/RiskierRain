using BepInEx;
using RiskierRain.CoreModules;
using RiskierRain.Items;
using R2API;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static GameObject overgrownPrinterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorWild");
        public static bool affectAurelionite = true;

        float baseDropChance = 4;
        float eliteBonusDropChance = 3;
        float specialBonusDropChance = 7;

        public static ExplicitPickupDropTable hordeDropTable;
        void HordeOfManyDropsWakeOfVultures()
        {
            Debug.Log("??????");
            ItemDef wakeItemDef = RoR2Content.Items.HeadHunter;//Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/HeadHunter/HeadHunter.asset").WaitForCompletion();
            //wakeItemDef.tier = ItemTier.Boss;
            //wakeItemDef.deprecatedTier = ItemTier.Boss;

            ExplicitPickupDropTable.PickupDefEntry pickupEntry = new ExplicitPickupDropTable.PickupDefEntry();
            pickupEntry.pickupDef = wakeItemDef;

            hordeDropTable = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            hordeDropTable.canDropBeReplaced = true;
            hordeDropTable.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[] { pickupEntry };

            //On.RoR2.DeathRewards.Awake += SetDropTableForHordesOfMany;
            //On.RoR2.TeleporterInteraction.Awake += SetBossDirectorDropTable;
            On.RoR2.BossGroup.OnMemberDiscovered += SetHordeDropTable;
        }

        private void SetHordeDropTable(On.RoR2.BossGroup.orig_OnMemberDiscovered orig, BossGroup self, CharacterMaster memberMaster)
        {
            orig(self, memberMaster);
            CharacterBody body = memberMaster.GetBody();
            if (body && body.isChampion == false)
            {
                DeathRewards deathRewards = body.GetComponent<DeathRewards>();
                deathRewards.bossDropTable = hordeDropTable;
            }
        }

        private void SetDropTableForHordesOfMany(On.RoR2.DeathRewards.orig_Awake orig, DeathRewards self)
        {
            orig(self);
            CharacterBody body = self.characterBody;
            if (body && /*!body.isChampion &&*/ body.isBoss)
            {
                Debug.Log(body.name);
                if(self.bossDropTable == null)
                    self.bossDropTable = hordeDropTable;
            }
        }

        void BossesDropBossItems()
        {
            affectAurelionite = CustomConfigFile.Bind<bool>("Boss Item Drop",
                "Enable boss item drop changes for Aurelionite", true,
                "The boss item drop changes make Aurel drop his item directly and have greens drop from the portal instead. " +
                "Turn this off if you dont want that.").Value;

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

        public void BossesDropTrophies(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);

            if (damageReport.victimTeamIndex == TeamIndex.Player)
                return;

            CharacterBody attackerBody = damageReport.attackerBody;
            CharacterBody enemyBody = damageReport.victimBody;
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

            DeathRewards deathRewards = GetDeathRewardsFromTarget(enemyBody);

            if (deathRewards != null && deathRewards.bossDropTable)
            {
                float dropChance = 0;

                if (enemyBodyIndex == BodyCatalog.FindBodyIndex("TitanGoldBody"))
                {
                    dropChance = 100;
                }
                else if (enemyBodyIndex == BodyCatalog.FindBodyIndex("SuperRoboBallBossBody"))
                {
                    dropChance = specialBonusDropChance;
                }
                else
                {
                    dropChance = baseDropChance;
                }

                if (enemyBody.isElite || enemyBodyIndex == BodyCatalog.FindBodyIndex("ElectricWormBody"))
                {
                    dropChance += eliteBonusDropChance;
                }

                bool isTricorn = false;
                bool isScalpel = false;
                if(enemyBody.HasBuff(Assets.bossHunterDebuff) || enemyBody.HasBuff(Assets.bossHunterDebuffWithScalpel))
                {
                    isTricorn = true;
                    dropChance += 100;
                }
                else if (DisposableScalpel.instance.GetCount(attackerBody) > 0 && dropChance < 100)
                {
                    isScalpel = true;
                    dropChance += DisposableScalpel.bonusDropChance;
                }

                if (Util.CheckRoll(dropChance, killerMaster))
                {
                    if (isScalpel && !isTricorn)
                    {
                        DisposableScalpel.ConsumeScalpel(attackerBody);
                    }

                    Vector3 vector = enemyBody ? enemyBody.corePosition : Vector3.zero;
                    Vector3 normalized = (vector - attackerBody.corePosition).normalized;

                    PickupDropletController.CreatePickupDroplet(
                        deathRewards.bossDropTable.GenerateDrop(attackerBody.equipmentSlot.rng), 
                        vector, normalized * 15f);
                }
            }
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
