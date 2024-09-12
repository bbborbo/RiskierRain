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
using RiskierRainContent.CoreModules;

namespace RiskierRainContent.Skills
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
        public static string Token = RiskierRainContent.modName + "SKILL";
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
        public SkillDef SkillDef;

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
            string s = $"SurvivorTweaks: {SkillName} initializing!";// to unlock {UnlockDef.cachedName}!";

            SkillDef = ScriptableObject.CreateInstance<SkillDef>();
            if (useSteppedDef)
            {
                SkillDef = ScriptableObject.CreateInstance<SteppedSkillDef>();
            }

            CoreModules.Assets.RegisterEntityState(ActivationState);
            SkillDef.activationState = new SerializableEntityStateType(ActivationState);

            SkillDef.skillNameToken = Token + SkillLangTokenName;
            SkillDef.skillName = SkillName;
            SkillDef.skillDescriptionToken = Token + SkillLangTokenName + "_DESCRIPTION";
            SkillDef.activationStateMachineName = "Weapon";

            SkillDef.keywordTokens = KeywordTokens;
            SkillDef.icon = CoreModules.Assets.mainAssetBundle.LoadAsset<Sprite>(CoreModules.Assets.iconsPath + "Skill/" + IconName + ".png");

            #region SkillData
            SkillDef.baseMaxStock = SkillData.baseMaxStock;
            SkillDef.baseRechargeInterval = SkillData.baseRechargeInterval;
            SkillDef.beginSkillCooldownOnSkillEnd = SkillData.beginSkillCooldownOnSkillEnd;
            SkillDef.canceledFromSprinting = RiskierRainContent.autosprintLoaded ? false : SkillData.canceledFromSprinting;
            SkillDef.cancelSprintingOnActivation = SkillData.cancelSprintingOnActivation;
            SkillDef.dontAllowPastMaxStocks = SkillData.dontAllowPastMaxStocks;
            SkillDef.fullRestockOnAssign = SkillData.fullRestockOnAssign;
            SkillDef.interruptPriority = SkillData.interruptPriority;
            SkillDef.isCombatSkill = SkillData.isCombatSkill;
            SkillDef.mustKeyPress = SkillData.mustKeyPress;
            SkillDef.rechargeStock = SkillData.rechargeStock;
            SkillDef.requiredStock = SkillData.requiredStock;
            SkillDef.resetCooldownTimerOnUse = SkillData.resetCooldownTimerOnUse;
            SkillDef.stockToConsume = SkillData.stockToConsume;
            #endregion

            CoreModules.Assets.skillDefs.Add(SkillDef);
            AddSkillDefToCharacter();
        }

        private void AddSkillDefToCharacter()
        {
            if (CharacterName == "")
                return;

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

            if (skillLocator != null)
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
                    //Debug.Log(s);

                    Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
                    skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
                    {
                        skillDef = SkillDef,
                        unlockableDef = UnlockDef,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
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
    }
}
