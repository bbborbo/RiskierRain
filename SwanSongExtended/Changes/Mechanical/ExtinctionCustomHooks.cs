using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
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
            $"<style=cStack>\n\n>Player Health Regeneration: <style=cArtifact>-40%</style></style> ";
        public static string extinctionDescStartingDifficulty =
            $"<style=cStack>\n>Starting Difficulty: <style=cArtifact>Very Hard</style></style>";
        public static string extinctionDescExtra =
            $"<style=cStack>\n>Difficulty Scaling: <style=cArtifact>+100%</style>" +
            $"\n>Tier 2 Elites appear starting on <style=cArtifact>Stage 4</style>" +
            $"\nTeleporter Visuals: <style=cArtifact>OFF</style>" +
            $"\n>Enemies gain <style=cArtifact>unique scaling</style></style>";
        public static string extinctionDesc =
            extinctionDescBase + extinctionDescExtra;

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

            DifficultyAPI.AddDifficulty(difficultyDefExtinction);

            LanguageAPI.Add(difficultyToken + "_NAME", extinctionName);
            LanguageAPI.Add(difficultyToken + "_DESC", extinctionDesc);

            RoR2Application.onLoadFinished += ExtinctionCustomHooks;
        }

        void ExtinctionCustomHooks()
        {
            if (is2R4RLoaded)
                return;

            LanguageAPI.Add(difficultyToken + "_DESC", extinctionDescBase + extinctionDescStartingDifficulty + extinctionDescExtra);

            foreach (CombatDirector.EliteTierDef etd in EliteAPI.VanillaEliteTiers)//CombatDirector.eliteTiers)
            {
                //Debug.Log(etd.eliteTypes[0].name);
                if (etd.eliteTypes[0] == RoR2Content.Elites.Poison || etd.eliteTypes[0] == RoR2Content.Elites.Haunted)
                {
                    etd.isAvailable = (SpawnCard.EliteRules rules) => Run.instance.stageClearCount >= 5
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == difficultyIndexExtinction);
                }
            }

            On.RoR2.TeleporterInteraction.BaseTeleporterState.OnEnter += TeleporterParticleScale;
            GetStatCoefficients += this.MonsoonPlusStatBuffs2;
            On.RoR2.Run.RecalculateDifficultyCoefficentInternal += DifficultyCoefficientChanges;
            IL.RoR2.UI.DifficultyBarController.DoBarUpdates += CorrectDifficultyBar;
        }

        private void CorrectDifficultyBar(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<Run>("get_ambientLevel"));
            if (!b)
            {
                DebugBreakpoint(nameof(CorrectDifficultyBar));
                return;
            }
            c.EmitDelegate<Func<float, float>>((levelIn) =>
            {
                if (Run.instance.selectedDifficulty != difficultyIndexExtinction)
                    return levelIn;
                return levelIn + 3f;
            });
        }
        private void TeleporterParticleScale(On.RoR2.TeleporterInteraction.BaseTeleporterState.orig_OnEnter orig, RoR2.TeleporterInteraction.BaseTeleporterState self)
        {
            orig(self);

            if (Run.instance.selectedDifficulty != difficultyIndexExtinction)
                return;

            TeleporterInteraction component = self.GetComponent<TeleporterInteraction>();
            bool flag5 = component && component.modelChildLocator;
            if (flag5)
            {
                Transform transform = component.transform.Find("TeleporterBaseMesh/BuiltInEffects/PassiveParticle, Sphere");
                transform.gameObject.SetActive(false);
            }
        }
        private void MonsoonPlusStatBuffs2(CharacterBody sender, StatHookEventArgs args)
        {
            DifficultyIndex selectedDifficulty = Run.instance.selectedDifficulty;
            if (selectedDifficulty != difficultyIndexExtinction)
                return;
            float ambientLevelBoost = eclipseDifficultyBoost;
            if (sender.teamComponent.teamIndex != TeamIndex.Player)
            {
                if (selectedDifficulty >= DifficultyIndex.Hard)
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

        private void DifficultyCoefficientChanges(On.RoR2.Run.orig_RecalculateDifficultyCoefficentInternal orig, Run self)
        {
            if (self.selectedDifficulty != difficultyIndexExtinction)
            {
                orig(self);
                return;
            }
            float runTimerMinutes = self.GetRunStopwatch() * 0.016666668f;
            int stageClearCount = self.stageClearCount;

            float difficultyCoefficient = GetDifficultyCoefficient(self, runTimerMinutes, stageClearCount, out float playerBaseFactor);
            float difficultyFactor = GetScalingValueForDifficulty(self.selectedDifficulty) - 1;//GetAmbientLevelBoost() / 2;

            //difficulty coefficient used for interactable costs and etc
            self.difficultyCoefficient = difficultyCoefficient;
            //difficulty coefficient used for enemy spawns
            self.compensatedDifficultyCoefficient = difficultyCoefficient + difficultyFactor;
            self.oneOverCompensatedDifficultyCoefficientSquared = 1 / (self.compensatedDifficultyCoefficient * self.compensatedDifficultyCoefficient);
            self.ambientLevel = Mathf.Min(1f + eclipseDifficultyBoost + (3f * (difficultyCoefficient - playerBaseFactor)), (float)Run.ambientLevelCap);

            int ambientLevelFloorLast = self.ambientLevelFloor;
            self.ambientLevelFloor = Mathf.FloorToInt(self.ambientLevel);
            if (ambientLevelFloorLast != self.ambientLevelFloor && ambientLevelFloorLast != 0 && self.ambientLevelFloor > ambientLevelFloorLast)
            {
                self.OnAmbientLevelUp();
            }
        }
        public static float GetScalingValueForDifficulty(DifficultyIndex difficulty)
        {
            DifficultyDef difficultyDef = DifficultyCatalog.GetDifficultyDef(difficulty);
            float scalingValue = difficultyDef.scalingValue;
            return scalingValue;
        }
        public static float GetDifficultyCoefficient(Run run, float timeInMinutes, int stageClearCount, out float playerBaseFactor)
        {
            float scalingValue = GetScalingValueForDifficulty(run.selectedDifficulty);
            float baseScalingFactor = 0.0506f * 1;

            float timeFactor = GetTimeDifficultyFactor(timeInMinutes, scalingValue);
            float stageFactor = GetStageDifficultyFactor(stageClearCount);

            playerBaseFactor = 1 + 0.3f * (run.participatingPlayerCount - 1);
            float playerScaleFactor = Mathf.Pow(run.participatingPlayerCount, 0.3f);
            float scalingFactor = baseScalingFactor * scalingValue * playerScaleFactor;

            return (playerBaseFactor + scalingFactor * timeInMinutes) * timeFactor * stageFactor;

            float GetTimeDifficultyFactor(float timeInMinutes, float scalingValue)
            {
                float timeFactor = Mathf.Pow(1 + (0 * scalingValue), timeInMinutes);
                return timeFactor;
            }
            float GetStageDifficultyFactor(int stageClearCount)
            {
                float stageFactor = Mathf.Pow(1.15f, (float)stageClearCount);

                int totalLoops = Mathf.FloorToInt((float)stageClearCount / 5);
                if (stageClearCount % 5 <= 1 && Stage.instance && SceneCatalog.GetSceneDefForCurrentScene().isFinalStage)
                    totalLoops -= 1;
                float loopFactor = Mathf.Pow(1, totalLoops);

                return stageFactor * loopFactor;
            }
        }
    }
}
