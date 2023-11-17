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

namespace RiskierRainContent
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static GameObject StormsRunBehaviorPrefab;
        public static EliteTierDef StormT1;
        public static EliteTierDef StormT2;

        void InitializeStorms()
        {
            CreateStormEliteTiers();
            CreateStormsRunBehaviorPrefab();
        }

        private void CreateStormEliteTiers()
        {
            StormT1 = new EliteTierDef();
            StormT1.costMultiplier = 2;
            StormT1.canSelectWithoutAvailableEliteDef = false;
            StormT1.isAvailable = ((SpawnCard.EliteRules rules) => StormDirector.instance && StormDirector.instance.hasBegunStorm);
            //EliteAPI.AddCustomEliteTier(StormT1);

            StormT2 = new EliteTierDef();
            StormT2.costMultiplier = 2;
            StormT2.canSelectWithoutAvailableEliteDef = false;
            StormT2.isAvailable = ((SpawnCard.EliteRules rules) => StormDirector.instance && StormDirector.instance.hasBegunStorm && 
                    !RiskierRainContent.is2R4RLoaded ? (Run.instance.loopClearCount > 0) :
                    ((Run.instance.stageClearCount >= 10 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty <= DifficultyIndex.Easy)
                    || (Run.instance.stageClearCount >= 5 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Normal)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty == DifficultyIndex.Hard)
                    || (Run.instance.stageClearCount >= 3 && rules == SpawnCard.EliteRules.Default && Run.instance.selectedDifficulty > DifficultyIndex.Hard)));
            //EliteAPI.AddCustomEliteTier(StormT2);
            return;
        }

        private static void CreateStormsRunBehaviorPrefab()
        {
            StormsRunBehaviorPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Common/DLC1RunBehavior.prefab").WaitForCompletion().InstantiateClone("2R4RExpansionRunBehavior", true);

            ExpansionRequirementComponent erc = StormsRunBehaviorPrefab.GetComponent<ExpansionRequirementComponent>();
            erc.requiredExpansion = RiskierRainContent.expansionDef;

            StormsRunBehaviorPrefab.AddComponent<StormDirector>();

            RiskierRainContent.expansionDef.runBehaviorPrefab = StormsRunBehaviorPrefab;
            Assets.networkedObjectPrefabs.Add(StormsRunBehaviorPrefab);
        }
    }

    public class StormDirector : MonoBehaviour
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
            float random = Run.instance.stageRng.RangeFloat(0, 1); // up to 1 minute of additional delay
            return (delay + random) * 60;
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
        public const float rainstormStormDelay = 5;
        public const float monsoonStormDelay = 3;
        public enum StormType
        {
            None,
            MeteorDefault,
            Lightning,
            Fire,
            Cold
        }

        public static StormDirector instance;
        StormType stormType = StormType.None;

        float stageBeginTime;
        public float stormStartDelay = -1;
        public float stormEarlyWarningTime => stormStartTime - 30;
        bool hasSentStormEarlyWarning = false;
        public float stormStartTime => stageBeginTime + stormStartDelay;
        public bool hasBegunStorm = false;

        internal bool teleporterActive = false;
        internal TeleporterInteraction teleporter;

        public void Start()
        {
            Debug.LogError("ASHDAJHSDHASDHJKASHJKDKSJSD STORMS DIRECTOR");

            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;


            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;

            On.RoR2.TeleporterInteraction.OnInteractionBegin += TeleporterInteraction_OnInteractionBegin;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter += TeleporterInteraction_ChargedState_OnEnter;
        }

        public void OnDestroy()
        {
            On.RoR2.Run.OnServerTeleporterPlaced -= Run_OnServerTeleporterPlaced;

            On.RoR2.TeleporterInteraction.OnInteractionBegin -= TeleporterInteraction_OnInteractionBegin;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter -= TeleporterInteraction_ChargedState_OnEnter;
        }
        public void RemoveStormController()
        {
            Destroy(this.gameObject);
        }

        private void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            stormType = StormType.None;
            stormStartDelay = -1;
            hasSentStormEarlyWarning = false;
            hasBegunStorm = false;
            teleporterActive = false;

            stormType = GetStormType();
            stormStartDelay = GetStormStartDelay();
            stageBeginTime = RoR2.Run.instance.GetRunStopwatch();
            Debug.LogWarning(stormType + ", " + stageBeginTime + " - " + stormStartTime);
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
            this.teleporter = self;
            this.teleporterActive = true;
        }

        void FixedUpdate()
        {
            if (stormType == StormType.None || !Run.instance)
                return;

            float currentTime = RoR2.Run.instance.GetRunStopwatch();
            if (!hasSentStormEarlyWarning && currentTime > stormEarlyWarningTime)
            {
                hasSentStormEarlyWarning = true;
                SendStormEarlyWarning();
            }
            if (!hasBegunStorm && currentTime > stormStartTime)
            {
                hasBegunStorm = true;
                SendStormWarning();
                BeginStorm();
            }
            if (hasBegunStorm)
            {
                //StormBehavior();
            }
        }

        private void SendStormEarlyWarning()
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

        private void SendStormWarning()
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

        private void BeginStorm()
        {
            GameObject go = new GameObject();
            go.AddComponent<StormHazardController>();
            //CombatDirector cd = go.AddComponent<CombatDirector>();
            //cd.onSpawnedServer.AddListener(new UnityEngine.Events.UnityAction<GameObject>(OnStormDirectorSpawnServer));
        }

        private void OnStormDirectorSpawnServer(GameObject masterObject)
        {
            EliteDef eliteDef = WhirlwindAspect.instance.EliteDef;
            EquipmentIndex? equipmentIndex;
            if (eliteDef == null)
            {
                equipmentIndex = null;
            }
            else
            {
                EquipmentDef eliteEquipmentDef = eliteDef.eliteEquipmentDef;
                equipmentIndex = ((eliteEquipmentDef != null) ? new EquipmentIndex?(eliteEquipmentDef.equipmentIndex) : null);
            }
            EquipmentIndex equipmentIndex2 = equipmentIndex ?? EquipmentIndex.None;
            CharacterMaster component = masterObject.GetComponent<CharacterMaster>();
            GameObject bodyObject = component.GetBodyObject();
            if (bodyObject)
            {
                foreach (EntityStateMachine entityStateMachine in bodyObject.GetComponents<EntityStateMachine>())
                {
                    entityStateMachine.initialStateType = entityStateMachine.mainStateType;
                }
            }
            if (equipmentIndex2 != EquipmentIndex.None)
            {
                component.inventory.SetEquipmentIndex(equipmentIndex2);
            }
        }
    }

    public class StormHazardController : MonoBehaviour
    {
        bool teleporterActive => StormDirector.instance.teleporterActive;
        private TeleporterInteraction teleporter => StormDirector.instance?.teleporter;

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
            try
            {
                if (teleporterActive)
                {
                    if (teleporter == null)
                    {
                        Debug.LogError("tp interaction null?");
                        return meteor;
                    }
                    if (teleporter.holdoutZoneController == null)
                    {
                        Debug.LogError("holdout zone null???");
                        return meteor;
                    }
                    //i have no goddamn clue what this does lmao
                    //this uses reflection to find the impact position of the meteor that was spawned -borbo
                    Vector3 impactPosition = (Vector3)meteor.GetType()
                        .GetField("impactPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(meteor); 

                    if (this.IsInRange(impactPosition, this.teleporter.transform.position, teleporter.holdoutZoneController.currentRadius + meteorBlastRadius))
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
