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
using static RiskierRainContent.CoreModules.StatHooks;
using System.Linq;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.CoreModules
{
    public partial class Assets : CoreModule
    {
        #region AssetBundles
        public static string GetAssetBundlePath(string bundleName)
        {
            return System.IO.Path.Combine(System.IO.Path.GetDirectoryName(RiskierRainContent.PInfo.Location), bundleName);
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
        public static List<ExpansionDef> expansionDefs = new List<ExpansionDef>();

        public static List<ItemDef> itemDefs = new List<ItemDef>();
        public static List<EquipmentDef> equipDefs = new List<EquipmentDef>();
        public static List<EliteDef> eliteDefs = new List<EliteDef>();

        public static List<GameObject> masterPrefabs = new List<GameObject>();
        public static List<GameObject> bodyPrefabs = new List<GameObject>();

        public static string executeKeywordToken = "DUCK_EXECUTION_KEYWORD";

        public override void Init()
        {
            CreateSquidBlasterBall();

            AddTrophyHunterDebuffs();
            AddCombatTelescopeCritChance();

            AddMaskHauntAssets();
            AddHarpoonAssets();
        }

        #region happiest mask
        public static BuffDef hauntDebuff;
        public static GameObject hauntEffectPrefab;
        private void AddMaskHauntAssets()
        {
            hauntDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                hauntDebuff.buffColor = new Color(0.9f, 0.7f, 1.0f);
                hauntDebuff.canStack = false;
                hauntDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion(); //replace me
                hauntDebuff.isDebuff = true;
                hauntDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
                hauntDebuff.name = "HappiestMaskHauntDebuff";
            }
            Assets.buffDefs.Add(hauntDebuff);

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            hauntEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HauntVisualEffect");
        }
        #endregion

        #region hunters harpoon
        public static BuffDef harpoonDebuff;
        public static GameObject harpoonEffectPrefab;
        private void AddHarpoonAssets()
        {
            harpoonDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                harpoonDebuff.buffColor = new Color(0.9f, 0.7f, 0.1f);
                harpoonDebuff.canStack = true;
                harpoonDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/MoveSpeedOnKill/texBuffKillMoveSpeed.tif").WaitForCompletion();
                harpoonDebuff.isDebuff = true;
                harpoonDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
                harpoonDebuff.name = "HarpoonTargetDebuff";
            }
            Assets.buffDefs.Add(harpoonDebuff);

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            harpoonEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HarpoonTargetVisualEffect");
        }
        #endregion

        public static GameObject squidBlasterBall;
        private void CreateSquidBlasterBall()
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
        private void AddTrophyHunterDebuffs()
        {
            bossHunterDebuffWithScalpel = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuffWithScalpel.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuffWithScalpel.canStack = false;
            bossHunterDebuffWithScalpel.isDebuff = true;
            bossHunterDebuffWithScalpel.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
            bossHunterDebuffWithScalpel.name = "TrophyHunterScalpelDebuff";
            bossHunterDebuffWithScalpel.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/LunarSkillReplacements/texBuffLunarDetonatorIcon.tif").WaitForCompletion();

            buffDefs.Add(bossHunterDebuffWithScalpel);
        }

        public static BuffDef combatTelescopeCritChance;
        private void AddCombatTelescopeCritChance()
        {
            combatTelescopeCritChance = ScriptableObject.CreateInstance<BuffDef>();

            combatTelescopeCritChance.buffColor = Color.red;
            combatTelescopeCritChance.canStack = false;
            combatTelescopeCritChance.isDebuff = false;
            combatTelescopeCritChance.name = "CombatTelescopeCrit";
            combatTelescopeCritChance.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/CritOnUse/texBuffFullCritIcon.tif").WaitForCompletion();

            buffDefs.Add(combatTelescopeCritChance);
        }



        private void AddExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );
            
            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = GetExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        static float GetExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    //bandit specials (finisher)
                    /*bool hasBanditExecutionBuff = body.HasBuff(desperadoExecutionDebuff) || body.HasBuff(lightsoutExecutionDebuff);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasBanditExecutionBuff);*/
                    
                    //rex harvest (finisher)
                    /*bool hasRexHarvestBuff = body.HasBuff(RoR2Content.Buffs.Fruiting);
                    newThreshold = ModifyExecutionThreshold(newThreshold, survivorExecuteThreshold, hasRexHarvestBuff);*/

                    /*bool hasHauntBuff = body.HasBuff(hauntDebuff);
                    newThreshold = ModifyExecutionThreshold(newThreshold, hauntExecutionThreshold, hasHauntBuff);*/

                    //guillotine
                    /*int executionBuffCount = body.GetBuffCount(executionDebuffIndex);
                    float threshold = newExecutionThresholdBase + newExecutionThresholdStack * executionBuffCount;
                    newThreshold = ModifyExecutionThreshold(newThreshold, threshold, executionBuffCount > 0);*/
                }
            }

            return newThreshold;
        }

        public static float ModifyExecutionThreshold(float currentThreshold, float newThreshold, bool condition)
        {
            if (condition)
            {
                return Mathf.Max(currentThreshold, newThreshold);
            }
            //else...
            return currentThreshold;
        }

        private HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(GetExecutionThreshold(values.cullFraction, self));

            return values;
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
