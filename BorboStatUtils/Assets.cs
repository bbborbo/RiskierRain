using BepInEx;
using R2API;
using RainrotSharedUtils.Components;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;

namespace RainrotSharedUtils
{
    public static class Assets
    {
        #region interfacing
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
        #endregion
        public static void Init()
        {
            CreateFrostNovaAssets();
            CreateSparkPickup();
        }

        #region spark pickup
        public const int maxNebulaBoosterStackCount = 5;
        public static float nebulaBoosterBuffDuration = 5;
        public static float nebulaBoosterBuffRadius = 50;

        public static GameObject sparkBoosterObject;
        public static Color32 sparkBoosterColor = new Color32(35, 115, 255, 255);
        public static BuffDef sparkBoosterBuff;
        public static float sparkBoosterDuration = 8f;
        public static float sparkBoosterAspdBonus = 0.25f;

        private static void CreateSparkPickup()
        {
            sparkBoosterBuff = ScriptableObject.CreateInstance<BuffDef>();
            sparkBoosterBuff.name = "bdSparkBoost";
            sparkBoosterBuff.buffColor = sparkBoosterColor;
            sparkBoosterBuff.canStack = maxNebulaBoosterStackCount > 1 ? true : false;
            Addressables.LoadAssetAsync<Sprite>("1597fa78f3a39cc4c9c58e8ed2cd42f0").Completed += ctx => 
                sparkBoosterBuff.iconSprite = ctx.Result;
            R2API.ContentAddition.AddBuffDef(sparkBoosterBuff);

            sparkBoosterObject = NewNebulaBooster("SparkBoosterPickup", sparkBoosterBuff, sparkBoosterColor, sparkBoosterDuration, 0.8f, 1.8f);

            GetStatCoefficients += SparkBoosterStats;
        }

        private static void SparkBoosterStats(CharacterBody sender, StatHookEventArgs args)
        {
            int buffCount = sender.GetBuffCount(sparkBoosterBuff);
            if (buffCount > 0)
                args.attackSpeedMultAdd += sparkBoosterAspdBonus * buffCount;
        }

        static GameObject NewNebulaBooster(string boosterName, BuffDef boosterBuff, Color32 boosterColor, float boosterDuration, float antiGravity = 1, float pickupRangeMultiplier = 3f)
        {
            GameObject baseObject = Addressables.LoadAssetAsync<GameObject>("7f9217d45f824f245862e65716abc746").WaitForCompletion();

            GameObject newBooster = baseObject.InstantiateClone(boosterName, true);

            //Tools.DebugMaterial(newBooster);
            //Tools.DebugParticleSystem(newBooster);


            ParticleSystemRenderer[] psrs = newBooster.GetComponentsInChildren<ParticleSystemRenderer>();
            for (int i = 0; i < psrs.Length; i++)
            {
                ParticleSystemRenderer psr = psrs[i];
                string name = psr.gameObject.name;
                Color32 color = Color.white;
                string matName = "";
                if (name == "Core")
                {
                    matName = "matSparkPickupCore";
                    color = boosterColor;
                }
                if (name == "Trail")
                {
                    matName = "matSparkPickupTrail";
                    color = Color.clear;
                }
                if (name == "Pulseglow")
                {
                    matName = "matSparkPickupGlow";
                    color = boosterColor;
                }

                if (matName != "")
                {
                    Material mat = UnityEngine.Object.Instantiate(psr.material);
                    psr.material = mat;
                    mat.name = matName;
                    mat.DisableKeyword("VERTEXCOLOR");
                    mat.SetFloat("_VertexColorOn", 0);
                    mat.SetColor("_TintColor", color);
                }
            }

            VelocityRandomOnStart boosterVROS = newBooster.GetComponent<VelocityRandomOnStart>();
            if (boosterVROS != null)
            {
                boosterVROS.minSpeed = 15;
                boosterVROS.maxSpeed = 25;
                boosterVROS.coneAngle = 360;
                boosterVROS.directionMode = VelocityRandomOnStart.DirectionMode.Sphere;
            }
            else
            {
                Debug.Log(boosterName + " HAS NO VROS????");
            }

            DestroyOnTimer boosterDOT = newBooster.GetComponent<DestroyOnTimer>();
            if (boosterDOT != null)
            {
                boosterDOT.duration = boosterDuration;
            }
            else
            {
                Debug.Log(boosterName + " HAS NO DOT????");
            }

            BeginRapidlyActivatingAndDeactivating boosterBRAAD = newBooster.GetComponent<BeginRapidlyActivatingAndDeactivating>();
            if (boosterBRAAD != null)
            {
                boosterBRAAD.delayBeforeBeginningBlinking = boosterDuration - 2;
                boosterBRAAD.blinkFrequency = 5;
            }
            else
            {
                Debug.Log(boosterName + " HAS NO BRAAD????");
            }

            if(antiGravity != 0)
            {
                Rigidbody rb = newBooster.GetComponent<Rigidbody>();
                if (antiGravity == 1)
                {
                    rb.useGravity = true;
                }
                else
                {
                    AntiGravityForce antiGrav = newBooster.AddComponent<AntiGravityForce>();
                    antiGrav.rb = rb;
                    antiGrav.antiGravityCoefficient = antiGravity;
                }
            }


            HealthPickup healthpickup = newBooster.GetComponentInChildren<HealthPickup>();
            NebulaPickup boosterPickup = healthpickup.gameObject.AddComponent<NebulaPickup>();
            boosterPickup.pickupEffect = healthpickup.pickupEffect;
            boosterPickup.baseObject = healthpickup.baseObject;
            boosterPickup.teamFilter = newBooster.GetComponent<TeamFilter>();

            if (boosterBuff != null)
            {
                boosterPickup.buffDef = boosterBuff;
            }
            else
            {
                Debug.Log(boosterName + "BOOSTER BUFFDEF WAS NULL");
            }

            GravitatePickup boosterGravitate = newBooster.GetComponentInChildren<GravitatePickup>();
            if (boosterGravitate != null)
            {
                boosterGravitate.acceleration = 2f;
                boosterGravitate.maxSpeed = 50;
                Collider gravitateTrigger = boosterGravitate.gameObject.GetComponent<Collider>();
                if (gravitateTrigger.isTrigger)
                {
                    gravitateTrigger.transform.localScale *= pickupRangeMultiplier;
                }
            }
            else
            {
                Debug.Log(boosterName + " HAS NO GRAVITATION????");
            }


            UnityEngine.Object.Destroy(healthpickup);

            R2API.ContentAddition.AddNetworkedObject(newBooster);

            return newBooster;
        }
#endregion

        #region chill rework
        internal static GameObject iceDelayBlastPrefab;

        public static GameObject iceNovaEffectStrong;
        public static GameObject iceNovaEffectWeak;
        public static GameObject iceNovaEffectLowPriority;

        public static Texture2D iceNovaRamp;
        public static Texture2D iceNovaRampPersistent;
        private static void CreateFrostNovaAssets()
        {
            iceNovaRamp = GetIceRemap(1.1f);
            iceNovaRampPersistent = GetIceRemap(0.4f, 0.1f);

            iceNovaEffectStrong = CreateSingleIceNova(iceNovaRamp, "Strong", 1.2f);
            iceNovaEffectWeak = CreateSingleIceNova(iceNovaRamp, "Weak", 0.85f);
            iceNovaEffectLowPriority = CreateSingleIceNova(iceNovaRamp, "LowPriority", 0.3f);

            iceDelayBlastPrefab = CreateIceDelayBlastPrefab();

            Texture2D GetIceRemap(float alphaMod = 1, float alphaAdd = 0.0f)
            {
                Gradient iceGrad = new Gradient
                {
                    mode = GradientMode.Blend,
                    alphaKeys = new GradientAlphaKey[8]
                    {
                        new GradientAlphaKey( 0f * alphaMod + alphaAdd, 0f ),
                        new GradientAlphaKey( 0f * alphaMod + alphaAdd, 0.14f ),
                        new GradientAlphaKey( 0.22f * alphaMod + alphaAdd, 0.46f ),
                        new GradientAlphaKey( 0.22f * alphaMod + alphaAdd, 0.61f),
                        new GradientAlphaKey( 0.72f * alphaMod + alphaAdd, 0.63f ),
                        new GradientAlphaKey( 0.72f * alphaMod + alphaAdd, 0.8f ),
                        new GradientAlphaKey( 0.87f * alphaMod + alphaAdd, 0.81f ),
                        new GradientAlphaKey( 0.87f * alphaMod + alphaAdd, 1f )
                    },
                    colorKeys = new GradientColorKey[8]
                    {
                        new GradientColorKey( new Color( 0f + alphaAdd, 0 + alphaAdd, 0f + alphaAdd ), 0f ),
                        new GradientColorKey( new Color( 0f + alphaAdd, 0f + alphaAdd, 0f + alphaAdd ), 0.14f ),
                        new GradientColorKey( new Color( 0.179f + alphaAdd , 0.278f + alphaAdd, 0.250f + alphaAdd ), 0.46f ),
                        new GradientColorKey( new Color( 0.179f + alphaAdd, 0.278f + alphaAdd, 0.250f + alphaAdd ), 0.61f ),
                        new GradientColorKey( new Color( 0.5f + alphaAdd, 0.8f + alphaAdd, 0.75f + alphaAdd ), 0.63f ),
                        new GradientColorKey( new Color( 0.5f + alphaAdd, 0.8f + alphaAdd, 0.75f + alphaAdd ), 0.8f ),
                        new GradientColorKey( new Color( 0.6f + alphaAdd, 0.9f + alphaAdd, 0.85f + alphaAdd ), 0.81f ),
                        new GradientColorKey( new Color( 0.6f + alphaAdd, 0.9f + alphaAdd, 0.85f + alphaAdd ), 1f )
                    }
                };
                return CreateNewRampTex(iceGrad);
            }
        }

        private static GameObject CreateSingleIceNova(Texture2D remapTex, string s, float alphaMod)
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/AffixWhiteExplosion").InstantiateClone("IceExplosion" + s, false);
            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();

            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);

            mat.SetTexture("_RemapTex", remapTex);
            Color c = mat.GetColor("_TintColor");
            c.a *= alphaMod;
            mat.SetColor("_TintColor", c);

            sphere.material = mat;
            RegisterEffect(obj);

            return obj;
        }

        private static GameObject CreateIceDelayBlastPrefab()
        {
            GameObject blast = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/GenericDelayBlast").InstantiateClone("IceDelayBlast", false);
            DelayBlast component = blast.GetComponent<DelayBlast>();
            component.crit = false;
            component.procCoefficient = 1.0f;
            component.maxTimer = 0.2f;
            component.falloffModel = BlastAttack.FalloffModel.None;
            component.explosionEffect = iceNovaEffectWeak;
            component.delayEffect = CreateIceDelayEffect();
            component.damageType = DamageType.Freeze2s;
            component.baseForce = 250f;

            ProjectileController pc = blast.AddComponent<ProjectileController>();

            //AltArtiPassive.iceBlast = blast;
            //projectilePrefabs.Add(blast);

            R2API.ContentAddition.AddProjectile(blast);

            return blast;
        }
        //called by CreateIceDelayBlastPrefab
        private static GameObject CreateIceDelayEffect()
        {
            GameObject obj = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/AffixWhiteDelayEffect").InstantiateClone("iceDelay", false);
            obj.GetComponent<DestroyOnTimer>().duration = 0.2f;

            ParticleSystemRenderer sphere = obj.transform.Find("Nova Sphere").GetComponent<ParticleSystemRenderer>();
            Material mat = UnityEngine.Object.Instantiate<Material>(sphere.material);
            mat.SetTexture("_RemapTex", iceNovaRamp);
            sphere.material = mat;

            RegisterEffect(obj);

            return obj;
        }
        #endregion

        #region shock rework

        #endregion
    }
}
