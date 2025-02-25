using BepInEx;
using EntityStates;
using EntityStates.ClayBoss;
using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static int ignitionTankBurnChance = 15;
        public float ignitionBurnDamage = 2f; //3f
        public float ignitionBurnDuration = 2f; //0f
        bool shouldIgnitionDamageAndDurationCompound = false;

        public static int stacheBurnChance = 25;

        public static int brandBurnChance = 30;
        void BurnReworks()
        {
            IgnitionTankRework();
        }

        #region ignition tank
        private void IgnitionTankRework()
        {
            GetMoreStatCoefficients += IgniTankBurnChance;
            On.RoR2.StrengthenBurnUtils.CheckDotForUpgrade += OverrideIgnitionBurn;
            LanguageAPI.Add("ITEM_STRENGTHENBURN_PICKUP", "Your ignite effects deal triple damage.");
            LanguageAPI.Add("ITEM_STRENGTHENBURN_DESC", 
                $"Gain <style=cIsDamage>{ignitionTankBurnChance}% ignite chance</style>. " +
                $"All ignition effects deal <style=cIsDamage>+{Tools.ConvertDecimal(ignitionBurnDamage)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(ignitionBurnDamage)} per stack)</style> more damage and last " +
                $"<style=cIsUtility>+{Tools.ConvertDecimal(ignitionBurnDuration)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(ignitionBurnDuration)} per stack)</style> longer.");
        }

        private void IgniTankBurnChance(CharacterBody sender, MoreStatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv && inv.GetItemCount(DLC1Content.Items.StrengthenBurn) > 0)
            {
                args.burnChanceOnHit += ignitionTankBurnChance;
            }
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
        #endregion
        
        public static int GetBurnCount(CharacterBody victimBody)
        {
            return victimBody.GetBuffCount(RoR2Content.Buffs.OnFire) + victimBody.GetBuffCount(DLC1Content.Buffs.StrongerBurn);
        }
    }
}
