using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin
    {
        public static int sacrificeDropCount = 12;
        public static int sacrificeDropCountRestricted = 5;
        public static float sacrificeDropRate = 8f; //5f
        public static float sacrificeDropRateLimited = 20f;

        public static int sonorousDropCountBase = 4;
        public static int sonorousDropCountStack = 2;
        public static int sonorousDropCountRestrictedBase = 5;
        public static int sonorousDropCountRestrictedStack = 1;
        public static float sonorousDropRateBase = 5f; //0.04f
        public static float sonorousDropRateStack = 5f;
        public static float sonorousDropRateLimited = 20f;

        private static int serverSacrificeDropTally = 0;
        private static int serverSonorousDropTally = 0;

        public static void DoSacrificeDropLimit()
        {
            IL.RoR2.Artifacts.SacrificeArtifactManager.OnServerCharacterDeath += DropLimitForSacrifice;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += DropLimitForSonorous;
            Stage.onServerStageBegin += ResetServerDropCount;

            LanguageAPI.Add("ITEM_ITEMDROPCHANCEONKILL_DESC", 
                $"On killing a large monster, it will <style=cIsDamage>always</style> drop an item. " +
                $"Elite monsters have a <style=cIsUtility>{sonorousDropRateBase}%</style> chance of " +
                $"dropping an item <style=cStack>(+{sonorousDropRateStack}% per stack)</style>.");
        }

        public static int GetDropCountForSacrifice()
        {
            if (!Run.instance)
                return 0;

            if (UsingRestrictedSacrifice())
            {
                return sacrificeDropCountRestricted * Run.instance.participatingPlayerCount;
            }
            return sacrificeDropCount * Run.instance.participatingPlayerCount;
        }

        public static int GetDropCountForSonorous()
        {
            if (!Run.instance)
                return 0;

            int sonorousCount = Util.GetItemCountForTeam(TeamIndex.Player, DLC2Content.Items.ItemDropChanceOnKill.itemIndex, false, true);
            if (UsingRestrictedSacrifice())
            {
                return sonorousDropCountRestrictedBase + sonorousDropCountRestrictedStack * (sonorousCount - 1);
            }
            return sonorousDropCountBase + sonorousDropCountStack * (sonorousCount - 1);
        }

        internal static bool UsingRestrictedSacrifice()
        {
            return Stage.instance.sceneDef.sceneType == SceneType.Intermission || Stage.instance.sceneDef.sceneType == SceneType.UntimedStage;
        }

        private static void ResetServerDropCount(Stage obj)
        {
            serverSacrificeDropTally = 0;
            serverSonorousDropTally = 0;
        }

        #region sacrifice
        private static void DropLimitForSacrifice(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            FixSacrificeDropChance(c);
            c.Index = 0;
            AddSacrificeDropTally(c);
        }

        private static void FixSacrificeDropChance(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(Util.GetExpAdjustedDropChancePercent)))
                && c.TryGotoPrev(MoveType.After,
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(FixSacrificeDropChance));
                return;
            }

            c.EmitDelegate<Func<float, float>>((_) =>
            {
                if (serverSacrificeDropTally >= GetDropCountForSacrifice())
                    if (!Util.CheckRoll(sacrificeDropRateLimited))
                        return 0;
                return sacrificeDropRate;
            });
        }

        private static void AddSacrificeDropTally(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<PickupDropletController>(nameof(PickupDropletController.CreatePickupDroplet))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(AddSacrificeDropTally));
                return;
            }

            c.EmitDelegate<Action>(() =>
            {
                serverSacrificeDropTally++;
            });
        }
        #endregion

        #region sonorous
        private static void DropLimitForSonorous(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            ILLabel the = c.DefineLabel();
            int masterLoc = 17;
            int itemCountLoc = 102;

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(RoR2.DLC2Content.Items.ItemDropChanceOnKill)))
                && c.TryGotoPrev(MoveType.After,
                x => x.MatchLdloc(out masterLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(DropLimitForSonorous), 1);
                return;
            }
            SonorousUseTeamItemCount(c, masterLoc);

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLoc)
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(DropLimitForSonorous), 2);
                return;
            }
            the = c.MarkLabel();

            FixSonorousDropChance(c, itemCountLoc, masterLoc);
            c.GotoLabel(the);
            AddSonorousDropTally(c, itemCountLoc);
        }

        private static void SonorousUseTeamItemCount(ILCursor c, int masterLoc)
        {
            void DoTheRoar()
            {
                c.Emit(OpCodes.Ldloc, masterLoc);
                c.EmitDelegate<Func<int, CharacterMaster, int>>((oldItemCount, master) => 
                { 
                    return Util.GetItemCountForTeam(master.teamIndex, DLC2Content.Items.ItemDropChanceOnKill.itemIndex, true, true);
                });
            }

            DoTheRoar();
            //emit a delegate that takes the previous int value and master and replaces the int value with Util.GetItemCountForTeam

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(SonorousUseTeamItemCount));
                return;
            }
            //do it again
            DoTheRoar();
        }

        private static void FixSonorousDropChance(ILCursor c, int itemCountLoc, int masterLoc)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_isElite"))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(itemCountLoc))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(masterLoc)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(FixSonorousDropChance));
                return;
            }

            c.Emit(OpCodes.Ldloc, itemCountLoc);
            c.EmitDelegate<Func<float, int, float>>((chanceIn, stack) =>
            {
                if (serverSonorousDropTally >= GetDropCountForSacrifice())
                    if(!Util.CheckRoll(sonorousDropRateLimited))
                        return 0;
                return sonorousDropRateBase + sonorousDropRateStack * (stack - 1);
            });
        }

        private static void AddSonorousDropTally(ILCursor c, int itemCountLoc)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_isElite"))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(itemCountLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<PickupDropletController>(nameof(PickupDropletController.CreatePickupDroplet))
                );
            if (!b)
            {
                DebugBreakpoint(nameof(AddSonorousDropTally));
                return;
            }

            c.EmitDelegate<Action>(() =>
            {
                serverSonorousDropTally++;
            });
        }
        #endregion
    }
}
