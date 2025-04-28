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
using SwanSongExtended.Characters;
using UnityEngine.AddressableAssets;
using ThreeEyedGames;
using RoR2.ExpansionManagement;
using static R2API.DamageAPI;
using static SwanSongExtended.Modules.Language.Styling;
using static MoreStats.OnHit;
using static R2API.RecalculateStatsAPI;

namespace SwanSongExtended.Modules
{
    public static class CommonAssets
    {
        private static AssetBundle _mainAssetBundle;
        public static AssetBundle mainAssetBundle
        {
            get
            {
                if (_mainAssetBundle == null)
                    _mainAssetBundle = Assets.LoadAssetBundle("itmightbebad");
                return _mainAssetBundle;
            }
            set
            {
                _mainAssetBundle = value;
            }
        }
        private static AssetBundle _orangeAssetBundle;
        public static AssetBundle orangeAssetBundle
        {
            get
            {
                if (_orangeAssetBundle == null)
                    _orangeAssetBundle = Assets.LoadAssetBundle("orangecontent");
                return _orangeAssetBundle;
            }
            set
            {
                _orangeAssetBundle = value;
            }
        }

        public static string dropPrefabsPath = "Assets/Models/DropPrefabs";
        public static string iconsPath = "Assets/Textures/Icons/";
        public static string eliteMaterialsPath = "Assets/Textures/Materials/Elite/";

        public static void Init()
        {
            AddAcridReworkAssets();
            CreateMeatballNapalmPool();
            CreateSquidBlasterBall();

            AddTrophyHunterDebuffs();
            AddCombatTelescopeCritChance();

            AddMaskHauntAssets();
            AddHarpoonAssets();
            AddRazorwireAssets();
            AddBrittleCrownAssets();
            AddGestureAssets();
        }

        #region acrid
        public static ModdedDamageType AcridFesterDamage;
        public static ModdedDamageType AcridCorrosiveDamage;
        public static BuffDef corrosionBuff;
        public static DotController.DotDef corrosionDotDef;
        public static DotController.DotIndex corrosionDotIndex;
        public static float contagiousTransferRate = 0.5f;
        public static int corrosionArmorReduction = 20;
        public static float corrosionDuration = 6f;
        public static float corrosionDamagePerSecond = 1f;
        public static float corrosionTickInterval = 1f;
        public static bool festerResetAllDots = false;
        public const string AcridFesterKeywordToken = "KEYWORD_FESTER";
        public const string AcridCorrosionKeywordToken = "KEYWORD_CORROSION";
        public const string AcridContagiousKeywordToken = "KEYWORD_CONTAGIOUS";
        private static void AddAcridReworkAssets()
        {
            OrbAPI.AddOrb<SwanSongExtended.Orbs.DiseaseOrb>();
            AcridFesterDamage = DamageAPI.ReserveDamageType();
            AcridCorrosiveDamage = DamageAPI.ReserveDamageType();

            corrosionBuff = Content.CreateAndAddBuff(
                "AcridCorrosion", 
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffBleedingIcon.tif").WaitForCompletion(), 
                Color.yellow, 
                false, true);
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
                        if (!festerResetAllDots ||
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

        public static BuffDef gestureQueueEquipBreak;
        private static void AddGestureAssets()
        {
            gestureQueueEquipBreak = Content.CreateAndAddBuff(
                "bdGestureBreakPending",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(),
                Color.cyan, false, false);
            gestureQueueEquipBreak.isHidden = true;
        }

        public static BuffDef brittleCrownCursePurchase;
        private static void AddBrittleCrownAssets()
        {
            brittleCrownCursePurchase = Content.CreateAndAddBuff(
                "bdBrittleCrownCursePurchase", 
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(), 
                Color.cyan, true, false);
        }

        public static string executeKeywordToken = "DUCK_EXECUTION_KEYWORD";

        #region razorwire
        public static BuffDef razorChargeBuff;
        private static void AddRazorwireAssets()
        {
            razorChargeBuff = Content.CreateAndAddBuff(
                "bdRazorChargeBuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(), //replace me
                Color.white,
                true,
                false);
            razorChargeBuff.isCooldown = true;
        }
        #endregion

        #region happiest mask
        public static BuffDef hauntDebuff;
        public static GameObject hauntEffectPrefab;
        private static void AddMaskHauntAssets()
        {
            hauntDebuff = Content.CreateAndAddBuff(
                "bdHappiestMaskHauntDebuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(), //replace me
                new Color(0.9f, 0.7f, 1.0f),
                false,
                true);
            hauntDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            hauntEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HauntVisualEffect");
        }
        #endregion

        #region hunters harpoon
        public static BuffDef harpoonDebuff;
        public static GameObject harpoonEffectPrefab;
        private static void AddHarpoonAssets()
        {
            harpoonDebuff = Content.CreateAndAddBuff(
                "bdHarpoonTargetDebuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(),
                new Color(0.9f, 0.7f, 0.1f),
                true,
                true);
            harpoonDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            harpoonEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HarpoonTargetVisualEffect");
        }
        #endregion

        public static GameObject squidBlasterBall;
        private static void CreateSquidBlasterBall()
        {
            squidBlasterBall = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBoss/TarSeeker.prefab").WaitForCompletion().InstantiateClone("MiredUrnTarball", true);

            ProjectileSteerTowardTarget pstt = squidBlasterBall.GetComponent<ProjectileSteerTowardTarget>(); //no homing
            if (pstt)
            {
                UnityEngine.Object.Destroy(pstt);
            }
            ProjectileDirectionalTargetFinder pdtf = squidBlasterBall.GetComponent<ProjectileDirectionalTargetFinder>();
            if (pdtf)
            {
                pdtf.ignoreAir = false;
            }
            //ProjectileCharacterController pcc = squidBlasterBall.GetComponent<ProjectileCharacterController>();
            //if (pcc)
            //{
            //    pcc.
            //}
            //CharacterController cc = squidBlasterBall.GetComponent<CharacterController>();
            //if (cc)
            //{
            //    UnityEngine.Object.Destroy(cc);
            //}
            ProjectileImpactExplosion pie = squidBlasterBall.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.lifetime = 1;
                pie.impactEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBoss/TarballExplosion.prefab").WaitForCompletion();
            }


            R2API.ContentAddition.AddProjectile(squidBlasterBall);
        }

        public static float survivorExecuteThreshold = 0.15f;
        public static float banditExecutionThreshold = 0.1f;
        public static float harvestExecutionThreshold = 0.2f;

        public static BuffDef bossHunterDebuffWithScalpel;
        private static void AddTrophyHunterDebuffs()
        {
            bossHunterDebuffWithScalpel = Content.CreateAndAddBuff(
                "bdTrophyHunterScalpelDebuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion(),
                new Color(0.2f, 0.9f, 0.8f, 1),
                false,
                true);
            bossHunterDebuffWithScalpel.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
        }

        public static BuffDef combatTelescopeCritChance;
        private static void AddCombatTelescopeCritChance()
        {
            combatTelescopeCritChance = Content.CreateAndAddBuff(
                "bdCombatTelescopeCrit",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion(),
                Color.red,
                false,
                false);
        }

        public static GameObject meatballNapalmPool;
        private static void CreateMeatballNapalmPool()
        {
            meatballNapalmPool = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/beetlequeenacid").InstantiateClone("NapalmFire", true);

            Color napalmColor = new Color32(255, 120, 0, 255);


            Transform pDotObjDecal = meatballNapalmPool.transform.Find("FX/Decal");
            Material napalmDecalMaterial = new Material(pDotObjDecal.GetComponent<Decal>().Material);
            napalmDecalMaterial.SetColor("_Color", napalmColor);
            pDotObjDecal.GetComponent<Decal>().Material = napalmDecalMaterial;

            ProjectileDotZone pdz = meatballNapalmPool.GetComponent<ProjectileDotZone>();
            pdz.lifetime = 5f;
            pdz.fireFrequency = 2f;
            pdz.damageCoefficient = 0.5f;
            pdz.overlapProcCoefficient = 0.5f;
            pdz.attackerFiltering = AttackerFiltering.Default;
            meatballNapalmPool.GetComponent<ProjectileDamage>().damageType = DamageType.IgniteOnHit;
            meatballNapalmPool.GetComponent<ProjectileController>().procCoefficient = 1f;

            float decalScale = 2.5f;
            meatballNapalmPool.GetComponent<Transform>().localScale = new Vector3(decalScale, decalScale, decalScale);

            Transform transform = meatballNapalmPool.transform.Find("FX");
            transform.Find("Spittle").gameObject.SetActive(false);

            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(
                LegacyResourcesAPI.Load<GameObject>("prefabs/FireTrail").GetComponent<DamageTrail>().segmentPrefab, transform.transform);
            ParticleSystem.MainModule main = gameObject.GetComponent<ParticleSystem>().main;
            main.duration = 8f;
            main.gravityModifier = -0.075f;
            ParticleSystem.MinMaxCurve startSizeX = main.startSizeX;
            startSizeX.constantMin *= 0.6f;
            startSizeX.constantMax *= 0.8f;
            ParticleSystem.MinMaxCurve startSizeY = main.startSizeY;
            startSizeY.constantMin *= 0.8f;
            startSizeY.constantMax *= 1f;
            ParticleSystem.MinMaxCurve startSizeZ = main.startSizeZ;
            startSizeZ.constantMin *= 0.6f;
            startSizeZ.constantMax *= 0.8f;
            ParticleSystem.MinMaxCurve startLifetime = main.startLifetime;
            startLifetime.constantMin = 0.9f;
            startLifetime.constantMax = 1.1f;
            gameObject.GetComponent<DestroyOnTimer>().enabled = false;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localPosition = Vector3.zero;
            gameObject.transform.localScale = Vector3.one;
            ParticleSystem.ShapeModule shape = gameObject.GetComponent<ParticleSystem>().shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.scale = Vector3.one * 0.5f;

            GameObject gameObject2 = transform.Find("Point Light").gameObject;
            Light component2 = gameObject2.GetComponent<Light>();
            component2.color = new Color(1f, 0.5f, 0f);
            component2.intensity = 6f;
            component2.range = 12f;

            Content.AddProjectilePrefab(meatballNapalmPool);
        }

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
            assetBundle = AssetBundle.LoadFromFile(Path.Combine(Path.GetDirectoryName(SwanSongPlugin.instance.Info.Location), bundleName));

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
    internal static class Prefabs
    {
        // module for creating body prefabs and whatnot
        // recommended to simply avoid touching this unless you REALLY need to

        // cache this just to give our ragdolls the same physic material as vanilla stuff
        private static PhysicMaterial ragdollMaterial;

        public static GameObject CreateDisplayPrefab(AssetBundle assetBundle, string displayPrefabName, GameObject prefab)
        {
            GameObject display = assetBundle.LoadAsset<GameObject>(displayPrefabName);
            if (display == null)
            {
                Log.Error($"could not load display prefab {displayPrefabName}. Make sure this prefab exists in assetbundle {assetBundle.name}");
                return null;
            }

            CharacterModel characterModel = display.GetComponent<CharacterModel>();
            if (!characterModel)
            {
                characterModel = display.AddComponent<CharacterModel>();
            }
            characterModel.baseRendererInfos = prefab.GetComponentInChildren<CharacterModel>().baseRendererInfos;

            Modules.Assets.ConvertAllRenderersToHopooShader(display);

            return display;
        }

        #region body setup

        public static GameObject LoadCharacterModel(AssetBundle assetBundle, string modelName)
        {
            GameObject model = assetBundle.LoadAsset<GameObject>(modelName);
            if (model == null)
            {
                Log.Error($"could not load model prefab {modelName}. Make sure this prefab exists in assetbundle {assetBundle.name}");
                return null;
            }
            return model;
        }

        public static GameObject LoadCharacterBody(AssetBundle assetBundle, string bodyName)
        {
            GameObject body = assetBundle.LoadAsset<GameObject>(bodyName);
            if (body == null)
            {
                Log.Error($"could not load body prefab {bodyName}. Make sure this prefab exists in assetbundle {assetBundle.name}");
                return null;
            }
            return body;
        }

        public static GameObject CloneCharacterBody(BodyInfo bodyInfo)
        {
            GameObject clonedBody = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/" + bodyInfo.bodyNameToClone + "Body");
            if (!clonedBody)
            {
                Log.Error(bodyInfo.bodyNameToClone + " Body to clone is not a valid body, character creation failed");
                return null;
            }

            GameObject newBodyPrefab = PrefabAPI.InstantiateClone(clonedBody, bodyInfo.bodyName);

            for (int i = newBodyPrefab.transform.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(newBodyPrefab.transform.GetChild(i).gameObject);
            }

            return newBodyPrefab;
        }

        /// <summary>
        /// clone a body according to your BodyInfo, load your model prefab from the assetbundle, and set up components on both objects through code
        /// </summary>
        public static GameObject CreateBodyPrefab(AssetBundle assetBundle, string modelPrefabName, BodyInfo bodyInfo)
        {
            return CreateBodyPrefab(LoadCharacterModel(assetBundle, modelPrefabName), bodyInfo);
        }
        /// <summary>
        /// clone a body according to your BodyInfo, pass in your model prefab, and set up components on both objects through code
        /// </summary>
        public static GameObject CreateBodyPrefab(GameObject model, BodyInfo bodyInfo)
        {
            return CreateBodyPrefab(CloneCharacterBody(bodyInfo), model, bodyInfo);
        }
        /// <summary>
        /// Pass in a body prefab, loads your model from the assetbundle, and set up components on both objects through code
        /// </summary>
        public static GameObject CreateBodyPrefab(GameObject newBodyPrefab, AssetBundle assetBundle, string modelName, BodyInfo bodyInfo)
        {
            return CreateBodyPrefab(newBodyPrefab, LoadCharacterModel(assetBundle, modelName), bodyInfo);
        }
        /// <summary>
        /// loads your body from the assetbundle, loads your model from the assetbundle, and set up components on both objects through code
        /// </summary>
        public static GameObject CreateBodyPrefab(AssetBundle assetBundle, string bodyPrefabName, string modelPrefabName, BodyInfo bodyInfo)
        {
            return CreateBodyPrefab(LoadCharacterBody(assetBundle, bodyPrefabName), LoadCharacterModel(assetBundle, modelPrefabName), bodyInfo);
        }
        /// <summary>
        /// Pass in a body prefab, pass in a model prefab, and set up components on both objects through code
        /// </summary>
        public static GameObject CreateBodyPrefab(GameObject newBodyPrefab, GameObject model, BodyInfo bodyInfo)
        {
            if (model == null || newBodyPrefab == null)
            {
                Log.Error($"Character creation failed. Model: {model}, Body: {newBodyPrefab}");
                return null;
            }

            SetupCharacterBody(newBodyPrefab, bodyInfo);

            Transform modelBaseTransform = AddCharacterModelToSurvivorBody(newBodyPrefab, model.transform, bodyInfo);

            SetupModelLocator(newBodyPrefab, modelBaseTransform, model.transform);
            SetupCharacterDirection(newBodyPrefab, modelBaseTransform, model.transform);
            SetupCameraTargetParams(newBodyPrefab, bodyInfo);
            //SetupRigidbody(newPrefab);
            SetupCapsuleCollider(newBodyPrefab);

            Modules.Content.AddCharacterBodyPrefab(newBodyPrefab);

            return newBodyPrefab;
        }

        private static void SetupCharacterBody(GameObject newBodyPrefab, BodyInfo bodyInfo)
        {
            CharacterBody bodyComponent = newBodyPrefab.GetComponent<CharacterBody>();
            //identity
            bodyComponent.baseNameToken = bodyInfo.bodyNameToken;
            bodyComponent.subtitleNameToken = bodyInfo.subtitleNameToken;
            bodyComponent.portraitIcon = bodyInfo.characterPortrait;
            bodyComponent.bodyColor = bodyInfo.bodyColor;

            bodyComponent._defaultCrosshairPrefab = bodyInfo.crosshair;
            bodyComponent.hideCrosshair = false;
            bodyComponent.preferredPodPrefab = bodyInfo.podPrefab;

            //stats
            bodyComponent.baseMaxHealth = bodyInfo.maxHealth;
            bodyComponent.baseRegen = bodyInfo.healthRegen;
            bodyComponent.baseArmor = bodyInfo.armor;
            bodyComponent.baseMaxShield = bodyInfo.shield;

            bodyComponent.baseDamage = bodyInfo.damage;
            bodyComponent.baseAttackSpeed = bodyInfo.attackSpeed;
            bodyComponent.baseCrit = bodyInfo.crit;

            bodyComponent.baseMoveSpeed = bodyInfo.moveSpeed;
            bodyComponent.baseJumpPower = bodyInfo.jumpPower;

            //level stats
            bodyComponent.autoCalculateLevelStats = bodyInfo.autoCalculateLevelStats;

            if (bodyInfo.autoCalculateLevelStats)
            {

                bodyComponent.levelMaxHealth = Mathf.Round(bodyComponent.baseMaxHealth * 0.3f);
                bodyComponent.levelMaxShield = Mathf.Round(bodyComponent.baseMaxShield * 0.3f);
                bodyComponent.levelRegen = bodyComponent.baseRegen * 0.2f;

                bodyComponent.levelMoveSpeed = 0f;
                bodyComponent.levelJumpPower = 0f;

                bodyComponent.levelDamage = bodyComponent.baseDamage * 0.2f;
                bodyComponent.levelAttackSpeed = 0f;
                bodyComponent.levelCrit = 0f;

                bodyComponent.levelArmor = 0f;

            }
            else
            {

                bodyComponent.levelMaxHealth = bodyInfo.healthGrowth;
                bodyComponent.levelMaxShield = bodyInfo.shieldGrowth;
                bodyComponent.levelRegen = bodyInfo.regenGrowth;

                bodyComponent.levelMoveSpeed = bodyInfo.moveSpeedGrowth;
                bodyComponent.levelJumpPower = bodyInfo.jumpPowerGrowth;

                bodyComponent.levelDamage = bodyInfo.damageGrowth;
                bodyComponent.levelAttackSpeed = bodyInfo.attackSpeedGrowth;
                bodyComponent.levelCrit = bodyInfo.critGrowth;

                bodyComponent.levelArmor = bodyInfo.armorGrowth;
            }
            //other
            bodyComponent.baseAcceleration = bodyInfo.acceleration;

            bodyComponent.baseJumpCount = bodyInfo.jumpCount;

            bodyComponent.sprintingSpeedMultiplier = 1.45f;

            bodyComponent.bodyFlags = CharacterBody.BodyFlags.ImmuneToExecutes;
            bodyComponent.rootMotionInMainState = false;

            bodyComponent.hullClassification = HullClassification.Human;

            bodyComponent.isChampion = false;
        }

        private static Transform AddCharacterModelToSurvivorBody(GameObject bodyPrefab, Transform modelTransform, BodyInfo bodyInfo)
        {
            Transform modelBase = bodyPrefab.transform.Find("ModelBase");
            if (modelBase == null) // if these objects exist, you must have set them as you want them in editor
            {
                modelBase = new GameObject("ModelBase").transform;
                modelBase.parent = bodyPrefab.transform;
                modelBase.localPosition = bodyInfo.modelBasePosition;
                modelBase.localRotation = Quaternion.identity;
            }

            modelTransform.parent = modelBase.transform;
            modelTransform.localPosition = Vector3.zero;
            modelTransform.localRotation = Quaternion.identity;

            Transform cameraPivot = bodyPrefab.transform.Find("CameraPivot");
            if (cameraPivot == null)
            {
                cameraPivot = new GameObject("CameraPivot").transform;
                cameraPivot.parent = bodyPrefab.transform;
                cameraPivot.localPosition = bodyInfo.cameraPivotPosition;
                cameraPivot.localRotation = Quaternion.identity;
            }

            Transform aimOrigin = bodyPrefab.transform.Find("AimOrigin");
            if (aimOrigin == null)
            {
                aimOrigin = new GameObject("AimOrigin").transform;
                aimOrigin.parent = bodyPrefab.transform;
                aimOrigin.localPosition = bodyInfo.aimOriginPosition;
                aimOrigin.localRotation = Quaternion.identity;
            }
            bodyPrefab.GetComponent<CharacterBody>().aimOriginTransform = aimOrigin;

            return modelBase.transform;
        }

        private static void SetupCharacterDirection(GameObject prefab, Transform modelBaseTransform, Transform modelTransform)
        {
            if (!prefab.GetComponent<CharacterDirection>())
                return;

            CharacterDirection characterDirection = prefab.GetComponent<CharacterDirection>();
            characterDirection.targetTransform = modelBaseTransform;
            characterDirection.overrideAnimatorForwardTransform = null;
            characterDirection.rootMotionAccumulator = null;
            characterDirection.modelAnimator = modelTransform.GetComponent<Animator>();
            characterDirection.driveFromRootRotation = false;
            characterDirection.turnSpeed = 720f;
        }

        private static void SetupCameraTargetParams(GameObject prefab, BodyInfo bodyInfo)
        {
            CameraTargetParams cameraTargetParams = prefab.GetComponent<CameraTargetParams>();
            cameraTargetParams.cameraParams = bodyInfo.cameraParams;
            cameraTargetParams.cameraPivotTransform = prefab.transform.Find("CameraPivot");
        }

        private static void SetupModelLocator(GameObject prefab, Transform modelBaseTransform, Transform modelTransform)
        {
            ModelLocator modelLocator = prefab.GetComponent<ModelLocator>();
            modelLocator.modelTransform = modelTransform;
            modelLocator.modelBaseTransform = modelBaseTransform;
        }

        //private static void SetupRigidbody(GameObject prefab)
        //{
        //    Rigidbody rigidbody = prefab.GetComponent<Rigidbody>();
        //    rigidbody.mass = 100f;
        //}

        private static void SetupCapsuleCollider(GameObject prefab)
        {
            //character collider MUST be commando's size!
            CapsuleCollider capsuleCollider = prefab.GetComponent<CapsuleCollider>();
            capsuleCollider.center = new Vector3(0f, 0f, 0f);
            capsuleCollider.radius = 0.5f;
            capsuleCollider.height = 1.82f;
            capsuleCollider.direction = 1;
        }
        #endregion body setup

        #region ModelSetup
        public static CharacterModel SetupCharacterModel(GameObject bodyPrefab, CustomRendererInfo[] customInfos = null)
        {

            CharacterModel characterModel = bodyPrefab.GetComponent<ModelLocator>().modelTransform.gameObject.GetComponent<CharacterModel>();
            bool preattached = characterModel != null;
            if (!preattached)
                characterModel = bodyPrefab.GetComponent<ModelLocator>().modelTransform.gameObject.AddComponent<CharacterModel>();

            characterModel.body = bodyPrefab.GetComponent<CharacterBody>();

            characterModel.autoPopulateLightInfos = true;
            characterModel.invisibilityCount = 0;
            characterModel.temporaryOverlays = new List<TemporaryOverlayInstance>();

            if (!preattached)
            {
                SetupCustomRendererInfos(characterModel, customInfos);
            }
            else
            {
                SetupPreAttachedRendererInfos(characterModel);
            }

            SetupHurtboxGroup(bodyPrefab, characterModel.gameObject);
            SetupAimAnimator(bodyPrefab, characterModel.gameObject);
            SetupFootstepController(characterModel.gameObject);
            SetupRagdoll(characterModel.gameObject);

            return characterModel;
        }

        public static void SetupPreAttachedRendererInfos(CharacterModel characterModel)
        {
            for (int i = 0; i < characterModel.baseRendererInfos.Length; i++)
            {
                if (characterModel.baseRendererInfos[i].defaultMaterial == null)
                {
                    characterModel.baseRendererInfos[i].defaultMaterial = characterModel.baseRendererInfos[i].renderer.sharedMaterial;
                }

                if (characterModel.baseRendererInfos[i].defaultMaterial == null)
                {
                    Log.Error($"no material for rendererinfo of this renderer: {characterModel.baseRendererInfos[i].renderer}");
                }
                characterModel.baseRendererInfos[i].defaultMaterial.ConvertDefaultShaderToHopoo();
            }
        }

        public static void SetupCustomRendererInfos(CharacterModel characterModel, CustomRendererInfo[] customInfos)
        {

            ChildLocator childLocator = characterModel.GetComponent<ChildLocator>();
            if (!childLocator)
            {
                Log.Error("Failed CharacterModel setup: ChildLocator component does not exist on the model");
                return;
            }

            List<CharacterModel.RendererInfo> rendererInfos = new List<CharacterModel.RendererInfo>();

            for (int i = 0; i < customInfos.Length; i++)
            {
                if (!childLocator.FindChild(customInfos[i].childName))
                {
                    Log.Error("Trying to add a RendererInfo for a renderer that does not exist: " + customInfos[i].childName);
                }
                else
                {
                    Renderer rend = childLocator.FindChild(customInfos[i].childName).GetComponent<Renderer>();
                    if (rend)
                    {

                        Material mat = customInfos[i].material;

                        if (mat == null)
                        {
                            if (customInfos[i].dontHotpoo)
                            {
                                mat = rend.sharedMaterial;
                            }
                            else
                            {
                                mat = rend.sharedMaterial.ConvertDefaultShaderToHopoo();
                            }
                        }

                        rendererInfos.Add(new CharacterModel.RendererInfo
                        {
                            renderer = rend,
                            defaultMaterial = mat,
                            ignoreOverlays = customInfos[i].ignoreOverlays,
                            defaultShadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On
                        });
                    }
                }
            }

            characterModel.baseRendererInfos = rendererInfos.ToArray();
        }

        private static void SetupHurtboxGroup(GameObject bodyPrefab, GameObject model)
        {
            SetupMainHurtboxesFromChildLocator(bodyPrefab, model);

            SetHurtboxesHealthComponents(bodyPrefab);
        }
        /// <summary>
        /// Sets up the main Hurtbox from a collider assigned to the child locator called "MainHurtbox".
        /// <para>If a "HeadHurtbox" child is also set up, automatically sets that one up and assigns that one as a sniper weakpoint. if not, MainHurtbox is set as a sniper weakpoint.</para>
        /// </summary>
        private static void SetupMainHurtboxesFromChildLocator(GameObject bodyPrefab, GameObject model)
        {
            if (bodyPrefab.GetComponent<HurtBoxGroup>() != null)
            {
                Log.Debug("Hitboxgroup already exists on model prefab. aborting code setup");
                return;
            }

            ChildLocator childLocator = model.GetComponent<ChildLocator>();

            if (!childLocator.FindChild("MainHurtbox"))
            {
                Log.Error("Could not set up main hurtbox: make sure you have a transform pair in your prefab's ChildLocator called 'MainHurtbox'");
                return;
            }

            HurtBoxGroup hurtBoxGroup = model.AddComponent<HurtBoxGroup>();

            HurtBox headHurtbox = null;
            GameObject headHurtboxObject = childLocator.FindChildGameObject("HeadHurtbox");
            if (headHurtboxObject)
            {
                Log.Debug("HeadHurtboxFound. Setting up");
                headHurtbox = headHurtboxObject.AddComponent<HurtBox>();
                headHurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
                headHurtbox.healthComponent = bodyPrefab.GetComponent<HealthComponent>();
                headHurtbox.isBullseye = false;
                headHurtbox.isSniperTarget = true;
                headHurtbox.damageModifier = HurtBox.DamageModifier.Normal;
                headHurtbox.hurtBoxGroup = hurtBoxGroup;
                headHurtbox.indexInGroup = 1;
            }

            HurtBox mainHurtbox = childLocator.FindChildGameObject("MainHurtbox").AddComponent<HurtBox>();
            mainHurtbox.gameObject.layer = LayerIndex.entityPrecise.intVal;
            mainHurtbox.healthComponent = bodyPrefab.GetComponent<HealthComponent>();
            mainHurtbox.isBullseye = true;
            mainHurtbox.isSniperTarget = headHurtbox == null;
            mainHurtbox.damageModifier = HurtBox.DamageModifier.Normal;
            mainHurtbox.hurtBoxGroup = hurtBoxGroup;
            mainHurtbox.indexInGroup = 0;

            if (headHurtbox)
            {
                hurtBoxGroup.hurtBoxes = new HurtBox[]
                {
                    mainHurtbox,
                    headHurtbox
                };
            }
            else
            {
                hurtBoxGroup.hurtBoxes = new HurtBox[]
                {
                    mainHurtbox,
                };
            }
            hurtBoxGroup.mainHurtBox = mainHurtbox;
            hurtBoxGroup.bullseyeCount = 1;
        }

        public static void SetHurtboxesHealthComponents(GameObject bodyPrefab)
        {
            HealthComponent healthComponent = bodyPrefab.GetComponent<HealthComponent>();

            foreach (HurtBoxGroup hurtboxGroup in bodyPrefab.GetComponentsInChildren<HurtBoxGroup>())
            {
                hurtboxGroup.mainHurtBox.healthComponent = healthComponent;
                for (int i = 0; i < hurtboxGroup.hurtBoxes.Length; i++)
                {
                    hurtboxGroup.hurtBoxes[i].healthComponent = healthComponent;
                }
            }
        }

        private static void SetupFootstepController(GameObject model)
        {
            FootstepHandler footstepHandler = model.AddComponent<FootstepHandler>();
            footstepHandler.baseFootstepString = "Play_player_footstep";
            footstepHandler.sprintFootstepOverrideString = "";
            footstepHandler.enableFootstepDust = true;
            footstepHandler.footstepDustPrefab = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/GenericFootstepDust");
        }

        private static void SetupRagdoll(GameObject model)
        {
            RagdollController ragdollController = model.GetComponent<RagdollController>();

            if (!ragdollController) return;

            if (ragdollMaterial == null) ragdollMaterial = RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterBodies/CommandoBody").GetComponentInChildren<RagdollController>().bones[1].GetComponent<Collider>().material;

            foreach (Transform boneTransform in ragdollController.bones)
            {
                if (boneTransform)
                {
                    boneTransform.gameObject.layer = LayerIndex.ragdoll.intVal;
                    Collider boneCollider = boneTransform.GetComponent<Collider>();
                    if (boneCollider)
                    {
                        //boneCollider.material = ragdollMaterial;
                        boneCollider.sharedMaterial = ragdollMaterial;
                    }
                    else
                    {
                        Log.Error($"Ragdoll bone {boneTransform.gameObject} doesn't have a collider. Ragdoll will break.");
                    }
                }
            }
        }

        private static void SetupAimAnimator(GameObject prefab, GameObject model)
        {
            AimAnimator aimAnimator = model.AddComponent<AimAnimator>();
            aimAnimator.directionComponent = prefab.GetComponent<CharacterDirection>();
            aimAnimator.pitchRangeMax = 60f;
            aimAnimator.pitchRangeMin = -60f;
            aimAnimator.yawRangeMin = -80f;
            aimAnimator.yawRangeMax = 80f;
            aimAnimator.pitchGiveupRange = 30f;
            aimAnimator.yawGiveupRange = 10f;
            aimAnimator.giveupDuration = 3f;
            aimAnimator.inputBank = prefab.GetComponent<InputBankTest>();
        }
        #endregion

        #region master
        public static void CreateGenericDoppelganger(GameObject bodyPrefab, string masterName, string masterToCopy) => CloneDopplegangerMaster(bodyPrefab, masterName, masterToCopy);
        public static GameObject CloneDopplegangerMaster(GameObject bodyPrefab, string masterName, string masterToCopy)
        {
            GameObject newMaster = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/" + masterToCopy + "MonsterMaster"), masterName, true);
            newMaster.GetComponent<CharacterMaster>().bodyPrefab = bodyPrefab;

            Modules.Content.AddMasterPrefab(newMaster);
            return newMaster;
        }

        public static GameObject CreateBlankMasterPrefab(GameObject bodyPrefab, string masterName)
        {
            GameObject masterObject = PrefabAPI.InstantiateClone(RoR2.LegacyResourcesAPI.Load<GameObject>("Prefabs/CharacterMasters/CommandoMonsterMaster"), masterName, true);
            //should the user call this themselves?
            Modules.ContentPacks.masterPrefabs.Add(masterObject);

            CharacterMaster characterMaster = masterObject.GetComponent<CharacterMaster>();
            characterMaster.bodyPrefab = bodyPrefab;

            AISkillDriver[] drivers = masterObject.GetComponents<AISkillDriver>();
            for (int i = 0; i < drivers.Length; i++)
            {
                UnityEngine.Object.Destroy(drivers[i]);
            }

            return masterObject;
        }

        public static GameObject LoadMaster(this AssetBundle assetBundle, GameObject bodyPrefab, string assetName)
        {
            GameObject newMaster = assetBundle.LoadAsset<GameObject>(assetName);

            BaseAI baseAI = newMaster.GetComponent<BaseAI>();
            if (baseAI == null)
            {
                baseAI = newMaster.AddComponent<BaseAI>();
                baseAI.aimVectorDampTime = 0.1f;
                baseAI.aimVectorMaxSpeed = 360;
            }
            baseAI.scanState = new EntityStates.SerializableEntityStateType(typeof(EntityStates.AI.Walker.Wander));

            EntityStateMachine stateMachine = newMaster.GetComponent<EntityStateMachine>();
            if (stateMachine == null)
            {
                AddEntityStateMachine(newMaster, "AI", typeof(EntityStates.AI.Walker.Wander), typeof(EntityStates.AI.Walker.Wander));
            }

            baseAI.stateMachine = stateMachine;

            CharacterMaster characterMaster = newMaster.GetComponent<CharacterMaster>();
            if (characterMaster == null)
            {
                characterMaster = newMaster.AddComponent<CharacterMaster>();
            }
            characterMaster.bodyPrefab = bodyPrefab;
            characterMaster.teamIndex = TeamIndex.Monster;

            Modules.Content.AddMasterPrefab(newMaster);
            return newMaster;
        }
        #endregion master

        public static void ClearEntityStateMachines(GameObject bodyPrefab)
        {
            EntityStateMachine[] machines = bodyPrefab.GetComponents<EntityStateMachine>();

            for (int i = machines.Length - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(machines[i]);
            }

            NetworkStateMachine networkMachine = bodyPrefab.GetComponent<NetworkStateMachine>();
            networkMachine.stateMachines = Array.Empty<EntityStateMachine>();

            CharacterDeathBehavior deathBehavior = bodyPrefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
            {
                deathBehavior.idleStateMachine = Array.Empty<EntityStateMachine>();
            }

            SetStateOnHurt setStateOnHurt = bodyPrefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt)
            {
                setStateOnHurt.idleStateMachine = Array.Empty<EntityStateMachine>();
            }
        }

        public static void AddMainEntityStateMachine(GameObject bodyPrefab, string machineName = "Body", Type mainStateType = null, Type initalStateType = null)
        {
            EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(bodyPrefab, machineName);
            if (entityStateMachine == null)
            {
                entityStateMachine = bodyPrefab.AddComponent<EntityStateMachine>();
            }
            else
            {
                Log.Message($"An Entity State Machine already exists with the name {machineName}. replacing.");
            }

            entityStateMachine.customName = machineName;

            if (mainStateType == null)
            {
                mainStateType = typeof(EntityStates.GenericCharacterMain);
            }
            entityStateMachine.mainStateType = new EntityStates.SerializableEntityStateType(mainStateType);

            if (initalStateType == null)
            {
                initalStateType = typeof(EntityStates.SpawnTeleporterState);
            }
            entityStateMachine.initialStateType = new EntityStates.SerializableEntityStateType(initalStateType);

            NetworkStateMachine networkMachine = bodyPrefab.GetComponent<NetworkStateMachine>();
            if (networkMachine)
            {
                networkMachine.stateMachines = networkMachine.stateMachines.Append(entityStateMachine).ToArray();
            }

            CharacterDeathBehavior deathBehavior = bodyPrefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
            {
                deathBehavior.deathStateMachine = entityStateMachine;
            }

            SetStateOnHurt setStateOnHurt = bodyPrefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt)
            {
                setStateOnHurt.targetStateMachine = entityStateMachine;
            }
        }

        //this but in reverse https://media.discordapp.net/attachments/875473107891150878/896193331720237106/caption-7.gif?ex=65989f94&is=65862a94&hm=e1f51da3ad190c00c5da1f90269d5ef10bedb0ae063c0f20aa0dd8721608018a&
        public static void AddEntityStateMachine(GameObject prefab, string machineName, Type mainStateType = null, Type initalStateType = null)
        {
            EntityStateMachine entityStateMachine = EntityStateMachine.FindByCustomName(prefab, machineName);
            if (entityStateMachine == null)
            {
                entityStateMachine = prefab.AddComponent<EntityStateMachine>();
            }
            else
            {
                Log.Message($"An Entity State Machine already exists with the name {machineName}. replacing.");
            }

            entityStateMachine.customName = machineName;

            if (mainStateType == null)
            {
                mainStateType = typeof(EntityStates.Idle);
            }
            entityStateMachine.mainStateType = new EntityStates.SerializableEntityStateType(mainStateType);

            if (initalStateType == null)
            {
                initalStateType = typeof(EntityStates.Idle);
            }
            entityStateMachine.initialStateType = new EntityStates.SerializableEntityStateType(initalStateType);

            NetworkStateMachine networkMachine = prefab.GetComponent<NetworkStateMachine>();
            if (networkMachine)
            {
                networkMachine.stateMachines = networkMachine.stateMachines.Append(entityStateMachine).ToArray();
            }

            CharacterDeathBehavior deathBehavior = prefab.GetComponent<CharacterDeathBehavior>();
            if (deathBehavior)
            {
                deathBehavior.idleStateMachine = deathBehavior.idleStateMachine.Append(entityStateMachine).ToArray();
            }

            SetStateOnHurt setStateOnHurt = prefab.GetComponent<SetStateOnHurt>();
            if (setStateOnHurt)
            {
                setStateOnHurt.idleStateMachine = setStateOnHurt.idleStateMachine.Append(entityStateMachine).ToArray();
            }
        }
        /// <summary>
        /// Sets up a hitboxgroup with passed in child transforms as hitboxes
        /// </summary>
        /// <param name="hitBoxGroupName">name that is used by melee or other overlapattacks</param>
        /// <param name="hitboxChildNames">childname of the transform set up in editor</param>
        public static void SetupHitBoxGroup(GameObject modelPrefab, string hitBoxGroupName, params string[] hitboxChildNames)
        {
            ChildLocator childLocator = modelPrefab.GetComponent<ChildLocator>();

            Transform[] hitboxTransforms = new Transform[hitboxChildNames.Length];
            for (int i = 0; i < hitboxChildNames.Length; i++)
            {
                hitboxTransforms[i] = childLocator.FindChild(hitboxChildNames[i]);

                if (hitboxTransforms[i] == null)
                {
                    Log.Error("missing hitbox for " + hitboxChildNames[i]);
                }
            }
            SetupHitBoxGroup(modelPrefab, hitBoxGroupName, hitboxTransforms);
        }
        /// <summary>
        /// Sets up a hitboxgroup with passed in transforms as hitboxes
        /// </summary>
        /// <param name="hitBoxGroupName">name that is used by melee or other overlapattacks</param>
        /// <param name="hitBoxTransforms">the transforms to be used in this hitboxgroup</param>
        public static void SetupHitBoxGroup(GameObject prefab, string hitBoxGroupName, params Transform[] hitBoxTransforms)
        {
            List<HitBox> hitBoxes = new List<HitBox>();

            foreach (Transform i in hitBoxTransforms)
            {
                if (i == null)
                {
                    Log.Error($"Error setting up hitboxGroup for {hitBoxGroupName}: hitbox transform was null");
                    continue;
                }
                HitBox hitBox = i.gameObject.AddComponent<HitBox>();
                i.gameObject.layer = LayerIndex.projectile.intVal;
                hitBoxes.Add(hitBox);
            }

            if (hitBoxes.Count == 0)
            {
                Log.Error($"No hitboxes were set up. aborting setting up hitboxGroup for {hitBoxGroupName}");
                return;
            }

            HitBoxGroup hitBoxGroup = prefab.AddComponent<HitBoxGroup>();

            hitBoxGroup.hitBoxes = hitBoxes.ToArray();

            hitBoxGroup.groupName = hitBoxGroupName;
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