using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;
using System.Linq;
using UnityEngine.AddressableAssets;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float drizzleStormDelay = 10;
        public static float rainstormStormDelay = 5;
        public static float monsoonStormDelay = 3;

        //this might suck
        GameObject teleporter;

        void InitializeStorms()
        {
            On.RoR2.Run.BeginStage += StormsBeginStage;
            On.RoR2.Run.EndStage += StormsEndStage;
            On.RoR2.Run.OnServerTeleporterPlaced += Run_OnServerTeleporterPlaced;
        }
        GameObject StormControllerInstance; //this probably sucks but im just tryna see if itll work lol
        private void StormsBeginStage(On.RoR2.Run.orig_BeginStage orig, RoR2.Run self)
        {
            orig(self);
            if (StormControllerInstance != null)
                Destroy(StormControllerInstance);
            StormControllerInstance = new GameObject();
            StageStormController guh = gameObject.AddComponent<StageStormController>();
            
            guh.teleporter = teleporter;
            teleporter = null;
        }

        private void StormsEndStage(On.RoR2.Run.orig_EndStage orig, RoR2.Run self)
        {
            orig(self);
            if (StormControllerInstance == null)
                return;
            Destroy(StormControllerInstance);
        }
        private void Run_OnServerTeleporterPlaced(On.RoR2.Run.orig_OnServerTeleporterPlaced orig, Run self, SceneDirector sceneDirector, GameObject teleporter)
        {
            orig.Invoke(self, sceneDirector, teleporter);
            
            this.teleporter = teleporter;
            
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

        public GameObject teleporter;

        void Start()
        {
            stageBeginTime = RoR2.Run.instance.GetRunStopwatch();
            CalculateStormStartDelay();
            DetermineStormType();
            stormStartTime = stageBeginTime + stormStartDelay;
        }
        void CalculateStormStartDelay()
        {
            float delay = 0;
            switch (Run.instance.selectedDifficulty)
            {
                default:
                    delay = RiskierRainPlugin.monsoonStormDelay;
                    break;
                case RoR2.DifficultyIndex.Normal:
                    delay = RiskierRainPlugin.rainstormStormDelay;
                    break;
                case RoR2.DifficultyIndex.Easy:
                    delay = RiskierRainPlugin.drizzleStormDelay;
                    break;
            }
            float random = Run.instance.stageRng.RangeFloat(0, 2);
            stormStartDelay = (delay + random) * 11;//60; small number for testing thx
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
                Destroy(this.gameObject);
            }
            float currentTime = RoR2.Run.instance.GetRunStopwatch();
            if (!hasSentStormWarning && currentTime > stormStartTime - 30)
            {
                string warningMessage = "";
                switch (stormType)
                {
                    case StormType.MeteorDefault:
                        warningMessage = "A meteor storm is approaching...";
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
                        warningMessage = "A shower of meteors begin to fall...";
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
                            scale = this.meteorbBastRadius
                        }, true);
                    }
                }
            }

            float num = Run.instance.time - this.meteorImpactDelay;
            float num2 = num - this.meteorTravelEffectDuration;
            for (int j = this.meteorList.Count - 1; j >= 0; j--)
            {
                MeteorStormController.Meteor meteor = this.meteorList[j];
                if (meteor.startTime < num2 && !meteor.didTravelEffect)
                {
                    this.DoMeteorEffect(meteor);
                }
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
                baseDamage = this.meteorBlastDamageCoefficient * 10,//change this flat value to a scaling thing yeagh 
                baseForce = this.meteorBlastForce,
                attackerFiltering = AttackerFiltering.AlwaysHit,
                crit = false,
                falloffModel = BlastAttack.FalloffModel.SweetSpot,
                attacker = this.teleporter,
                bonusForce = Vector3.zero,
                damageColorIndex = DamageColorIndex.Fragile,
                position = meteor.impactPosition,
                procChainMask = default(ProcChainMask),
                procCoefficient = 0f,
                teamIndex = TeamIndex.Monster,//this might unintentionally harm void enemies and such but idk how to make something trhat exclusively harms the player team
                radius = meteorbBastRadius
            }.Fire();
        }

        private void DoMeteorEffect(MeteorStormController.Meteor meteor)
        {
            meteor.didTravelEffect = true;
            if (this.meteorImpactEffectPrefab)
            {
                EffectManager.SpawnEffect(this.meteorTravelEffectPrefab, new EffectData
                {
                    origin = meteor.impactPosition
                }, true);
            }
        }

        //all the projectile/prefab stuff
        public float waveMinInterval = 1;
        public float waveMaxInterval = 4;

        private List<MeteorStormController.Meteor> meteorList;
        private List<MeteorStormController.MeteorWave> waveList;
        private float waveTimer;

        //meteors:
        GameObject meteorTravelEffectPrefab;
        GameObject meteorWarningEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikePredictionEffect.prefab").WaitForCompletion();
        GameObject meteorImpactEffectPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Meteor/MeteorStrikeImpact.prefab").WaitForCompletion();
        public float meteorTravelEffectDuration = 3;
        public float meteorImpactDelay = 3;
        public float meteorBlastDamageCoefficient = 5;
        public float meteorbBastRadius = 10;
        public float meteorBlastForce = 0;

    }
}
