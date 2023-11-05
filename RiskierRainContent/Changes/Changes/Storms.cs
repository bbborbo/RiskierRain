using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using System.Linq;
using UnityEngine.AddressableAssets;
using EntityStates;

namespace RiskierRain
{
    public partial class RiskierRainContent : BaseUnityPlugin
    {
        public static float drizzleStormDelay = 10;
        public static float rainstormStormDelay = 5;
        public static float monsoonStormDelay = 3;

        //this might suck
        GameObject teleporter;

        void InitializeStorms()
        {
            On.RoR2.Run.EndStage += StormsEndStage;
            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
        }
        StageStormController StormControllerInstance; //this probably sucks but im just tryna see if itll work lol

        private void CreateNewStormController()
        {
            if (StormControllerInstance != null)
            {
                Destroy(StormControllerInstance.gameObject);
            }

            GameObject go = new GameObject();
            StormControllerInstance = go.AddComponent<StageStormController>();
        }

        private void StormsEndStage(On.RoR2.Run.orig_EndStage orig, RoR2.Run self)
        {
            orig(self);
            if (StormControllerInstance == null)
                return;
           StormControllerInstance.RemoveStormController();
        }
        private void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            orig(self, sceneDirector, teleporter);

            CreateNewStormController();
            if (StormControllerInstance != null && teleporter != null)
            {
                Debug.Log("woag00");
                StormControllerInstance.teleporter = teleporter;
            }
        }
    }

    public class StageStormController : MonoBehaviour
    {
        public enum StormType
        {
            MeteorDefault,
            Lightning,
            Fire,
            Cold
        }

        float stageBeginTime;
        float stormStartDelay;
        float stormStartTime;
        StormType stormType;
        bool hasSentStormWarning = false;
        bool hasBegunStorm = false;
        bool teleporterActive = false;

        public GameObject teleporter;
        private TeleporterInteraction teleporterInteraction;


        public void Start()
        {
            stageBeginTime = RoR2.Run.instance.GetRunStopwatch();
            CalculateStormStartDelay();
            DetermineStormType();
            stormStartTime = stageBeginTime + stormStartDelay;

            On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor += MeteorWave_GetNextMeteor;
            On.RoR2.TeleporterInteraction.OnInteractionBegin += TeleporterInteraction_OnInteractionBegin;
            On.RoR2.TeleporterInteraction.ChargedState.OnEnter += TeleporterInteraction_ChargedState_OnEnter;
        }

        private void TeleporterInteraction_ChargedState_OnEnter(On.RoR2.TeleporterInteraction.ChargedState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            teleporterActive = false;
        }

        public void OnDestroy()
        {
            On.RoR2.MeteorStormController.MeteorWave.GetNextMeteor -= MeteorWave_GetNextMeteor;
            On.RoR2.TeleporterInteraction.OnInteractionBegin -= TeleporterInteraction_OnInteractionBegin;
        }
        public void RemoveStormController()
        {
            Destroy(this.gameObject);
        }

        private void TeleporterInteraction_OnInteractionBegin(On.RoR2.TeleporterInteraction.orig_OnInteractionBegin orig, TeleporterInteraction self, Interactor activator)
        {
            orig.Invoke(self, activator);
            this.teleporterInteraction = self;
            this.teleporterActive = true;
        }

        void CalculateStormStartDelay()
        {
            float delay = 0;
            switch (Run.instance.selectedDifficulty)
            {
                default:
                    delay = RiskierRainContent.monsoonStormDelay;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    delay = RiskierRainContent.rainstormStormDelay;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    delay = RiskierRainContent.drizzleStormDelay;
                    break;
            }
            float random = Run.instance.stageRng.RangeFloat(0, 1);
            stormStartDelay = (delay + random) * 60;
        }
        void DetermineStormType()
        {
            stormType = StormType.MeteorDefault;
            /*switch (RoR2.Run.instance)
            {

            }*/
        }

        void FixedUpdate()
        {
            if (Run.instance == null)
            {
                RemoveStormController();
            }
            float currentTime = RoR2.Run.instance.GetRunStopwatch();
            if (!hasSentStormWarning && currentTime > stormStartTime - 30)
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


                hasSentStormWarning = true;
            }
            if (!hasBegunStorm && currentTime > stormStartTime)
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
                ActivateStorm();
            }
            if (teleporter == null) 
            {
                Debug.Log("oh god oh fuck");
            }
            if (hasBegunStorm)
            {
                StormBehavior();
            }
        }

        void StormBehavior()
        {
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

        void ActivateStorm()
        {
            hasBegunStorm = true;

            this.meteorList = new List<MeteorStormController.Meteor>();
            this.waveList = new List<MeteorStormController.MeteorWave>();
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
                    if (teleporterInteraction == null)
                    {
                        Debug.LogError("tp interaction null?");
                        return meteor;
                    }
                    if (teleporterInteraction.holdoutZoneController == null)
                    {
                        Debug.LogError("holdout zone null???");
                        return meteor;
                    }
                    //i have no goddamn clue what this does lmao
                    //this uses reflection to find the impact position of the meteor that was spawned -borbo
                    Vector3 impactPosition = (Vector3)meteor.GetType()
                        .GetField("impactPosition", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public).GetValue(meteor); 

                    if (this.IsInRange(impactPosition, this.teleporter.transform.position, teleporterInteraction.holdoutZoneController.currentRadius + meteorBlastRadius))
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

    }
}
