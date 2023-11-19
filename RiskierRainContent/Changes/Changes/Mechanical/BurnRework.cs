using BepInEx;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RiskierRainContent.BurnStatHook;

namespace RiskierRainContent
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static int ignitionTankBurnChance = 15;
        public float ignitionBurnDamage = 2f; //3f
        public float ignitionBurnDuration = 2f; //0f
        bool shouldIgnitionDamageAndDurationCompound = false;

        public static int stacheBurnChance = 25;

        public static int brandBurnChance = 30;
        void BurnReworks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += BurnChanceHook;

            IgnitionTankRework();
        }

        #region ignition tank
        private void IgnitionTankRework()
        {
            BurnStatCoefficient += IgnitionTankBurnChance;
            On.RoR2.StrengthenBurnUtils.CheckDotForUpgrade += OverrideIgnitionBurn;
            LanguageAPI.Add("ITEM_STRENGTHENBURN_PICKUP", "Your ignite effects deal triple damage.");
            LanguageAPI.Add("ITEM_STRENGTHENBURN_DESC", 
                $"Gain <style=cIsDamage>{ignitionTankBurnChance}% ignite chance</style>. " +
                $"All ignition effects deal <style=cIsDamage>+{Tools.ConvertDecimal(ignitionBurnDamage)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(ignitionBurnDamage)} per stack)</style> more damage and last " +
                $"<style=cIsUtility>+{Tools.ConvertDecimal(ignitionBurnDuration)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(ignitionBurnDuration)} per stack)</style> longer.");
        }

        private void OverrideIgnitionBurn(On.RoR2.StrengthenBurnUtils.orig_CheckDotForUpgrade orig, Inventory inventory, ref InflictDotInfo dotInfo)
        {
            if (dotInfo.dotIndex == DotController.DotIndex.Burn || dotInfo.dotIndex == DotController.DotIndex.Helfire)
            {
                int itemCount = inventory.GetItemCount(DLC1Content.Items.StrengthenBurn);
                if (itemCount > 0)
                {
                    dotInfo.preUpgradeDotIndex = new DotController.DotIndex?(dotInfo.dotIndex);
                    dotInfo.dotIndex = DotController.DotIndex.StrongerBurn;
                    float damageBoost = 1 + ignitionBurnDamage * itemCount;
                    float durationBoost = 1 + ignitionBurnDuration * itemCount;
                    if (shouldIgnitionDamageAndDurationCompound)
                    {
                        dotInfo.totalDamage *= (damageBoost * durationBoost);
                        dotInfo.damageMultiplier *= damageBoost;
                    }
                    else
                    {
                        dotInfo.totalDamage *= damageBoost;
                        dotInfo.damageMultiplier *= (damageBoost / durationBoost);
                    }
                }
            }
        }

        private void IgnitionTankBurnChance(CharacterBody sender, BurnEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv && inv.GetItemCount(DLC1Content.Items.StrengthenBurn) > 0)
            {
                args.burnChance += ignitionTankBurnChance;
            }
        }
        #endregion
        private void BurnChanceHook(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody victimBody = victim ? victim.GetComponent<CharacterBody>() : null;

                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                Inventory inventory = attackerBody?.inventory;

                if (attackerBody && victimBody && inventory)
                {
                    BurnStatHook.InvokeStatHook(attackerBody);

                    uint? maxStacksFromAttacker = null;
                    if ((damageInfo != null) ? damageInfo.inflictor : null)
                    {
                        ProjectileDamage component = damageInfo.inflictor.GetComponent<ProjectileDamage>();
                        if (component && component.useDotMaxStacksFromAttacker)
                        {
                            maxStacksFromAttacker = new uint?(component.dotMaxStacksFromAttacker);
                        }
                    }

                    int burnProcChance = BurnStatHook.GetBurnChance().burnChance;
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
                            StrengthenBurnUtils.CheckDotForUpgrade(inventory, ref inflictDotInfo);
                            DotController.InflictDot(ref inflictDotInfo);
                        }
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        public static int GetBurnCount(CharacterBody victimBody)
        {
            return victimBody.GetBuffCount(RoR2Content.Buffs.OnFire) + victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
        }
    }

    public class BurnStatHook
    {
        public class BurnEventArgs : EventArgs
        {
            public int burnChance = 0;
        }
        private static BurnEventArgs BurnMods;

        public delegate void StatHookEventHandler(CharacterBody sender, BurnEventArgs args);
        public static event StatHookEventHandler BurnStatCoefficient;

        public static BurnEventArgs GetBurnChance()
        {
            return BurnMods;
        }

        public static void InvokeStatHook(CharacterBody self)
        {
            BurnMods = new BurnEventArgs();
            if(BurnStatCoefficient != null)
            {
                foreach (StatHookEventHandler @event in BurnStatCoefficient.GetInvocationList())
                {
                    try
                    {
                        @event(self, BurnMods);
                    }
                    catch
                    {

                    }
                }
            }
        }
    }
}
