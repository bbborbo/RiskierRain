using R2API;
using RoR2;
using RoR2.Navigation;
using SwanSongExtended.Components;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Events;
using UnityEngine.Networking;
using static SwanSongExtended.PersistentListeners;

namespace SwanSongExtended.Artifacts
{
    public class BonusPillarsArtifact : ArtifactBase<BonusPillarsArtifact>
    {
        public static InteractableSpawnCard[] allPillarSpawnCards;
        public static float pillarMonsterCredit = 700f; //700f
        public static float pillarGoldRewardCoefficient = 0.5f; //1
        public static int pillarsPerStage = 1;
        bool useBasicObjective = false;

        static string objectiveToken = "OBJECTIVE_BONUSPILLARS";
        static string objectiveDesc = "Charge the Pillar of Creation";

        public override string ArtifactName => "the Inventor";

        public override string ArtifactDescription => "Optional Pillars of Creation appear on every stage.";

        public override string ArtifactLangTokenName => "BONUSPILLARS";

        public override Sprite ArtifactSelectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/bonuspillars.png");

        public override Sprite ArtifactDeselectedIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/bonuspillarsoff.png");

        public override void Hooks()
        {

        }
        public override void Lang()
        {
            base.Lang();
            LanguageAPI.Add(objectiveToken, objectiveDesc);
        }
        public override void Init()
        {
            base.Init();

            allPillarSpawnCards = new InteractableSpawnCard[4] {
                CreatePillarSpawnCard(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryBlood_prefab),
                CreatePillarSpawnCard(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryDesign_prefab),
                CreatePillarSpawnCard(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatteryMass_prefab),
                CreatePillarSpawnCard(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon2.MoonBatterySoul_prefab)
            };

            InteractableSpawnCard CreatePillarSpawnCard(string addressablePath)
            {
                InteractableSpawnCard spawnCard = ScriptableObject.CreateInstance<InteractableSpawnCard>();

                GameObject prefab = Addressables.LoadAssetAsync<GameObject>(addressablePath).WaitForCompletion();
                string name = $"{prefab.name}StageOnly";

                prefab = prefab.InstantiateClone(name);

                PillarItemDropper dropper = prefab.GetComponent<PillarItemDropper>();
                dropper.shouldDropItem = true;
                dropper.shouldDropPotential = true;
                if (useBasicObjective)
                {
                    GenericObjectiveProvider objective = prefab.AddComponent<GenericObjectiveProvider>();
                    objective.objectiveToken = objectiveToken;
                }

                CombatDirector combatDirector = prefab.GetComponent<CombatDirector>();
                combatDirector.monsterCredit = pillarMonsterCredit;
                combatDirector.goldRewardCoefficient = pillarGoldRewardCoefficient;
                Addressables.LoadAssetAsync<DirectorCardCategorySelection>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_moon.dccsMoonMonsters_asset).Completed += 
                    (ctx) => combatDirector.monsterCards = ctx.Result;
                //combatDirector.monsterCards

                spawnCard.name = "isc" + name;

                spawnCard.prefab = prefab;
                spawnCard.sendOverNetwork = true;
                spawnCard.hullSize = HullClassification.BeetleQueen;
                spawnCard.nodeGraphType = MapNodeGroup.GraphType.Ground;
                spawnCard.requiredFlags = NodeFlags.TeleporterOK;
                spawnCard.forbiddenFlags = NodeFlags.NoChestSpawn;
                spawnCard.occupyPosition = true;

                return spawnCard;
            }
        }

        public override void OnArtifactDisabledServer()
        {
            RoR2.Stage.onServerStageBegin -= BonusPillarOnStageBegin;
        }

        public override void OnArtifactEnabledServer()
        {
            RoR2.Stage.onServerStageBegin += BonusPillarOnStageBegin;
        }

        private void BonusPillarOnStageBegin(Stage obj)
        {
            if(StormRunBehavior.GetStormType(obj.sceneDef) != StormType.None)
            {
                SpawnMoonPillars(Run.instance.stageRng);
            }
        }
        static void SpawnMoonPillars(Xoroshiro128Plus rng)
        {
            List<GameObject> createdPillarObjects = new List<GameObject>(pillarsPerStage);

            int pillarTypeCount = allPillarSpawnCards.Length;
            int[] pillarTypeSpawnCount = new int[pillarTypeCount];
            WeightedSelection<int> spawnCardSelection = new WeightedSelection<int>(pillarTypeCount);

            for (int i = 0; i < pillarsPerStage; i++)
            {
                spawnCardSelection.Clear();
                for (int j = 0; j < pillarTypeCount; j++)
                {
                    float weight = 1f - (pillarTypeSpawnCount[j] / (float)pillarsPerStage);
                    if (weight > 0f)
                    {
                        spawnCardSelection.AddChoice(j, weight);
                    }
                }

                DirectorPlacementRule placementRule = new DirectorPlacementRule
                {
                    placementMode = SceneInfo.instance && SceneInfo.instance.approximateMapBoundMesh ? DirectorPlacementRule.PlacementMode.RandomNormalized : DirectorPlacementRule.PlacementMode.Random
                };

                int pillarIndex = spawnCardSelection.Evaluate(rng.nextNormalizedFloat);
                DirectorSpawnRequest spawnRequest = new DirectorSpawnRequest(allPillarSpawnCards[pillarIndex], placementRule, rng);

                GameObject pillarObject = DirectorCore.instance.TrySpawnObject(spawnRequest);
                if (pillarObject)
                {
                    createdPillarObjects.Add(pillarObject);
                    pillarTypeSpawnCount[pillarIndex]++;
                }
            }
        }
    }
}
