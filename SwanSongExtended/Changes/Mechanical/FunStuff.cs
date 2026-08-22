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
            AddDeathQuoteToken("RIPMGSGHBAB.", "{0} went KA-BOOM.");
            AddDeathQuoteToken("You have made a poor balancing decision.", "{0} has made a poor balancing decision.");
            AddDeathQuoteToken("Consider playing SUPERBUG instead?", "{0} is considering playing SUPERBUG instead.");
            AddDeathQuoteToken("Was it worth it?", "{0} is wondering if it was worth it.");
            AddDeathQuoteToken("You didn't play fast enough.", "{0} thought about uninstalling Swan Song.");
            AddDeathQuoteToken("You were splatted.", "{0} was splatted.");
            AddDeathQuoteToken("Get got.", "{0} got got.");
            AddDeathQuoteToken("Congratulations on the spontaneous lobotomy!", "{0} was spontaneously lobotomized.");
            AddDeathQuoteToken("Your innards became outards.", "{0}'s innards became outards.");
            AddDeathQuoteToken("Your plea for death is answered.", "{0}'s plea for death was answered.");
            AddDeathQuoteToken("Curiosity killed the {0}.");

            IL.RoR2.GlobalEventManager.OnPlayerCharacterDeath += GlobalEventManager_OnPlayerCharacterDeath;

            void AddDeathQuoteToken(string defaultText, string? secondPlayerText = null)
            {
                string baseToken = "PLAYER_DEATH_QUOTE_SWANSONG_" + swanSongDeathQuoteTokens.Count;
                LanguageAPI.Add(baseToken, defaultText);
                LanguageAPI.Add(baseToken + "_2P", secondPlayerText ?? defaultText);
                swanSongDeathQuoteTokens.Add(baseToken);
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
