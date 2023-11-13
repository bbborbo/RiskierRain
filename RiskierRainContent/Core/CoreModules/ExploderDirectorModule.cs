using EntityStates.BeetleQueenMonster;
using EntityStates.Missions.BrotherEncounter;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.CoreModules
{
    class ExploderDirectorModule : CoreModule
    {
        public static SpawnCard exploderCard = Resources.Load<SpawnCard>("spawncards/characterspawncards/cscLunarExploder");
        List<DirectorCard> exploderCards = new List<DirectorCard>();
        public static ExploderDirector exploderDirector;

        internal static float phaseTwoRate = 0f;
        internal static float phaseThreeRate = 1f;
        public override void Init()
        {
            //DirectorAPI.MonsterActions += GetExploderCard;

            On.EntityStates.Missions.BrotherEncounter.Phase1.OnEnter += InitializeExploderDirector;
            On.EntityStates.Missions.BrotherEncounter.Phase2.OnEnter += DisableExploderDirector;
            On.EntityStates.Missions.BrotherEncounter.Phase3.OnEnter += RestoreExploderDirector;
            On.EntityStates.Missions.BrotherEncounter.Phase4.OnEnter += KillExploderDirector;
        }

        private void GetExploderCard(List<DirectorAPI.DirectorCardHolder> cardList, DirectorAPI.StageInfo currentStage)
        {
            Debug.Log("jsd fjsn");
            if(currentStage.stage == DirectorAPI.Stage.Commencement)
            {
                List<DirectorAPI.DirectorCardHolder> removeList = new List<DirectorAPI.DirectorCardHolder>();
                foreach (DirectorAPI.DirectorCardHolder dc in cardList)
                {
                    if(dc.MonsterCategory == DirectorAPI.MonsterCategory.BasicMonsters)
                    {
                        DirectorCard card = dc.Card;
                        SpawnCard spawnCard = card.spawnCard;
                        GameObject cardPrefab = spawnCard.prefab;
                        if(spawnCard.name == "cscLunarExploder")
                        {
                            exploderCards.Add(card);
                        }
                    }
                }
                foreach (DirectorAPI.DirectorCardHolder dc in removeList)
                {
                    cardList.Remove(dc);
                }
            }
        }

        private void InitializeExploderDirector(On.EntityStates.Missions.BrotherEncounter.Phase1.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase1 self)
        {
            orig(self);
            if(exploderDirector == null)
            {
                GameObject ExploderDirectorObject = new GameObject("CommencementPerfected_ChimeraExploderDirector_OrTheCPCEDForShort");
                ExploderDirectorObject.transform.position = new Vector3(-88f, 491.1f, 0f);
                exploderDirector = ExploderDirectorObject.AddComponent<ExploderDirector>();
                exploderDirector.baseState = (BrotherEncounterBaseState)self;
            }
        }

        private void DisableExploderDirector(On.EntityStates.Missions.BrotherEncounter.Phase2.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase2 self)
        {
            orig(self);
            exploderDirector.SetDirectorRate(phaseTwoRate);
        }

        private void RestoreExploderDirector(On.EntityStates.Missions.BrotherEncounter.Phase3.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase3 self)
        {
            orig(self);
            exploderDirector.SetDirectorRate(phaseThreeRate);
        }

        private void KillExploderDirector(On.EntityStates.Missions.BrotherEncounter.Phase4.orig_OnEnter orig, EntityStates.Missions.BrotherEncounter.Phase4 self)
        {
            orig(self);
            UnityEngine.Object.Destroy(exploderDirector.gameObject);
        }
    }

    public class ExploderDirector : MonoBehaviour
    {
        public void SetDirectorRate(float rate)
        {
            if (rate <= 0)
            {
                exploderSpawnTimer = 0;
                spawnRateModifier = 0;
                isActive = false;
            }
            else 
            {
                isActive = true;
                spawnRateModifier = rate;
                spawnFrequency = baseSpawnFrequency / spawnRateModifier;
            }

        }

        internal BrotherEncounterBaseState baseState;
        bool isActive = false;
        static float baseSpawnFrequency = 20f; //seconds it takes to spawn a round of exploders, assuming spawnRateModifier is 1
        static float spawnRateModifier = 1f; //how fast it reaches the spawn frequency threshold
        float spawnFrequency;
        float exploderSpawnTimer = 0f;

        public const float rampHeight = 524.3f;
        public List<ExploderSpawnNode> SpawnNodes = new List<ExploderSpawnNode>();


        void Start()
        {
            if (baseState != null)
            {
                CreateSpawnNodes();
                SetDirectorRate(1f);
                isActive = true;
            }
            else
            {
                Destroy(this);
            }
        }

        void CreateSpawnNodes()
        {
            if (!NetworkServer.active)
                return;

            Vector3[] SpawnLocations = new Vector3[4]
            {
                new Vector3(65f, rampHeight, 155f),
                new Vector3(65, rampHeight, -145),
                new Vector3(-240, rampHeight, 155),
                new Vector3(-240, rampHeight, -145)
            };
            for(int i = 0; i < SpawnLocations.Length; i++)
            {
                GameObject node = new GameObject($"CommencementPerfected_ExploderSpawnNode_{i}");
                node.transform.position = SpawnLocations[i];
                node.transform.parent = this.transform;
                ExploderSpawnNode spawnNode = node.AddComponent<ExploderSpawnNode>();
                SpawnNodes.Add(spawnNode);
            }
        }

        void FixedUpdate()
        {
            if(NetworkServer.active && spawnFrequency != 0)
            {
                //Debug.Log(exploderSpawnTimer);
                exploderSpawnTimer += Time.fixedDeltaTime * spawnRateModifier;
                while (exploderSpawnTimer > spawnFrequency)
                {
                    exploderSpawnTimer -= spawnFrequency;

                    //start spawning exploders
                    //int spawnCount = Run.instance.livingPlayerCount;

                    List<CharacterMaster> playersList = new List<CharacterMaster>();
                    foreach (PlayerCharacterMasterController player in PlayerCharacterMasterController.instances)
                    {
                        if (player.body.healthComponent.alive)
                        {
                            playersList.Add(player.master);
                        }
                    }

                    StartExploderWaveStacked(playersList.Count, playersList);
                }
            }
            else
            {
                Debug.Log("uuu");
            }
            if (baseState.outer.destroying)
            {
                SetDirectorRate(0);
                Debug.Log("Destroying Exploder Director! Ahhhhhh!!!!!!");
                Destroy(this);
            }
        }

        #region begin wave
        private void StartExploderWaveStacked(int spawnCount, List<CharacterMaster> playersList) //queues exploder spawns at the point closest to each player
        {
            for(int i = 0; i < playersList.Count; i++)
            {
                CharacterMaster currentPlayer = playersList[i];
                Vector3 playerPosition = currentPlayer.GetBody().transform.position;

                float closestNodeDistance = Mathf.Infinity;
                ExploderSpawnNode closestAvailableNode = null;
                foreach(ExploderSpawnNode currentNode in SpawnNodes)
                {
                    float currentDistance = Vector3.Distance(currentNode.transform.position, playerPosition);
                    //Debug.Log(currentDistance);
                    Vector2 searchRange = Vector2.zero;
                    if (currentDistance < closestNodeDistance)
                    {
                        closestAvailableNode = currentNode;
                        closestNodeDistance = currentDistance;
                    }
                }

                int remainingPlayersToSpawnFrom = playersList.Count - i;
                int spawnsForThisPlayer = spawnCount / remainingPlayersToSpawnFrom;
                if (spawnCount % remainingPlayersToSpawnFrom != 0)
                {
                    spawnsForThisPlayer = Mathf.CeilToInt(spawnsForThisPlayer);
                }

                if(closestAvailableNode != null && spawnsForThisPlayer > 0)
                {
                    closestAvailableNode.QueueSpawns(spawnsForThisPlayer);
                }
            }
        }

        private void StartExploderWaveDispersed(int spawnCount, List<CharacterMaster> playersList, Vector2 searchRange) //tries to queue exploder spawns between all available spawn points
        {
            if(searchRange.x >= searchRange.y && searchRange.y != 0)
            {
                StartExploderWaveStacked(spawnCount, playersList);
                return;
            }
            for (int i = 0; i < playersList.Count; i++)
            {
                CharacterMaster currentPlayer = playersList[i];

                float closestNodeDistance = Mathf.Infinity;
                ExploderSpawnNode closestAvailableNode = null;
                foreach (ExploderSpawnNode currentNode in SpawnNodes)
                {
                    if(currentNode.spawnQueue <= searchRange.y)
                    {
                        float currentDistance = Vector3.Distance(currentNode.transform.position, currentPlayer.transform.position);
                        if (currentDistance < closestNodeDistance)
                        {
                            closestAvailableNode = currentNode;
                            closestNodeDistance = currentDistance;
                        }
                    }
                    else
                    {
                        if(searchRange.x < currentNode.spawnQueue)
                        {
                            searchRange.x = searchRange.y;
                        }
                        searchRange.y = currentNode.spawnQueue;
                    }
                }

                int remainingPlayersToSpawnFrom = playersList.Count - i;
                int spawnsForThisPlayer = spawnCount / remainingPlayersToSpawnFrom;
                if (spawnCount % remainingPlayersToSpawnFrom != 0)
                {
                    spawnsForThisPlayer = Mathf.CeilToInt(spawnsForThisPlayer);
                }

                if (closestAvailableNode != null)
                {
                    if (spawnsForThisPlayer > 0)
                    {
                        closestAvailableNode.QueueSpawns(spawnsForThisPlayer);
                    }
                }
                else
                {

                }
            }
        }
        #endregion
    }
    public class ExploderSpawnNode : MonoBehaviour
    {
        internal int spawnQueue = 0;
        static float spawnInterval = 1f;
        float spawnTimer = 0f;

        void Start()
        {
            spawnQueue = 0;
            Debug.Log(this.transform.position);
        }

        public void QueueSpawns(int spawnCount, float delay = 0)
        {
            spawnQueue += spawnCount;
            spawnTimer = 0 - delay;
        }

        void FixedUpdate()
        {
            if(spawnQueue > 0)
            {
                spawnTimer += Time.fixedDeltaTime;
            }
            else
            {
                spawnTimer = 0;
                return;
            }

            while (spawnTimer > spawnInterval && spawnQueue > 0)
            {
                spawnTimer -= spawnInterval;
                spawnQueue -= 1;
                Debug.Log($"Spawn {ExploderDirectorModule.exploderCard.name} at position {this.transform.position}!. Remaining queue: {spawnQueue}");

                DirectorSpawnRequest directorSpawnRequest = new DirectorSpawnRequest(ExploderDirectorModule.exploderCard, new DirectorPlacementRule
                {
                    placementMode = DirectorPlacementRule.PlacementMode.Approximate,
                    minDistance = 0f,
                    maxDistance = 10f,
                    spawnOnTarget = transform
                }, RoR2Application.rng);
                directorSpawnRequest.teamIndexOverride = new TeamIndex?(TeamIndex.Monster);
                directorSpawnRequest.ignoreTeamMemberLimit = true;

                DirectorCore instance = DirectorCore.instance;
                if (instance == null)
                {
                    return;
                }
                instance.TrySpawnObject(directorSpawnRequest);
                //create exploder here
            }
        }
    }
}
