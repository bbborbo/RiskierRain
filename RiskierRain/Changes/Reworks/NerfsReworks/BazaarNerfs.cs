using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain
{
    internal partial class RiskierRainPlugin : BaseUnityPlugin
    {
        public static float newtAltarChance = 0.3f;

        public void NerfBazaarStuff()
        {
            On.RoR2.SceneDirector.Start += SceneDirector_Start;
        }

        private void SceneDirector_Start(On.RoR2.SceneDirector.orig_Start orig, RoR2.SceneDirector director)
        {
            orig(director);

            if (NetworkServer.active && SceneInfo.instance.sceneDef.baseSceneName != "bazaar")
            {
                List<GameObject> randomNewts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue" || obj.name == "NewtStatue (1)" || obj.name == "NewtStatue (2)" || obj.name == "NewtStatue (3)" || obj.name == "NewtStatue (4)").ToList();
                List<GameObject> guaranteedNewts = Resources.FindObjectsOfTypeAll<GameObject>().Where(obj => obj.name == "NewtStatue, Guarantee" || obj.name == "NewtStatue, Guaranteed" || obj.name == "NewtStatue (Permanent)").ToList();

                randomNewts.Concat(guaranteedNewts);

                foreach (var newt in randomNewts)
                {
                    if (newtAltarChance >= 1 || director.rng.nextNormalizedFloat <= newtAltarChance)
                    {
                        newt.SetActive(true);
                        break;
                    }
                    else
                        newt.SetActive(false);
                }
            }
        }
    }
}