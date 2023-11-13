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
            CreateMiredUrnTarball();

            AddShatterspleenSpikeBuff();
            AddRazorwireCooldown();
            AddTrophyHunterDebuffs();
            AddShredDebuff();
            AddCooldownBuff();
            AddAspdPenaltyDebuff();
            AddHopooDamageBuff();
            AddCombatTelescopeCritChance();
            AddVoidCradleCurse();
            AddJetpackSpeedBoost();
            AddShockDebuff();
            AddShockCooldown();
            AddPlanulaChargeBuff();
            AddMaskHauntAssets();

            On.RoR2.CharacterBody.RecalculateStats += RecalcStats_Stats;


            LanguageAPI.Add(shredKeywordToken, $"<style=cKeywordName>Shred</style>" +
                $"<style=cSub>Apply a stacking debuff that increases ALL damage taken by {shredArmorReduction}% per stack. Critical Strikes apply more Shred.</style>");
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
                hauntDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
                hauntDebuff.isDebuff = true;
                hauntDebuff.name = "HappiestMaskHauntDebuff";
            }
            Assets.buffDefs.Add(hauntDebuff);

            GameObject deathMarkVisualEffect = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/DeathMark/DeathMarkEffect.prefab").WaitForCompletion();
            hauntEffectPrefab = PrefabAPI.InstantiateClone(deathMarkVisualEffect, "HauntVisualEffect");
        }
        #endregion

        public static GameObject miredUrnTarball;
        private void CreateMiredUrnTarball()
        {
            miredUrnTarball = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ClayBoss/TarSeeker.prefab").WaitForCompletion().InstantiateClone("MiredUrnTarball", true);

            ProjectileImpactExplosion pie = miredUrnTarball.GetComponent<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.lifetime = 2;
            }

            R2API.ContentAddition.AddProjectile(miredUrnTarball);
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

        public static BuffDef planulaChargeBuff;
        private void AddPlanulaChargeBuff()
        {
            planulaChargeBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                planulaChargeBuff.buffColor = new Color(0.8f, 0.6f, 0.1f);
                planulaChargeBuff.canStack = true;
                planulaChargeBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
                planulaChargeBuff.isDebuff = false;
                planulaChargeBuff.name = "PlanulaChargeBuff";
            }
            Assets.buffDefs.Add(planulaChargeBuff);
        }

        public static BuffDef jetpackSpeedBoost;
        private void AddJetpackSpeedBoost()
        {
            jetpackSpeedBoost = ScriptableObject.CreateInstance<BuffDef>();
            {
                jetpackSpeedBoost.buffColor = new Color(0.9f, 0.2f, 0.2f);
                jetpackSpeedBoost.canStack = false;
                jetpackSpeedBoost.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
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
                voidCradleCurse.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffVoidFog");
            }
            buffDefs.Add(voidCradleCurse);
        }

        public static BuffDef hopooDamageBuff;
        public static BuffDef hopooDamageBuffTemporary;
        private void AddHopooDamageBuff()
        {
            hopooDamageBuff = CreateHopooDamageBuff("Permanent");
            hopooDamageBuffTemporary = CreateHopooDamageBuff("Temporary");
            buffDefs.Add(hopooDamageBuff);
            buffDefs.Add(hopooDamageBuffTemporary);
        }
        private BuffDef CreateHopooDamageBuff(string suffix)
        {
            BuffDef hopoo = ScriptableObject.CreateInstance<BuffDef>();
            {
                hopoo.buffColor = Color.cyan;
                hopoo.canStack = true;
                hopoo.isDebuff = false;
                hopoo.name = "HopooFeatherDamageBuff" + suffix;
                hopoo.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            return hopoo;
        }

        public static BuffDef banditShredDebuff;
        public static int shredArmorReduction = 15;
        private void AddShredDebuff()
        {
            banditShredDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                banditShredDebuff.buffColor = Color.red;
                banditShredDebuff.canStack = true;
                banditShredDebuff.isDebuff = true;
                banditShredDebuff.name = "BanditShredDebuff";
                banditShredDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffCrippleIcon");
            }
            buffDefs.Add(banditShredDebuff);
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
            int shredBuffCount = self.GetBuffCount(Assets.banditShredDebuff);
            if (shredBuffCount > 0)
            {
                self.armor -= shredBuffCount * shredArmorReduction;
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
                aspdPenaltyDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffSlow50Icon");
            }
            buffDefs.Add(aspdPenaltyDebuff);
        }


        public static BuffDef captainCdrBuff;
        public static float captainCdrPercent = 0.25f;

        private void AddCooldownBuff()
        {
            captainCdrBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                captainCdrBuff.buffColor = Color.yellow;
                captainCdrBuff.canStack = false;
                captainCdrBuff.isDebuff = false;
                captainCdrBuff.name = "CaptainBeaconCdr";
                captainCdrBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
            }
            buffDefs.Add(captainCdrBuff);
        }

        public static float survivorExecuteThreshold = 0.15f;
        public static float banditExecutionThreshold = 0.1f;
        public static float harvestExecutionThreshold = 0.2f;

        public static BuffDef bossHunterDebuff;
        public static BuffDef bossHunterDebuffWithScalpel;
        private void AddTrophyHunterDebuffs()
        {
            bossHunterDebuff = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuff.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuff.canStack = false;
            bossHunterDebuff.isDebuff = true;
            bossHunterDebuff.name = "TrophyHunterDebuff";
            bossHunterDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuff);

            bossHunterDebuffWithScalpel = ScriptableObject.CreateInstance<BuffDef>();

            bossHunterDebuffWithScalpel.buffColor = new Color(0.2f, 0.9f, 0.8f, 1);
            bossHunterDebuffWithScalpel.canStack = false;
            bossHunterDebuffWithScalpel.isDebuff = true;
            bossHunterDebuffWithScalpel.name = "TrophyHunterScalpelDebuff";
            bossHunterDebuffWithScalpel.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

            buffDefs.Add(bossHunterDebuffWithScalpel);
        }

        public static BuffDef shatterspleenSpikeBuff;
        private void AddShatterspleenSpikeBuff()
        {
            shatterspleenSpikeBuff = ScriptableObject.CreateInstance<BuffDef>();

            shatterspleenSpikeBuff.buffColor = new Color(0.7f, 0.0f, 0.2f, 1);
            shatterspleenSpikeBuff.canStack = true;
            shatterspleenSpikeBuff.isDebuff = false;
            shatterspleenSpikeBuff.name = "ShatterspleenSpikeCharge";
            shatterspleenSpikeBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffLunarDetonatorIcon");

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
            noRazorwire.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffEntangleIcon");

            buffDefs.Add(noRazorwire);
        }

        public static BuffDef combatTelescopeCritChance;
        private void AddCombatTelescopeCritChance()
        {
            combatTelescopeCritChance = ScriptableObject.CreateInstance<BuffDef>();

            combatTelescopeCritChance.buffColor = Color.red;
            combatTelescopeCritChance.canStack = false;
            combatTelescopeCritChance.isDebuff = false;
            combatTelescopeCritChance.name = "CombatTelescopeCrit";
            combatTelescopeCritChance.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffFullCritIcon");

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
