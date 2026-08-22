using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        private static List<string> swanSongDeathQuoteTokens = new List<string>();
        public static void AddDeathMessages()
        {
            string baseToken = "PLAYER_DEATH_QUOTE_SWANSONG_";
            AddDeathQuoteToken(baseToken + "0", "SDIYBT.", "RIPMGSGHBAB.");
            AddDeathQuoteToken(baseToken + "1", "You have made a poor balancing decision.", "{0} has made a poor balancing decision.");
            AddDeathQuoteToken(baseToken + "2", "Consider playing SUPERBUG instead.", "{0} is considering playing SUPERBUG instead.");
            AddDeathQuoteToken(baseToken + "3", "I hope it was worth it.", "{0} is wondering if it was worth it.");

            IL.RoR2.GlobalEventManager.OnPlayerCharacterDeath += GlobalEventManager_OnPlayerCharacterDeath;

            void AddDeathQuoteToken(string token, string text, string? multiplayerText = null)
            {
                LanguageAPI.Add(token, text);
                LanguageAPI.Add(token + "_2P", multiplayerText ?? text);
                swanSongDeathQuoteTokens.Add(token);
            }
        }

        private static void GlobalEventManager_OnPlayerCharacterDeath(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<GlobalEventManager>(nameof(GlobalEventManager.standardDeathQuoteTokens)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(GlobalEventManager_OnPlayerCharacterDeath));
                return;
            }
            c.EmitDelegate<Func<string, string>>((tokenIn) =>
            {
                int standard = GlobalEventManager.standardDeathQuoteTokens.Length;
                int index = UnityEngine.Random.Range(0, standard + swanSongDeathQuoteTokens.Count);
                if (index < standard)
                    return tokenIn;
                else return swanSongDeathQuoteTokens[index - standard];
            });
        }
    }
}
