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
using RiskierRainContent.CoreModules;
using RiskierRainContent.Changes.Aspects;
using static RoR2.CombatDirector;
using RoR2.UI;
using UnityEngine.Networking;

namespace RiskierRainContent
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static GameObject StormsRunBehaviorPrefab;
        public static GameObject StormsControllerPrefab;
        public static EliteTierDef StormT1;
        public static EliteTierDef StormT2;

        void InitializeStorms()
        {
            CreateStormEliteTiers();
            CreateStormsRunBehaviorPrefab();

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
            StormT1.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormEventDirector.instance && StormEventDirector.instance.hasBegunStorm);
            StormT1.eliteTypes = new EliteDef[0];
            //EliteAPI.AddCustomEliteTier(StormT1);

            StormT2 = new EliteTierDef();
            StormT2.costMultiplier = 2;
            StormT2.canSelectWithoutAvailableEliteDef = false;
            StormT2.isAvailable = ((SpawnCard.EliteRules rules) => rules == SpawnCard.EliteRules.Default && StormEventDirector.instance && StormEventDirector.instance.hasBegunStorm && 
                    !RiskierRainContent.is2R4RLoaded ? (Run.instance.loopClearCount > 0) :
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
            erc.requiredExpansion = RiskierRainContent.expansionDef;

            StormsRunBehaviorPrefab.AddComponent<StormEventDirector>();

            RiskierRainContent.expansionDef.runBehaviorPrefab = StormsRunBehaviorPrefab;
            Assets.networkedObjectPrefabs.Add(StormsRunBehaviorPrefab);

            StormsControllerPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Common/Director.prefab").WaitForCompletion().InstantiateClone("2R4RStormController", true);
            MonoBehaviour[] components = StormsControllerPrefab.GetComponentsInChildren<MonoBehaviour>();
            bool directorInstanceFound = false;
            foreach(MonoBehaviour component in components)
            {
                if(component is CombatDirector && directorInstanceFound == false)
                {
                    CombatDirector cd = (component as CombatDirector);
                    cd.creditMultiplier = 0.5f;
                    cd.expRewardCoefficient = 1f;
                    cd.goldRewardCoefficient = 1f;
                    cd.minRerollSpawnInterval = 15f;
                    cd.maxRerollSpawnInterval = 25f;
                    cd.teamIndex = TeamIndex.Monster;

                    directorInstanceFound = true;
                }
                else
                {
                    Destroy(component);
                }
            }

            StormsControllerPrefab.AddComponent<StormHazardController>();
            StormsControllerPrefab.AddComponent<NetworkIdentity>();
            Assets.networkedObjectPrefabs.Add(StormsRunBehaviorPrefab);
        }
    }

    /// <summary>
    /// Handles event timing + resetting on stage start
    /// </summary>
    public class StormEventDirector : MonoBehaviour
    {
        public static float GetStormStartDelay()
        {
            if (instance != null && instance.stormStartDelay != -1)
                return instance.stormStartDelay;

            float delay = 0;
            switch (Run.instance.selectedDifficulty)
            {
                default:
                    delay = monsoonStormDelay;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    delay = rainstormStormDelay;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    delay = drizzleStormDelay;
                    break;
            }
            return delay * 60;
        }
        public static StormType GetStormType()
        {
            SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            StormType st = StormType.None;
            if (currentScene.sceneType == SceneType.Stage)
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

        public const float drizzleStormDelay = 10;
        public const float rainstormStormDelay = 7;
        public const float monsoonStormDelay = 4;
        public enum StormType
        {
            None,
            MeteorDefault,
            Lightning,
            Fire,
            Cold
        }

        public static StormEventDirector instance;
        internal CombatDirector stormControllerInstance;
        StormType stormType = StormType.None;

        float stageBeginTime;
        public float stormStartDelay = -1;
        public float stormStartRandomDelay = -1;
        public float stormEarlyWarningDelay = 0;
        public float stormEarlyWarningTime => stormStartTime - stormEarlyWarningDelay;
        bool hasSentStormEarlyWarning = false;
        public float stormStartTime => stageBeginTime + stormStartDelay + stormStartRandomDelay;
        public bool hasBegunStorm = false;

        internal bool teleporterActive = false;
        internal GameObject teleporter;
        internal List<HoldoutZoneController> holdoutZones = new List<HoldoutZoneController>();


        private Dictionary<HUD, GameObject> hudPanels;

        public void Start()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
            On.RoR2.Run.EndStage += Run_EndStage;

            On.RoR2.TeleporterInteraction.OnInteractionBegin += TeleporterInteraction_OnInteractionBegin;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter += TeleporterInteraction_ChargedState_OnEnter;

            On.RoR2.HoldoutZoneController.OnEnable += RegisterHoldoutZone;
            On.RoR2.HoldoutZoneController.OnDisable += UnregisterHoldoutZone;

            hudPanels = new Dictionary<HUD, GameObject>();
        }

        public void OnDestroy()
        {
            On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;
            On.RoR2.Run.EndStage -= Run_EndStage;

            On.RoR2.TeleporterInteraction.OnInteractionBegin -= TeleporterInteraction_OnInteractionBegin;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter -= TeleporterInteraction_ChargedState_OnEnter;

            On.RoR2.HoldoutZoneController.OnEnable -= RegisterHoldoutZone;
            On.RoR2.HoldoutZoneController.OnDisable -= UnregisterHoldoutZone;
        }

        #region hooks
        private void Run_EndStage(On.RoR2.Run.orig_EndStage orig, Run self)
        {
            if(stormControllerInstance.gameObject != null)
            {
                stormControllerInstance.onSpawnedServer.RemoveListener(OnStormDirectorSpawnServer);
                Destroy(stormControllerInstance.gameObject);
            }
            stormType = StormType.None;
            stormStartDelay = -1;
            hasSentStormEarlyWarning = false;
            hasBegunStorm = false;
            teleporterActive = false;
            this.teleporter = null;
        }
        private void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            stormType = GetStormType();
            stageBeginTime = RoR2.Run.instance.GetRunStopwatch();
            stormStartDelay = GetStormStartDelay(); 
            stormEarlyWarningDelay = stormStartDelay / 6;
            stormStartRandomDelay = Run.instance.stageRng.RangeInt(0, 60); // up to 1 minute of additional delay
            this.teleporter = teleporter;

            orig(self, sceneDirector, teleporter);
        }

        private void TeleporterInteraction_ChargedState_OnEnter(On.RoR2.TeleporterInteraction.ChargedState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            teleporterActive = false;
        }

        private void TeleporterInteraction_OnInteractionBegin(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor activator)
        {
            orig.Invoke(self, activator);
            this.teleporterActive = true;
        }

        private void RegisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnEnable orig, HoldoutZoneController self)
        {
            orig(self);
            holdoutZones.Add(self);
        }
        private void UnregisterHoldoutZone(On.RoR2.HoldoutZoneController.orig_OnDisable orig, HoldoutZoneController self)
        {
            orig(self);
            holdoutZones.Remove(self);
        }
        #endregion

        void FixedUpdate()
        {
            if (stormType == StormType.None || !Run.instance || !this.teleporter)
            {
                if(this.hudPanels.Count > 0)
                {
                    foreach (HUD hud in HUD.readOnlyInstanceList)
                    {
                        SetHudCountdownEnabled(hud, false);
                    }
                }
                return;
            }

            float currentTime = RoR2.Run.instance.GetRunStopwatch();
            if (!hasSentStormEarlyWarning && currentTime > stormEarlyWarningTime)
            {
                hasSentStormEarlyWarning = true;
                DoStormEarlyWarning();
            }
            if (!hasBegunStorm && currentTime > stormStartTime)
            {
                hasBegunStorm = true;
                DoStormWarning();
                BeginStorm();
            }

            if (hasSentStormEarlyWarning && !teleporterActive)
            {
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, hud.targetBodyObject);
                }
                SetCountdownTime(Mathf.Max(0, stormStartTime - currentTime));
            }
            else
            {
                foreach (HUD hud in HUD.readOnlyInstanceList)
                {
                    SetHudCountdownEnabled(hud, false);
                }
            }
        }
        #region warning message
        private void DoStormEarlyWarning()
        {
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

            //the message thing. make its own method mebbe
            RoR2.Chat.AddMessage(warningMessage);
        }

        private void DoStormWarning()
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
        }
        #endregion
        private void SetHudCountdownEnabled(HUD hud, bool shouldEnableCountdownPanel)
        {
            shouldEnableCountdownPanel &= base.enabled;
            GameObject gameObject;
            this.hudPanels.TryGetValue(hud, out gameObject);
            if (gameObject != shouldEnableCountdownPanel)
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

        #region do storms
        private void BeginStorm()
        {
            GameObject stormController = Instantiate(RiskierRainContent.StormsControllerPrefab);

            stormControllerInstance = stormController.GetComponent<CombatDirector>();
            //cd._monsterCards = ClassicStageInfo.instance.monsterCategories;
            stormControllerInstance.onSpawnedServer.AddListener(new UnityEngine.Events.UnityAction<GameObject>(OnStormDirectorSpawnServer));
            
            Debug.LogWarning("Beginning Storm");
        }

        private void OnStormDirectorSpawnServer(GameObject masterObject)
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
        #endregion
    }

    /// <summary>
    /// Handles storm environmental hazards
    /// </summary>
    public class StormHazardController : MonoBehaviour
    {
        private List<HoldoutZoneController> holdoutZones => StormEventDirector.instance.holdoutZones;
        bool teleporterActive => StormEventDirector.instance.teleporterActive;

        //all the projectile/prefab stuff
        public float waveMinInterval = 1f;
        public float waveMaxInterval = 2f;

        private List<MeteorStormController.Meteor> meteorList;
        private List<MeteorStormController.MeteorWave> waveList;
        private float waveTimer;

        //meteors:
        GameObject meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion();
        GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").WaitForCompletion();
        public float meteorTravelEffectDuration = 2;
        public float meteorImpactDelay = 2.5f;
        public float meteorBlastDamageCoefficient = 10;
        public float meteorBlastRadius = 14;
        public float meteorBlastForce = 0;

        public void Start()
        {
            this.meteorList = new List<MeteorStormController.Meteor>();
            this.waveList = new List<MeteorStormController.MeteorWave>();

            On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor += MeteorWave_GetNextMeteor;
        }
        public void OnDestroy()
        {
            On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor -= MeteorWave_GetNextMeteor;
        }
        public void RemoveStormController()
        {
            Destroy(this.gameObject);
        }

        void FixedUpdate()
        {
            if (Run.instance == null)
            {
                RemoveStormController();
                return;
            }

            //thisa is just for meteor stuff; we can make it work for the other storsm when they start existing lol.
            this.waveTimer -= Time.fixedDeltaTime;
            if (this.waveTimer <= 0f)
            {
                this.waveTimer = UnityEngine.Random.Range(this.waveMinInterval, this.waveMaxInterval);
                MeteorStormController.MeteorWave item = new MeteorStormController.MeteorWave(CharacterBody.readOnlyInstancesList.ToArray<CharacterBody>(), base.transform.position);
                this.waveList.Add(item);
            }

            for (int i = this.waveList.Count - 1; i >= 0; i--)
            {
                MeteorStormController.MeteorWave meteorWave = this.waveList[i];
                meteorWave.timer -= Time.fixedDeltaTime;
                if (meteorWave.timer <= 0f)
                {
                    meteorWave.timer = UnityEngine.Random.Range(0.05f, 1f);
                    MeteorStormController.Meteor nextMeteor = meteorWave.GetNextMeteor(); // getnextmeteor handles some stuff here, we can look into canibalizing it for more adaptable stuff
                    if (nextMeteor == null)
                    {
                        this.waveList.RemoveAt(i);
                    }
                    else if (nextMeteor.valid)
                    {
                        this.meteorList.Add(nextMeteor);
                        EffectManager.SpawnEffect(this.meteorWarningEffectPrefab, new EffectData
                        {
                            origin = nextMeteor.impactPosition,
                            scale = this.meteorBlastRadius
                        }, true);
                    }
                }
            }

            float num = Run.instance.time - this.meteorImpactDelay;
            float num2 = num - this.meteorTravelEffectDuration;
            for (int j = this.meteorList.Count - 1; j >= 0; j--)
            {
                MeteorStormController.Meteor meteor = this.meteorList[j];
                if (meteor.startTime < num)
                {
                    this.meteorList.RemoveAt(j);
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
            EffectManager.SpawnEffect(this.meteorImpactEffectPrefab, effectData, true);
            new BlastAttack
            {
                inflictor = base.gameObject,
                baseDamage = this.meteorBlastDamageCoefficient * Run.instance.ambientLevel,//multiplies by ambient level. if this is unsatisfactory change later
                baseForce = this.meteorBlastForce,
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
            if (holdoutZones.Count == 0)
                return meteor;

            try
            {
                foreach(HoldoutZoneController holdoutZone in holdoutZones)
                {
                    if(holdoutZone == null)
                    {
                        holdoutZones.Remove(holdoutZone);
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

                    if (this.IsInRange(impactPosition, holdoutZone.transform.position, holdoutZone.currentRadius + meteorBlastRadius))
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
        }
        private bool IsInRange(Vector3 a, Vector3 b, float dist)
        {
            return (a - b).sqrMagnitude <= dist * dist;
        }
    }
}
