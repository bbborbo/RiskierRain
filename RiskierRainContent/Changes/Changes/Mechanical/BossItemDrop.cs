using BepInEx;
using RiskierRainContent.CoreModules;
using RiskierRainContent.Items;
using R2API;
using RoR2;
using RoR2.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static GameObject overgrownPrinterPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/chest/DuplicatorWild");
        public static bool affectAurelionite = true;

        float baseDropChance = 4;
        float eliteBonusDropChance = 3;
        float specialBonusDropChance = 7;

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
                Debug.LogError(BodyCatalog.GetBodyName(enemyBodyIndex));

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

                PickupIndex drop = deathRewards.bossDropTable.GenerateDrop(attackerBody.equipmentSlot.rng);

                if (Util.CheckRoll(dropChance, killerMaster) && drop != PickupCatalog.FindPickupIndex("VoidCoin"))
                {
                    if (isScalpel && !isTricorn)
                    {
                        DisposableScalpel.ConsumeScalpel(attackerBody);
                    }

                    int quantumCodexCount = VoidGhorsTome.instance.GetCount(attackerBody);
                    if(quantumCodexCount > 0)
                    {
                        if (Util.CheckRoll(VoidGhorsTome.GetCurrentVoidChance(quantumCodexCount) * 100))
                        {
                            if (VoidGhorsTome.voidBossDropTable.selector.Count > 0)
                                drop = VoidGhorsTome.voidBossDropTable.GenerateDrop(attackerBody.equipmentSlot.rng);
                        }
                    }

                    Vector3 vector = enemyBody ? enemyBody.corePosition : Vector3.zero;
                    Vector3 normalized = (vector - attackerBody.corePosition).normalized;

                    PickupDropletController.CreatePickupDroplet(
                        drop, vector, normalized * 15f);
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
