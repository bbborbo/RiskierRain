﻿using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using SwanSongExtended.Modules;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public int scopeBaseCrit = 5;
        public int scopeStackCrit = 0;
        public int scopeBaseStationaryCrit = 40;
        public int scopeStackStationaryCrit = 0;
		public void ReworkLaserScope()
        {
            //IL.RoR2.CharacterBody.RecalculateStats += RevokeScopeRights;
            RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/DLC1/CritDamage/CritDamage.asset").WaitForCompletion(), ItemTier.Tier2);
            GetStatCoefficients += ScopeCritChance;
            On.RoR2.CharacterBody.OnInventoryChanged += AddScopeItemBehavior;

            LanguageAPI.Add("ITEM_CRITDAMAGE_NAME", "Combat Telescope");
            LanguageAPI.Add("ITEM_CRITDAMAGE_PICKUP", "Increases 'Critical Strike' chance and damage while stationary.");
            LanguageAPI.Add("ITEM_CRITDAMAGE_DESC", 
                $"<style=cIsDamage>Critical Strikes</style> deal an additional <style=cIsDamage>100% damage</style> <style=cStack>(+100% per stack)</style>. " +
                $"Gain <style=cIsDamage>{scopeBaseCrit}% critical chance</style>, " +
                $"or <style=cIsDamage>{scopeBaseStationaryCrit}%</style> after standing still " +
                $"for <style=cIsUtility>{CombatTelescopeBehavior.combatTelescopeWaitTime}</style> seconds.");
        }

        private void AddScopeItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);

            self.AddItemBehavior<CombatTelescopeBehavior>(self.inventory.GetItemCount(DLC1Content.Items.CritDamage));
        }

        private void ScopeCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.inventory)
            {
                int scopeCount = sender.inventory.GetItemCount(DLC1Content.Items.CritDamage);
                if (scopeCount > 0)
                {
                    int critAdd = scopeBaseCrit;// + scopeStackCrit * (scopeCount - 1);

                    int buffCount = sender.GetBuffCount(CommonAssets.combatTelescopeCritChance);
                    if (buffCount > 0)
                    {
                        critAdd = scopeBaseStationaryCrit;// + scopeStackStationaryCrit * (buffCount - 1);
                    }

                    args.critAdd += critAdd;
                }
            }
        }

        private void RevokeScopeRights(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "CritDamage"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }

	public class CombatTelescopeBehavior : RoR2.CharacterBody.ItemBehavior
    {
        public static float combatTelescopeWaitTime = 0.2f;
        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                float notMovingStopwatch = this.body.notMovingStopwatch;

                if (stack > 0 && notMovingStopwatch >= combatTelescopeWaitTime)
                {
                    if (!body.HasBuff(CommonAssets.combatTelescopeCritChance))
                    {
                        this.body.AddBuff(CommonAssets.combatTelescopeCritChance);
                        return;
                    }
                }
                else if (body.HasBuff(CommonAssets.combatTelescopeCritChance))
                {
                    body.RemoveBuff(CommonAssets.combatTelescopeCritChance);
                }
            }
        }

        private void OnDisable()
        {
            this.body.RemoveBuff(CommonAssets.combatTelescopeCritChance);
        }
    }
}
