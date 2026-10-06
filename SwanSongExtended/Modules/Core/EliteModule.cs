using SwanSongExtended;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using R2API;
using SwanSongExtended.Storms;
using BepInEx.Configuration;

namespace SwanSongExtended.Modules
{
    public static class EliteModule
    {
        public static ConfigEntry<bool> enableElites { get; private set; }
        public static ConfigEntry<bool> enableStormElites { get; private set; }
        public static List<CustomEliteDef> Elites = new List<CustomEliteDef>();
        public static Texture defaultShaderRamp = CommonAssets.mainAssetBundle.LoadAsset<Texture>(CommonAssets.eliteMaterialsPath + "texRampFrenzied.tex");

        public static void Init()
        {
            enableElites = Modules.Config.SectionEnableConfig("Elites");
            enableStormElites = Modules.Config.SectionEnableConfig("Storms (Elites)");
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
                    case EliteTiers.Common:
                        if (enableElites.Value == false)
                            break;
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[1].eliteTypes, eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[2].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[3].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[4].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Uncommon:
                        if (enableElites.Value == false)
                            break;
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[3].eliteTypes, eliteDef.honorEliteDef != null ? eliteDef.honorEliteDef : eliteDef.eliteDef);
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[4].eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.Rare:
                        if (enableElites.Value == false)
                            break;
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[5].eliteTypes, eliteDef.eliteDef);
                        break;

                    case EliteTiers.Lunar:
                        if (enableElites.Value == false)
                            break;
                        HG.ArrayUtils.ArrayAppend(ref R2API.EliteAPI.VanillaEliteTiers[6].eliteTypes, eliteDef.eliteDef);
                        break;

                    case EliteTiers.Storm:
                        HG.ArrayUtils.ArrayAppend(ref StormsCore.StormEliteT1.eliteTypes, eliteDef.eliteDef);
                        break;
                    case EliteTiers.StormBoss:
                        HG.ArrayUtils.ArrayAppend(ref StormsCore.StormEliteT2.eliteTypes, eliteDef.eliteDef);
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
            Common,
            Uncommon,
            Rare,
            Storm,
            StormBoss,
            Lunar,
            Other
        }
        #endregion
    }
}
