using BepInEx.Configuration;
using EntityStates;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.States;
using RoR2;
using RoR2.CharacterAI;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Skills
{
    class PlaceFlamerTurret : SkillBase<PlaceFlamerTurret>
    {
        public static bool initialized = false;

        public static GameObject FlamerTurretMaster;
        public static GameObject FlamerTurretBody;
        public override string SkillName => "TR24 Dragon Turret";

        public override string SkillDescription => 
            $"Place a fragile turret that <style=cIsUtility>inherits all your items</style>. " +
            $"Throws a flame for DRINK THE FUCKEN GAS damage, exploding when destroyed for AND KILLETH damage. " +
            $"Can place up to 3.";

        public override string SkillLangTokenName => "PLACEFLAMERTURRET";

        public override UnlockableDef UnlockDef => null;

        public override string IconName => "";

        public override Type ActivationState => typeof(PlaceFlamer);

        public override string CharacterName => "ENGIBODY";

        public override SkillFamilyName SkillSlot => SkillFamilyName.Special;

        public override SimpleSkillData SkillData => new SimpleSkillData
        { 
            interruptPriority = InterruptPriority.Any,
            baseMaxStock = 3,
            stockToConsume = 0,
            baseRechargeInterval = 18,
            cancelSprintingOnActivation = true
        };

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateSkill();
            CreateLang();
            Hooks();

            initialized = true;
            if (FireTurretFlamer.initialized)
                CreateFlamerTurret();
        }

        public static void CreateFlamerTurret()
        {
            if (FlamerTurretBody != null && FlamerTurretMaster != null)
                return;
            if (!PlaceFlamerTurret.initialized || !FireTurretFlamer.initialized)
                return;

            Debug.LogError("Flamer turret initializing! This should probably be in a different class but whatever");
            //ill be real i basically stole this from chaotic skills 
            // body
            FlamerTurretBody = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiTurretBody.prefab").WaitForCompletion()
                .InstantiateClone("BorboFlamerTurretBody", true);

            CharacterBody body = FlamerTurretBody.GetComponent<CharacterBody>();
            body.baseMaxHealth *= 0.5f;
            body.levelMaxHealth = body.baseMaxHealth * 0.2f;


            SkillLocator locator = FlamerTurretBody.GetComponent<SkillLocator>();
            SkillFamily family = ScriptableObject.CreateInstance<SkillFamily>();
            (family as ScriptableObject).name = "TurretPrimary";
            family.variants = new SkillFamily.Variant[1];

            family.variants[0] = new SkillFamily.Variant
            {
                skillDef = FireTurretFlamer.instance.SkillDef
            };

            locator.primary._skillFamily = family;

            ModelLocator model = FlamerTurretBody.GetComponent<ModelLocator>();
            GameObject muzzleGlow = new GameObject("MuzzleGlow");
            Light light = muzzleGlow.AddComponent<Light>();
            light.range = 5f;
            light.color = Color.red;
            light.intensity = 5f;
            ChildLocator childLocator = model.modelTransform.gameObject.GetComponent<ChildLocator>();
            muzzleGlow.transform.parent = childLocator.FindChild("Muzzle");
            muzzleGlow.transform.position = childLocator.FindChild("Muzzle").position;

            // master
            FlamerTurretMaster = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiTurretMaster.prefab").WaitForCompletion()
                .InstantiateClone("BorboFlamerTurretMaster", true);

            foreach (AISkillDriver driver in FlamerTurretMaster.GetComponents<AISkillDriver>())
            {
                switch (driver.customName)
                {
                    case "FireAtEnemy":
                        driver.maxDistance = 25f;
                        driver.minDistance = 5f;
                        driver.activationRequiresTargetLoS = true;
                        driver.selectionRequiresTargetLoS = true;
                        driver.activationRequiresAimTargetLoS = true;
                        break;
                    default:
                        break;
                }
            }

            FlamerTurretMaster.GetComponent<CharacterMaster>().bodyPrefab = FlamerTurretBody;

            ContentAddition.AddBody(FlamerTurretBody);
            ContentAddition.AddMaster(FlamerTurretMaster);

            AllyCaps.RegisterAllyCap(FlamerTurretBody, 3, 4);
        }
    }
}
