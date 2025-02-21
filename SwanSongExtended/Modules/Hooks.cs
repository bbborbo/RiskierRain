using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Modules
{
    public static class Hooks
    {
        public static void Init()
        {
            HitHooks.Init();
            ShieldDelayHooks.Init();
        }
    }
    public static class SquidHooks
    {
        public static void Init()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += SquidOnDeath;
        }

        private static void SquidOnDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            int squidCount = CountSquids(damageReport.victimBody);
            if (squidCount <= 0) { return; }
            SquidDeathBlast(damageReport.victimBody, squidCount);
        }

        private static void SquidDeathBlast(CharacterBody body, int squidCount)
        {
            //temp location. put somewhere good later! when you do that you can also 
            float radiusBase = 28f;
            //float durationBase = 12f;
            //EffectManager.SpawnEffect(scugNovaEffectPrefab, new EffectData
            //{
            //    origin = body.transform.position,
            //    scale = radiusBase
            //}, true);
            //ChillRework.ChillRework.ApplyChillSphere(body.corePosition, radiusBase, body.teamComponent.teamIndex, durationBase);
            BlastAttack squidNova = new BlastAttack()
            {
                baseDamage = (4f + 1f * squidCount --)* body.damage, //400% to trigger those effects? i think???
                radius = radiusBase,
                procCoefficient = 1f,
                position = body.transform.position,
                attacker = body.gameObject,
                baseForce = 900,
                crit = Util.CheckRoll(body.crit, body.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.ClayGoo,
                teamIndex = TeamComponent.GetObjectTeam(body.gameObject)
            };
            squidNova.Fire();
        }

        private static int CountSquids(CharacterBody body)
        {
            if (!body) { return 0; }
            TeamIndex team = body.teamComponent.teamIndex;
            int num = 0;//number of squid items on at least one time. idk man helpp
            using (IEnumerator<CharacterMaster> enumerator = CharacterMaster.readOnlyInstancesList.GetEnumerator())//gets each character on the (a?) team and checks each ones inventory
            {
                while (enumerator.MoveNext())
                {
                    int itemCount = enumerator.Current.inventory.GetItemCount(RoR2Content.Items.Squid);
                    if (itemCount > 0)
                    {
                        num += itemCount;
                    }
                }
            }

            return num;
        }
    }
    public static class HitHooks
    {
        public static void Init()
        {
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
                        GetHitBehavior?.Invoke(attackerBody, damageInfo, victimBody);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }
    }
    public static class ShieldDelayHooks
    {
        public class ShieldRechargeHookEventArgs : EventArgs
        {
            /// <summary>Added to the direct multiplier to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float reductionInSeconds = 0f;
            public float reductionInPercent = 0f;
        }

        public static void Init()
        {
            On.RoR2.CharacterBody.FixedUpdate += ShieldDelayBuff;
        }

        public delegate void ShieldRechargeHookEventHandler(CharacterBody sender, ShieldRechargeHookEventArgs args);
        public static event ShieldRechargeHookEventHandler GetShieldRechargeStat;
        private static void ShieldDelayBuff(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            ShieldRechargeHookEventArgs shieldStatMods = new ShieldRechargeHookEventArgs();
            GetShieldRechargeStat?.Invoke(self, shieldStatMods);


            float h = 7 / Mathf.Max(7 - shieldStatMods.reductionInSeconds, 0.1f);
            self.outOfDangerStopwatch += Mathf.Max((h - 1) * Time.fixedDeltaTime, -1);

            orig(self);
        }
    }
}
