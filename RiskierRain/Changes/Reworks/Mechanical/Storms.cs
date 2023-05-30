using BepInEx;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float drizzleStormDelay = 10;
        public static float rainstormStormDelay = 5;
        public static float monsoonStormDelay = 3;
        void InitializeStorms()
        {
            On.RoR2.Run.BeginStage += StormsBeginStage;
            On.RoR2.Run.EndStage += StormsEndStage;
        }
        GameObject StormControllerInstance; //this probably sucks but im just tryna see if itll work lol
        private void StormsBeginStage(On.RoR2.Run.orig_BeginStage orig, RoR2.Run self)
        {
            orig(self);
            if (StormControllerInstance != null)
                Destroy(StormControllerInstance);
            StormControllerInstance = new GameObject();
            StageStormController guh = gameObject.AddComponent<StageStormController>();
        }

        private void StormsEndStage(On.RoR2.Run.orig_EndStage orig, RoR2.Run self)
        {
            orig(self);
            if (StormControllerInstance == null)
                return;
            Destroy(StormControllerInstance);
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
            switch (RoR2.Run.instance.selectedDifficulty)
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
            float random = RoR2.Run.instance.stageRng.RangeFloat(0, 2);
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
        }

        void ActivateStorm()
        {
            hasBegunStorm = true;


        }
    }
}
