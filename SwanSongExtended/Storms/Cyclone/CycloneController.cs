using EntityStates;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Navigation;
using SwanSongExtended.Elites;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RainrotSharedUtils.Shelters.ShelterUtilsModule;

namespace SwanSongExtended.Storms
{
    public class CycloneController : MonoBehaviour
    {
        public enum CycloneState
        {
            Idle,
            PreparingCyclone,
            ElectingLeader,
            PreparingSquall,
            FiringSquall
        }
        public CycloneState cycloneState
        {
            get
            {
                if (this.currentState == null)
                    return CycloneState.Idle;
                return currentState.cycloneState;
            }
        }
        public static CycloneController instance;
        public GameObject beamPreVfxInstance;
        public bool telegraphActive = false;
        internal void UpdatePreBeamTransform()
        {
            if(!telegraphActive)
            {
                RoR2Application.onLateUpdate -= UpdatePreBeamTransform;
                return;
            }
            Ray beamRay = leaderElite.GetBeamRay();
            beamPreVfxInstance.transform.SetPositionAndRotation(beamRay.origin, Quaternion.LookRotation(beamRay.direction));
        }

        //private CombatDirector combatDirector;
        public EntityStateMachine cycloneStateMachine;
        public AISkillDriver HowlSquallDriver;
        private CycloneController.BaseCycloneState currentState
        {
            get
            {
                return this.cycloneStateMachine.state as CycloneController.BaseCycloneState;
            }
        }

        public AffixSquallBehavior leaderElite { get; private set; }
        public void DemoteCurrentLeader()
        {
            if (leaderElite != null && leaderElite.body.HasBuff(StormsCore.CycloneLeader))
                leaderElite.body.RemoveBuff(StormsCore.CycloneLeader);
            leaderElite = null;
        }
        public GameObject primaryCycloneInstance { get; private set; }

        public static bool GetShouldConverge()
        {
            return instance != null
                && (instance.primaryCycloneInstance != null || instance.leaderElite != null);
        }
        public static Vector3 convergePositionGround;
        public static Vector3 convergePositionAir;

        public float accumulatedSquallCharge = 0;
        public float accumulatedSquallTime = 0;
        public int squallContributorCountCurrent = 0;
        public int squallContributorCountHighest = 0;
        public float squallTimeFired = 0;

        public static void AddSquallCharge(float charge)
        {
            if (CycloneController.instance == null)
                return;
            if (CycloneController.instance.cycloneState == CycloneState.FiringSquall)
                return;
            instance.accumulatedSquallCharge += charge;
        }

        public static void AddSquallTime(float time)
        {
            if (CycloneController.instance == null)
                return;
            Debug.Log(instance.cycloneState.ToString());
            if (CycloneController.instance.cycloneState == CycloneState.FiringSquall)
                return;
            instance.accumulatedSquallTime += time;
        }
        public static void SetContributorCount(int count)
        {
            if (CycloneController.instance == null)
                return;
            if (CycloneController.instance.cycloneState == CycloneState.FiringSquall)
                return;
            instance.squallContributorCountCurrent = count;
            if(count > instance.squallContributorCountHighest)
                instance.squallContributorCountHighest = count;
        }

        void Awake()
        {
            cycloneStateMachine = EntityStateMachine.FindByCustomName(this.gameObject, StormsCore.esmCycloneName);

            //HowlChaseDriver = this.gameObject.AddComponent<AISkillDriver>();
            //HowlChaseDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            //HowlChaseDriver.buttonPressType = AISkillDriver.ButtonPressType.Hold;
            //HowlChaseDriver.movementType = AISkillDriver.MovementType.FollowMoveTarget;
            //HowlChaseDriver.moveTargetType = AISkillDriver.TargetType.Custom;
            HowlSquallDriver = this.gameObject.AddComponent<AISkillDriver>();
            HowlSquallDriver.aimType = AISkillDriver.AimType.AtCurrentEnemy;
            HowlSquallDriver.buttonPressType = AISkillDriver.ButtonPressType.Abstain;
            HowlSquallDriver.movementType = AISkillDriver.MovementType.Stop;
            HowlSquallDriver.moveTargetType = AISkillDriver.TargetType.Custom;
            HowlSquallDriver.activationRequiresAimConfirmation = false;
            HowlSquallDriver.activationRequiresAimTargetLoS = false;
            HowlSquallDriver.activationRequiresTargetLoS = false;
            HowlSquallDriver.aimVectorDampTimeOverride = WhirlwindAspect.squallAimDamping;
            HowlSquallDriver.aimVectorMaxSpeedOverride = WhirlwindAspect.squallAimMaxSpeed;
            HowlSquallDriver.customName = "FireSquall";
            HowlSquallDriver.driverUpdateTimerOverride = 2f;
            HowlSquallDriver.maxDistance = float.PositiveInfinity;
            HowlSquallDriver.moveInputScale = 1;
            HowlSquallDriver.shouldFireEquipment = true;
            HowlSquallDriver.selectionRequiresAimTarget = false;
            HowlSquallDriver.selectionRequiresOnGround = false;
            HowlSquallDriver.selectionRequiresTargetLoS = false;
            HowlSquallDriver.selectionRequiresTargetNonFlier = false;
        }
        void OnEnable()
        {
            if (instance != null)
                Destroy(this);
            instance = this;
        }
        void OnDisable()
        {
            instance = null;
        }

        void FixedUpdate()
        {
            LocateConvergePosition();
        }
        void LocateConvergePosition()
        {
            if (!GetShouldConverge())
                return;
            NodeGraph airNodes = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Air);
            NodeGraph groundNodes = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);

            GameObject targetObject = primaryCycloneInstance;
            if (leaderElite != null && leaderElite.gameObject != null && leaderElite.body.HasBuff(StormsCore.CycloneProtection))
            {
                targetObject = leaderElite.gameObject;
            }
            if (targetObject == null)
                return;

            NodeGraph.NodeIndex airNode = airNodes.FindClosestNode(targetObject.transform.position, HullClassification.Golem);
            if (airNodes.GetNodePosition(airNode, out Vector3 airPos))
            {
                convergePositionAir = airPos;
            }
            NodeGraph.NodeIndex groundNode = groundNodes.FindClosestNode(targetObject.transform.position, HullClassification.BeetleQueen);
            if (groundNodes.GetNodePosition(groundNode, out Vector3 groundPos))
            {
                convergePositionGround = groundPos;
            }
        }

        internal abstract class BaseCycloneState : BaseState
        {
            public GameObject beamVfxInstance => instance.beamPreVfxInstance;
            internal AffixSquallBehavior leaderElite
            {
                get
                {
                    if (CycloneController.instance == null)
                        return null;
                    return CycloneController.instance.leaderElite;
                }
                set
                {
                    if (CycloneController.instance == null)
                        return;
                    CycloneController.instance.leaderElite = value;
                }
            }
            public abstract CycloneState cycloneState { get; }
            public abstract BaseCycloneState GetNextState();
            public override void OnEnter()
            {
                Debug.LogError(cycloneState.ToString());
                UpdateTelegraph(false);
            }
            internal void UpdateTelegraph(bool newValue)
            {
                if (newValue == instance.telegraphActive)//(instance.beamPreVfxInstance != null))
                    return;
                instance.telegraphActive = newValue;
                if (newValue == true)
                {
                    instance.beamPreVfxInstance = UnityEngine.Object.Instantiate<GameObject>(WhirlwindAspect.squallPreBeamVfxPrefab);
                    instance.beamPreVfxInstance.transform.SetParent(leaderElite.body.aimOriginTransform, true);
                    instance.UpdatePreBeamTransform();
                    RoR2Application.onLateUpdate += instance.UpdatePreBeamTransform;
                    //Util.PlaySound(EntityStates.VoidRaidCrab.SpinBeamAttack.enterSoundString, base.gameObject);
                }
                else
                {
                    RoR2Application.onLateUpdate -= instance.UpdatePreBeamTransform;
                    Destroy(instance.beamPreVfxInstance);
                    VfxKillBehavior.KillVfxObject(instance.beamPreVfxInstance);
                    instance.beamPreVfxInstance = null;
                }
            }

            public void ElectLeader(AffixSquallBehavior candidateElite)
            {
                if (leaderElite == candidateElite || candidateElite == null)
                    return;
                DemoteCurrentLeader();
                candidateElite.body.AddBuff(StormsCore.CycloneLeader);
                leaderElite = candidateElite;
                EntityStateMachine esm = EntityStateMachine.FindByCustomName(leaderElite.gameObject, "Body");
                if (esm != null)
                    esm.SetNextStateToMain();
            }
            public void DemoteCurrentLeader()
            {
                instance.DemoteCurrentLeader();
            }
            public static bool IsAnyEliteInCyclone()
            {
                return AffixSquallBehavior.readOnlyInstancesList.Any(x => x.body.HasBuff(StormsCore.CycloneProtection));
            }
        }

        internal class PrepareCyclone : BaseCycloneState
        {
            public override CycloneState cycloneState => CycloneState.PreparingCyclone;
            float refreshCountdown = 0;
            float refreshInterval = 3f;

            public override void OnEnter()
            {
                if (CycloneController.instance.primaryCycloneInstance)
                    Destroy(CycloneController.instance.primaryCycloneInstance);

                ResetRefreshCountdown();
                DemoteCurrentLeader();

                if(instance.accumulatedSquallTime <= 0)
                {
                    instance.accumulatedSquallTime = 0;
                    instance.accumulatedSquallCharge = 0;
                    instance.squallTimeFired = 0;
                }
                instance.squallContributorCountHighest = 0;
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (!NetworkServer.active)
                    return;

                if(instance.accumulatedSquallTime > 0)
                {
                    instance.accumulatedSquallTime -= StormsCore.squallReelectionRallyTimeLoss * Time.fixedDeltaTime;
                    if (instance.accumulatedSquallTime <= 0)
                    {
                        instance.accumulatedSquallTime = 0;
                        instance.accumulatedSquallCharge = 0;
                    }
                }

                //if (!StormRunBehavior.hasBegunStorm)
                //    return;

                if (refreshCountdown > 0)
                {
                    refreshCountdown -= Time.fixedDeltaTime;
                    return;
                }

                IEnumerable<AffixSquallBehavior> list = AffixSquallBehavior.readOnlyInstancesList.Where(
                        x => x.body.teamComponent.teamIndex != TeamIndex.Player 
                        && !IsBodySheltered(x.body)
                    );
                int count = list.Count();
                if (list.Any(x => !IsBodySheltered(x.body)))
                {
                    list.Where(x => !IsBodySheltered(x.body));
                    count = list.Count();
                }
                else
                {
                    if (count <= 0)
                    {
                        ResetRefreshCountdown();
                        return;
                        //list = AffixSquallBehavior.readOnlyInstancesList.Where(
                        //        x => x.body.teamComponent.teamIndex != TeamIndex.Player
                        //    ).ToList();
                        //count = list.Count;
                        //if (count <= 0)
                        //{
                        //}
                    }
                }
                //just fucking pick a random one i guess.
                //this is a stand in for using the cool and awesome density based formula i envisioned
                AffixSquallBehavior randomElite = list.ElementAt(UnityEngine.Random.Range(0, count - 1));

                NodeGraph nodeGraph = SceneInfo.instance.GetNodeGraph(MapNodeGroup.GraphType.Ground);
                NodeGraph.NodeIndex node = nodeGraph.FindClosestNode(randomElite.transform.position, HullClassification.BeetleQueen, maxDistance: 50f);

                if(nodeGraph.GetNodePosition(node, out Vector3 nodePosition))
                {
                    //if nearest node to leader is sheltered. not likely since leader cant be sheltered anyways
                    if (IsPositionSheltered(nodePosition))
                    {
                        List<NodeGraph.NodeIndex> validNodes =
                            nodeGraph.FindNodesInRange(nodePosition, 0, StormsCore.cycloneRadius * 1.5f, HullMask.Golem);
                        List<Vector3> validPositions = new List<Vector3>();
                        foreach(NodeGraph.NodeIndex node2 in validNodes)
                        {
                            if (nodeGraph.GetNodePosition(node2, out Vector3 temp) && !IsPositionSheltered(temp))
                                validPositions.Add(temp);
                        }
                            //.Where(x => nodeGraph.GetNodePosition(x, out Vector3 temp) && !IsPositionSuperSheltered(temp)).ToList();
                        if(validPositions.Count <= 0)
                        {
                            ResetRefreshCountdown();
                            return;
                        }

                        nodePosition = validPositions[UnityEngine.Random.Range(0, validPositions.Count - 1)];
                    }
                    ////i dont think this check will be here in the final formula 
                    //if(randomElite.body.master.aiComponents[0].broadNavigationAgent.reac
                    //    (nodePosition - randomElite.transform.position).sqrMagnitude > StormsCore.cycloneRadius * StormsCore.cycloneRadius)
                    //{
                    //    ResetRefreshCountdown();
                    //    return;
                    //}

                    Debug.LogError("Electing leader and placing cyclone");
                    GameObject cycloneInstance = UnityEngine.Object.Instantiate(StormsCore.cycloneWardPrefab, nodePosition, Quaternion.identity);
                    cycloneInstance.GetComponent<TeamFilter>().teamIndex = TeamIndex.None;
                    CycloneController.instance.primaryCycloneInstance = cycloneInstance;
                    ElectLeader(randomElite);
                    NetworkServer.Spawn(cycloneInstance);

                    StormsCore.cycloneMaterial.SetFloat("_TriplanarOn", 0);
                    StormsCore.cycloneMaterial.SetInt("_TriplanarOn", 0);
                    StormsCore.cycloneMaterial.SetFloat("_TriplanarOff", 1);
                    StormsCore.cycloneMaterial.SetInt("_TriplanarOff", 1);

                    outer.SetNextState(GetNextState());
                }
            }

            void ResetRefreshCountdown()
            {
                refreshCountdown = refreshInterval;
            }

            public override BaseCycloneState GetNextState()
            {
                BaseCycloneState nextState = new PrepareSquall();
                (nextState as PrepareSquall).leaderEliteObject = leaderElite.gameObject;
                return nextState;
            }
        }

        internal class ElectLeader : BaseCycloneState
        {
            internal float attemptElectLeaderCountdown = 0f;
            bool shouldMoveCyclone = false;
            public override CycloneState cycloneState => CycloneState.ElectingLeader;

            public override void OnEnter()
            {
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (!NetworkServer.active)
                    return;

                if(attemptElectLeaderCountdown > 0)
                {
                    attemptElectLeaderCountdown -= Time.fixedDeltaTime;
                    return;
                }

                if(AffixSquallBehavior.readOnlyInstancesList.Count <= 0)
                {
                    GetNextState();
                }

                //just fuckin pick a random one i guess
                List<AffixSquallBehavior> list = 
                    AffixSquallBehavior.readOnlyInstancesList.Where(
                        x => x.body.teamComponent.teamIndex != TeamIndex.Player 
                        && x.body.HasBuff(StormsCore.CycloneProtection))
                    .ToList();
                int count = list.Count();
                if (count == 0)
                {
                    instance.accumulatedSquallTime -= StormsCore.squallReelectionRallyTimeLoss * StormsCore.squallReelectionInterval;
                    if(instance.accumulatedSquallTime <= 0 || instance.squallContributorCountCurrent <= 0)
                    {
                        FizzleOut();
                        return;
                    }
                    attemptElectLeaderCountdown = StormsCore.squallReelectionInterval;
                    return;
                }
                ElectLeader(list[UnityEngine.Random.Range(0, count - 1)]);
                outer.SetNextState(GetNextState());
                //foreach(AffixSquallBehavior elite in AffixSquallBehavior.readOnlyInstancesList)
                //{
                //
                //}
            }

            void FizzleOut()
            {
                instance.accumulatedSquallCharge = 0;
                instance.accumulatedSquallTime = 0;
                shouldMoveCyclone = true;
                outer.SetNextState(GetNextState());
            }

            public override BaseCycloneState GetNextState()
            {
                //if no cyclone or timed out with no leader
                if (instance.primaryCycloneInstance == null 
                    || instance.leaderElite == null
                    || shouldMoveCyclone == true)
                    return new PrepareCyclone();

                BaseCycloneState nextState = new PrepareSquall();
                (nextState as PrepareSquall).leaderEliteObject = leaderElite.gameObject;
                return nextState;
            }
        }

        internal class PrepareSquall : BaseCycloneState
        {
            private CycloneState nextState = CycloneState.PreparingSquall;
            public override CycloneState cycloneState => CycloneState.PreparingSquall;
            public GameObject leaderEliteObject;
            float squallTimeCache = 0;
            public override void OnSerialize(NetworkWriter writer)
            {
                base.OnSerialize(writer);
                writer.Write(leaderEliteObject);
            }
            public override void OnDeserialize(NetworkReader reader)
            {
                base.OnDeserialize(reader);
                leaderEliteObject = reader.ReadGameObject();
                if (leaderEliteObject != null)
                    leaderElite = leaderEliteObject.GetComponent<AffixSquallBehavior>();
            }

            public override void OnEnter()
            {
                base.OnEnter();
                squallTimeCache = instance.accumulatedSquallTime;
                //UpdateTelegraph(true);
            }
            public override void OnExit()
            {
                base.OnExit();
                UpdateTelegraph(false);
            }


            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (!NetworkServer.active)
                    return;

                bool reelectionTimePassed = base.fixedAge > 1f;
                if (leaderElite == null || (reelectionTimePassed && instance.accumulatedSquallTime == squallTimeCache))
                {
                    Log.Debug("PrepareSquall: Entering reelection");
                    UpdateTelegraph(false);
                    outer.SetNextState(GetNextState());
                    return;
                }
                UpdateTelegraph(reelectionTimePassed);

                if (GetSquallThresholdMet())
                {
                    Log.Debug("PrepareSquall: Beginning to fire");
                    outer.SetNextState(new FireSquall());
                }

                //nextState = CycloneState.PreparingSquall;
                //
                //if(leaderElite == null)
                //{
                //    nextState = CycloneState.ElectingLeader;
                //    //if (instance.accumulatedSquallCharge <= 0)
                //    //    nextState = CycloneState.PreparingCyclone;
                //}
                //else if(GetSquallThresholdMet())
                //{
                //    nextState = CycloneState.FiringSquall;
                //}
                //
                //if (nextState != CycloneState.PreparingSquall)
                //    outer.SetNextState(GetNextState());
            }
            bool GetSquallThresholdMet()
            {
                if (instance.accumulatedSquallTime < StormsCore.squallRallyTimeMin)
                    return false;
                if (instance.accumulatedSquallTime >= StormsCore.squallRallyTimeMax)
                    return true;

                float maxCharge = StormsCore.squallRallyTimeMin * StormsCore.squallRallyContributorThreshold;
                float requiredTime = Util.Remap(instance.accumulatedSquallCharge, 0, maxCharge, StormsCore.squallRallyTimeMax, StormsCore.squallRallyTimeMin);

                return instance.accumulatedSquallTime >= requiredTime;
            }

            /// <summary>
            /// using this as a way to enter elect leader
            /// </summary>
            /// <returns></returns>
            public override BaseCycloneState GetNextState()
            {
                BaseCycloneState nextState;
                UpdateTelegraph(false);
                nextState = new ElectLeader();
                //add a slight delay to reelect leader if no charge was added, to allow for enemies to enter the cyclone before dispelling
                if (instance.squallContributorCountCurrent <= 0)
                    (nextState as ElectLeader).attemptElectLeaderCountdown = 2f;
                return nextState;

                //switch (this.nextState)
                //{
                //    default:
                //    case CycloneState.ElectingLeader:
                //        nextState = new ElectLeader();
                //        //add a slight delay to reelect leader if no charge was added, to allow for enemies to enter the cyclone before dispelling
                //        if(instance.accumulatedSquallCharge <= 0)
                //            (nextState as ElectLeader).attemptElectLeaderCountdown = 1f;
                //        break;
                //    case CycloneState.FiringSquall:
                //        nextState = new FireSquall();
                //        break;
                //    case CycloneState.PreparingCyclone:
                //        nextState = new PrepareCyclone();
                //        break;
                //}
            }
        }

        internal class FireSquall : BaseCycloneState
        {
            bool resetting = false;
            public override CycloneState cycloneState => CycloneState.FiringSquall;

            public override void OnEnter()
            {
                if(leaderElite != null)
                    leaderElite.isFiring = true;
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if(leaderElite == null)
                {
                    Log.Debug("FireSquall: Beginning reelection");
                    outer.SetNextState(new ElectLeader());
                    return;
                }

                float fireDuration = StormsCore.squallFireDurationMin + StormsCore.squallFireDurationBonusPerOverspill * Tools.CountOverspillTriangular(instance.squallContributorCountHighest);
                if(base.fixedAge + instance.squallTimeFired >= fireDuration)
                {
                    Log.Debug("End squall");
                    outer.SetNextState(GetNextState());
                }
            }

            public override void OnExit()
            {
                if (leaderElite != null)
                    leaderElite.isFiring = false;
                base.OnExit();
                if (!resetting)
                    instance.squallTimeFired = 0;
                else
                    instance.squallTimeFired += base.fixedAge;
            }

            public override BaseCycloneState GetNextState()
            {
                resetting = true;
                instance.accumulatedSquallCharge = 0;
                instance.accumulatedSquallTime = 0;
                return new PrepareCyclone();
            }
        }
        internal class Idle : BaseCycloneState
        {
            public override CycloneState cycloneState => CycloneState.Idle;

            public override BaseCycloneState GetNextState()
            {
                return new PrepareSquall();
            }
        }
    }
}
