using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using RoR2;
using UnityEngine.AddressableAssets;
using RiskierRain.CoreModules;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public void ChangeSpawnlists()
        {
            //roost
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.DistantRoost);

#pragma warning disable CS0618 // It tells me its obsolete but its just easier to do it this way

            Helpers.AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.DistantRoost); //roost needed a loop boss and i couldnt think of anything better
            Helpers.AddNewMonsterToStage(DirectorCards.SolusTransporter, MonsterCategory.Minibosses, DirectorAPI.Stage.DistantRoost);

            //included an example of using it the right way though
            var monsterCardHolder = new DirectorCardHolder
            {
                Card = DirectorCards.Vulture,
                MonsterCategory = MonsterCategory.Minibosses
            };
            Helpers.AddNewMonsterToStage(monsterCardHolder, false, DirectorAPI.Stage.DistantRoost);

            //plains
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.TitanicPlains);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusInvalidator, DirectorAPI.Stage.TitanicPlains);


            //siphoned
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SiphonedForest);

            Helpers.AddNewMonsterToStage(DirectorCards.BlindVerminSnowy, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.Bison, MonsterCategory.Minibosses, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest); //what was i cooking

            //verdant
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.VerdantFalls);
            //viscous
            Helpers.AddNewMonsterToStage(DirectorCards.SolusScorcher, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ViscousFalls);

            //shabodes
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusProspector, DirectorAPI.Stage.ShatteredAbodes);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.ShatteredAbodes);

            Helpers.AddNewMonsterToStage(DirectorCards.Dunestrider, MonsterCategory.Champions, DirectorAPI.Stage.ShatteredAbodes);
            //impact
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.ShatteredAbodes);

            Helpers.AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.DisturbedImpact);
            Helpers.AddNewMonsterToStage(DirectorCards.Dunestrider, MonsterCategory.Champions, DirectorAPI.Stage.DisturbedImpact);


            //wetland
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.WetlandAspect);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusTransporter, DirectorAPI.Stage.WetlandAspect);

            Helpers.AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.WetlandAspect);
            Helpers.AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.WetlandAspect);
            //Helpers.AddNewMonsterToStage(DirectorCards.ImpOverlord, MonsterCategory.Champions, DirectorAPI.Stage.WetlandAspect);

            //aqwuaduct
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusInvalidator, DirectorAPI.Stage.AbandonedAqueduct);

            Helpers.AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.AbandonedAqueduct);
            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.AbandonedAqueduct);
            Helpers.AddNewMonsterToStage(DirectorCards.SolusScorcher, MonsterCategory.BasicMonsters, DirectorAPI.Stage.AbandonedAqueduct);

            //sanctuary
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.AphelianSanctuary);

            Helpers.AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.AphelianSanctuary);

            //precipice
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.PretendersPrecipice);

            Helpers.AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.PretendersPrecipice);
            Helpers.AddNewMonsterToStage(DirectorCards.SolusExtractor, MonsterCategory.BasicMonsters, DirectorAPI.Stage.PretendersPrecipice);
            Helpers.AddNewMonsterToStage(DirectorCards.SolusTransporter, MonsterCategory.Minibosses, DirectorAPI.Stage.PretendersPrecipice);

            //scorchewd acres
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.ScorchedAcres);

            Helpers.AddNewMonsterToStage(DirectorCards.Gup, MonsterCategory.Minibosses, DirectorAPI.Stage.ScorchedAcres);

            //rallypoint delta
            Helpers.AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.RallypointDelta);
            Helpers.AddNewMonsterToStage(DirectorCards.SolusInvalidator, MonsterCategory.Minibosses, DirectorAPI.Stage.RallypointDelta);

            //sulfur pools
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SulfurPools);

            Helpers.AddNewMonsterToStage(DirectorCards.Larva, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SulfurPools);
            Helpers.AddNewMonsterToStage(DirectorCards.ClayApothecary, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);
            Helpers.AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);

            //treeborn
            Helpers.AddNewMonsterToStage(DirectorCards.Grandparent, MonsterCategory.Champions, DirectorAPI.Stage.TreebornColony);
            //dieback
            Helpers.AddNewMonsterToStage(DirectorCards.Grandparent, MonsterCategory.Champions, DirectorAPI.Stage.GoldenDieback);

            //abyssal
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.SolusScorcher, DirectorAPI.Stage.AbyssalDepths);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitanAbyssalDepths, DirectorAPI.Stage.AbyssalDepths);

            //sirens call
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SirensCall);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Gup, DirectorAPI.Stage.SirensCall);

            Helpers.AddNewMonsterToStage(DirectorCards.SolusProspector, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SirensCall);

            //grove
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SunderedGrove);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.SunderedGrove);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SunderedGrove);

            Helpers.AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SunderedGrove);
            Helpers.AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);
            Helpers.AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);

            //conduit canyon
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ClayTemplar, DirectorAPI.Stage.ConduitCanyon);

            Helpers.AddNewMonsterToStage(DirectorCards.Imp, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ConduitCanyon);
            Helpers.AddNewMonsterToStage(DirectorCards.Bronzong, MonsterCategory.BasicMonsters, DirectorAPI.Stage.ConduitCanyon);

            //Sky meadow
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SkyMeadow);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SkyMeadow);
            //Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.XiConstruct, DirectorAPI.Stage.SkyMeadow);

            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SkyMeadow);
#pragma warning restore CS0618 // Type or member is obsolet
        }
        public void AddMonsterCardToSpawnlist(DirectorCardCategorySelection categorySelection, DirectorCard directorCard, MonsterCategory monsterCategory)
        {
            categorySelection.AddCard((int)monsterCategory, directorCard);
        }
    }
}
