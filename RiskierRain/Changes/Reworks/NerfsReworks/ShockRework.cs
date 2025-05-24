using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.CoreModules.StatHooks;
using static R2API.RecalculateStatsAPI;
using EntityStates;
using RiskierRain.CoreModules;
using System.Runtime.CompilerServices;
using MonoMod.RuntimeDetour;
using System.Reflection;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float shockForceExitFraction = 0.10f;
        private void ShockBuff()
        {
            LanguageAPI.Add("KEYWORD_SHOCKING",
                $"<style=cKeywordName>Shocking</style>" +
                $"<style=cSub>Interrupts enemies and stuns them. " +
                $"The stun is broken if the target takes more than " +
                $"<style=cIsHealth>{Tools.ConvertDecimal(shockForceExitFraction)}</style> " +
                $"of their maximum health in damage. " +
                $"</style>");
            ShockState.healthFractionToForceExit = shockForceExitFraction;
            On.EntityStates.ShockState.OnExit += ShockSparkOnExit;
            //On.EntityStates.ShockState.OnEnter += ShockBuffEnter;
            //On.EntityStates.ShockState.OnExit += ShockBuffExit;
            //On.RoR2.HealthComponent.TakeDamageProcess += ShockHit;
        }

        private void ShockSparkOnExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            if (self.healthFraction - self.characterBody.healthComponent.combinedHealthFraction > ShockState.healthFractionToForceExit)
            {

            }
            orig(self);
        }

        private void ShockHit(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            if (damageInfo.damageType.damageType.HasFlag(DamageType.Shock5s))
            {
                //GameObject attacker = damageInfo.attacker;
                self.body.AddTimedBuff(CoreModules.Assets.shockMarker, CoreModules.Assets.shockMarkerDuration);//add authority
            }
            orig(self, damageInfo);
        }


        private void ShockBuffEnter(On.EntityStates.ShockState.orig_OnEnter orig, EntityStates.ShockState self)
        {
            orig(self);
        }

        private void ShockBuffExit(On.EntityStates.ShockState.orig_OnExit orig, EntityStates.ShockState self)
        {
            if (self == null)
            {
                Debug.Log("shockstate null");
            }
            else
            {
                if (self.characterBody == null)
                {
                    Debug.Log("body null");
                }
                else
                {
                    if (self.characterBody.HasBuff(CoreModules.Assets.shockMarker))//it breaks here!
                    {
                        HealthComponent hcVictim = self.healthComponent;
                        GameObject attackerObject = hcVictim.lastHitAttacker;
                        if (attackerObject == null)
                        {
                        }
                        else
                        {
                            CharacterBody attacker = attackerObject.GetComponent<CharacterBody>();
                            if (attacker != null)
                            {
                                if (attacker.maxShield > 0 && attacker.healthComponent?.shield != attacker.maxShield)
                                {
                                    ShockHeal(attacker.healthComponent);
                                }

                            }
                        }
                    }
                }
            }
            //self.characterBody.RemoveBuff(Assets.shockMarker);
            orig(self);
        }
        private void ShockHeal(HealthComponent attacker)
        {
            if (!attacker.body.HasBuff(CoreModules.Assets.shockHealCooldown))
            {
                float missingShieldPercent = (attacker.body.maxShield - attacker.shield) / attacker.body.maxShield;
                float maxShieldPercent = attacker.body.maxShield / attacker.fullCombinedHealth;
                int cooldownToApply = (int)((maxShieldPercent * missingShieldPercent) * 20);
                for (int i = 0; i < cooldownToApply; i++)
                {
                    attacker.body.AddTimedBuff(CoreModules.Assets.shockHealCooldown, i + 1);
                }
                attacker.ForceShieldRegen(); //the buff runs out slightly before the shockstate does, for some reason. im gonna call it a feature for now
            }
        }
    }
}
