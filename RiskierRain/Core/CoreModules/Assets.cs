using EntityStates.ClayBoss;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static RiskierRain.CoreModules.StatHooks;
using System.Linq;

namespace RiskierRain.CoreModules
{
    public partial class Assets : CoreModule
    {
        #region AssetBundles
        public static string GetAssetBundlePath(string bundleName)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(RiskierRainContent.RiskierRainContent.PInfo.Location), bundleName);
        }

        private static AssetBundle _mainAssetBundle;
        public static AssetBundle mainAssetBundle
        {
            get
            {
                if (_mainAssetBundle == null)
                    _mainAssetBundle = AssetBundle.LoadFromFile(GetAssetBundlePath("itmightbebad"));
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
                    _orangeAssetBundle = AssetBundle.LoadFromFile(GetAssetBundlePath("orangecontent"));
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
        #endregion
        public static bool RegisterEntityState(Type entityState)
        {
            //Check if the entity state has already been registered, is abstract, or is not a subclass of the base EntityState
            if (entityStates.Contains(entityState) || !entityState.IsSubclassOf(typeof(EntityStates.EntityState)) || entityState.IsAbstract)
            {
                //LogCore.LogE(entityState.AssemblyQualifiedName + " is either abstract, not a subclass of an entity state, or has already been registered.");
                //LogCore.LogI("Is Abstract: " + entityState.IsAbstract + " Is not Subclass: " + !entityState.IsSubclassOf(typeof(EntityState)) + " Is already added: " + EntityStateDefinitions.Contains(entityState));
                return false;
            }
            //If not, add it to our EntityStateDefinitions
            entityStates.Add(entityState);
            return true;
        }
        public static EffectDef CreateEffect(GameObject effect)
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

            var def = new EffectDef
            {
                prefab = effect,
                prefabEffectComponent = effectComp,
                prefabVfxAttributes = vfxAttrib,
                prefabName = effect.name,
                spawnSoundEventName = effectComp.soundName
            };

            effectDefs.Add(def);
            return def;
        }

        public static List<ArtifactDef> artifactDefs = new List<ArtifactDef>();
        public static List<BuffDef> buffDefs = new List<BuffDef>();
        public static List<EffectDef> effectDefs = new List<EffectDef>();
        public static List<SkillFamily> skillFamilies = new List<SkillFamily>();
        public static List<SkillDef> skillDefs = new List<SkillDef>();
        public static List<GameObject> projectilePrefabs = new List<GameObject>();
        public static List<GameObject> networkedObjectPrefabs = new List<GameObject>();
        public static List<Type> entityStates = new List<Type>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();
        public static List<EliteDef> eliteDefs = new List<EliteDef>();

        public static List<GameObject> masterPrefabs = new List<GameObject>();
        public static List<GameObject> bodyPrefabs = new List<GameObject>();

        public static string executeKeywordToken = "DUCK_EXECUTION_KEYWORD";
        public static string shredKeywordToken = "DUCK_SHRED_KEYWORD";

        public override void Init()
        {
            CreateVoidtouchedSingularity();

            AddCaptainCooldownBuff();
            AddShatterspleenSpikeBuff();
            AddRazorwireCooldown();
            AddAspdPenaltyDebuff();
            AddVoidCradleCurse();
            AddJetpackSpeedBoost();
            AddShockDebuff();
            AddShockCooldown();
            AddCommanderRollBuff();

            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;
            On.EntityStates.BaseState.AddRecoil += OnAddRecoil;
            On.RoR2.CharacterBody.AddSpreadBloom += OnAddSpreadBloom;
        }

        private void OnAddSpreadBloom(On.RoR2.CharacterBody.orig_AddSpreadBloom orig, CharacterBody self, float value)
        {
            if (self.HasBuff(commandoRollBuff))
                return;
            orig(self, value);
        }

        private void OnAddRecoil(On.EntityStates.BaseState.orig_AddRecoil orig, EntityStates.BaseState self, float verticalMin, float verticalMax, float horizontalMin, float horizontalMax)
        {
            if (self.HasBuff(commandoRollBuff))
                return;
            orig(self, verticalMin, verticalMax, horizontalMin, horizontalMax);
        }

        public static BuffDef commandoRollBuff;
        private void AddCommanderRollBuff()
        {
            commandoRollBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                commandoRollBuff.buffColor = new Color(0.8f, 0.6f, 0.1f);
                commandoRollBuff.canStack = false;
                commandoRollBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();
                commandoRollBuff.isDebuff = false;
                commandoRollBuff.name = "CommandoDualieRoll";
            }
            Assets.buffDefs.Add(commandoRollBuff);
        }

        public static GameObject voidtouchedSingularityDelay;
        public static GameObject voidtouchedSingularity;
        private void CreateVoidtouchedSingularity()
        {
            float singularityRadius = 8; //15
            GameObject singularity = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/ElementalRingVoid/ElementalRingVoidBlackHole.prefab").WaitForCompletion();
            voidtouchedSingularity = singularity.InstantiateClone("VoidtouchedSingularity", true);

            ProjectileFuse singularityPf = voidtouchedSingularity.GetComponent<ProjectileFuse>();
            if (singularityPf)
            {
                singularityPf.fuse = 3;
            }
            RadialForce singularityRF = voidtouchedSingularity.GetComponent<RadialForce>();
            if (singularityRF)
            {
                singularityRF.radius = singularityRadius;
                voidtouchedSingularity.transform.localScale *= (singularityRadius / 15);
            }
            R2API.ContentAddition.AddProjectile(voidtouchedSingularity);

            GameObject willowispDelay = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ExplodeOnDeath/WilloWispDelay.prefab").WaitForCompletion();
            voidtouchedSingularityDelay = willowispDelay.InstantiateClone("VoidtouchedDelayBlast", true);

            DelayBlast singularityDelayDB = voidtouchedSingularityDelay.GetComponent<DelayBlast>();
            if (singularityDelayDB)
            {
                singularityDelayDB.explosionEffect = voidtouchedSingularity;
            }

            R2API.ContentAddition.AddNetworkedObject(voidtouchedSingularityDelay);
        }

        public static BuffDef jetpackSpeedBoost;
        private void AddJetpackSpeedBoost()
        {
            jetpackSpeedBoost = ScriptableObject.CreateInstance<BuffDef>();
            {
                jetpackSpeedBoost.buffColor = new Color(0.9f, 0.2f, 0.2f);
                jetpackSpeedBoost.canStack = false;
                jetpackSpeedBoost.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();
                jetpackSpeedBoost.isDebuff = false;
                jetpackSpeedBoost.name = "MageJetpackSpeedBoost";
            }
            Assets.buffDefs.Add(jetpackSpeedBoost);
        }

        public static BuffDef voidCradleCurse;
        private void AddVoidCradleCurse()
        {
            voidCradleCurse = ScriptableObject.CreateInstance<BuffDef>();
            {
                voidCradleCurse.buffColor = Color.black;
                voidCradleCurse.canStack = false;
                voidCradleCurse.isDebuff = false;
                voidCradleCurse.name = "VoidCradleCurse";
                voidCradleCurse.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffVoidFog.tif").WaitForCompletion();
            }
            buffDefs.Add(voidCradleCurse);
        }

        public static BuffDef shockMarker;
        public static int shockMarkerDuration = 4;
        void AddShockDebuff()
        {

            shockMarker = ScriptableObject.CreateInstance<BuffDef>();
            {

                shockMarker.name = "Shock";
                shockMarker.buffColor = new Color(0f, 0f, 0.6f);
                shockMarker.canStack = false;
                shockMarker.isDebuff = true;
                shockMarker.isHidden = true;
                shockMarker.iconSprite = null;// RiskierRainPlugin.mainAssetBundle.LoadAsset<Sprite>("RoR2/Base/ShockNearby/texBuffTeslaIcon.png");
            };
            buffDefs.Add(shockMarker);

        }
        //shockheal coolodwn
        public static BuffDef shockHealCooldown;
        void AddShockCooldown()
        {

            shockHealCooldown = ScriptableObject.CreateInstance<BuffDef>();
            {

                shockHealCooldown.name = "Shock";
                shockHealCooldown.buffColor = new Color(0f, 0f, 0.6f);
                shockHealCooldown.canStack = true;
                shockHealCooldown.isDebuff = false;
                shockHealCooldown.isCooldown = true;
                shockHealCooldown.iconSprite = null;// RiskierRainPlugin.mainAssetBundle.LoadAsset<Sprite>("RoR2/Base/ShockNearby/texBuffTeslaIcon.png");
            };
            buffDefs.Add(shockHealCooldown);

        }

        private void RecalcStats_Stats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(Assets.aspdPenaltyDebuff))
            {
                self.attackSpeed *= (1 - Assets.aspdPenaltyPercent);
            }
            if (self.HasBuff(captainCdrBuff))
            {
                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    float mult = (1 - captainCdrPercent);
                    ApplyCooldownScale(skillLocator.primary, mult);
                    ApplyCooldownScale(skillLocator.secondary, mult);
                    ApplyCooldownScale(skillLocator.utility, mult);
                    ApplyCooldownScale(skillLocator.special, mult);
                }
            }
        }

        public static BuffDef aspdPenaltyDebuff;
        public static float aspdPenaltyPercent = 0.20f;
        private void AddAspdPenaltyDebuff()
        {
            aspdPenaltyDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                aspdPenaltyDebuff.buffColor = Color.red;
                aspdPenaltyDebuff.canStack = false;
                aspdPenaltyDebuff.isDebuff = false;
                aspdPenaltyDebuff.name = "AttackSpeedPenalty";
                aspdPenaltyDebuff.iconSprite = null;// LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffSlow50Icon");
            }
            buffDefs.Add(aspdPenaltyDebuff);
        }


        public static BuffDef captainCdrBuff;
        public static float captainCdrPercent = 0.25f;

        private void AddCaptainCooldownBuff()
        {
            captainCdrBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                captainCdrBuff.buffColor = Color.yellow;
                captainCdrBuff.canStack = false;
                captainCdrBuff.isDebuff = false;
                captainCdrBuff.name = "CaptainBeaconCdr";
                captainCdrBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();
            }
            buffDefs.Add(captainCdrBuff);
        }

        public static float survivorExecuteThreshold = 0.15f;
        public static float banditExecutionThreshold = 0.1f;
        public static float harvestExecutionThreshold = 0.2f;

        public static BuffDef shatterspleenSpikeBuff;
        private void AddShatterspleenSpikeBuff()
        {
            shatterspleenSpikeBuff = ScriptableObject.CreateInstance<BuffDef>();

            shatterspleenSpikeBuff.buffColor = new Color(0.7f, 0.0f, 0.2f, 1);
            shatterspleenSpikeBuff.canStack = true;
            shatterspleenSpikeBuff.isDebuff = false;
            shatterspleenSpikeBuff.name = "ShatterspleenSpikeCharge";
            shatterspleenSpikeBuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();

            buffDefs.Add(shatterspleenSpikeBuff);
        }

        public static BuffDef noRazorwire;
        private void AddRazorwireCooldown()
        {
            noRazorwire = ScriptableObject.CreateInstance<BuffDef>();

            noRazorwire.buffColor = Color.black;
            noRazorwire.canStack = false;
            noRazorwire.isDebuff = true;
            noRazorwire.name = "NoRazorwire";
            noRazorwire.iconSprite = null;//LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffEntangleIcon");

            buffDefs.Add(noRazorwire);
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
    }
}
