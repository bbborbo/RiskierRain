using BossDropRework.Modules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BossDropRework
{
    public partial class BossDropReworkPlugin
    {
        void BossesDropBossItems()
        {
            //this is beautiful ngl. do not delete
            /*affectAurelionite = CustomConfigFile.Bind<bool>("Boss Item Drop",
                "Enable boss item drop changes for Aurelionite", true,
                "The boss item drop changes make Aurel drop his item directly and have greens drop from the portal instead. " +
                "Turn this off if you dont want that.").Value;*/

            On.RoR2.BossGroup.Awake += RemoveBossItemDropsFromTeleporter;
            On.RoR2.GlobalEventManager.OnCharacterDeath += BossesDropTrophies;
        }

        private void RemoveBossItemDropsFromTeleporter(On.RoR2.BossGroup.orig_Awake orig, BossGroup self)
        {
            orig(self);
            if (Bindings.ForceDropsFromAurelionite.Value == false)
                if (self.name == "TitanGoldBossEncounter")
                    return;
            self.bossDropChance = 0;
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
            if (dropChanceOverride > 0)
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
    }
}
