using RiskierRainContent;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRainContent.CoreModules
{
    public class EliteModule : CoreModule
    {
        //i love you nebby <3
        public static List<CustomEliteDef> Elites = new List<CustomEliteDef>();
        public static Texture defaultShaderRamp = Assets.mainAssetBundle.LoadAsset<Texture>(Assets.eliteMaterialsPath + "texRampFrenzied.tex");

        public override void Init()
        {
            RoR2Application.onLoad += AddElites;
        }

        private static void AddElites()
        {
            foreach (CustomEliteDef eliteDef in Elites)
            {
                switch (eliteDef.eliteTier)
                {
                    case EliteTiers.Tier1:
                        HG.ArrayUtils.ArrayAppend(ref CombatDirector.eliteTiers[1].eliteTypes, eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref CombatDirector.eliteTiers[2].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Tier2:
                        HG.ArrayUtils.ArrayAppend(ref CombatDirector.eliteTiers[3].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Other:
                        break;
                }
            }
        }

        #region EliteDef
        public class CustomEliteDef : ScriptableObject
        {
            public EliteDef eliteDef;
            public EliteTiers eliteTier;
            public Color lightColor = Color.clear;
            public Texture eliteRamp;
            public Material overlayMaterial;
            public GameObject spawnEffect;
        }
        public enum EliteTiers
        {
            Tier1,
            Tier2,
            Other
        }
        #endregion
    }
}
