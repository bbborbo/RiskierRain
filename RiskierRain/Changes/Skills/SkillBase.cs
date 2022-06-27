using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using EntityStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Skills
{
    public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
    {
        public static T instance { get; private set; }

        public SkillBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ArtificerExtended SkillBase was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class SkillBase
    {
        public static Dictionary<string, SkillLocator> characterSkillLocators = new Dictionary<string, SkillLocator>();
        public static string Token = RiskierRainPlugin.modName + "SKILL";
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }
        public abstract string SkillLangTokenName { get; }

        //public abstract string UnlockString { get; }
        public abstract UnlockableDef UnlockDef { get; }
        public abstract string IconName { get; }
        public abstract Type ActivationState { get; }
        public abstract string CharacterName { get; }
        public abstract SkillFamilyName SkillSlot { get; }
        public abstract SimpleSkillData SkillData { get; }
        public string[] KeywordTokens;
        public virtual bool useSteppedDef { get; set; } = false;

        string GetElementString(MageElement type)
        {
            string s = "";

            switch (type)
            {
                default:
                    s = "_MAGIC";
                    break;
                case MageElement.Fire:
                    s = "_FIRE";
                    break;
                case MageElement.Lightning:
                    s = "_LIGHTNING";
                    break;
                case MageElement.Ice:
                    s = "_ICE";
                    break;
            }

            return s;
        }

        public abstract void Init(ConfigFile config);

        protected void CreateLang()
        {
            LanguageAPI.Add(Token + SkillLangTokenName, SkillName);
            LanguageAPI.Add(Token + SkillLangTokenName + "_DESCRIPTION", SkillDescription);
        }

        protected void CreateSkill()
        {
            SkillLocator skillLocator;
            if (characterSkillLocators.ContainsKey(CharacterName))
            {
                skillLocator = characterSkillLocators[CharacterName];
            }
            else
            {
                GameObject body = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/" + CharacterName);
                skillLocator = body?.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    characterSkillLocators.Add(CharacterName, skillLocator);
                }
            }

            if(skillLocator != null)
            {
                SkillFamily skillFamily = null;

                switch (SkillSlot)
                {
                    case SkillFamilyName.Primary:
                        skillFamily = skillLocator.primary.skillFamily;
                        break;
                    case SkillFamilyName.Secondary:
                        skillFamily = skillLocator.secondary.skillFamily;
                        break;
                    case SkillFamilyName.Utility:
                        skillFamily = skillLocator.utility.skillFamily;
                        break;
                    case SkillFamilyName.Special:
                        skillFamily = skillLocator.special.skillFamily;
                        break;
                    case SkillFamilyName.Misc:
                        Debug.Log("Special case!");
                        break;
                }

                if (skillFamily != null)
                {
                    string s = $"SurvivorTweaks: {SkillName} initializing!";// to unlock {UnlockDef.cachedName}!";
                    //Debug.Log(s);

                    var skillDef = ScriptableObject.CreateInstance<SkillDef>();
                    if (useSteppedDef)
                    {
                        skillDef = ScriptableObject.CreateInstance<SteppedSkillDef>();
                    }

                    RegisterEntityState(ActivationState);
                    skillDef.activationState = new SerializableEntityStateType(ActivationState);

                    skillDef.skillNameToken = Token + SkillLangTokenName;
                    skillDef.skillName = SkillName;
                    skillDef.skillDescriptionToken = Token + SkillLangTokenName + "_DESCRIPTION";
                    skillDef.activationStateMachineName = "Weapon";

                    skillDef.keywordTokens = KeywordTokens;
                    if(RiskierRainPlugin.iconBundle != null)
                        skillDef.icon = RiskierRainPlugin.iconBundle.LoadAsset<Sprite>(RiskierRainPlugin.iconsPath + IconName + ".png");

                    #region SkillData
                    skillDef.baseMaxStock = SkillData.baseMaxStock;
                    skillDef.baseRechargeInterval = SkillData.baseRechargeInterval;
                    skillDef.beginSkillCooldownOnSkillEnd = SkillData.beginSkillCooldownOnSkillEnd;
                    skillDef.canceledFromSprinting = RiskierRainPlugin.autosprintLoaded ? false : SkillData.canceledFromSprinting;
                    skillDef.cancelSprintingOnActivation = SkillData.cancelSprintingOnActivation;
                    skillDef.dontAllowPastMaxStocks = SkillData.dontAllowPastMaxStocks;
                    skillDef.fullRestockOnAssign = SkillData.fullRestockOnAssign;
                    skillDef.interruptPriority = SkillData.interruptPriority;
                    skillDef.isCombatSkill = SkillData.isCombatSkill;
                    skillDef.mustKeyPress = SkillData.mustKeyPress;
                    skillDef.rechargeStock = SkillData.rechargeStock;
                    skillDef.requiredStock = SkillData.requiredStock;
                    skillDef.resetCooldownTimerOnUse = SkillData.resetCooldownTimerOnUse;
                    skillDef.stockToConsume = SkillData.stockToConsume;
                    #endregion


                    CoreModules.Assets.skillDefs.Add(skillDef);
                    Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
                    skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
                    {
                        skillDef = skillDef,
                        unlockableDef = UnlockDef,
                        viewableNode = new ViewablesCatalog.Node(skillDef.skillNameToken, false, null)
                    };
                }
                else
                {
                    Debug.Log($"No skill family {SkillSlot.ToString()} found from " + CharacterName);
                }
            }
            else
            {
                Debug.Log("No skill locator found from " + CharacterName);
            }
        }

        public abstract void Hooks();

        internal UnlockableDef GetUnlockDef(Type type)
        {
            UnlockableDef u = null;

            /*foreach (KeyValuePair<UnlockBase, UnlockableDef> keyValuePair in Main.UnlockBaseDictionary)
            {
                string key = keyValuePair.Key.ToString();
                UnlockableDef value = keyValuePair.Value;
                if (key == type.ToString())
                {
                    u = value;
                    //Debug.Log($"Found an Unlock ID Match {value} for {type.Name}! ");
                    break;
                }
            }*/

            return u;
        }
        public static bool RegisterEntityState(Type entityState)
        {
            //Check if the entity state has already been registered, is abstract, or is not a subclass of the base EntityState
            if (RiskierRainPlugin.entityStates.Contains(entityState) || !entityState.IsSubclassOf(typeof(EntityStates.EntityState)) || entityState.IsAbstract)
            {
                //LogCore.LogE(entityState.AssemblyQualifiedName + " is either abstract, not a subclass of an entity state, or has already been registered.");
                //LogCore.LogI("Is Abstract: " + entityState.IsAbstract + " Is not Subclass: " + !entityState.IsSubclassOf(typeof(EntityState)) + " Is already added: " + EntityStateDefinitions.Contains(entityState));
                return false;
            }
            //If not, add it to our EntityStateDefinitions
            RiskierRainPlugin.entityStates.Add(entityState);
            return true;
        }
    }
}
