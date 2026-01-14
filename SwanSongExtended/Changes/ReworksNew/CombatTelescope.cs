using BepInEx;
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

using RoR2.Items;
[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class CombatTelescope : ReworkBase<CombatTelescope>
    {
        public static BuffDef combatTelescopeCritChance;
        public static int scopeBaseCrit = 5;
        public static int scopeStackCrit = 0;
        public static int scopeBaseStationaryCrit = 40;
        public static int scopeStackStationaryCrit = 0;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_CritDamage.CritDamage_asset;

        public override string ItemName => "Combat Telescope";

        public override string ItemPickupDesc => "Increases 'Critical Strike' chance and damage while stationary.";

        public override string ItemFullDesc => 
            $"<style=cIsDamage>Critical Strikes</style> deal an additional <style=cIsDamage>100% damage</style> <style=cStack>(+100% per stack)</style>. " +
            $"Gain <style=cIsDamage>{scopeBaseStationaryCrit}% Critical Strike chance</style> " +
            $"after standing still for " +
            $"<style=cIsUtility>{CombatTelescopeBehavior.combatTelescopeWaitTime}</style> seconds.";

        public override void Init()
        {
            combatTelescopeCritChance = Content.CreateAndAddBuff(
                "bdCombatTelescopeCrit",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.red,
                false,
                false);
            base.Init();
        }
        public override void OnItemLoaded(ItemDef item)
        {
            base.OnItemLoaded(item);

            item.tier = ItemTier.Tier2;
            item.deprecatedTier = ItemTier.Tier2;

            Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Laser_Scope.png");
            if (sprite)
                itemDef.pickupIconSprite = sprite;
        }
        public override void Hooks()
        {
            GetStatCoefficients += ScopeCritChance;
        }
        private void ScopeCritChance(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.inventory)
            {
                int scopeCount = sender.inventory.GetItemCountEffective(DLC1Content.Items.CritDamage);
                if (scopeCount > 0)
                {
                    int critAdd = scopeBaseCrit;// + scopeStackCrit * (scopeCount - 1);

                    int buffCount = sender.GetBuffCount(combatTelescopeCritChance);
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
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            c.Emit(OpCodes.Ldc_I4, 0);
            c.Emit(OpCodes.Mul);
        }
    }

    public class CombatTelescopeBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => DLC1Content.Items.CritDamage;

        public static float combatTelescopeWaitTime = 0.2f;
        private void FixedUpdate()
        {
            float notMovingStopwatch = this.body.notMovingStopwatch;

            if (stack > 0 && notMovingStopwatch >= combatTelescopeWaitTime)
            {
                if (!body.HasBuff(CombatTelescope.combatTelescopeCritChance))
                {
                    this.body.AddBuff(CombatTelescope.combatTelescopeCritChance);
                    return;
                }
            }
            else if (body.HasBuff(CombatTelescope.combatTelescopeCritChance))
            {
                body.RemoveBuff(CombatTelescope.combatTelescopeCritChance);
            }
        }

        private void OnDestroy()
        {
            if (body.HasBuff(CombatTelescope.combatTelescopeCritChance))
               this.body.RemoveBuff(CombatTelescope.combatTelescopeCritChance);
        }
    }
}
