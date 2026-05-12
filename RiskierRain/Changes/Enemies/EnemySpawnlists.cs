using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using RoR2;
using UnityEngine.AddressableAssets;
using RiskierRain.CoreModules;

namespace RiskierRain.Changes
{
    public static partial class EnemyChanges
    {
        static void AddNewMonsterToStage(DirectorCard card, MonsterCategory category, DirectorAPI.Stage stage, bool addToFamilies = false)
        {
            //included an example of using it the right way though
            var monsterCardHolder = new DirectorCardHolder
            {
                Card = card,
                MonsterCategory = category
            };
            Helpers.AddNewMonsterToStage(monsterCardHolder, addToFamilies, stage);
        }

        static void AddMonsterCardToSpawnlist(DirectorCardCategorySelection categorySelection, DirectorCard directorCard, MonsterCategory monsterCategory)
        {
            categorySelection.AddCard((int)monsterCategory, directorCard);
        }

        public static void ChangeSpawnlists()
        {
            ChangeSpawnlistRoost();
            ChangeSpawnlistPlains();
            ChangeSpawnlistSiphoned();
            ChangeSpawnlistFalls();
            ChangeSpawnlistAbodes();
            ChangeSpawnlistWetland();
            ChangeSpawnlistAqueduct();
            ChangeSpawnlistSanctuary();
            ChangeSpawnlistPrecipice();
            ChangeSpawnlistScorched();
            ChangeSpawnlistRallypoint();
            ChangeSpawnlistSulfur();
            ChangeSpawnlistTreeborn();
            ChangeSpawnlistAbyssal();
            ChangeSpawnlistSirens();
            ChangeSpawnlistGrove();
            ChangeSpawnlistConduit();
            ChangeSpawnlistMeadow();
        }

        private static void ChangeSpawnlistMeadow()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SkyMeadow);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SkyMeadow);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.XiConstruct, DirectorAPI.Stage.SkyMeadow);

            AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SkyMeadow);
        }
        private static void ChangeSpawnlistConduit()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ClayTemplar, DirectorAPI.Stage.ConduitCanyon);

            AddNewMonsterToStage(DirectorCards.Imp, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ConduitCanyon);
            AddNewMonsterToStage(DirectorCards.Bronzong, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ConduitCanyon);
        }
        private static void ChangeSpawnlistGrove()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SunderedGrove);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.SunderedGrove);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SunderedGrove);

            AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SunderedGrove);
            AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);
            AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);
        }
        private static void ChangeSpawnlistSirens()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SirensCall);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Gup, DirectorAPI.Stage.SirensCall);

            AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SirensCall);
        }
        private static void ChangeSpawnlistAbyssal()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.AbyssalDepths);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitanAbyssalDepths, DirectorAPI.Stage.AbyssalDepths);
        }

        private static void ChangeSpawnlistTreeborn()
        {
            AddNewMonsterToStage(DirectorCards.Grandparent, MonsterCategory.Champions, DirectorAPI.Stage.TreebornColony);
            //dieback
            AddNewMonsterToStage(DirectorCards.Grandparent, MonsterCategory.Champions, DirectorAPI.Stage.GoldenDieback);
        }
        private static void ChangeSpawnlistSulfur()
        {

            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SulfurPools);

            AddNewMonsterToStage(DirectorCards.Larva, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SulfurPools);
            AddNewMonsterToStage(DirectorCards.ClayApothecary, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);
            AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);
        }
        private static void ChangeSpawnlistRallypoint()
        {
            AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.RallypointDelta);
            AddNewMonsterToStage(DirectorCards.SolusInvalidator, MonsterCategory.Minibosses, DirectorAPI.Stage.RallypointDelta);
        }
        private static void ChangeSpawnlistScorched()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.ScorchedAcres);

            AddNewMonsterToStage(DirectorCards.Gup, MonsterCategory.Minibosses, DirectorAPI.Stage.ScorchedAcres);
        }
        private static void ChangeSpawnlistPrecipice()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.PretendersPrecipice);

            AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.PretendersPrecipice);
            AddNewMonsterToStage(DirectorCards.SolusExtractor, MonsterCategory.BasicMonsters, DirectorAPI.Stage.PretendersPrecipice);
            AddNewMonsterToStage(DirectorCards.SolusTransporter, MonsterCategory.Minibosses, DirectorAPI.Stage.PretendersPrecipice);
        }
        private static void ChangeSpawnlistSanctuary()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.AphelianSanctuary);

            AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.AphelianSanctuary);
            AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.AphelianSanctuary);
        }
        private static void ChangeSpawnlistAqueduct()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusInvalidator, DirectorAPI.Stage.AbandonedAqueduct);

            AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.AbandonedAqueduct);
            AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.AbandonedAqueduct);
            AddNewMonsterToStage(DirectorCards.SolusScorcher, MonsterCategory.BasicMonsters, DirectorAPI.Stage.AbandonedAqueduct);
        }
        private static void ChangeSpawnlistWetland()
        {

            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.WetlandAspect);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusTransporter, DirectorAPI.Stage.WetlandAspect);

            AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.WetlandAspect);
            AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.WetlandAspect);
            //Helpers.AddNewMonsterToStage(DirectorCards.ImpOverlord, MonsterCategory.Champions, DirectorAPI.Stage.WetlandAspect);
        }
        private static void ChangeSpawnlistAbodes()
        {

            //shabodes
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusProspector, DirectorAPI.Stage.ShatteredAbodes);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.ShatteredAbodes);

            AddNewMonsterToStage(DirectorCards.Dunestrider, MonsterCategory.Champions, DirectorAPI.Stage.ShatteredAbodes);
            //disturbed impact
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.DisturbedImpact);

            AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.DisturbedImpact);
            AddNewMonsterToStage(DirectorCards.Dunestrider, MonsterCategory.Champions, DirectorAPI.Stage.DisturbedImpact);
        }
        private static void ChangeSpawnlistFalls()
        {

            //verdant falls
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.VerdantFalls);
            //viscous falls
            AddNewMonsterToStage(DirectorCards.SolusScorcher, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ViscousFalls);
        }
        private static void ChangeSpawnlistSiphoned()
        {
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SiphonedForest);

            AddNewMonsterToStage(DirectorCards.BlindVerminSnowy, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SiphonedForest);
            AddNewMonsterToStage(DirectorCards.Bison, MonsterCategory.Minibosses, DirectorAPI.Stage.SiphonedForest);
            AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest);
            AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest); //what was i cooking
        }
        private static void ChangeSpawnlistPlains()
        {

            //RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.TitanicPlains);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusInvalidator, DirectorAPI.Stage.TitanicPlains);
        }
        private static void ChangeSpawnlistRoost()
        {

            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.DistantRoost);

            AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.DistantRoost); //roost needed a loop boss and i couldnt think of anything better
            AddNewMonsterToStage(DirectorCards.SolusTransporter, MonsterCategory.Minibosses, DirectorAPI.Stage.DistantRoost);

            AddNewMonsterToStage(DirectorCards.Vulture, MonsterCategory.Minibosses, DirectorAPI.Stage.DistantRoost, false);
        }
    }
}
