using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using UnityEngine;

namespace NegativeRegenFix
{
    [BepInPlugin(guid, teamName, modName)]
    public class NegativeRegenFix : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "NegativeRegenFix";
        public const string version = "1.0.0";
        #endregion

        public void Awake()
        {
            IL.RoR2.CharacterBody.RecalculateStats += RegenMultiplierFix;
        }
        private static void RegenMultiplierFix(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //int regenLocation = 66;

            //go to regen section
            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterBody>("baseRegen"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<CharacterBody>("levelRegen")
                );

            int regenMultLocation = 72;
            int endRegenLocation = 73;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(1),
                x => x.MatchStloc(out regenMultLocation)
                );
            endRegenLocation = regenMultLocation + 1;

            c.GotoNext(MoveType.After,
                /*x => x.MatchLdloc(regenMultLocation),
                x => x.MatchMul(),*/
                x => x.MatchStloc(endRegenLocation)
                );

            c.Emit(OpCodes.Ldloc, endRegenLocation);
            c.Emit(OpCodes.Ldloc, regenMultLocation);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, float, CharacterBody, float>>((currentRegen, regenMult, body) =>
            {
                if (body.HasBuff(RoR2Content.Buffs.AffixEcho) || body.HasBuff(DLC1Content.Buffs.EliteEarth))
                    return 0;

                //calculate base regen/before items
                float bodyRegen = body.baseRegen + body.levelRegen * (body.level - 1);

                //subtract base regen from the calculated total to get the bonus regen
                float extraRegen = (currentRegen / regenMult) - bodyRegen;

                //end regen accomodating for negative regen using absolute value
                float endRegen = (bodyRegen + Mathf.Abs(bodyRegen) * (regenMult - 1)) + (extraRegen * regenMult);

                //for some stupid fucking reason the tonic regen buff is applied after all other stats so i have to accomodate for it separately and preemptively
                //thanks hopooo
                if (body.HasBuff(RoR2Content.Buffs.TonicBuff) && endRegen < 0)
                {
                    endRegen += Mathf.Abs(endRegen) * 3;
                    endRegen /= 4;
                }

                return endRegen;
            });
            c.Emit(OpCodes.Stloc, endRegenLocation);
        }
    }
}
