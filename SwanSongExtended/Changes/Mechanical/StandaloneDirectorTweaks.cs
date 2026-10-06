using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public static void EmergencyDirectorTweaks()
        {
            RoR2.RoR2Application.onLoad += EmergencyDirectorHooks;
        }

        static bool boostSpawnRates = true;
        private static void EmergencyDirectorHooks()
        {
            if (Tools.isLoaded("com.RiskOfBrainrot.RiskierRain"))
                return;
            if(Tools.isLoaded("com.Nuxlar.EarlySpawnBoost") || Tools.isLoaded("com.score.DirectorReworkPlus") || Tools.isLoaded("com.RiskyLives.RiskyMod") || Tools.isLoaded("BALLS.WellRoundedBalance"))
            {
                boostSpawnRates = false;
            }
            On.RoR2.CombatDirector.Awake += EmergencyDirectorChange;
        }

        private static void EmergencyDirectorChange(On.RoR2.CombatDirector.orig_Awake orig, RoR2.CombatDirector self)
        {
            if(self.gameObject.name == "Teleporter1(Clone)" || Run.instance == null)
            {
                orig(self);
                return;
            }
            if(Run.instance.selectedDifficulty == difficultyIndexExtinction)
            {
                self.eliteBias *= 0.5f;
                orig(self);
                return;
            }
            if(boostSpawnRates == true && Run.instance.stageClearCount < 2 && Run.instance.IsExpansionEnabled(expansionDefSS2))
            {
                self.creditMultiplier *= 1.2f;
            }
            orig(self);
        }
    }
}
