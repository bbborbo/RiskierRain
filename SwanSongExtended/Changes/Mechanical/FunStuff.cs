using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        private static List<string> swanSongDeathQuoteTokens = new List<string>();
        private static List<string> swanSongStormDeathQuoteTokens = new List<string>();
        public static void AddDeathMessages()
        {
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "You didn't go fast enough.", "{0} thought about uninstalling Swan Song.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "Was it worth it?", "{0} is wondering if it was worth it.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "Sending complaints about storms..", "{0} is sending complaints about storms..");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "Should've checked the forecast..", "{0} didn't check the forecast.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "Consider playing SUPERBUG instead?", "{0} is considering playing SUPERBUG instead.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "You were washed away.", "{0} was washed away.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "The storm blew you away.", "The storm blew {0} away.");
            AddDeathQuoteTokenToList(ref swanSongStormDeathQuoteTokens, "You'll get the hang of it!", "{0} is still learning!");

            AddDeathQuoteToken("RIPMGSGHBAB.", "{0} went KA-BOOM.");
            AddDeathQuoteToken("You have made a poor balancing decision.", "{0} has made a poor balancing decision.");
            AddDeathQuoteToken("You were splatted.", "{0} was splatted.");
            AddDeathQuoteToken("Get got.", "{0} got got.");
            AddDeathQuoteToken("Congratulations on the spontaneous lobotomy!", "{0} was spontaneously lobotomized.");
            AddDeathQuoteToken("Your innards became outards.", "{0}'s innards became outards.");
            AddDeathQuoteToken("Your plea for death is answered.", "{0}'s plea for death was answered.");
            AddDeathQuoteToken("Must've been the wind.", "{0} blames it on the weather.");
            AddDeathQuoteToken("Counting, or not counting Elite violence?", "{0} had embarrassing last words.");
            AddDeathQuoteToken("Flopped!", "{0} was a flop.");
            AddDeathQuoteToken("Maybe that's configurable?", "{0} wants to change the config.");
            AddDeathQuoteToken("Remember to take breaks.", "{0} suggests we should take a break.");
            AddDeathQuoteToken("Remember to drink water.", "{0} wants a water break.");
            AddDeathQuoteToken("Didn't account for that?", "{0} didn't account for that.");
            AddDeathQuoteToken("You were slain..", "{0} was slain..");
            AddDeathQuoteToken("You weren't green enough..", "{0} wasnt green enough..");
            //AddDeathQuoteToken("Curiosity killed the {0}.");

            IL.RoR2.GlobalEventManager.OnPlayerCharacterDeath += GlobalEventManager_OnPlayerCharacterDeath;

            void AddDeathQuoteToken(string defaultText, string? secondPlayerText = null)
            {
                AddDeathQuoteTokenToList(ref swanSongDeathQuoteTokens, defaultText, secondPlayerText);
            }
            void AddDeathQuoteTokenToList(ref List<string> list, string secondPerson, string? thirdPerson = null)
            {
                string baseToken = "PLAYER_DEATH_QUOTE_SWANSONG_" + swanSongDeathQuoteTokens.Count;
                LanguageAPI.Add(baseToken + "_2P", secondPerson); //IDK WHY FIRST PERSON IS 2P ?????????
                LanguageAPI.Add(baseToken, thirdPerson ?? secondPerson);
                list.Add(baseToken);
            }
        }

        private static void GlobalEventManager_OnPlayerCharacterDeath(MonoMod.Cil.ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld<GlobalEventManager>(nameof(GlobalEventManager.standardDeathQuoteTokens)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchStloc(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(GlobalEventManager_OnPlayerCharacterDeath));
                return;
            }
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<string, DamageReport, string>>((tokenIn, damageReport) =>
            {
                if(swanSongStormDeathQuoteTokens.Count > 0 
                    && StormsCore.IsStormDamage(damageReport.damageInfo, damageReport.attackerBody)
                    )
                {
                    return swanSongStormDeathQuoteTokens[UnityEngine.Random.Range(0, swanSongStormDeathQuoteTokens.Count)];
                }
                int standard = GlobalEventManager.standardDeathQuoteTokens.Length;
                int index = UnityEngine.Random.Range(0, standard + swanSongDeathQuoteTokens.Count);
                if (index < standard || index - standard > swanSongDeathQuoteTokens.Count)
                    return tokenIn;
                else return swanSongDeathQuoteTokens[index - standard];
            });
        }
    }
}
