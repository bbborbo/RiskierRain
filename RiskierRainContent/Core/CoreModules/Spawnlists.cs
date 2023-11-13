using System;
using System.Collections.Generic;
using System.Text;
using BepInEx;
using R2API;
using static R2API.DirectorAPI;
using RoR2;
using UnityEngine.AddressableAssets;
using RiskierRainContent.CoreModules;

namespace RiskierRainContent.CoreModules
{
    public class Spawnlists : CoreModule
    {
        public override void Init()
        {
            //get spawncards
            SpawnCards.Init();
        }
        public static void AddMonsterCardToSpawnlist(DirectorCardCategorySelection categorySelection, DirectorCard directorCard, MonsterCategory monsterCategory)
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
        public static CharacterSpawnCard Gup;

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

        public static CharacterSpawnCard OverloadingWorm;

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
            Gup = Addressables.LoadAssetAsync<CharacterSpawnCard>("RoR2/DLC1/Gup/cscGupBody.asset").WaitForCompletion();

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

            OverloadingWorm = LegacyResourcesAPI.Load<CharacterSpawnCard>("spawncards/characterspawncards/cscelectricworm");

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
        public static DirectorCard Gup;

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

        public static DirectorCard OverloadingWorm;

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
            Gup = BuildDirectorCard(SpawnCards.Gup, 1, 3, DirectorCore.MonsterSpawnDistance.Standard);

            TitanBlackBeach = BuildDirectorCard(SpawnCards.TitanBlackBeach);
            TitanDampCave = BuildDirectorCard(SpawnCards.TitanDampCave);
            TitanGolemPlains = BuildDirectorCard(SpawnCards.TitanGolemPlains);
            TitanGooLake = BuildDirectorCard(SpawnCards.TitanGooLake);

            Vagrant = BuildDirectorCard(SpawnCards.Vagrant);
            BeetleQueen = BuildDirectorCard(SpawnCards.BeetleQueen);
            Dunestrider = BuildDirectorCard(SpawnCards.Dunestrider);

            ImpOverlord = BuildDirectorCard(SpawnCards.ImpOverlord, 1, 1, DirectorCore.MonsterSpawnDistance.Standard);
            Grovetender = BuildDirectorCard(SpawnCards.Grovetender);
            RoboBall = BuildDirectorCard(SpawnCards.RoboBall, 1, 3, DirectorCore.MonsterSpawnDistance.Standard);
            MagmaWorm = BuildDirectorCard(SpawnCards.MagmaWorm, 1, 2, DirectorCore.MonsterSpawnDistance.Standard);

            OverloadingWorm = BuildDirectorCard(SpawnCards.OverloadingWorm);

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
