using RainrotSharedUtils.Difficulties;
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
        public static StormRunBehavior instance;
        public StormController stormControllerInstance;
        public StormType stormType { get; private set; } = StormType.None;


        public static StormType GetStormType(SceneDef currentScene)
        {
            //SceneDef currentScene = SceneCatalog.GetSceneDefForCurrentScene();
            StormType st = StormType.None;
            if (IsStormStage(currentScene))
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
        public static bool IsStormStage(SceneDef currentScene)
        {
            if (currentScene.sceneType != SceneType.Stage) return false;
            if (currentScene.isFinalStage) return false;
            if (currentScene.sceneAddress == SceneCatalog.GetSceneDefFromSceneName("conduitcanyon").sceneAddress) return false;
            return true;
        }
        public static bool hasBegunStorm
        {
            get
            {
                if (instance == null)
                    return false;
                if (instance.stormControllerInstance == null)
                    return false;
                if (instance.stormControllerInstance.stormState >= StormController.StormState.Active)
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

            if (!DifficultyUtilsModule.ValidateCachedDifficultyStats())
                return;

            float stormTime = DifficultyUtilsModule.cachedDifficultyStats.desiredStormTime_ForSwanSong;
            float warningTime = DifficultyUtilsModule.cachedDifficultyStats.desiredStormWarningTime_ForSwanSong;
            if (stormTime == -1)
                return;

            GameObject stormControllerObject = Instantiate(StormsCore.StormsControllerPrefab);
            stormControllerInstance = stormControllerObject.GetComponent<StormController>();

            if (Run.instance.stageClearCount == 0 && DifficultyUtilsModule.cachedDifficultyStats.delayFirstStorm_ForSwanSong)
                stormTime += StormsCore.firstStageStormDelayMinutes;
            stormTime += Run.instance.stageRng.RangeFloat(0, stormMaxRandomDelayMinutes);

            stormControllerInstance.BeginStormApproach(stormTime, warningTime);

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
