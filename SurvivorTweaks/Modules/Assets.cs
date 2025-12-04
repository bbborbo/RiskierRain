using System.Reflection;
using R2API;
using UnityEngine;
using UnityEngine.Networking;
using RoR2;
using System.IO;
using System.Collections.Generic;
using RoR2.UI;
using RoR2.Projectile;
using Path = System.IO.Path;
using RoR2.Skills;
using EntityStates;
using System;
using RoR2.CharacterAI;
using System.Linq;
using UnityEngine.AddressableAssets;
using RoR2.ExpansionManagement;
using static R2API.DamageAPI;
using static SurvivorTweaks.Modules.Language.Styling;
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;

namespace SurvivorTweaks.Modules
{
    public static class CommonAssets
    {
        private static AssetBundle _mainAssetBundle;
        public static AssetBundle mainAssetBundle
        {
            get
            {
                if (_mainAssetBundle == null)
                    _mainAssetBundle = Assets.LoadAssetBundle("survivortweaks");
                return _mainAssetBundle;
            }
            set
            {
                _mainAssetBundle = value;
            }
        }

        public static string dropPrefabsPath = "Assets/Models/DropPrefabs";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static string eliteMaterialsPath = "Assets/Textures/Materials/Elite/";


        public static BuffDef commandoRollBuff;

        public static BuffDef jetpackSpeedBoost;
        public static float jetpackSpeedPercent = 0.15f;


        public static BuffDef captainCdrBuff;
        public static float captainCdrPercent = 0.25f;

        public static BuffDef aspdPenaltyDebuff;
        public static float aspdPenaltyPercent = 0.25f;

        public static BuffDef desperadoExecutionDebuff;
        public static BuffDef lightsoutExecutionDebuff;
        public static void Init()
        {
            AddAcridReworkAssets();
            AddAspdPenaltyDebuff();
            AddCommanderRollBuff();
            AddJetpackSpeedBoost();
            AddBanditExecutionBuffs();
            AddCaptainCooldownBuff();

            GetStatCoefficients += CommonAssetStats;
            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;
            On.EntityStates.BaseState.AddRecoil += OnAddRecoil;
            On.RoR2.CharacterBody.AddSpreadBloom += OnAddSpreadBloom;
        }

        public static void AddBanditExecutionBuffs()
        {
            desperadoExecutionDebuff = Content.CreateAndAddBuff("bdDesperadoExecute", null, Color.black, false, true);
            desperadoExecutionDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            desperadoExecutionDebuff.isHidden = true;

            lightsoutExecutionDebuff = Content.CreateAndAddBuff("bdLightsOutExecute", null, Color.black, false, true);
            lightsoutExecutionDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            lightsoutExecutionDebuff.isHidden = true;
        }

        public static void OnAddSpreadBloom(On.RoR2.CharacterBody.orig_AddSpreadBloom orig, CharacterBody self, float value)
        {
            if (self.HasBuff(commandoRollBuff))
                return;
            orig(self, value);
        }

        public static void OnAddRecoil(On.EntityStates.BaseState.orig_AddRecoil orig, EntityStates.BaseState self, float verticalMin, float verticalMax, float horizontalMin, float horizontalMax)
        {
            if (self.HasBuff(commandoRollBuff))
                return;
            orig(self, verticalMin, verticalMax, horizontalMin, horizontalMax);
        }
        public static void AddCommanderRollBuff()
        {
            commandoRollBuff = Content.CreateAndAddBuff("bdDualieRoll",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion(),
                new Color(0.8f, 0.6f, 0.1f), false, false);
        }
        public static void AddJetpackSpeedBoost()
        {
            jetpackSpeedBoost = Content.CreateAndAddBuff("bdJetpackSpeed",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.2f, 0.2f), false, false);
        }


        private static void CommonAssetStats(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(aspdPenaltyDebuff))
            {
                args.attackSpeedReductionMultAdd += aspdPenaltyPercent;
            }
            if (sender.HasBuff(jetpackSpeedBoost))
            {
                args.moveSpeedMultAdd += jetpackSpeedPercent;
            }
            if (sender.HasBuff(captainCdrBuff))
            {
                SkillLocator skillLocator = sender.skillLocator;
                if (skillLocator != null)
                {
                    float mult = (1 - captainCdrPercent);
                    ApplyCooldownScale(skillLocator.primary, mult);
                    ApplyCooldownScale(skillLocator.secondary, mult);
                    ApplyCooldownScale(skillLocator.utility, mult);
                    ApplyCooldownScale(skillLocator.special, mult);
                }
            }

            void ApplyCooldownScale(GenericSkill skillSlot, float cooldownScale)
            {
                if (skillSlot != null)
                    skillSlot.cooldownScale *= cooldownScale;
            }
        }
        public static void RecalcStats_Stats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
        }
        public static void AddAspdPenaltyDebuff()
        {
            aspdPenaltyDebuff = Content.CreateAndAddBuff("bdAttackSpeedPenalty",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffSlow50Icon.tif").WaitForCompletion(),
                Color.red, false, false);
        }

        public static void AddCaptainCooldownBuff()
        {
            aspdPenaltyDebuff = Content.CreateAndAddBuff("bdCaptainRestock",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion(),
                Color.yellow, false, false);
        }

        #region acrid
        public static ModdedDamageType AcridFesterDamage;
        public static ModdedDamageType AcridCorrosiveDamage;
        public static BuffDef corrosionBuff;
        public static DotController.DotDef corrosionDotDef;
        public static DotController.DotIndex corrosionDotIndex;
        public static float contagiousTransferRate = 0.5f;
        public static int corrosionArmorReduction = 15;
        public static float corrosionDuration = 8f;
        public static float corrosionDamagePerSecond = 1f;
        public static float corrosionTickInterval = 1f;
        public static bool festerResetOnlyKitDots = true;
        public const string AcridFesterKeywordToken = "KEYWORD_FESTER";
        public const string AcridCorrosionKeywordToken = "KEYWORD_CORROSION";
        public const string AcridContagiousKeywordToken = "KEYWORD_CONTAGIOUS";
        private static void AddAcridReworkAssets()
        {
            OrbAPI.AddOrb<Orbs.DiseaseOrb>();
            AcridFesterDamage = DamageAPI.ReserveDamageType();
            AcridCorrosiveDamage = DamageAPI.ReserveDamageType();

            corrosionBuff = Content.CreateAndAddBuff(
                "AcridCorrosion", 
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffBleedingIcon.tif").WaitForCompletion(), 
                Color.yellow, 
                true, true);
            corrosionBuff.isDOT = true;
            corrosionDotDef = new DotController.DotDef
            {
                associatedBuff = corrosionBuff,
                damageCoefficient = corrosionDamagePerSecond * corrosionTickInterval,
                damageColorIndex = DamageColorIndex.Poison,
                interval = corrosionTickInterval
            };
            corrosionDotIndex = DotAPI.RegisterDotDef(corrosionDotDef, (self, dotStack) =>
            {

            });

            LanguageAPI.Add(AcridFesterKeywordToken, KeywordText("Festering", 
                $"Striking enemies {UtilityColor("resets")} the duration of all " +
                $"{HealingColor("Poison")}, {VoidColor("Blight")}, and {DamageColor("Corrosion")} stacks."));
            LanguageAPI.Add(AcridCorrosionKeywordToken, KeywordText("Caustic", 
                $"Deal {DamageColor(Tools.ConvertDecimal(corrosionDamagePerSecond) + " base damage")} over {UtilityColor($"{corrosionDuration}s")}. " +
                $"Reduce armor by {DamageColor(corrosionArmorReduction.ToString())}."));
            LanguageAPI.Add(AcridContagiousKeywordToken, KeywordText("Contagious", 
                $"This skill transfers {DamageColor(Tools.ConvertDecimal(contagiousTransferRate))} of {UtilityColor("every damage over time stack")} " +
                $"to nearby enemies."));

            GetStatCoefficients += CorrosionArmorReduction;
            GetHitBehavior += FesterOnHit;
        }

        private static void CorrosionArmorReduction(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(corrosionBuff))
            {
                args.armorAdd -= corrosionArmorReduction;
            }
        }

        private static void FesterOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (damageInfo.HasModdedDamageType(AcridFesterDamage))
            {
                DotController dotController = DotController.FindDotController(victimBody.gameObject);
                if (dotController)
                {
                    foreach (DotController.DotStack dotStack in dotController.dotStackList)
                    {
                        //if fester is set to only reset acrid's dots and the dot index is not of one of acrid's dots
                        if (festerResetOnlyKitDots &&
                            (dotStack.dotIndex != DotController.DotIndex.Blight
                            && dotStack.dotIndex != DotController.DotIndex.Poison
                            && dotStack.dotIndex != corrosionDotIndex))
                            continue;

                        float duration = dotStack.totalDuration * damageInfo.procCoefficient;
                        if (dotStack.timer < duration)
                            dotStack.timer = duration;
                    }
                }
            }
            if (damageInfo.HasModdedDamageType(AcridCorrosiveDamage))
            {
                uint? maxStacksFromAttacker = null;
                if ((damageInfo != null) ? damageInfo.inflictor : null)
                {
                    ProjectileDamage component = damageInfo.inflictor.GetComponent<ProjectileDamage>();
                    if (component && component.useDotMaxStacksFromAttacker)
                    {
                        maxStacksFromAttacker = new uint?(component.dotMaxStacksFromAttacker);
                    }
                }

                InflictDotInfo inflictDotInfo = new InflictDotInfo
                {
                    attackerObject = damageInfo.attacker,
                    victimObject = victimBody.gameObject,
                    totalDamage = new float?(attackerBody.baseDamage * corrosionDamagePerSecond * corrosionDuration * damageInfo.procCoefficient),
                    damageMultiplier = 1f,
                    dotIndex = corrosionDotIndex,
                    maxStacksFromAttacker = maxStacksFromAttacker
                };
                DotController.InflictDot(ref inflictDotInfo);
            }
        }
        #endregion
    }

    // for simplifying rendererinfo creation
    public class CustomRendererInfo
    {
        //the childname according to how it's set up in your childlocator
        public string childName;
        //the material to use. pass in null to use the material in the bundle
        public Material material = null;
        //don't set the hopoo shader on the material, and simply use the material from your prefab, unchanged
        public bool dontHotpoo = false;
        //ignores shields and other overlays. use if you're not using a hopoo shader
        public bool ignoreOverlays = false;
    }

    internal static class Assets
    {
        //cache bundles if multiple characters use the same one
        internal static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();

        internal static AssetBundle LoadAssetBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName))
            {
                return loadedBundles[bundleName];
            }

            AssetBundle assetBundle = null;
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(SurvivorTweaksPlugin.instance.Info.Location), bundleName));

            loadedBundles[bundleName] = assetBundle;

            return assetBundle;

        }

        internal static GameObject CloneTracer(string originalTracerName, string newTracerName)
        {
            if (RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName) == null) 
                return null;

            GameObject newTracer = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/" + originalTracerName), newTracerName, true);

            if (!newTracer.GetComponent<EffectComponent>()) newTracer.AddComponent<EffectComponent>();
            if (!newTracer.GetComponent<VFXAttributes>()) newTracer.AddComponent<VFXAttributes>();
            if (!newTracer.GetComponent<NetworkIdentity>()) newTracer.AddComponent<NetworkIdentity>();
            
            newTracer.GetComponent<Tracer>().speed = 250f;
            newTracer.GetComponent<Tracer>().length = 50f;

            Modules.Content.CreateAndAddEffectDef(newTracer);

            return newTracer;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            if (!objectToConvert) return;

            foreach (MeshRenderer i in objectToConvert.GetComponentsInChildren<MeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }

            foreach (SkinnedMeshRenderer i in objectToConvert.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                if (i)
                {
                    if (i.sharedMaterial)
                    {
                        i.sharedMaterial.ConvertDefaultShaderToHopoo();
                    }
                }
            }
        }

        internal static GameObject LoadCrosshair(string crosshairName)
        {
            GameObject loadedCrosshair = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/" + crosshairName + "Crosshair");
            if (loadedCrosshair == null)
            {
                Log.Error($"could not load crosshair with the name {crosshairName}. defaulting to Standard");

                return RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Crosshair/StandardCrosshair");
            }

            return loadedCrosshair;
        }

        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, bool parentToTransform) => LoadEffect(assetBundle, resourceName, "", parentToTransform);
        internal static GameObject LoadEffect(this AssetBundle assetBundle, string resourceName, string soundName = "", bool parentToTransform = false)
        {
            GameObject newEffect = assetBundle.LoadAsset<GameObject>(resourceName);

            if (!newEffect)
            {
                Log.ErrorAssetBundle(resourceName, assetBundle.name);
                return null;
            }

            newEffect.AddComponent<DestroyOnTimer>().duration = 12;
            newEffect.AddComponent<NetworkIdentity>();
            newEffect.AddComponent<VFXAttributes>().vfxPriority = VFXAttributes.VFXPriority.Always;
            EffectComponent effect = newEffect.AddComponent<EffectComponent>();
            effect.applyScale = false;
            effect.effectIndex = EffectIndex.Invalid;
            effect.parentToReferencedTransform = parentToTransform;
            effect.positionAtReferencedTransform = true;
            effect.soundName = soundName;

            Modules.Content.CreateAndAddEffectDef(newEffect);

            return newEffect;
        }

        internal static GameObject CreateProjectileGhostPrefab(this AssetBundle assetBundle, string ghostName)
        {
            GameObject ghostPrefab = assetBundle.LoadAsset<GameObject>(ghostName);
            if (ghostPrefab == null)
            {
                Log.Error($"Failed to load ghost prefab {ghostName}");
            }
            if (!ghostPrefab.GetComponent<NetworkIdentity>()) ghostPrefab.AddComponent<NetworkIdentity>();
            if (!ghostPrefab.GetComponent<ProjectileGhostController>()) ghostPrefab.AddComponent<ProjectileGhostController>();

            Modules.Assets.ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        internal static GameObject CreateProjectileGhostPrefab(GameObject ghostObject, string newName)
        {
            if (ghostObject == null)
            {
                Log.Error($"Failed to load ghost prefab {ghostObject.name}");
            }
            GameObject go = PrefabAPI.InstantiateClone(ghostObject, newName);
            if (!go.GetComponent<NetworkIdentity>()) go.AddComponent<NetworkIdentity>();
            if (!go.GetComponent<ProjectileGhostController>()) go.AddComponent<ProjectileGhostController>();

            //Modules.Assets.ConvertAllRenderersToHopooShader(go);

            return go;
        }

        internal static GameObject CloneProjectilePrefab(string prefabName, string newPrefabName)
        {
            GameObject newPrefab = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/" + prefabName), newPrefabName);
            return newPrefab;
        }

        internal static GameObject LoadAndAddProjectilePrefab(this AssetBundle assetBundle, string newPrefabName)
        {
            GameObject newPrefab = assetBundle.LoadAsset<GameObject>(newPrefabName);
            if(newPrefab == null)
            {
                Log.ErrorAssetBundle(newPrefabName, assetBundle.name);
                return null;
            }

            Content.AddProjectilePrefab(newPrefab);
            return newPrefab;
        }
    }
    internal static class Materials
    {
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

        #region shaders lol

        public static void SwapShadersFromMaterialsInBundle(AssetBundle bundle)
        {
            if (bundle.isStreamedSceneAssetBundle)
            {
                Debug.LogWarning($"Cannot swap material shaders from a streamed scene assetbundle.");
                return;
            }

            Material[] assetBundleMaterials = bundle.LoadAllAssets<Material>().Where(mat => mat.shader.name.StartsWith("Stubbed")).ToArray();

            for (int i = 0; i < assetBundleMaterials.Length; i++)
            {
                var material = assetBundleMaterials[i];
                if (!material.shader.name.StartsWith("Stubbed"))
                {
                    Debug.LogWarning($"The material {material} has a shader which's name doesnt start with \"Stubbed\" ({material.shader.name}), this is not allowed for stubbed shaders for MSU. not swapping shader.");
                    continue;
                }
                try
                {
                    SwapShader(material);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"Failed to swap shader of material {material}: {ex}");
                }
            }
        }
        private static void SwapShader(Material material)
        {
            var shaderName = material.shader.name.Substring("Stubbed".Length);
            var adressablePath = $"{shaderName}.shader";
            Shader shader = Addressables.LoadAssetAsync<Shader>(adressablePath).WaitForCompletion();
            material.shader = shader;
            MaterialsWithSwappedShaders.Add(material);
        }
        public static List<Material> MaterialsWithSwappedShaders { get; } = new List<Material>();
        #endregion

        private static List<Material> cachedMaterials = new List<Material>();

        internal static Shader hotpoo = RoR2.LegacyResourcesAPI.Load<Shader>("Shaders/Deferred/HGStandard");

        public static Material LoadMaterial(this AssetBundle assetBundle, string materialName) => CreateHopooMaterialFromBundle(assetBundle, materialName);
        public static Material CreateHopooMaterialFromBundle(this AssetBundle assetBundle, string materialName)
        {
            Material tempMat = cachedMaterials.Find(mat =>
            {
                materialName.Replace(" (Instance)", "");
                return mat.name.Contains(materialName);
            });
            if (tempMat)
            {
                Log.Debug($"{tempMat.name} has already been loaded. returning cached");
                return tempMat;
            }
            tempMat = assetBundle.LoadAsset<Material>(materialName);

            if (!tempMat)
            {
                Log.ErrorAssetBundle(materialName, assetBundle.name);
                return new Material(hotpoo);
            }

            return tempMat.ConvertDefaultShaderToHopoo();
        }

        public static Material SetHopooMaterial(this Material tempMat) => ConvertDefaultShaderToHopoo(tempMat);
        public static Material ConvertDefaultShaderToHopoo(this Material tempMat)
        {
            if (cachedMaterials.Contains(tempMat))
            {
                Log.Debug($"{tempMat.name} has already been loaded. returning cached");
                return tempMat;
            }

            float? bumpScale = null;
            Color? emissionColor = null;

            //grab values before the shader changes
            if (tempMat.IsKeywordEnabled("_NORMALMAP"))
            {
                bumpScale = tempMat.GetFloat("_BumpScale");
            }
            if (tempMat.IsKeywordEnabled("_EMISSION"))
            {
                emissionColor = tempMat.GetColor("_EmissionColor");
            }

            //set shader
            tempMat.shader = hotpoo;

            //apply values after shader is set
            tempMat.SetTexture("_EmTex", tempMat.GetTexture("_EmissionMap"));
            tempMat.EnableKeyword("DITHER");

            if (bumpScale != null)
            {
                tempMat.SetFloat("_NormalStrength", (float)bumpScale);
                tempMat.SetTexture("_NormalTex", tempMat.GetTexture("_BumpMap"));
            }
            if (emissionColor != null)
            {
                tempMat.SetColor("_EmColor", (Color)emissionColor);
                tempMat.SetFloat("_EmPower", 1);
            }

            //set this keyword in unity if you want your model to show backfaces
            //in unity, right click the inspector tab and choose Debug
            if (tempMat.IsKeywordEnabled("NOCULL"))
            {
                tempMat.SetInt("_Cull", 0);
            }
            //set this keyword in unity if you've set up your model for limb removal item displays (eg. goat hoof) by setting your model's vertex colors
            if (tempMat.IsKeywordEnabled("LIMBREMOVAL"))
            {
                tempMat.SetInt("_LimbRemovalOn", 1);
            }

            cachedMaterials.Add(tempMat);
            return tempMat;
        }

        /// <summary>
        /// Makes this a unique material if we already have this material cached (i.e. you want an altered version). New material will not be cached
        /// <para>If it was not cached in the first place, simply returns as it is already unique.</para>
        /// </summary>
        public static Material MakeUnique(this Material material)
        {

            if (cachedMaterials.Contains(material))
            {
                return new Material(material);
            }
            return material;
        }

        public static Material SetColor(this Material material, Color color)
        {
            material.SetColor("_Color", color);
            return material;
        }

        public static Material SetNormal(this Material material, float normalStrength = 1)
        {
            material.SetFloat("_NormalStrength", normalStrength);
            return material;
        }

        public static Material SetEmission(this Material material) => SetEmission(material, 1);
        public static Material SetEmission(this Material material, float emission) => SetEmission(material, emission, Color.white);
        public static Material SetEmission(this Material material, float emission, Color emissionColor)
        {
            material.SetFloat("_EmPower", emission);
            material.SetColor("_EmColor", emissionColor);
            return material;
        }
        public static Material SetCull(this Material material, bool cull = false)
        {
            material.SetInt("_Cull", cull ? 1 : 0);
            return material;
        }

        public static Material SetSpecular(this Material material, float strength)
        {
            material.SetFloat("_SpecularStrength", strength);
            return material;
        }
        public static Material SetSpecular(this Material material, float strength, float exponent)
        {
            material.SetFloat("_SpecularStrength", strength);
            material.SetFloat("SpecularExponent", exponent);
            return material;
        }
    }
    internal static class Particles
    {
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
            ParticleSystem[] partSystems = model.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem partSys in partSystems)
            {
                ParticleSystem ps = partSys;
                Debug.Log("Particle: " + ps.name.ToString());
            }
        }
    }
    internal class Content
    {
        //consolidate contentaddition here in case something breaks and/or want to move to r2api
        internal static void AddExpansionDef(ExpansionDef expansion)
        {
            ContentPacks.expansionDefs.Add(expansion);
        }

        internal static void AddCharacterBodyPrefab(GameObject bprefab)
        {
            ContentPacks.bodyPrefabs.Add(bprefab);
        }

        internal static void AddMasterPrefab(GameObject prefab)
        {
            ContentPacks.masterPrefabs.Add(prefab);
        }

        internal static void AddProjectilePrefab(GameObject prefab)
        {
            ContentPacks.projectilePrefabs.Add(prefab);
        }

        internal static void AddSurvivorDef(SurvivorDef survivorDef)
        {

            ContentPacks.survivorDefs.Add(survivorDef);
        }
        internal static void AddItemDef(ItemDef itemDef)
        {
            ContentPacks.itemDefs.Add(itemDef);
        }
        internal static void AddEliteDef(EliteDef eliteDef)
        {
            ContentPacks.eliteDefs.Add(eliteDef);
        }
        internal static void AddArtifactDef(ArtifactDef artifactDef)
        {
            ContentPacks.artifactDefs.Add(artifactDef);
        }

        internal static void AddNetworkedObjectPrefab(GameObject prefab)
        {
            ContentPacks.networkedObjectPrefabs.Add(prefab);
        }
        internal static void CreateSurvivor(GameObject bodyPrefab, GameObject displayPrefab, Color charColor, string tokenPrefix) { CreateSurvivor(bodyPrefab, displayPrefab, charColor, tokenPrefix, null, 100f); }
        internal static void CreateSurvivor(GameObject bodyPrefab, GameObject displayPrefab, Color charColor, string tokenPrefix, float sortPosition) { CreateSurvivor(bodyPrefab, displayPrefab, charColor, tokenPrefix, null, sortPosition); }
        internal static void CreateSurvivor(GameObject bodyPrefab, GameObject displayPrefab, Color charColor, string tokenPrefix, UnlockableDef unlockableDef) { CreateSurvivor(bodyPrefab, displayPrefab, charColor, tokenPrefix, unlockableDef, 100f); }
        internal static void CreateSurvivor(GameObject bodyPrefab, GameObject displayPrefab, Color charColor, string tokenPrefix, UnlockableDef unlockableDef, float sortPosition)
        {
            SurvivorDef survivorDef = ScriptableObject.CreateInstance<SurvivorDef>();
            survivorDef.bodyPrefab = bodyPrefab;
            survivorDef.displayPrefab = displayPrefab;
            survivorDef.primaryColor = charColor;

            survivorDef.cachedName = bodyPrefab.name.Replace("Body", "");
            survivorDef.displayNameToken = tokenPrefix + "NAME";
            survivorDef.descriptionToken = tokenPrefix + "DESCRIPTION";
            survivorDef.outroFlavorToken = tokenPrefix + "OUTRO_FLAVOR";
            survivorDef.mainEndingEscapeFailureFlavorToken = tokenPrefix + "OUTRO_FAILURE";

            survivorDef.desiredSortPosition = sortPosition;
            survivorDef.unlockableDef = unlockableDef;

            Modules.Content.AddSurvivorDef(survivorDef);
        }

        internal static void AddUnlockableDef(UnlockableDef unlockableDef)
        {
            ContentPacks.unlockableDefs.Add(unlockableDef);
        }
        internal static UnlockableDef CreateAndAddUnlockbleDef(string identifier, string nameToken, Sprite achievementIcon)
        {
            UnlockableDef unlockableDef = ScriptableObject.CreateInstance<UnlockableDef>();
            unlockableDef.cachedName = identifier;
            unlockableDef.nameToken = nameToken;
            unlockableDef.achievementIcon = achievementIcon;

            AddUnlockableDef(unlockableDef);

            return unlockableDef;
        }

        internal static void AddSkillDef(SkillDef skillDef)
        {
            ContentPacks.skillDefs.Add(skillDef);
        }

        internal static void AddSkillFamily(SkillFamily skillFamily)
        {
            ContentPacks.skillFamilies.Add(skillFamily);
        }

        internal static void AddEntityState(Type entityState)
        {
            ContentPacks.entityStates.Add(entityState);
        }

        internal static void AddBuffDef(BuffDef buffDef)
        {
            ContentPacks.buffDefs.Add(buffDef);
        }
        internal static BuffDef CreateAndAddBuff(string buffName, Sprite buffIcon, Color buffColor, bool canStack, bool isDebuff)
        {
            BuffDef buffDef = ScriptableObject.CreateInstance<BuffDef>();
            buffDef.name = buffName;
            buffDef.buffColor = buffColor;
            buffDef.canStack = canStack;
            buffDef.isDebuff = isDebuff;
            buffDef.eliteDef = null;
            buffDef.iconSprite = buffIcon;

            AddBuffDef(buffDef);

            return buffDef;
        }

        internal static void AddEffectDef(EffectDef effectDef)
        {
            ContentPacks.effectDefs.Add(effectDef);
        }
        internal static EffectDef CreateAndAddEffectDef(GameObject effectPrefab)
        {
            EffectDef effectDef = new EffectDef(effectPrefab);

            AddEffectDef(effectDef);

            return effectDef;
        }

        internal static void AddNetworkSoundEventDef(NetworkSoundEventDef networkSoundEventDef)
        {
            ContentPacks.networkSoundEventDefs.Add(networkSoundEventDef);
        }
        internal static NetworkSoundEventDef CreateAndAddNetworkSoundEventDef(string eventName)
        {
            NetworkSoundEventDef networkSoundEventDef = ScriptableObject.CreateInstance<NetworkSoundEventDef>();
            networkSoundEventDef.akId = AkSoundEngine.GetIDFromString(eventName);
            networkSoundEventDef.eventName = eventName;

            AddNetworkSoundEventDef(networkSoundEventDef);

            return networkSoundEventDef;
        }
    }
    internal static class Skills
    {
        public static Dictionary<string, SkillLocator> characterSkillLocators = new Dictionary<string, SkillLocator>();

        #region genericskills
        public static void CreateSkillFamilies(GameObject targetPrefab) => CreateSkillFamilies(targetPrefab, SkillSlot.Primary, SkillSlot.Secondary, SkillSlot.Utility, SkillSlot.Special);
        /// <summary>
        /// Create in order the GenericSkills for the skillslots desired, and create skillfamilies for them.
        /// </summary>
        /// <param name="targetPrefab">Body prefab to add GenericSkills</param>
        /// <param name="slots">Order of slots to add to the body prefab.</param>
        public static void CreateSkillFamilies(GameObject targetPrefab, params SkillSlot[] slots)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();

            for (int i = 0; i < slots.Length; i++)
            {
                switch (slots[i])
                {
                    case SkillSlot.Primary:
                        skillLocator.primary = CreateGenericSkillWithSkillFamily(targetPrefab, "Primary");
                        break;
                    case SkillSlot.Secondary:
                        skillLocator.secondary = CreateGenericSkillWithSkillFamily(targetPrefab, "Secondary");
                        break;
                    case SkillSlot.Utility:
                        skillLocator.utility = CreateGenericSkillWithSkillFamily(targetPrefab, "Utility");
                        break;
                    case SkillSlot.Special:
                        skillLocator.special = CreateGenericSkillWithSkillFamily(targetPrefab, "Special");
                        break;
                    case SkillSlot.None:
                        break;
                }
            }
        }

        public static void ClearGenericSkills(GameObject targetPrefab)
        {
            foreach (GenericSkill obj in targetPrefab.GetComponentsInChildren<GenericSkill>())
            {
                UnityEngine.Object.DestroyImmediate(obj);
            }
        }

        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, SkillSlot skillSlot, bool hidden = false)
        {
            SkillLocator skillLocator = targetPrefab.GetComponent<SkillLocator>();
            switch (skillSlot)
            {
                case SkillSlot.Primary:
                    return skillLocator.primary = CreateGenericSkillWithSkillFamily(targetPrefab, "Primary", hidden);
                case SkillSlot.Secondary:
                    return skillLocator.secondary = CreateGenericSkillWithSkillFamily(targetPrefab, "Secondary", hidden);
                case SkillSlot.Utility:
                    return skillLocator.utility = CreateGenericSkillWithSkillFamily(targetPrefab, "Utility", hidden);
                case SkillSlot.Special:
                    return skillLocator.special = CreateGenericSkillWithSkillFamily(targetPrefab, "Special", hidden);
                case SkillSlot.None:
                    Log.Error("Failed to create GenericSkill with skillslot None. If making a GenericSkill outside of the main 4, specify a familyName, and optionally a genericSkillName");
                    return null;
            }
            return null;
        }
        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, string familyName, bool hidden = false) => CreateGenericSkillWithSkillFamily(targetPrefab, familyName, familyName, hidden);
        public static GenericSkill CreateGenericSkillWithSkillFamily(GameObject targetPrefab, string genericSkillName, string familyName, bool hidden = false)
        {
            GenericSkill skill = targetPrefab.AddComponent<GenericSkill>();
            skill.skillName = genericSkillName;
            skill.hideInCharacterSelect = hidden;

            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            (newFamily as ScriptableObject).name = targetPrefab.name + familyName + "Family";
            newFamily.variants = new SkillFamily.Variant[0];

            skill._skillFamily = newFamily;

            Content.AddSkillFamily(newFamily);
            return skill;
        }
        #endregion

        #region skillfamilies

        //everything calls this
        public static void AddSkillToFamily(SkillFamily skillFamily, SkillDef skillDef, UnlockableDef unlockableDef = null)
        {
            Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);

            skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
            {
                skillDef = skillDef,
                unlockableDef = unlockableDef,
                viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
            };
        }

        public static void AddSkillsToFamily(SkillFamily skillFamily, params SkillDef[] skillDefs)
        {
            foreach (SkillDef skillDef in skillDefs)
            {
                AddSkillToFamily(skillFamily, skillDef);
            }
        }

        public static void AddPrimarySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().primary.skillFamily, skillDefs);
        }
        public static void AddSecondarySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().secondary.skillFamily, skillDefs);
        }
        public static void AddUtilitySkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().utility.skillFamily, skillDefs);
        }
        public static void AddSpecialSkills(GameObject targetPrefab, params SkillDef[] skillDefs)
        {
            AddSkillsToFamily(targetPrefab.GetComponent<SkillLocator>().special.skillFamily, skillDefs);
        }

        /// <summary>
        /// pass in an amount of unlockables equal to or less than skill variants, null for skills that aren't locked
        /// <code>
        /// AddUnlockablesToFamily(skillLocator.primary, null, skill2UnlockableDef, null, skill4UnlockableDef);
        /// </code>
        /// </summary>
        public static void AddUnlockablesToFamily(SkillFamily skillFamily, params UnlockableDef[] unlockableDefs)
        {
            for (int i = 0; i < unlockableDefs.Length; i++)
            {
                SkillFamily.Variant variant = skillFamily.variants[i];
                variant.unlockableDef = unlockableDefs[i];
                skillFamily.variants[i] = variant;
            }
        }
        #endregion

        #region entitystates
        public static ComboSkillDef.Combo ComboFromType(Type t)
        {
            ComboSkillDef.Combo combo = new ComboSkillDef.Combo();
            combo.activationStateType = new SerializableEntityStateType(t);
            return combo;
        }
        #endregion
    }
}