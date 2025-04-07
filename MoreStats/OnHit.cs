using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.StatHooks;

namespace MoreStats
{
    public static class OnHit
    {
        static bool initialized = false;
        internal static void Init()
        {
            if (initialized)
                return;
            initialized = true;

            On.RoR2.GlobalEventManager.ProcessHitEnemy += GlobalEventManager_OnHitEnemy;
        }

        public delegate void HitHookEventHandler(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody);
        public static event HitHookEventHandler GetHitBehavior;
        private static void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody attackerBody = null;
                CharacterBody victimBody = null;
                if (damageInfo.attacker.TryGetComponent(out attackerBody) && victim.TryGetComponent(out victimBody))
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    if (attackerMaster != null)
                    {
                        //ignite
                        DoIgniteOnHit(damageInfo, victim, attackerBody, victimBody);

                        //other on-hit events
                        GetHitBehavior?.Invoke(attackerBody, damageInfo, victimBody);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        private static void DoIgniteOnHit(DamageInfo damageInfo, GameObject victim, CharacterBody attackerBody, CharacterBody victimBody)
        {
            Inventory inv = attackerBody.inventory;

            uint? maxStacksFromAttacker = null;
            if ((damageInfo != null) ? damageInfo.inflictor : null)
            {
                ProjectileDamage component = damageInfo.inflictor.GetComponent<ProjectileDamage>();
                if (component && component.useDotMaxStacksFromAttacker)
                {
                    maxStacksFromAttacker = new uint?(component.dotMaxStacksFromAttacker);
                }
            }

            float burnProcChance = GetMoreStatsFromBody(attackerBody).burnChance;
            if (burnProcChance > 0)
            {
                //Debug.Log("Burn proc chance: " + burnProcChance);
                if (Util.CheckRoll(burnProcChance, attackerBody.master))
                {
                    InflictDotInfo inflictDotInfo = new InflictDotInfo
                    {
                        attackerObject = damageInfo.attacker,
                        victimObject = victim,
                        totalDamage = new float?(damageInfo.damage * 0.5f),
                        damageMultiplier = 1f,
                        dotIndex = DotController.DotIndex.Burn,
                        maxStacksFromAttacker = maxStacksFromAttacker
                    };
                    StrengthenBurnUtils.CheckDotForUpgrade(inv, ref inflictDotInfo);
                    DotController.InflictDot(ref inflictDotInfo);
                }
            }
        }
    }
}
