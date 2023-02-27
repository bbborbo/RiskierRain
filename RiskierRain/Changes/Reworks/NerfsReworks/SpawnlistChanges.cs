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

            //roost
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.DistantRoost);

            //plains
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.TitanicPlains);

            //siphoned
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SiphonedForest);
            // Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SiphonedForest);
            // Helpers.AddNewMonsterToStage(verminSnowyDirectorCard, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SiphonedForest);

            //wetland
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.WetlandAspect);

            //sanctuary
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.AphelianSanctuary);

            //

            //abyssal
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitanAbyssalDepths, DirectorAPI.Stage.AbyssalDepths);

            //grove
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.SunderedGrove);

            //Sky meadow
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.XiConstruct, DirectorAPI.Stage.SkyMeadow);


        }
    }
}
