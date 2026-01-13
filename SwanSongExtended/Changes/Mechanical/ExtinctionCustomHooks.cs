using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RainrotSharedUtils.Difficulties;
using RainrotSharedUtils.MoreProjectiles;
using RoR2;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin
    {
        public const string difficultyToken = "DIFFICULTYSWANSONG";

        public static float eclipseDifficultyBoost = 6;
        public static DifficultyDef difficultyDefExtinction;
        public static DifficultyIndex difficultyIndexExtinction;
        public static string extinctionName = "Extinction";
        public static string extinctionDescBase =
            $"The air is heavy with grief. This inhospitable wasteland will not spare you. Only for the determined and truly masochistic. " +
            $"<style=cStack>\n\n>Player Health Regeneration: <style=cArtifact>-40%</style></style> " +
            $"<style=cStack>\n>Difficulty Scaling: <style=cArtifact>+100%</style>";
        public static string extinctionDescStartingDifficulty =
            $"<style=cStack>\n>Starting Difficulty: <style=cArtifact>Very Hard</style></style>";
        public static string extinctionDescExtra =
            $"\n>Rare Elites: <style=cArtifact>Stage 4</style>" +
            $"\n>Teleporter Visuals: <style=cArtifact>OFF</style>" +
            $"\n>Enemies gain <style=cArtifact>unique scaling</style>" +
            $"\n>Enemies gain <style=cArtifact>tripled projectiles</style></style>";
        public static string extinctionDesc =
            extinctionDescBase + extinctionDescStartingDifficulty + extinctionDescExtra;

        public static bool EnableMoreProjectilesForExtinctionEnemies(CharacterBody sender)
        {
            return sender.teamComponent.teamIndex != TeamIndex.Player && Run.instance.selectedDifficulty == difficultyIndexExtinction;
        }
        private void CreateDifficultyDef()
        {
            difficultyDefExtinction = new DifficultyDef(
                scalingValue: 4,
                nameToken: difficultyToken + "_NAME",
                iconPath: "",
                descriptionToken: difficultyToken + "_DESC",
                color: Color.blue,
                serverTag: "",
                countsAsHardMode: true
                );

            difficultyIndexExtinction = DifficultyAPI.AddDifficulty(difficultyDefExtinction);

            MoreDifficultyStats extinctionStats = DifficultyUtilsModule.GetMoreDifficultyStats(difficultyIndexExtinction);
            extinctionStats.ambientLevelCap = 999;
            extinctionStats.delayFirstStorm_ForSwanSong = false;
            extinctionStats.desiredStormTime_ForSwanSong = StormsCore.monsoonStormDelayMinutes;
            extinctionStats.desiredStormWarningTime_ForSwanSong = StormsCore.monsoonStormWarningMinutes;
            extinctionStats.startingDifficultyCoefficientBoost = difficultyDefExtinction.scalingValue - 1;
            extinctionStats.startingDifficultyDisplay = (float)MoreDifficultyStats.StartingDifficulty.VeryHard;
            extinctionStats.startingLevelBoost = eclipseDifficultyBoost;
            extinctionStats.stormIntensifyStrength_ForSwanSong = StormsCore.stormStrengthIncreaseBase + StormsCore.stormStrengthIncreasePerDifficulty * 4;
            extinctionStats.teleporterParticleRangeMultiplier = 0;
            extinctionStats.tier2EliteStage = 4;
            DifficultyUtilsModule.difficultyCustomStats[difficultyIndexExtinction] = extinctionStats;

            LanguageAPI.Add(difficultyToken + "_NAME", extinctionName);
            LanguageAPI.Add(difficultyToken + "_DESC", extinctionDesc);

            RoR2Application.onLoadFinished += ExtinctionCustomHooks;
            MoreProjectilesModule.MoreProjectilesProvider += EnableMoreProjectilesForExtinctionEnemies;
        }

        void ExtinctionCustomHooks()
        {
            if (is2R4RLoaded)
            {
                Log.Error("Extinction no custom hooks");
                return;
            }

            Log.Error("Extinction custom hooks");

            GetStatCoefficients += this.MonsoonPlusStatBuffs2;
        }
        private void MonsoonPlusStatBuffs2(CharacterBody sender, StatHookEventArgs args)
        {
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            if (selectedDifficulty != difficultyIndexExtinction)
                return;
            float ambientLevelBoost = eclipseDifficultyBoost;
            if (sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                float compensatedLevel = sender.level - ambientLevelBoost;

                if (sender.baseNameToken != "JELLYFISH_BODY_NAME")
                {
                    args.attackSpeedMultAdd += Mathf.Clamp01(compensatedLevel / 200f) * 4f;
                }

                if (sender.isChampion)
                {
                    args.armorAdd += 3 * compensatedLevel;
                }
                else
                {
                    args.moveSpeedMultAdd += Mathf.Clamp01(compensatedLevel / 200f) * 2f;
                }
            }
        }
    }
}
