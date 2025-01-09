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
