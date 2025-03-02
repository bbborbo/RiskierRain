using RiskierRainContent;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;

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

        const int tier1NoHonor = 1;
        const int tier1Honor = 2;
        const int tier1PlusHonor = 3;
        const int tier1PlusNoHonor = 4;
        const int tier2 = 5;
        const int lunar = 6;
        private static void AddElites()
        {
            foreach (CustomEliteDef eliteDef in Elites)
            {
                switch (eliteDef.eliteTier)
                {
                    case EliteTiers.Tier1:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[tier1NoHonor].eliteTypes, eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[tier1Honor].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[tier1PlusHonor].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[tier1PlusNoHonor].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Tier2:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[tier2].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Lunar:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[lunar].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.StormT1:
                        HG.ArrayUtils.ArrayAppend(ref RiskierRainContent.StormT1.eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.StormT2:
                        HG.ArrayUtils.ArrayAppend(ref RiskierRainContent.StormT2.eliteTypes, eliteDef.eliteDef);
                        break;
                    default:
                        break;
                }
            }
        }

        #region EliteDef
        public class CustomEliteDef : ScriptableObject
        {
            public EliteDef eliteDef;
            public EliteDef honorEliteDef;
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
            Lunar,
            StormT1,
            StormT2,
            Other
        }
        #endregion
    }
}
