using RoR2;
using RoR2.ContentManagement;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SurvivorTweaks.SurvivorTweaks
{
    public abstract class SurvivorTweakBase<T> : SurvivorTweakBase where T : SurvivorTweakBase<T>
    {
        public static T instance { get; private set; }

        public SurvivorTweakBase()
        {
            if (instance != null) throw new InvalidOperationException(
                $"Singleton class \"{typeof(T).Name}\" inheriting {SurvivorTweaksPlugin.modName} {typeof(SurvivorTweakBase).Name} was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class SurvivorTweakBase : SharedBase
    {
        public override string ConfigName => "Survivor Tweaks : " + survivorName;
        public override AssetBundle assetBundle => SurvivorTweaksPlugin.mainAssetBundle;
        public override string TOKEN_PREFIX => "";
        public override string TOKEN_IDENTIFIER => "";
        public abstract string survivorName { get; }
        public abstract string bodyName { get; }

        public GameObject bodyObject;

        public SkillLocator skillLocator;
        public SkillFamily primary;
        public SkillFamily secondary;
        public SkillFamily utility;
        public SkillFamily special;

        public override void Init()
        {
            base.Init();
        }
        public override void Hooks()
        {
            
        }
        public override void Lang()
        {
            
        }

        public void GetBodyObject()
        {
            Debug.LogWarning($"FruitySurvivorTweaks: Using GetBodyObject for {bodyName}.");
            this.bodyObject = GetBodyObject(bodyName);
        }
        public static GameObject GetBodyObject(string name)
        {
            return LegacyResourcesAPI.Load<GameObject>($"prefabs/characterbodies/{name}");
        }
        public void GetSkillsFromBodyObject(GameObject bodyObject)
        {
            if (Modules.Skills.characterSkillLocators.ContainsKey(bodyName))
            {
                skillLocator = Modules.Skills.characterSkillLocators[bodyName];
            }
            else
            {
                skillLocator = bodyObject.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(bodyName, skillLocator);
                }
                /*
                GameObject body = null;// RalseiSurvivor.instance.bodyPrefab;
                skillLocator = body?.GetComponent<SkillLocator>();
                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(name, skillLocator);
                }

                LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/" + name);
                skillLocator = body?.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(name, skillLocator);
                }*/
            }

            if (bodyObject != null)
            {
                if (skillLocator)
                {
                    primary = skillLocator.primary.skillFamily;
                    secondary = skillLocator.secondary.skillFamily;
                    utility = skillLocator.utility.skillFamily;
                    special = skillLocator.special.skillFamily;
                }
                else
                {
                    Debug.Log($"Skill locator from body {bodyName} is null!");
                }
            }
            else
            {
                Debug.Log($"Body object from name {bodyName} is null!");
            }
        }
    }
}
