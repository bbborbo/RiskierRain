using RoR2;
using SwanSongExtended.Interactables;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Storms.StormsCore;

namespace SwanSongExtended.Storms
{
    /// <summary>
    /// Creates a StormController for each stage with appropriate properties
    /// </summary>
    public class StormRunBehavior : MonoBehaviour
    {
        public static List<HoldoutZoneController> holdoutZones = new List<HoldoutZoneController>();

        public static StormRunBehavior instance;
        public StormController stormControllerInstance;
        public StormType stormType { get; private set; } = StormType.None;


        public static StormType GetStormType(SceneDef currentScene)
        {
            //SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
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

        public void Start()
        {
            if (instance != null && instance != this)
            {
                Destroy(this);
                return;
            }
            instance = this;

            RoR2.Stage.onStageStartGlobal += OnStageBeginGlobal;
        }

        private void OnStageBeginGlobal(Stage obj)
        {
            stormType = GetStormType(obj.sceneDef);
            if (stormType == StormType.None)
                return;

            GameObject stormControllerObject = Instantiate(StormsCore.StormsControllerPrefab);
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

            if (Run.instance.stageClearCount == 0)
                a += 1.5f;
            a += Run.instance.stageRng.RangeFloat(0, 1);

            stormControllerInstance.BeginStormApproach(a, b);

            if (NetworkServer.active)
            {
                WishboneCarcass.ScatterWishbones();
            }
        }

        #region hooks
        public void OnDestroy()
        {
            if(instance == this)
            {
                RoR2.Stage.onStageStartGlobal -= OnStageBeginGlobal;
            }
        }
        #endregion
    }
}
