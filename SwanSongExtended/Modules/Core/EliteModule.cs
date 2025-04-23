using SwanSongExtended;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;

namespace SwanSongExtended.Modules
{
    public static class EliteModule
    {
        //i love you nebby <3
        public static List<CustomEliteDef> Elites = new List<CustomEliteDef>();
        public static Texture defaultShaderRamp = CommonAssets.mainAssetBundle.LoadAsset<Texture>(CommonAssets.eliteMaterialsPath + "texRampFrenzied.tex");

        public static void Init()
        {
            RoR2Application.onLoad += AddElites;
        }

        private static void AddElites()
        {
            foreach (CustomEliteDef eliteDef in Elites)
            {
                switch (eliteDef.eliteTier)
                {
                    default:
                        break;
                    case EliteTiers.Tier1:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[1].eliteTypes, eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[2].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[3].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[4].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Tier1AndHalf:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[3].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[4].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Tier2:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[5].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.StormT1:
                        HG.ArrayUtils.ArrayAppend(ref SwanSongPlugin.StormT1.eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.StormT2:
                        HG.ArrayUtils.ArrayAppend(ref SwanSongPlugin.StormT2.eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Lunar:
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[6].eliteTypes, eliteDef.eliteDef);
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
            Tier1AndHalf,
            Tier2,
            StormT1,
            StormT2,
            Lunar,
            Other
        }
        #endregion
    }
}
