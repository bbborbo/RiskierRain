using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using System.Linq;
using UnityEngine.AddressableAssets;
using EntityStates;
using R2API;
using RoR2.ExpansionManagement;
using SwanSongExtended.Elites;
using static RoR2.CombatDirector;
using RoR2.UI;
using UnityEngine.Networking;
using static SwanSongExtended.StormRunBehaviorController;
using SwanSongExtended.Modules;

namespace SwanSongExtended
{
    public partial class SwanSongPlugin : BaseUnityPlugin
    {
        public static GameObject StormsRunBehaviorPrefab;
        public static GameObject StormsControllerPrefab;
        public static EliteTierDef StormT1;
        public static EliteTierDef StormT2;

        void InitializeStorms()
        {
            CreateStormEliteTiers();
            CreateStormsRunBehaviorPrefab();

            meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion().InstantiateClone("StormStrikePredictionEffect");
            meteorWarningEffectPrefab.transform.localScale = Vector3.one * StormRunBehaviorController.meteorBlastRadius * 0.85f;
            DestroyOnTimer DOT = meteorWarningEffectPrefab.GetComponent<DestroyOnTimer>();
            if (DOT)
            {
                DOT.duration = StormRunBehaviorController.meteorImpactDelay + 0.5f;
            }
            Transform indicator = meteorWarningEffectPrefab.transform.Find("GroundSlamIndicator");
            if (indicator)
            {
                AnimateShaderAlpha asa = indicator.GetComponent<AnimateShaderAlpha>();
                if (asa)
                {
                    asa.timeMax = StormRunBehaviorController.meteorImpactDelay + 0.1f;
                }
                MeshRenderer meshRenderer = indicator.GetComponent<MeshRenderer>();
                if (meshRenderer)
                {
                    Material mat = UnityEngine.Object.Instantiate(meshRenderer.material);
                    mat.name = "matStormStrikeImpactIndicator";
                    meshRenderer.material = mat;
                    mat.SetFloat("_Boost", 0.64f);
                    mat.SetFloat("_AlphaBoost", 4.29f);
                    mat.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>("RoR2/Base/Common/ColorRamps/texRampArtifactShellSoft.png").WaitForCompletion());
                    mat.SetColor("_TintColor", Color.white);
                }
            }
            Content.CreateAndAddEffectDef(meteorWarningEffectPrefab);

            LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "Meteor Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_LIGHTNING_2R4R", "Thunderstorm Imminent");
            LanguageAPI.Add($"OBJECTIVE_FIRE_2R4R", "Fire Storm Imminent");
            LanguageAPI.Add($"OBJECTIVE_COLD_2R4R", "Blizzard Imminent");
            //LanguageAPI.Add($"OBJECTIVE_METEORDEFAULT_2R4R", "");
        }

        private void CreateStormEliteTiers()
        {
            StormT1 = new EliteTierDef();
            StormT1.costMultiplier = 2;
            StormT1.canSelectWithoutAvailableEliteDef = false;
            StormT1.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehaviorController.instance && StormRunBehaviorController.instance.hasBegunStorm);
            StormT1.eliteTypes = new EliteDef[0];
            //EliteAPI.AddCustomEliteTier(StormT1);

            StormT2 = new EliteTierDef();
            StormT2.costMultiplier = 2;
            StormT2.canSelectWithoutAvailableEliteDef = false;
            StormT2.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormRunBehaviorController.instance && StormRunBehaviorController.instance.hasBegunStorm && 
                    !SwanSongPlugin.is2R4RLoaded ? (Run.instance.loopClearCount > 0) :
                    ((Run.instance.stageClearCount >= 10 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount >= 5 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard)));
            StormT2.eliteTypes = new EliteDef[0];
            //EliteAPI.AddCustomEliteTier(StormT2);
        }

        private static void CreateStormsRunBehaviorPrefab()
        {
            StormsRunBehaviorPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/DLC1RunBehavior.prefab").WaitForCompletion().InstantiateClone("2R4RExpansionRunBehavior", true);

            ExpansionRequirementComponent erc = StormsRunBehaviorPrefab.GetComponent<ExpansionRequirementComponent>();
            erc.requiredExpansion = SwanSongPlugin.expansionDefSS2;

            StormsRunBehaviorPrefab.AddComponent<StormRunBehaviorController>();

            SwanSongPlugin.expansionDefSS2.runBehaviorPrefab = StormsRunBehaviorPrefab;

            StormsControllerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion().InstantiateClone("2R4RStormController", true);
            MonoBehaviour[] components = StormsControllerPrefab.GetComponentsInChildren<MonoBehaviour>();
            bool directorInstanceFound = false;
            foreach(MonoBehaviour component in components)
            {
                if(component is CombatDirector cd && directorInstanceFound == false)
                {
                    cd.creditMultiplier = 0.5f;
                    cd.expRewardCoefficient = 1f;
                    cd.goldRewardCoefficient = 0f;
                    cd.minRerollSpawnInterval = 15f;
                    cd.maxRerollSpawnInterval = 25f;
                    cd.teamIndex = TeamIndex.Monster;

                    directorInstanceFound = true;
                    cd.onSpawnedServer.AddListener(OnStormDirectorSpawnServer);

                }
                else
                {
                    Destroy(component);
                }
            }

            EntityStateMachine esm = StormsControllerPrefab.AddComponent<EntityStateMachine>();
            esm.initialStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            esm.mainStateType = new SerializableEntityStateType(typeof(StormController.StormApproach));
            StormsControllerPrefab.AddComponent<StormController>();
            StormsControllerPrefab.AddComponent<NetworkIdentity>();

            Content.AddNetworkedObjectPrefab(StormsRunBehaviorPrefab);
            Content.AddEntityState(typeof(StormController.IdleState));
            Content.AddEntityState(typeof(StormController.StormApproach));
            Content.AddEntityState(typeof(StormController.StormWarning));
            Content.AddEntityState(typeof(StormController.StormActive));

            void OnStormDirectorSpawnServer(GameObject masterObject)
            {
                EliteDef eliteDef = WhirlwindAspect.instance.EliteDef;
                if (Util.CheckRoll(50))
                    eliteDef = SurgingAspect.instance.EliteDef;

                EquipmentIndex equipmentIndex = EquipmentIndex.None;
                if (eliteDef != null)
                {
                    EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                    equipmentIndex = ((eliteEquipmentDef != null) ? eliteEquipmentDef.equipmentIndex : EquipmentIndex.None);
                }

                CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
                GameObject bodyObject = component.GetBodyObject();
                if (bodyObject)
                {
                    foreach (EntityStateMachine entityStateMachine in bodyObject.GetComponents<EntityStateMachine>())
                    {
                        entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                    }
                }
                if (equipmentIndex != EquipmentIndex.None)
                {
                    Debug.LogWarning("Spawning Storm Elite: " + eliteDef.name);
                    component.inventory.SetEquipmentIndex(equipmentIndex);
                }
            }
        }
    }

    /// <summary>
    /// Creates a StormController for each stage with appropriate properties
    /// </summary>
    public class StormRunBehaviorController : MonoBehaviour
    {
        public static StormType GetStormType()
        {
            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            StormType st = StormType.None;
            if (currentScene.sceneType == SceneType.Stage && !currentScene.isFinalStage)
            {
                switch (currentScene.baseSceneName)
                {
                    default:
                        st = StormType.MeteorDefault;
                        break;
                }
            }

            return st;
        }

        public const float drizzleStormDelayMinutes = 10;
        public const float drizzleStormWarningMinutes = 3;
        public const float rainstormStormDelayMinutes = 7;
        public const float rainstormStormWarningMinutes = 2;
        public const float monsoonStormDelayMinutes = 3.5f;
        public const float monsoonStormWarningMinutes = 1f;

        public enum StormType
        {
            None,
            MeteorDefault,
            Lightning,
            Fire,
            Cold
        }

        public static StormRunBehaviorController instance;
        public StormController stormControllerInstance;
        public bool hasBegunStorm
        {
            get
            {
                if (stormControllerInstance == null)
                    return false;
                if (stormControllerInstance.stormState >= StormController.StormState.Active)
                    return true;
                return false;
            }
        }
        public StormType stormType { get; private set; } = StormType.None;


        internal List<HoldoutZoneController> holdoutZones = new List<HoldoutZoneController>();


        //meteors:
        public static GameObject meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion();
        public static GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").WaitForCompletion();
        public static float waveMinInterval = 1.3f;
        public static float waveMaxInterval = 2f;
        public static float meteorTravelEffectDuration = 0f;
        public static float meteorImpactDelay = 2.5f;
        public static float meteorBlastDamageCoefficient = 55;
        public static float meteorBlastDamageScalarPerLevel = 0.5f;
        public static float meteorBlastRadius = 12;
        public static float meteorBlastForce = 0;

        public void Start()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            On.RoR2.Stage.BeginServer += Stage_BeginServer;

            On.RoR2.HoldoutZoneController.OnEnable += RegisterHoldoutZone;
            On.RoR2.HoldoutZoneController.OnDisable += UnregisterHoldoutZone;

        }

        public void OnDestroy()
        {
            On.RoR2.Stage.BeginServer -= Stage_BeginServer;

            On.RoR2.HoldoutZoneController.OnEnable -= RegisterHoldoutZone;
            On.RoR2.HoldoutZoneController.OnDisable -= UnregisterHoldoutZone;
        }

        #region hooks

        private void Stage_BeginServer(On.RoR2.Stage.orig_BeginServer orig, Stage self)
        {
            orig(self);

            stormType = GetStormType();
            if (stormType == StormType.None)
                return;

            GameObject stormControllerObject = Instantiate(SwanSongPlugin.StormsControllerPrefab);
            stormControllerInstance = stormControllerObject.GetComponent<StormController>();

            float a = drizzleStormDelayMinutes;
            float b = drizzleStormWarningMinutes;
            if (Run.instance.selectedDifficulty >= DifficultyIndex.Hard)
            {
                a = monsoonStormDelayMinutes;
                b = monsoonStormWarningMinutes;
            }
            else if (Run.instance.selectedDifficulty == DifficultyIndex.Normal)
            {
                a = rainstormStormDelayMinutes;
                b = rainstormStormWarningMinutes;
            }
            stormControllerInstance.BeginStormApproach(a + Run.instance.stageRng.RangeInt(0, 1), b);
        }

        private void RegisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            if(!holdoutZones.Contains(self))
                holdoutZones.Add(self);
        }
        private void UnregisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnDisable orig, HoldoutZoneController self)
        {
            orig(self);
            if(holdoutZones.Contains(self))
                holdoutZones.Remove(self);
        }
        #endregion
    }

    /// <summary>
    /// Handles storm event timing and hazards during storms
    /// </summary>
    [RequireComponent(typeof(EntityStateMachine), typeof(CombatDirector))]
    public class StormController : MonoBehaviour
    {
        public CombatDirector combatDirector;
        public EntityStateMachine mainStateMachine;
        private StormController.BaseStormState currentState
        {
            get
            {
                return this.mainStateMachine.state as StormController.BaseStormState;
            }
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
        protected List<HoldoutZoneController> holdoutZones => StormRunBehaviorController.instance.holdoutZones;
        internal float stormDelayTime = 0;
        internal float stormWarningTime = 0;
        

        public void Awake()
        {
            combatDirector = GetComponent<CombatDirector>();
            combatDirector.enabled = false;
            mainStateMachine = GetComponent<EntityStateMachine>();
        }

        public void BeginStormApproach(float stormDelayTime, float stormWarningTime)
        {
            this.stormDelayTime = stormDelayTime * 60;
            this.stormWarningTime = stormWarningTime * 60;
            Debug.Log("Starting storm approach");
            mainStateMachine.SetNextState(new StormApproach());
        }
        public void ForceBeginStorm()
        {
            if(this.stormState < StormState.ApproachWarning)
            {
                mainStateMachine.SetNextState(new StormWarning());
            }
        }

        public enum StormState
        {
            Idle,
            Approaching,
            ApproachWarning,
            Active
        }
        internal abstract class BaseStormState : BaseState
        {
            public abstract StormState stormState { get; }
            private protected StormRunBehaviorController.StormType stormType => StormRunBehaviorController.instance.stormType;
            private protected StormController stormController { get; private set; }
            public override void OnEnter()
            {
                base.OnEnter();
                this.stormController = base.GetComponent<StormController>();
            }

            public void EnableDirector()
            {
                //stormController.combatDirector.enabled = true;
            }
        }
        internal class IdleState : BaseStormState
        {
            public override StormState stormState => StormState.Idle;
        }
        internal class StormApproach : BaseStormState
        {
            public override StormState stormState => StormState.Approaching;
            public override void OnEnter()
            {
                base.OnEnter();
            }

            public override void FixedUpdate()
            {
                base.FixedUpdate();
                if(base.fixedAge >= stormController.stormDelayTime && NetworkServer.active)
                {
                    if(stormType > StormType.None)
                    {
                        outer.SetNextState(new StormWarning());
                    }
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
        internal class StormWarning : BaseStormState
        {
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

                string warningMessage = "";
                switch (stormType)
                {
                    case StormRunBehaviorController.StormType.MeteorDefault:
                        warningMessage = "<style=cIsUtility>A meteor storm is approaching...</style>";
                        break;
                    case StormRunBehaviorController.StormType.Lightning:
                        warningMessage = "A storm approaches...";
                        break;
                    case StormRunBehaviorController.StormType.Fire:
                        warningMessage = "A meteor storm is approaching...";
                        break;
                    case StormRunBehaviorController.StormType.Cold:
                        warningMessage = "The air around you begins to freeze...";
                        break;
                }

                RoR2.Chat.AddMessage(warningMessage);
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
                if (base.fixedAge >= stormController.stormWarningTime && NetworkServer.active)
                {
                    string warningMessage = "";
                    switch (stormType)
                    {
                        case StormType.MeteorDefault:
                            warningMessage = "<style=cIsUtility>A shower of meteors begins to fall...</style>";
                            break;
                        case StormType.Lightning:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                        case StormType.Fire:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                        case StormType.Cold:
                            warningMessage = "A meteor storm is approaching...";
                            break;
                    }

                    RoR2.Chat.AddMessage(warningMessage);

                    outer.SetNextState(new StormActive());
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
                    SetHudCountdownEnabled(hud, hud.targetBodyObject != null);
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
        internal class StormActive : BaseStormState
        {
            public override StormState stormState => StormState.Active;

            //all the projectile/prefab stuff

            private List<MeteorStormController.Meteor> meteorsToDetonate;
            private List<MeteorStormController.MeteorWave> meteorWaves;
            private float waveTimer;

            public override void OnEnter()
            {
                base.OnEnter();
                this.meteorsToDetonate = new List<MeteorStormController.Meteor>();
                this.meteorWaves = new List<MeteorStormController.MeteorWave>();

                On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor += MeteorWave_GetNextMeteor;
                EnableDirector();
            }
            public override void OnExit()
            {
                base.OnExit();
                On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor -= MeteorWave_GetNextMeteor;
            }
            public override void FixedUpdate()
            {
                base.FixedUpdate();
                //thisa is just for meteor stuff; we can make it work for the other storsm when they start existing lol.
                this.waveTimer -= Time.fixedDeltaTime;
                if (this.waveTimer <= 0f)
                {
                    this.waveTimer = UnityEngine.Random.Range(StormRunBehaviorController.waveMinInterval, StormRunBehaviorController.waveMaxInterval);
                    MeteorStormController.MeteorWave item = new MeteorStormController.MeteorWave(CharacterBody.readOnlyInstancesList.ToArray<CharacterBody>(), base.transform.position);
                    this.meteorWaves.Add(item);
                }

                for (int i = this.meteorWaves.Count - 1; i >= 0; i--)
                {
                    MeteorStormController.MeteorWave meteorWave = this.meteorWaves[i];
                    meteorWave.timer -= Time.fixedDeltaTime;
                    if (meteorWave.timer <= 0f)
                    {
                        meteorWave.timer = UnityEngine.Random.Range(0.05f, 1f);
                        MeteorStormController.Meteor nextMeteor = meteorWave.GetNextMeteor(); // getnextmeteor handles some stuff here, we can look into canibalizing it for more adaptable stuff
                        if (nextMeteor == null)
                        {
                            this.meteorWaves.RemoveAt(i);
                        }
                        else if (nextMeteor.valid)
                        {
                            this.meteorsToDetonate.Add(nextMeteor);
                            EffectManager.SpawnEffect(StormRunBehaviorController.meteorWarningEffectPrefab, new EffectData
                            {
                                origin = nextMeteor.impactPosition,
                                scale = StormRunBehaviorController.meteorBlastRadius
                            }, true);
                        }
                    }
                }

                float num = Run.instance.time - StormRunBehaviorController.meteorImpactDelay;
                float num2 = num - StormRunBehaviorController.meteorTravelEffectDuration;
                for (int j = this.meteorsToDetonate.Count - 1; j >= 0; j--)
                {
                    MeteorStormController.Meteor meteor = this.meteorsToDetonate[j];
                    if (meteor.startTime < num)
                    {
                        this.meteorsToDetonate.RemoveAt(j);
                        this.DetonateMeteor(meteor);
                    }
                }
            }

            private void DetonateMeteor(MeteorStormController.Meteor meteor)
            {
                EffectData effectData = new EffectData
                {
                    origin = meteor.impactPosition
                };
                EffectManager.SpawnEffect(StormRunBehaviorController.meteorImpactEffectPrefab, effectData, true);
                new BlastAttack
                {
                    inflictor = base.gameObject,
                    baseDamage = StormRunBehaviorController.meteorBlastDamageCoefficient * (1 + meteorBlastDamageScalarPerLevel * Run.instance.ambientLevel),//multiplies by ambient level. if this is unsatisfactory change later
                    baseForce = StormRunBehaviorController.meteorBlastForce,
                    attackerFiltering = AttackerFiltering.Default,
                    crit = false,
                    falloffModel = BlastAttack.FalloffModel.SweetSpot,
                    attacker = this.gameObject,//this.teleporter ,
                    bonusForce = Vector3.zero,
                    damageColorIndex = DamageColorIndex.Fragile,
                    position = meteor.impactPosition,
                    procChainMask = default(ProcChainMask),
                    procCoefficient = 0f,
                    teamIndex = TeamIndex.Monster,// | TeamIndex.Void | TeamIndex.Neutral,
                    radius = meteorBlastRadius
                }.Fire();
            }

            //teleporter safe zone
            private object MeteorWave_GetNextMeteor(On.RoR2.MeteorStormController.MeteorWave.orig_GetNextMeteor orig, object self)
            {
                object meteor = orig.Invoke(self);
                if (stormController.holdoutZones.Count == 0)
                    return meteor;

                try
                {
                    foreach (HoldoutZoneController holdoutZone in stormController.holdoutZones)
                    {
                        if (holdoutZone == null)
                        {
                            stormController.holdoutZones.Remove(holdoutZone);
                            continue;
                        }
                        if (!holdoutZone.isActiveAndEnabled || holdoutZone.charge <= 0)
                        {
                            continue;
                        }

                        //i have no goddamn clue what this does lmao
                        //this uses reflection to find the impact position of the meteor that was spawned -borbo
                        Vector3 impactPosition = (Vector3)meteor.GetType()
                            .GetField("impactPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(meteor);

                        if (IsInRange(impactPosition, holdoutZone.transform.position, holdoutZone.currentRadius + meteorBlastRadius))
                        {
                            meteor = null;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogError(ex.Message);
                }

                return meteor;

                bool IsInRange(Vector3 a, Vector3 b, float dist)
                {
                    return (a - b).sqrMagnitude <= dist * dist;
                }
            }
            public override InterruptPriority GetMinimumInterruptPriority()
            {
                return InterruptPriority.Death;
            }
        }
    }
}
