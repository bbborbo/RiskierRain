using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using RoR2;
using UnityEngine.AddressableAssets;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {

        public void ChangeSpawnlists()
        {
            //get spawncards
            SpawnCards.Init();
           
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
            Helpers.AddNewMonsterToStage(DirectorCards.ImpOverlord, MonsterCategory.Champions, DirectorAPI.Stage.WetlandAspect);

            //aqwuaduct
            Helpers.AddNewMonsterToStage(DirectorCards.MagmaWorm, MonsterCategory.Champions, DirectorAPI.Stage.AbandonedAqueduct);

            //sanctuary
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.BeetleQueen, DirectorAPI.Stage.AphelianSanctuary);

            Helpers.AddNewMonsterToStage(DirectorCards.Parent, MonsterCategory.Minibosses, DirectorAPI.Stage.AphelianSanctuary);
            Helpers.AddNewMonsterToStage(DirectorCards.Grovetender, MonsterCategory.Champions, DirectorAPI.Stage.AphelianSanctuary);

            //scorchewd acres
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.ScorchedAcres);

            //rallypoint delta
            Helpers.AddNewMonsterToStage(DirectorCards.RoboBall, MonsterCategory.Champions, DirectorAPI.Stage.RallypointDelta);

            //sulfur pools
            Helpers.RemoveExistingMonsterFromStage(Helpers.MonsterNames.Beetle, DirectorAPI.Stage.SulfurPools);

            Helpers.AddNewMonsterToStage(DirectorCards.Larva, MonsterCategory.BasicMonsters, DirectorAPI.Stage.SulfurPools);
            Helpers.AddNewMonsterToStage(DirectorCards.ClayApothecary, MonsterCategory.Minibosses, DirectorAPI.Stage.SulfurPools);

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

    public static class SpawnCards
    {
        public static bool initialized = false;

        public static CharacterSpawnCard AlphaConstruct;

        public static CharacterSpawnCard Beetle;
        public static CharacterSpawnCard Lemurian;
        public static CharacterSpawnCard Larva;

        public static CharacterSpawnCard Wisp;
        public static CharacterSpawnCard Jellyfish;
        public static CharacterSpawnCard BlindPestSnowy;
        public static CharacterSpawnCard BlindVerminSnowy;

        public static CharacterSpawnCard Imp;
        public static CharacterSpawnCard Vulture;

        public static CharacterSpawnCard Golem;
        public static CharacterSpawnCard BeetleGuard;
        public static CharacterSpawnCard Mushrum;
        public static CharacterSpawnCard Bison;
        public static CharacterSpawnCard ClayApothecary;

        public static CharacterSpawnCard ElderLemurian;
        public static CharacterSpawnCard Parent;

        public static CharacterSpawnCard Bronzong;
        public static CharacterSpawnCard GreaterWisp;

        public static CharacterSpawnCard TitanBlackBeach;
        public static CharacterSpawnCard TitanDampCave;
        public static CharacterSpawnCard TitanGolemPlains;
        public static CharacterSpawnCard TitanGooLake;

        public static CharacterSpawnCard Vagrant;
        public static CharacterSpawnCard BeetleQueen;
        public static CharacterSpawnCard Dunestrider;

        public static CharacterSpawnCard MagmaWorm;
        public static CharacterSpawnCard ImpOverlord;
        public static CharacterSpawnCard Grovetender;
        public static CharacterSpawnCard RoboBall;
        public static CharacterSpawnCard XiConstruct;

        public static CharacterSpawnCard Reminder;

        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            AlphaConstruct = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMinorConstruct.asset").WaitForCompletion();

            Beetle = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetle");
            Lemurian = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csclemurian");

            Wisp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csclesserwisp");
            Jellyfish = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscjellyfish");

            Imp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscimp");
            Vulture = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscvulture");

            Golem = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Golem/cscGolem.asset").WaitForCompletion();
            BeetleGuard = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetleguard");
            Mushrum = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscminimushroom");
            Bison = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Bison/cscBison.asset").WaitForCompletion();

            ElderLemurian = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/LemurianBruiser/cscLemurianBruiser.asset").WaitForCompletion();
            Parent = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/Base/Parent/cscParent.asset").WaitForCompletion();


            Bronzong = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbell");
            GreaterWisp = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscgreaterwisp");

            TitanBlackBeach = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitanblackbeach");
            TitanDampCave = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitandampcave");
            TitanGolemPlains = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitangolemplains");
            TitanGooLake = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/csctitangoolake");

            Vagrant = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscvagrant");
            BeetleQueen = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscbeetlequeen");
            Dunestrider = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscclayboss");

            MagmaWorm = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscmagmaworm");
            ImpOverlord = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscimpboss");
            Grovetender = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscgravekeeper");
            RoboBall = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscroboballboss");

            Reminder = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscelectricworm");

            BlindVerminSnowy = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/Vermin/cscVerminSnowy.asset").WaitForCompletion();
            BlindPestSnowy = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/FlyingVermin/cscFlyingVerminSnowy.asset").WaitForCompletion();
            ClayApothecary = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/ClayGrenadier/cscClayGrenadier.asset").WaitForCompletion();
            Larva = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/AcidLarva/cscAcidLarva.asset").WaitForCompletion();

            XiConstruct = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/MajorAndMinorConstruct/cscMegaConstruct.asset").WaitForCompletion();

            DirectorCards.Init();
        }
    }
    public static class DirectorCards
    {
        public static bool initialized = false;

        public static DirectorCard AlphaConstruct;
        public static DirectorCard AlphaConstructNear;

        public static DirectorCard Beetle;
        public static DirectorCard Lemurian;
        public static DirectorCard Larva;

        public static DirectorCard Wisp;
        public static DirectorCard Jellyfish;
        public static DirectorCard BlindPestSnowy;
        public static DirectorCard BlindVerminSnowy;

        public static DirectorCard Imp;
        public static DirectorCard Vulture;

        public static DirectorCard Golem;
        public static DirectorCard BeetleGuard;
        public static DirectorCard Mushrum;
        public static DirectorCard ClayApothecary;
        public static DirectorCard Bison;
        public static DirectorCard BisonLoop;

        public static DirectorCard ElderLemurian;
        public static DirectorCard Parent;

        public static DirectorCard Bronzong;
        public static DirectorCard GreaterWisp;

        public static DirectorCard TitanBlackBeach;
        public static DirectorCard TitanDampCave;
        public static DirectorCard TitanGolemPlains;
        public static DirectorCard TitanGooLake;

        public static DirectorCard Vagrant;
        public static DirectorCard BeetleQueen;
        public static DirectorCard Dunestrider;

        public static DirectorCard MagmaWorm;
        public static DirectorCard ImpOverlord;
        public static DirectorCard Grovetender;
        public static DirectorCard RoboBall;

        public static DirectorCard Reminder;

        public static DirectorCard XiConstruct;

        public static DirectorCard LunarGolemSkyMeadow;
        public static DirectorCard LunarGolemSkyMeadowBasic;

        public static bool logCardInfo = false;
        public static void Init()
        {
            if (initialized) return;
            initialized = true;

            AlphaConstruct = BuildDirectorCard(SpawnCards.AlphaConstruct, 1, 1, DirectorCore.MonsterSpawnDistance.Standard);
            AlphaConstructNear = BuildDirectorCard(SpawnCards.AlphaConstruct, 1, 1, DirectorCore.MonsterSpawnDistance.Close);

            Beetle = BuildDirectorCard(SpawnCards.Beetle);
            Lemurian = BuildDirectorCard(SpawnCards.Lemurian);
            Larva = BuildDirectorCard(SpawnCards.Larva);

            Wisp = BuildDirectorCard(SpawnCards.Wisp);
            Jellyfish = BuildDirectorCard(SpawnCards.Jellyfish, 1, 0, DirectorCore.MonsterSpawnDistance.Far);
            BlindPestSnowy = BuildDirectorCard(SpawnCards.BlindPestSnowy);
            BlindVerminSnowy = BuildDirectorCard(SpawnCards.BlindVerminSnowy);

            Imp = BuildDirectorCard(SpawnCards.Imp);
            Vulture = BuildDirectorCard(SpawnCards.Vulture, 1, 3, DirectorCore.MonsterSpawnDistance.Standard);

            Golem = BuildDirectorCard(SpawnCards.Golem);
            BeetleGuard = BuildDirectorCard(SpawnCards.BeetleGuard);
            Mushrum = BuildDirectorCard(SpawnCards.Mushrum); //These are considered basic monsters in Vanilla, but they fit all the criteria of a miniboss enemy.
            ClayApothecary = BuildDirectorCard(SpawnCards.ClayApothecary);
            Bison = BuildDirectorCard(SpawnCards.Bison, 1, 2, DirectorCore.MonsterSpawnDistance.Standard);
            Bronzong = BuildDirectorCard(SpawnCards.Bronzong);  //Basic Monster on SkyMeadow
            GreaterWisp = BuildDirectorCard(SpawnCards.GreaterWisp);

            ElderLemurian = BuildDirectorCard(SpawnCards.ElderLemurian, 1, 3, DirectorCore.MonsterSpawnDistance.Standard);
            Parent = BuildDirectorCard(SpawnCards.Parent, 1, 3, DirectorCore.MonsterSpawnDistance.Standard);

            TitanBlackBeach = BuildDirectorCard(SpawnCards.TitanBlackBeach);
            TitanDampCave = BuildDirectorCard(SpawnCards.TitanDampCave);
            TitanGolemPlains = BuildDirectorCard(SpawnCards.TitanGolemPlains);
            TitanGooLake = BuildDirectorCard(SpawnCards.TitanGooLake);

            Vagrant = BuildDirectorCard(SpawnCards.Vagrant);
            BeetleQueen = BuildDirectorCard(SpawnCards.BeetleQueen);
            Dunestrider = BuildDirectorCard(SpawnCards.Dunestrider);

            ImpOverlord = BuildDirectorCard(SpawnCards.ImpOverlord, 1, 1, DirectorCore.MonsterSpawnDistance.Standard);
            Grovetender = BuildDirectorCard(SpawnCards.Grovetender);
            RoboBall = BuildDirectorCard(SpawnCards.RoboBall, 1 , 3, DirectorCore.MonsterSpawnDistance.Standard);
            MagmaWorm = BuildDirectorCard(SpawnCards.MagmaWorm, 1, 2, DirectorCore.MonsterSpawnDistance.Standard);

            Reminder = BuildDirectorCard(SpawnCards.Reminder);

            XiConstruct = BuildDirectorCard(SpawnCards.XiConstruct, 1, 2, DirectorCore.MonsterSpawnDistance.Standard);
        }

        public static DirectorCard BuildDirectorCard(CharacterSpawnCard spawnCard)
        {
            return BuildDirectorCard(spawnCard, 1, 0, DirectorCore.MonsterSpawnDistance.Standard);
        }

        public static DirectorCard BuildDirectorCard(CharacterSpawnCard spawnCard, int weight, int minStages, DirectorCore.MonsterSpawnDistance spawnDistance)
        {
            DirectorCard dc = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = weight,
                preventOverhead = false,
                minimumStageCompletions = minStages,
                spawnDistance = spawnDistance
            };
            return dc;
        }
        public static DirectorCard BuildDirectorCard(InteractableSpawnCard spawnCard, int weight, int minStages)
        {
            DirectorCard dc = new DirectorCard
            {
                spawnCard = spawnCard,
                selectionWeight = weight,
                preventOverhead = false,
                minimumStageCompletions = minStages
            };
            return dc;
        }
    }
}
