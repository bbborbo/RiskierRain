using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using RoR2;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {

        public void ChangeSpawnlists()
        {
           // //get spawncards
           // SpawnCard verminSnowyCard = LegacyResourcesAPI.Load<SpawnCard>("RoR2/DLC1/Vermin/cscVerminSnowy.asset");
           // DirectorCardCategorySelection snowyForestDCCS = LegacyResourcesAPI.Load<DirectorCardCategorySelection>("RoR2/DLC1/snowyforest/dccsSnowyForestMonstersDLC1.asset");
           //
           // DirectorCard verminSnowyDirectorCard = new DirectorCard
           // {
           //     spawnCard = verminSnowyCard,
           //     selectionWeight = 1000,
           //     spawnDistance = DirectorCore.MonsterSpawnDistance.Close,
           //     preventOverhead = true,
           //     minimumStageCompletions = 0,                
           // };

            //siphoned
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SiphonedForest);
            // Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SiphonedForest);
            // Helpers.AddNewMonsterToStage(verminSnowyDirectorCard, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SiphonedForest);

            //abyssal
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitanAbyssalDepths, DirectorAPI.Stage.AbyssalDepths);


        }
    }
}
