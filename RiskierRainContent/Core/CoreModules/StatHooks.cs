using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.CoreModules
{
    public class StatHooks : CoreModule
    {
        public static void ApplyCooldownScale(GenericSkill skillSlot, float cooldownScale)
        {
            if (skillSlot != null)
                skillSlot.cooldownScale *= cooldownScale;
        }
        public class BorboStatHookEventArgs : EventArgs
        {
            /// <summary>Added to the direct multiplier to attack speed. ATTACK_SPEED ~ (BASE_ATTACK_SPEED + baseAttackSpeedAdd) * (ATTACK_SPEED_MULT + attackSpeedMultAdd).</summary>
            public float attackSpeedMultAdd = 1f;
            public float attackSpeedDivAdd = 1f;
        }

        public override void Init()
        {
            On.RoR2.CharacterBody.RecalculateStats += AttackSpeedBs;
            On.RoR2.GlobalEventManager.OnHitEnemy += GlobalEventManager_OnHitEnemy;
        }

        public delegate void StatHookEventHandler(CharacterBody sender, BorboStatHookEventArgs args);
        public static event StatHookEventHandler BorboStatCoefficients;
        private void AttackSpeedBs(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            BorboStatHookEventArgs attackerStatMods = new BorboStatHookEventArgs();
            BorboStatCoefficients?.Invoke(self, attackerStatMods);

            float attackSpeedModifier = attackerStatMods.attackSpeedMultAdd / attackerStatMods.attackSpeedDivAdd;
            self.attackSpeed *= attackSpeedModifier;
        }

        public delegate void HitHookEventHandler(CharacterBody body, DamageInfo damageInfo, GameObject victim);
        public static event HitHookEventHandler GetHitBehavior;
        private void GlobalEventManager_OnHitEnemy(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if(damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if(attackerBody != null)
                {
                    CharacterMaster attackerMaster = attackerBody.master;
                    if(attackerMaster != null)
                    {
                        GetHitBehavior?.Invoke(attackerBody, damageInfo, victim);
                    }
                }
            }
            orig(self, damageInfo, victim);
        }
    }
}
