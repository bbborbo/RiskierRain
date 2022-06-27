using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;

using System.Security;
using System.Security.Permissions;
#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace RiskierRain.SurvivorTweaks
{
    public abstract class SurvivorTweakModule
    {
        public abstract string survivorName { get; }
        public abstract string bodyName { get; }

        public GameObject bodyObject;

        public SkillLocator skillLocator;
        public SkillFamily primary;
        public SkillFamily secondary;
        public SkillFamily utility;
        public SkillFamily special;

        public abstract void Init();

        public void GetBodyObject()
        {
            this.bodyObject = GetBodyObject(bodyName);
        }
        public static GameObject GetBodyObject(string name)
        {
            return LegacyResourcesAPI.Load<GameObject>($"prefabs/characterbodies/{name}");
        }
        public void GetSkillsFromBodyObject(GameObject bodyObject)
        {
            if(bodyObject != null)
            {
                skillLocator = bodyObject.GetComponent<SkillLocator>();
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
