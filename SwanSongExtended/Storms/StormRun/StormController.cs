using EntityStates;
using RainrotSharedUtils.Shelters;
using RoR2;
using RoR2.UI;
using SwanSongExtended.Components;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SwanSongExtended.Storms.StormRunBehavior;
using static SwanSongExtended.Storms.StormsCore;
using static R2API.DamageAPI;
using RainrotSharedUtils.Difficulties;

namespace SwanSongExtended.Storms
{
    /// <summary>
     /// Handles storm event timing and hazards during storms
     /// </summary>
    [RequireComponent(typeof(EntityStateMachine), typeof(CombatDirector))]
    public class StormController : MonoBehaviour
    {
        public enum StormState
        {
            Idle,
            Approaching,
            ApproachWarning,
            Active
        }
        public StormState stormState
        {
            get
            {
                if (this.currentState == null)
                    return StormState.Idle;
                return currentState.stormState;
            }
        }
        private CombatDirector combatDirector;
        public EntityStateMachine mainStateMachine;
        private StormController.BaseStormState currentState
        {
            get
            {
                return this.mainStateMachine.state as StormController.BaseStormState;
            }
        }
        internal float stormDelayTime = 0;
        internal float stormWarningTime = 0;
        bool shelterObjectiveActive = false;
        public static bool bossHealthBarActive { get; private set; } = false;


        public void Awake()
        {
            bossHealthBarActive = false;
            combatDirector = GetComponent<CombatDirector>();
            combatDirector.enabled = false;
            mainStateMachine = EntityStateMachine.FindByCustomName(this.gameObject, StormsCore.esmStormName);//GetComponent<EntityStateMachine>();

            BossGroup.onBossGroupStartServer += CheckForBossHealthBar;
            BossGroup.onBossGroupDefeatedServer += CheckForBossHealthBar;
        }

        private void CheckForBossHealthBar(BossGroup group)
        {
            List<BossGroup> instancesList = InstanceTracker.GetInstancesList<BossGroup>();
            for (int i = 0; i < instancesList.Count; i++)
            {
                if (instancesList[i].shouldDisplayHealthBarOnHud == true)
                {
                    bossHealthBarActive = true;
                    return;
                }
            }
            bossHealthBarActive = false;
        }

        void OnDestroy()
        {
            SetShelterObjective(false);
        }

        public void SetShelterObjective(bool enable)
        {
            if (enable != shelterObjectiveActive)
            {
                if (enable)
                {
                    Log.Debug("Enabling storm shelter objective");
                    ObjectivePanelController.collectObjectiveSources += this.OnCollectObjectiveSources;
                    shelterObjectiveActive = true;
                }
                else
                {
                    Log.Debug("Disabling storm shelter objective");
                    ObjectivePanelController.collectObjectiveSources -= this.OnCollectObjectiveSources;
                    shelterObjectiveActive = false;
                }
            }
        }
        private void OnCollectObjectiveSources(CharacterMaster master, List<ObjectivePanelController.ObjectiveSourceDescriptor> objectiveSourcesList)
        {
            objectiveSourcesList.Add(new ObjectivePanelController.ObjectiveSourceDescriptor
            {
                master = master,
                objectiveType = typeof(StormObjectiveTracker),
                source = base.gameObject
            });
        }

        public void BeginStormApproach(float stormDelayTime, float stormWarningTime)
        {
            this.stormDelayTime = stormDelayTime * 60;
            this.stormWarningTime = stormWarningTime * 60;
            Log.Debug("Starting storm approach");
            mainStateMachine.SetNextState(new StormApproach());
        }
        public void ForceBeginStorm()
        {
            if (this.stormState < StormState.ApproachWarning)
            {
                mainStateMachine.SetNextState(new StormWarning());
            }
        }

        public static void BroadcastStormWarningMessage(StormType stormType)
        {
            if (!NetworkServer.active)
                return;
            string warningMessage = "";
            switch (stormType)
            {
                case StormType.MeteorDefault:
                    warningMessage = "<style=cIsUtility>A meteor storm is approaching...</style>";
                    break;
                case StormType.Lightning:
                    warningMessage = "A storm approaches...";
                    break;
                case StormType.Fire:
                    warningMessage = "A meteor storm is approaching...";
                    break;
                case StormType.Cold:
                    warningMessage = "The air around you begins to freeze...";
                    break;
            }

            RoR2.Chat.ServerAttemptBroadcastChat(warningMessage);
        }
        public static void BroadcastStormActiveMessage(StormType stormType)
        {
            if (!NetworkServer.active)
                return;

            string warningMessage = "";
            switch (stormType)
            {
                case StormType.MeteorDefault:
                    warningMessage = "<style=cIsUtility>A shower of meteors begins to fall...</style>";
                    break;
                    //case StormType.Lightning:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
                    //case StormType.Fire:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
                    //case StormType.Cold:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
            }

            RoR2.Chat.ServerAttemptBroadcastChat(warningMessage);
        }
        public static void BroadcastStormIntensifyMessage(StormType stormType)
        {
            string warningMessage = "";
            switch (stormType)
            {
                case StormType.MeteorDefault:
                    warningMessage = "<style=cIsUtility>The storm intensifies...</style>";
                    break;
                    //case StormType.Lightning:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
                    //case StormType.Fire:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
                    //case StormType.Cold:
                    //    warningMessage = "A meteor storm is approaching...";
                    //    break;
            }

            Chat.ServerAttemptBroadcastChat(warningMessage);
        }

        internal abstract class BaseStormState : BaseState
        {
            /// <summary>
            /// Run Delta Time is used in place of Time.fixedDeltaTime to account for time skips and time freezes in the storm cycle
            /// </summary>
            float runDeltaTimeThisFrame = 0;
            /// <summary>
            /// cached value of the last frame's run timestamp. used to calculate runDeltaTime
            /// </summary>
            float runTimeStamp = float.NegativeInfinity;
            public abstract StormState stormState { get; }
            private protected StormType stormType => StormRunBehavior.instance.stormType;
            private protected StormController stormController { get; private set; }
            public override void OnEnter()
            {
                Debug.Log(stormState.ToString());
                base.OnEnter();
                if (runTimeStamp == float.NegativeInfinity)
                    runTimeStamp = Run.instance.GetRunStopwatch();
                this.stormController = base.GetComponent<StormController>();
            }

            /// <summary>
            /// Run Delta Time is used in place of Time.fixedDeltaTime to account for time skips and time freezes in the storm cycle
            /// </summary>
            public float GetRunDeltaTime()
            {
                return runDeltaTimeThisFrame;
            }

            public override void FixedUpdate()
            {
                if (Run.instance)
                {
                    this.runDeltaTimeThisFrame = Run.instance.GetRunStopwatch() - runTimeStamp;
                    this.fixedAge += runDeltaTimeThisFrame;
                    runTimeStamp = Run.instance.GetRunStopwatch();
                }

                if(this.stormState >= StormState.ApproachWarning && TeleporterInteraction.instance)
                {
                    if (TeleporterInteraction.instance)
                    {
                        //if charging, hide objective. if not charging, show objective
                        stormController.SetShelterObjective(!TeleporterInteraction.instance.isCharging);
                    }
                }
                else
                {
                    stormController.SetShelterObjective(false);
                }
            }

            public void EnableDirector()
            {
                if (stormController.combatDirector == null)
                {
                    Debug.LogError("StormController: Combat Director null!");
                    return;
                }

                if (!NetworkServer.active)
                    return;
                if (stormController.combatDirector.enabled)
                    return;
                stormController.combatDirector.enabled = true;
                stormController.combatDirector.monsterCredit += StormsCore.stormDirectorCreditStimulus;
                stormController.combatDirector.monsterSpawnTimer = 0;
            }
            public float GetStormIntensityIncrement()
            {
                if (!DifficultyUtilsModule.ValidateCachedDifficultyStats())
                    return 0;

                return DifficultyUtilsModule.cachedDifficultyStats.stormIntensifyStrength_ForSwanSong;
            }
            public virtual void SetNextState()
            {
                BaseStormState nextState = this.GetNextState();
                nextState.runTimeStamp = this.runTimeStamp;
                nextState.runDeltaTimeThisFrame = this.runDeltaTimeThisFrame;
                this.outer.SetNextState(nextState);
            }

            public abstract BaseStormState GetNextState();
        }
        internal class StormActive : BaseStormState
        {
            public override BaseStormState GetNextState()
            {
                return new StormController.IdleState();
            }

            public override StormState stormState => StormState.Active;

            //all the projectile/prefab stuff

            private List<MeteorStormController.Meteor> meteorsToDetonate;
            private List<MeteorStormController.MeteorWave> meteorWaves;
            private float waveTimer;
            float stormStrength = 0;
            float stormStrengthIncreaseCountdown = 0;

            public override void OnEnter()
            {
                base.OnEnter();

                if (!Run.instance)
                {
                    SetNextState();
                    return;
                }
                if (!NetworkServer.active)
                    return;

                BroadcastStormActiveMessage(stormType);

                WishboneCarcassComponent.ClearAllCarcasses();
                stormStrengthIncreaseCountdown = stormStrengthIncreaseTimerSeconds;
                this.meteorsToDetonate = new List<MeteorStormController.Meteor>();
                this.meteorWaves = new List<MeteorStormController.MeteorWave>();
                //On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor += MeteorWave_GetNextMeteor;

                EnableDirector();
            }

            public override void OnExit()
            {
                base.OnExit();
                //On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor -= MeteorWave_GetNextMeteor;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (!NetworkServer.active)
                    return;
                stormStrengthIncreaseCountdown -= GetRunDeltaTime();
                if (stormStrengthIncreaseCountdown <= 0 && Run.instance)
                {
                    stormStrengthIncreaseCountdown += StormsCore.stormStrengthIncreaseTimerSeconds;
                    stormStrength += GetStormIntensityIncrement();
                    BroadcastStormIntensifyMessage(stormType);
                }

                //thisa is just for meteor stuff; we can make it work for the other storsm when they start existing lol.
                this.waveTimer -= Time.fixedDeltaTime;
                if (this.waveTimer <= 0f)
                {
                    this.waveTimer = UnityEngine.Random.Range(waveMinInterval, waveMaxInterval) / (1 + stormStrength);
                    MeteorStormController.MeteorWave item =
                        new MeteorStormController.MeteorWave(
                            CharacterBody.readOnlyInstancesList
                                .Where(body => /*!ShelterUtilsModule.IsBodySheltered(body) &&*/
                                (body.teamComponent.teamIndex == TeamIndex.Player && !body.isFlying)
                                || body.IsStormElite() || Util.CheckRoll(meteorTargetEnemyChance))
                                .ToArray<CharacterBody>(),
                            TeleporterInteraction.instance ? TeleporterInteraction.instance.transform.position : base.transform.position);
                    item.hitChance = 1 - waveMissChance;
                    this.meteorWaves.Add(item);
                    this.meteorWaves.Add(item);

                    AddShelterPerimeterStrikes();
                    AddCharacterTargetedStrikes();
                }

                //float timeOfImpact = float.PositiveInfinity;
                //if (Run.instance)
                //    timeOfImpact = Run.instance.time - meteorImpactDelay;
                //float timeOfTelegraph = timeOfImpact - meteorTravelEffectDuration;
                //for (int j = this.meteorsToDetonate.Count - 1; j >= 0; j--)
                //{
                //    MeteorStormController.Meteor meteor = this.meteorsToDetonate[j];
                //    if (meteor.startTime < timeOfImpact)
                //    {
                //        this.meteorsToDetonate.RemoveAt(j);
                //        this.DetonateMeteor(meteor);
                //    }
                //}
            }

            private void AddCharacterTargetedStrikes()
            {
                for (int i = this.meteorWaves.Count - 1; i >= 0; i--)
                {
                    MeteorStormController.MeteorWave meteorWave = this.meteorWaves[i];
                    meteorWave.timer -= GetRunDeltaTime();
                    if (meteorWave.timer <= 0f)
                    {
                        meteorWave.timer = UnityEngine.Random.Range(0.05f, 1f);
                        MeteorStormController.Meteor nextMeteor = meteorWave.GetNextMeteor(); // getnextmeteor handles some stuff here, we can look into canibalizing it for more adaptable stuff
                        bool meteorViable = GetMeteorViable(nextMeteor);

                        if (!meteorViable)
                        {
                            this.meteorWaves.RemoveAt(i);
                        }
                        else
                        {
                            SpawnMeteor(nextMeteor);
                            this.meteorsToDetonate.Add(nextMeteor);
                            EffectManager.SpawnEffect(meteorWarningEffectPrefab, new EffectData
                            {
                                origin = nextMeteor.impactPosition,
                                scale = meteorBlastRadius
                            }, true);
                        }
                    }
                }
            }

            private void AddShelterPerimeterStrikes()
            {
                foreach (ShelterProviderBehavior shelter in ShelterProviderBehavior.readOnlyInstancesList)
                {
                    if (shelter.fallbackRadius <= 1)
                        continue;
                    if (shelter.isHazardZone)
                    {
                        float shelterArea = 3 * shelter.fallbackRadius * shelter.fallbackRadius;
                        continue;
                    }

                    float shelterPerimeter = 6 * shelter.fallbackRadius;
                    float meteorCount = shelterPerimeter / (meteorBlastRadius * shelterPerimeterStrikeGap);
                    float remainder = meteorCount - (float)Math.Truncate(meteorCount);
                    if (Util.CheckRoll0To1(remainder))
                        Mathf.CeilToInt(meteorCount);
                    else
                        Mathf.FloorToInt(meteorCount);

                    for (int i = 0; i < meteorCount; i++)
                    {
                        float rand = UnityEngine.Random.Range(0f, 2f);
                        float distance = shelter.fallbackRadius + (meteorBlastRadius * rand);
                        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                        Vector3 vector = new Vector3(dir.x, 0, dir.y) * distance;

                        MeteorStormController.Meteor meteor = new MeteorStormController.Meteor();
                        meteor.startTime = Run.instance.time;
                        meteor.impactPosition = shelter.transform.position + vector;

                        Vector3 origin = meteor.impactPosition + Vector3.up * 6f;
                        Vector3 onUnitSphere = UnityEngine.Random.onUnitSphere;
                        onUnitSphere.y = -1f;
                        RaycastHit raycastHit;
                        if (Physics.Raycast(origin, onUnitSphere, out raycastHit, 12f, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                        {
                            meteor.impactPosition = raycastHit.point;
                        }
                        else if (Physics.Raycast(meteor.impactPosition, Vector3.down, out raycastHit, float.PositiveInfinity, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                        {
                            meteor.impactPosition = raycastHit.point;
                        }
                        else
                        {
                            meteor.valid = false;
                        }

                        if (GetMeteorViable(meteor))
                        {
                            SpawnMeteor(meteor);
                        }
                    }
                }
            }

            private void SpawnMeteor(MeteorStormController.Meteor meteor)
            {
                //this.meteorsToDetonate.Add(meteor);
                //EffectManager.SpawnEffect(meteorWarningEffectPrefab, new EffectData
                //{
                //    origin = meteor.impactPosition,
                //    scale = meteorBlastRadius
                //}, true);

                if (!NetworkServer.active)
                    return;
                GameObject gameObject2 = UnityEngine.Object.Instantiate<GameObject>(
                    LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast"),
                    meteor.impactPosition, Quaternion.identity);
                gameObject2.transform.localScale = Vector3.one * meteorBlastRadius;
                DelayBlast component = gameObject2.GetComponent<DelayBlast>();
                int level = 1;
                if (Run.instance)
                    level = Run.instance.ambientLevelFloor;
                if (component)
                {
                    component.position = meteor.impactPosition;
                    component.baseDamage = meteorBlastDamageCoefficient * (1 + meteorBlastDamageScalarPerLevel * level);//multiplies by ambient level. if this is unsatisfactory change later
                    component.baseForce = meteorBlastForce;
                    component.attacker = gameObject;
                    component.radius = meteorBlastRadius;
                    component.crit = false;
                    component.procCoefficient = 0f;
                    component.maxTimer = meteorImpactDelay;
                    component.falloffModel = meteorFalloffModel;
                    component.explosionEffect = meteorImpactEffectPrefab;
                    component.delayEffect = meteorWarningEffectPrefab;
                    component.damageType = DamageType.Generic;
                    component.damageType.AddModdedDamageType(StormsCore.stormDamageType);
                    TeamFilter component2 = gameObject2.GetComponent<TeamFilter>();
                    if (component2)
                    {
                        component2.teamIndex = TeamIndex.Monster;
                    }
                }
            }

            private bool GetMeteorViable(MeteorStormController.Meteor nextMeteor)
            {
                if (nextMeteor == null)
                    return false;
                if (!nextMeteor.valid)
                    return false;

                Vector3 impactPosition = nextMeteor.impactPosition;
                    //(Vector3)nextMeteor.GetType().GetField("impactPosition", 
                    //System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
                    //).GetValue(nextMeteor);
                if (ShelterUtilsModule.IsPositionSheltered(impactPosition, meteorBlastRadius))
                    return false;
                //if (!Physics.Raycast(impactPosition + Vector3.up * 0.1f, Vector3.down, out RaycastHit hit, 3f, LayerIndex.world.mask))
                //    return false;
                //if (Vector3.Dot(hit.normal, Vector3.up) < 0.8f)
                //    return false;
                return true;
            }

            private void DetonateMeteor(MeteorStormController.Meteor meteor)
            {
                int level = 1;
                if (Run.instance)
                    level = Run.instance.ambientLevelFloor;
                EffectData effectData = new EffectData
                {
                    origin = meteor.impactPosition
                };
                EffectManager.SpawnEffect(meteorImpactEffectPrefab, effectData, true);
                BlastAttack blast = new BlastAttack
                {
                    inflictor = base.gameObject,
                    baseDamage = meteorBlastDamageCoefficient * (1 + meteorBlastDamageScalarPerLevel * level),//multiplies by ambient level. if this is unsatisfactory change later
                    baseForce = meteorBlastForce,
                    attackerFiltering = AttackerFiltering.Default,
                    crit = false,
                    falloffModel = meteorFalloffModel,
                    attacker = this.gameObject,//this.teleporter ,
                    bonusForce = Vector3.zero,
                    damageColorIndex = DamageColorIndex.Fragile,
                    position = meteor.impactPosition,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = 0f,
                    teamIndex = TeamIndex.Monster,// | TeamIndex.Void | TeamIndex.Neutral,
                    radius = meteorBlastRadius
                };
                blast.AddModdedDamageType(StormsCore.stormDamageType);
                blast.Fire();
            }

            /// <summary>
            /// deprecated
            /// </summary>
            /// <param name="orig"></param>
            /// <param name="self"></param>
            /// <returns></returns>
            private MeteorStormController.Meteor MeteorWave_GetNextMeteor(On.RoR2.MeteorStormController.MeteorWave.orig_GetNextMeteor orig, MeteorStormController.MeteorWave self)
            {
                MeteorStormController.Meteor meteor = orig.Invoke(self);
                if (meteor != null && meteor.impactPosition == self.targets[self.currentStep].corePosition)
                    return null;
                return meteor;
            }

            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class StormWarning : BaseStormState
        {
            public override BaseStormState GetNextState()
            {
                if (!NetworkServer.active)
                    return new StormController.IdleState();
                return new StormController.StormActive();
            }
            private Dictionary<HUD, GameObject> hudPanels;
            public override StormState stormState => StormState.ApproachWarning;
            public override void OnEnter()
            {
                hudPanels = new Dictionary<HUD, GameObject>();
                base.OnEnter();

                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, hud.targetBodyObject != null);
                }
                SetCountdownTime(Mathf.Max(0, stormController.stormWarningTime - base.fixedAge));

                BroadcastStormWarningMessage(stormType);
            }
            public override void OnExit()
            {
                base.OnExit();
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, false);
                }
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (base.fixedAge >= stormController.stormWarningTime)
                {
                    SetNextState();
                }

                if (stormType == StormType.None || !Run.instance)
                {
                    if (this.hudPanels.Count > 0)
                    {
                        foreach (HUD hud in HUD.readOnlyInstanceList)
                        {
                            SetHudCountdownEnabled(hud, false);
                        }
                    }
                    return;
                }
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, hud.targetBodyObject != null && StormController.bossHealthBarActive == false);
                }
                SetCountdownTime(Mathf.Max(0, stormController.stormWarningTime - base.fixedAge));
            }


            private void SetHudCountdownEnabled(HUD hud, bool shouldEnableCountdownPanel)
            {
                shouldEnableCountdownPanel &= outer.enabled;
                if (hudPanels.TryGetValue(hud, out GameObject gameObject) != shouldEnableCountdownPanel)
                {
                    if (shouldEnableCountdownPanel && stormType != StormType.None)
                    {
                        RectTransform rectTransform = hud.GetComponent<ChildLocator>().FindChild("TopCenterCluster") as RectTransform;
                        if (rectTransform)
                        {
                            GameObject value = UnityEngine.Object.Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/UI/HudModules/HudCountdownPanel"), rectTransform);
                            LanguageTextMeshController ltmc = value.GetComponentInChildren<LanguageTextMeshController>();
                            ltmc._token = $"OBJECTIVE_{stormType.ToString().ToUpper()}_2R4R";
                            ltmc.token = $"OBJECTIVE_{stormType.ToString().ToUpper()}_2R4R";
                            this.hudPanels[hud] = value;
                            return;
                        }
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(gameObject);
                        this.hudPanels.Remove(hud);
                    }
                }
            }
            private void SetCountdownTime(double secondsRemaining)
            {
                foreach (KeyValuePair<HUD, GameObject> keyValuePair in this.hudPanels)
                {
                    keyValuePair.Value.GetComponent<TimerText>().seconds = secondsRemaining;
                }
                //AkSoundEngine.SetRTPCValue("EscapeTimer", Util.Remap((float)secondsRemaining, 0f, this.countdownDuration, 0f, 100f));
            }

            public override void Update()
            {
                base.Update();
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class StormApproach : BaseStormState
        {
            public override BaseStormState GetNextState()
            {
                if (stormType > StormType.None)
                {
                    if (stormController.stormWarningTime > 0)
                        return new StormWarning();
                    else
                        return new StormActive();
                }
                return new StormController.IdleState();
            }
            public override StormState stormState => StormState.Approaching;
            public override void OnEnter()
            {
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if (base.fixedAge >= stormController.stormDelayTime)
                {
                    SetNextState();
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class IdleState : BaseStormState
        {
            public override BaseStormState GetNextState()
            {
                return new IdleState();
            }
            public override StormState stormState => StormState.Idle;
        }
    }
}
