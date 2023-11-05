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
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.DistantRoost);

            Helpers.AddNewMonsterToStage(DirectorCards.Vulture, MonsterCategory.Minibosses, DirectorAPI.Stage.DistantRoost);
            Helpers.AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.DistantRoost); //roost needed a loop boss and i couldnt think of anything better

            //plains
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.TitanicPlains);


            //siphoned
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SiphonedForest);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SiphonedForest);

            Helpers.AddNewMonsterToStage(DirectorCards.BlindVerminSnowy, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.Bison, MonsterCategory.Minibosses, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest);
            Helpers.AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SiphonedForest); //what was i cooking


            //wetland
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.WetlandAspect);

            Helpers.AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.WetlandAspect);
            Helpers.AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.WetlandAspect);
            Helpers.AddNewMonsterToStage(DirectorCards.ImpOverlord, MonsterCategory.Champions, DirectorAPI.Stage.WetlandAspect);

            //aqwuaduct
            Helpers.AddNewMonsterToStage(DirectorCards.ElderLemurian, MonsterCategory.Minibosses, DirectorAPI.Stage.AbandonedAqueduct);
            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.AbandonedAqueduct);

            //sanctuary
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.AphelianSanctuary);

            Helpers.AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.AphelianSanctuary);

            //scorchewd acres
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.ScorchedAcres);

            Helpers.AddNewMonsterToStage(DirectorCards.Gup, MonsterCategory.Minibosses, DirectorAPI.Stage.ScorchedAcres);

            //rallypoint delta
            Helpers.AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.RallypointDelta);

            //sulfur pools
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SulfurPools);

            Helpers.AddNewMonsterToStage(DirectorCards.Larva, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SulfurPools);
            Helpers.AddNewMonsterToStage(DirectorCards.ClayApothecary, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);
            Helpers.AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);

            //abyssal
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitanAbyssalDepths, DirectorAPI.Stage.AbyssalDepths);

            //sirewns call
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SirensCall);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Gup, DirectorAPI.Stage.SirensCall);

            //grove
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SunderedGrove);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.StoneTitan, DirectorAPI.Stage.SunderedGrove);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.WanderingVagrant, DirectorAPI.Stage.SunderedGrove);

            Helpers.AddNewMonsterToStage(DirectorCards.AlphaConstruct, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SunderedGrove);
            Helpers.AddNewMonsterToStage(DirectorCards.XiConstruct, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);
            Helpers.AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.SunderedGrove);

            //Sky meadow
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.LesserWisp, DirectorAPI.Stage.SkyMeadow);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.ElderLemurian, DirectorAPI.Stage.SkyMeadow);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.XiConstruct, DirectorAPI.Stage.SkyMeadow);

            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.SkyMeadow);
        }
        public void AddMonsterCardToSpawnlist(DirectorCardCategorySelection categorySelection, DirectorCard directorCard, MonsterCategory monsterCategory)
        {
            categorySelection.AddCard((int)monsterCategory, directorCard);
        }
    }
}
