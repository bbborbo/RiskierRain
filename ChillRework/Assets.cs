using BepInEx;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ChillRework
{
    public partial class ChillReworkPlugin : BaseUnityPlugin
    {
        public static EffectDef RegisterEffect(GameObject effect)
        {
            if (effect == null)
            {
                Debug.LogError("Effect prefab was null");
                return null;
            }

            var effectComp = effect.GetComponent<EffectComponent>();
            if (effectComp == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have an EffectComponent.", effect.name);
                return null;
            }

            var vfxAttrib = effect.GetComponent<VFXAttributes>();
            if (vfxAttrib == null)
            {
                Debug.LogErrorFormat("Effect prefab: \"{0}\" does not have a VFXAttributes component.", effect.name);
                return null;
            }
            R2API.ContentAddition.AddEffect(effect);
            
            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };
            return def;
        }
        private static Texture2D CreateNewRampTex(Gradient grad)
        {
            var tex = new Texture2D(256, 8, TextureFormat.RGBA32, false);

            Color tempC;
            var tempCs = new Color[8];

            for (Int32 i = 0; i < 256; i++)
            {
                tempC = grad.Evaluate(i / 255f);
                for (Int32 j = 0; j < 8; j++)
                {
                    tempCs[j] = tempC;
                }

                tex.SetPixels(i, 0, 1, 8, tempCs);
            }
            tex.wrapMode = TextureWrapMode.Clamp;
            tex.Apply();
            return tex;
        }

        #region ice nova
        public static GameObject iceExplosion;
        private Texture2D iceBombTex;
        private void CreateIceNovaAssets()
        {
            iceExplosion = CreateIceExplosion();
            R2API.ContentAddition.AddProjectile(iceExplosion);
        }
        private GameObject CreateIceExplosion()
        {
            GameObject blast = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast").InstantiateClone("IceDelayBlast", false);
            DelayBlast component = blast.GetComponent<DelayBlast>();
            component.crit = false;
            component.procCoefficient = 1.0f;
            component.maxTimer = 0.25f;
            component.falloffModel = BlastAttack.FalloffModel.None;
            component.explosionEffect = this.CreateIceExplosionEffect();
            component.delayEffect = this.CreateIceDelayEffect();
            component.damageType = DamageType.Freeze2s;
            component.baseForce = 250f;

            //AltArtiPassive.iceBlast = blast;
            //projectilePrefabs.Add(blast);
            return blast;
        }
        //called by CreateIceExplosion
        private GameObject CreateIceDelayEffect()
        {
            this.CreateIceBombTex();

            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect").InstantiateClone("iceDelay", false);
            obj.GetComponent<DestroyOnTimer>().duration = 0.2f;

            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", this.iceBombTex);
            sphere.material = mat;

            RegisterEffect(obj);

            return obj;
        }
        //called by CreateIceExplosion
        private GameObject CreateIceExplosionEffect()
        {
            this.CreateIceBombTex();

            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion").InstantiateClone("IceExplosion", false);
            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", this.iceBombTex);
            sphere.material = mat;

            RegisterEffect(obj);

            return obj;
        }
        //called by CreateIce____Effect
        private void CreateIceBombTex()
        {
            if (this.iceBombTex != null)
            {
                return;
            }

            var iceGrad = new Gradient
            {
                mode = GradientMode.Blend,
                alphaKeys = new GradientAlphaKey[8]
                {
                    new GradientAlphaKey( 0f, 0f ),
                    new GradientAlphaKey( 0f, 0.14f ),
                    new GradientAlphaKey( 0.22f, 0.46f ),
                    new GradientAlphaKey( 0.22f, 0.61f),
                    new GradientAlphaKey( 0.72f, 0.63f ),
                    new GradientAlphaKey( 0.72f, 0.8f ),
                    new GradientAlphaKey( 0.87f, 0.81f ),
                    new GradientAlphaKey( 0.87f, 1f )
                },
                colorKeys = new GradientColorKey[8]
                {
                    new GradientColorKey( new Color( 0f, 0f, 0f ), 0f ),
                    new GradientColorKey( new Color( 0f, 0f, 0f ), 0.14f ),
                    new GradientColorKey( new Color( 0.179f, 0.278f, 0.250f ), 0.46f ),
                    new GradientColorKey( new Color( 0.179f, 0.278f, 0.250f ), 0.61f ),
                    new GradientColorKey( new Color( 0.5f, 0.8f, 0.75f ), 0.63f ),
                    new GradientColorKey( new Color( 0.5f, 0.8f, 0.75f ), 0.8f ),
                    new GradientColorKey( new Color( 0.6f, 0.9f, 0.85f ), 0.81f ),
                    new GradientColorKey( new Color( 0.6f, 0.9f, 0.85f ), 1f )
                }
            };

            this.iceBombTex = CreateNewRampTex(iceGrad);
        }
        #endregion
    }
}
