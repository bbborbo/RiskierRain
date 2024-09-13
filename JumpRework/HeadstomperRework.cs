using EntityStates.Headstompers;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace JumpRework
{
    public partial class JumpReworkPlugin
    {
        public static int fallBootsJumpCount = 3;
        public bool fallBootsSuperJumpLast = true;
        public float superJumpStrengthFirst = 1.2f; //2
        public float superJumpStrengthLast = 2f; //2
        private void StompersRework()
        {
            IL.EntityStates.Headstompers.HeadstompersIdle.FixedUpdateAuthority += HeadstompersJumpBoost;
            LanguageAPI.Add("ITEM_FALLBOOTS_PICKUP", "Quadruple jump. Hold 'Interact' to slam down to the ground.");
            LanguageAPI.Add("ITEM_FALLBOOTS_DESC",
                $"Gain <style=cIsUtility>{fallBootsJumpCount}</style> jumps. " +
                $"Creates a <style=cIsDamage>5m-100m</style> radius <style=cIsDamage>kinetic explosion</style> " +
                $"on hitting the ground, dealing " +
                $"<style=cIsDamage>1000%-10000%</style> base damage " +
                $"that scales up with <style=cIsDamage>fall distance</style>. " +
                $"Recharges in <style=cIsDamage>10</style> " +
                $"<style=cStack>(-50% per stack)</style> seconds.");
        }

        private void HeadstompersJumpBoost(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<BaseHeadstompersState>("get_isGrounded")
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<bool, HeadstompersIdle, bool>>((isGrounded, self) =>
            {
                bool shouldSuperJump = isGrounded;
                if (fallBootsSuperJumpLast) //override superjump on first jump if true
                {
                    CharacterMotor motor = self.bodyMotor;
                    if (motor)
                    {
                        shouldSuperJump = IsLastJump(motor, self.body);
                    }
                }

                return shouldSuperJump;
            });

            c.GotoNext(MoveType.After,
                x => x.MatchLdflda<Vector3>("y"),
                x => x.MatchDup()
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, fallBootsSuperJumpLast ? superJumpStrengthLast : superJumpStrengthFirst);
        }
    }
}
