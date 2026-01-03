using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using SwanSongExtended.Modules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static MoreStats.OnHit;
using static MoreStats.StatHooks;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public void HuntersHarpoonRework()
        {
            IL.RoR2.CharacterBody.RecalculateStats += ChangeMoveSpeed;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += ChangeDuration;
            LanguageAPI.Add("ITEM_MOVESPEEDONKILL_DESC", 
                "Killing an enemy increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>125%</style> " +
                "for <style=cIsUtility>1</style> <style=cStack>(+1 per stack)</style> seconds. " +
                "Consecutive kills increase buff duration to up to 25 seconds.");
        }

        public static void ChangeMoveSpeed(ILContext il)
        {
            ILCursor c = new(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdarg(0),
                x => x.MatchLdsfld("RoR2.DLC1Content/Buffs", "KillMoveSpeed"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.GetBuffCount))
                );
            if(!b)
            {
                Log.DebugBreakpoint(nameof(ChangeMoveSpeed));
                return;
            }

            c.Next.Operand = 1.25f;
            c.Index += 4;
            c.EmitDelegate<Func<int, int>>((buffCount) =>
             {
                 if (buffCount > 0)
                    return 1;
                 return 0;
             });
        }

        private void ChangeDuration(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int attackerBodyLoc = 16;
            int itemCountLoc = 54;
            int buffCountLoc = 86;
            int iteratorLoc = 91;
            ILLabel indexOne = c.DefineLabel();
            ILLabel indexTwo = c.DefineLabel();

            //get the item count location, this is used as a starting point
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "MoveSpeedOnKill"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out itemCountLoc)
                );
            if (!b1)
            {
                Log.DebugBreakpoint(nameof(ChangeDuration), 1);
                return;
            }

            //go back and get the index to change the buff count. we wont change anything yet until the hook is for sure completable
            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(5),
                x => x.MatchStloc(out buffCountLoc)
                );
            if (!b2)
            {
                Log.DebugBreakpoint(nameof(ChangeDuration), 2);
                return;
            }
            c.Index++;
            indexOne = c.MarkLabel();

            //get the attacker body location
            bool b3 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(out attackerBodyLoc),
                x => x.MatchLdsfld("RoR2.DLC1Content/Buffs", "KillMoveSpeed"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.ClearTimedBuffs))
                );
            if (!b3)
            {
                Log.DebugBreakpoint(nameof(ChangeDuration), 3);
                return;
            }

            //go to buff adding and save the index again
            bool b4 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.AddTimedBuff))
                );
            if (!b4)
            {
                Log.DebugBreakpoint(nameof(ChangeDuration), 4);
                return;
            }
            indexTwo = c.MarkLabel();

            bool b5 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(out iteratorLoc),
                x => x.MatchLdloc(buffCountLoc)
                );
            if(!b5)
            {
                Log.DebugBreakpoint(nameof(ChangeDuration), 5);
                return;
            }

            //go to where buffs are added and make the buff duration one second per buff
            c.GotoLabel(indexTwo);
            c.Emit(OpCodes.Ldloc, iteratorLoc);
            c.EmitDelegate<Func<float, int, float>>((baseBuffDuration, iterator) =>
            {
                return iterator + 1;
            });

            //this makes it so the new buff count is always 1 more than the current buff count
            c.GotoLabel(indexOne);
            c.Emit(OpCodes.Ldloc, itemCountLoc);
            c.Emit(OpCodes.Ldloc, attackerBodyLoc);
            c.EmitDelegate<Func<int, int, CharacterBody, int>>((vanillaBuffCount, itemCount, attackerBody) =>
            {
                if (itemCount > 25)
                    return itemCount;

                int buffCount = attackerBody.GetBuffCount(DLC1Content.Buffs.KillMoveSpeed);
                int newBuffCount = Mathf.Min(25, buffCount + itemCount);
                return newBuffCount;
            });
        }
    }
}
