﻿using BepInEx;
using BepInEx.Bootstrap;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2;

namespace RiskierRainContent
{
    public static class Tools
    {
        #region AssetBundle
        /// <summary>
        /// Loads an embedded asset bundle
        /// </summary>
        /// <param name="resourceBytes">The bytes returned by Properties.Resources.ASSETNAME</param>
        /// <returns>The loaded bundle</returns>
        public static AssetBundle LoadAssetBundle(Byte[] resourceBytes)
        {
            if (resourceBytes == null) throw new ArgumentNullException(nameof(resourceBytes));
            return AssetBundle.LoadFromMemory(resourceBytes);
        }

        /// <summary>
        /// A simple helper to generate a unique mod prefix for you.
        /// </summary>
        /// <param name="plugin">A reference to your plugin. (this.GetModPrefix)</param>
        /// <param name="bundleName">A unique name for the bundle (Unique within your mod)</param>
        /// <returns>The generated prefix</returns>
        public static string modPrefix = String.Format("@{0}+{1}", "ArtificerExtended", "artiskillicons");

        public static String GetModPrefix(this BepInEx.BaseUnityPlugin plugin, String bundleName)
        {
            return String.Format("@{0}+{1}", plugin.Info.Metadata.Name, bundleName);
        }
        #endregion

        internal static bool isLoaded(string modguid)
        {
            foreach (KeyValuePair<string, PluginInfo> keyValuePair in Chainloader.PluginInfos)
            {
                string key = keyValuePair.Key;
                PluginInfo value = keyValuePair.Value;
                bool flag = key == modguid;
                if (flag)
                {
                    return true;
                }
            }
            return false;
        }
        internal static string ConvertDecimal(float d)
        {
            return (d * 100f).ToString() + "%";
        }

        #region Materials + Etc
        internal static void GetMaterial(GameObject model, string childObject, Color color, ref Material material, float scaleMultiplier = 1, bool replaceAll = false)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;

                if (string.Equals(renderer.name, childObject))
                {
                    if (color == Color.clear)
                    {
                        UnityEngine.GameObject.Destroy(renderer);
                        return;
                    }

                    if (material == null)
                    {
                        material = new Material(renderer.material);
                        material.mainTexture = renderer.material.mainTexture;
                        material.shader = renderer.material.shader;
                        material.color = color;
                    }
                    renderer.material = material;
                    renderer.transform.localScale *= scaleMultiplier;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugMaterial(GameObject model)
        {
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();

            foreach (Renderer renderer in renderers)
            {
                Renderer smr = renderer;
                Debug.Log("Material: " + smr.name.ToString());
            }
        }

        internal static void GetParticle(GameObject model, string childObject, Color color, float sizeMultiplier = 1, bool replaceAll = false)
        {
            ParticleSystem[] partSystems = model.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                var main = ps.main;
                var lifetime = ps.colorOverLifetime;
                var speed = ps.colorBySpeed;

                if (string.Equals(ps.name, childObject))
                {
                    main.startColor = color;
                    main.startSizeMultiplier *= sizeMultiplier;
                    lifetime.color = color;
                    speed.color = color;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugParticleSystem(GameObject model)
        {
            ParticleSystem[] partSystems = model.GetComponents<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                Debug.Log("Particle: " + ps.name.ToString());
            }
        }


        internal static void GetLight(GameObject model, string childObject, Color color, bool replaceAll = false)
        {
            Light[] lights = model.GetComponentsInChildren<Light>();

            foreach (Light li in lights)
            {
                Light l = li;
                if (string.Equals(l.name, childObject))
                {
                    l.color = color;
                    if (!replaceAll)
                        break;
                }
            }
        }
        internal static void DebugLight(GameObject model)
        {
            Light[] lights = model.GetComponentsInChildren<Light>();

            foreach (Light li in lights)
            {
                Light l = li;
                Debug.Log("Light: " + l.name.ToString());
            }
        }
        #endregion

        #region Buffs?
        public static void ClearDotStacksForType(this DotController dotController, DotController.DotIndex dotIndex)
        {
            for (int i = dotController.dotStackList.Count - 1; i >= 0; i--)
            {
                if (dotController.dotStackList[i].dotIndex == dotIndex)
                {
                    dotController.RemoveDotStackAtServer(i);
                }
            }
        }


        #endregion
    }
}
