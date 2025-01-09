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
using SwanSongExtended.Modules;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Skills
{
    public abstract class SkillBase<T> : SkillBase where T : SkillBase<T>
    {
        public static T instance { get; private set; }

        public SkillBase()
        {
            if (instance != null) throw new InvalidOperationException(
                $"Singleton class \"{typeof(T).Name}\" inheriting {SwanSongPlugin.modName} {typeof(SkillBase).Name} was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class SkillBase : SharedBase
    {
        public static string Token = SwanSongPlugin.DEVELOPER_PREFIX + "SKILL";
        public abstract string SkillName { get; }
        public abstract string SkillDescription { get; }
        public abstract string SkillLangTokenName { get; }

        //public abstract string UnlockString { get; }
        public abstract UnlockableDef UnlockDef { get; }
        public abstract Sprite Icon { get; }
        public abstract Type ActivationState { get; }
        public abstract Type BaseSkillDef { get; }
        public abstract string CharacterName { get; }
        public abstract SkillSlot SkillSlot { get; }
        public abstract float BaseCooldown { get; }
        public abstract InterruptPriority InterruptPriority { get; }
        public abstract SimpleSkillData SkillData { get; }
        public string[] KeywordTokens;
        public virtual string ActivationStateMachineName { get; set; } = "Weapon";
        public SkillDef SkillDef { 
            get 
            {
                if (_SkillDef == null)
                    _SkillDef = (SkillDef)ScriptableObject.CreateInstance(BaseSkillDef);
                return _SkillDef;
            }
            set
            {
                _SkillDef = value;
            }
        }
        private SkillDef _SkillDef;

        public override void Init()
        {
            base.Init();
            CreateSkill();
            AddSkillToSkillFamily();
        }


        public override void Lang()
        {
            LanguageAPI.Add(Token + SkillLangTokenName, SkillName);
            LanguageAPI.Add(Token + SkillLangTokenName + "_DESCRIPTION", SkillDescription);
        }
        public Sprite LoadSpriteFromBundle(string name) { return assetBundle.LoadAsset<Sprite>(SwanSongPlugin.iconsPath + "Skill/" + name + ".png"); }
        public Sprite LoadSpriteFromRor(string path) { return Addressables.LoadAssetAsync<Sprite>(path).WaitForCompletion(); }
        public Sprite LoadSpriteFromRorSkill(string path) { return Addressables.LoadAssetAsync<SkillDef>(path).WaitForCompletion().icon; }
        private void CreateSkill()
        {
            if(SkillDef == null)
                SkillDef = (SkillDef)ScriptableObject.CreateInstance(BaseSkillDef);

            Content.AddEntityState(ActivationState);
            SkillDef.activationState = new SerializableEntityStateType(ActivationState);

            SkillDef.skillNameToken = Token + SkillLangTokenName;
            SkillDef.skillName = SkillName;
            SkillDef.skillDescriptionToken = Token + SkillLangTokenName + "_DESCRIPTION";
            SkillDef.activationStateMachineName = ActivationStateMachineName;

            SkillDef.keywordTokens = KeywordTokens;
            SkillDef.icon = Icon; // assetBundle.LoadAsset<Sprite>(FortunesPlugin.iconsPath + "Skill/" + IconName + ".png");

            #region SkillData
            SkillDef.baseRechargeInterval = Bind(BaseCooldown, "Base Cooldown");
            SkillDef.baseMaxStock = Bind(SkillData.baseMaxStock, "Base Max Stock");
            SkillDef.rechargeStock = Mathf.Min(Bind(SkillData.rechargeStock, "Recharge Stock"), SkillDef.baseMaxStock);
            SkillDef.interruptPriority = this.InterruptPriority;
            SkillDef.beginSkillCooldownOnSkillEnd = SkillData.beginSkillCooldownOnSkillEnd;
            SkillDef.dontAllowPastMaxStocks = SkillData.dontAllowPastMaxStocks;
            SkillDef.fullRestockOnAssign = SkillData.fullRestockOnAssign;
            SkillDef.isCombatSkill = Bind(SkillData.isCombatSkill, "Is Combat Skill");
            SkillDef.mustKeyPress = Bind(SkillData.mustKeyPress, "Must Key Press", "Setting to FALSE will allow the skill to be recast after it ends as long as the button is held.");
            SkillDef.requiredStock = SkillData.requiredStock;
            SkillDef.resetCooldownTimerOnUse = SkillData.resetCooldownTimerOnUse;
            SkillDef.stockToConsume = SkillData.stockToConsume;

            SkillDef.cancelSprintingOnActivation = Bind(SkillData.cancelSprintingOnActivation, "Cancels Sprinting", "Recommended to use HuntressBuffULTIMATE for intended behavior.");
            SkillDef.forceSprintDuringState = Bind(SkillData.forceSprintingDuringState, "Force Sprinting During State", "Used by mobility skills.");
            this.SkillDef.canceledFromSprinting = 
                !(SwanSongPlugin.autosprintLoaded || !SkillData.cancelSprintingOnActivation || SkillData.forceSprintingDuringState)
                && Bind(SkillData.canceledFromSprinting, "Canceled From Sprinting", 
                "Note: Only set to true if AUTOSPRINT isnt loaded, the skill cancels sprinting, and the skill doesn't force sprinting. " +
                "This avoids situations where the skill can cancel itself without additional input.");
            #endregion

            Content.AddSkillDef(SkillDef);
        }
        protected void AddSkillToSkillFamily()
        {
            //if the skill shouldnt initialize to a character
            if (/*SkillSlot != SkillSlot.None ||*/ string.IsNullOrEmpty(CharacterName))
                return;

            string s = Log.Combine("Skills", SkillName);
            SkillLocator skillLocator;
            string name = CharacterName;
            if (Modules.Skills.characterSkillLocators.ContainsKey(name))
            {
                skillLocator = Modules.Skills.characterSkillLocators[name];
            }
            else
            {
                GameObject body = LegacyResourcesAPI.Load<GameObject>("prefabs/characterbodies/" + name);
                skillLocator = body?.GetComponent<SkillLocator>();

                if (skillLocator)
                {
                    Modules.Skills.characterSkillLocators.Add(name, skillLocator);
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

            if (skillLocator != null)
            {
                SkillFamily skillFamily = null;

                //get skill family from skill slot
                switch (SkillSlot)
                {
                    case SkillSlot.Primary:
                        skillFamily = skillLocator.primary.skillFamily;
                        break;
                    case SkillSlot.Secondary:
                        skillFamily = skillLocator.secondary.skillFamily;
                        break;
                    case SkillSlot.Utility:
                        skillFamily = skillLocator.utility.skillFamily;
                        break;
                    case SkillSlot.Special:
                        skillFamily = skillLocator.special.skillFamily;
                        break;
                    case SkillSlot.None:
                        Log.Warning(s + "Special case!");
                        break;
                }

                if (skillFamily != null)
                {
                    Log.Debug(s + "initializing!");

                    Array.Resize(ref skillFamily.variants, skillFamily.variants.Length + 1);
                    skillFamily.variants[skillFamily.variants.Length - 1] = new SkillFamily.Variant
                    {
                        skillDef = SkillDef,
                        unlockableDef = UnlockDef,
                        viewableNode = new ViewablesCatalog.Node(SkillDef.skillNameToken, false, null)
                    };
                    Log.Debug(s + "success!");
                }
                else
                {
                    Log.Error(s + $"No skill family {SkillSlot.ToString()} found from " + CharacterName);
                }
            }
            else
            {
                Log.Error(s + "No skill locator found from " + CharacterName);
            }
        }

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
        public class SimpleSkillData
        {
            public SimpleSkillData(int baseMaxStock = 1, bool beginSkillCooldownOnSkillEnd = false,
                bool canceledFromSprinting = false, bool cancelSprintingOnActivation = true, bool forceSprintingDuringState = false,
                bool dontAllowPastMaxStocks = true, bool fullRestockOnAssign = true, 
                bool isCombatSkill = true, bool mustKeyPress = false, int rechargeStock = 1,
                int requiredStock = 1, bool resetCooldownTimerOnUse = false, int stockToConsume = 1)
            {
                this.baseMaxStock = baseMaxStock;
                this.beginSkillCooldownOnSkillEnd = beginSkillCooldownOnSkillEnd;
                this.canceledFromSprinting = canceledFromSprinting;
                this.cancelSprintingOnActivation = cancelSprintingOnActivation;
                this.forceSprintingDuringState = forceSprintingDuringState;
                this.dontAllowPastMaxStocks = dontAllowPastMaxStocks;
                this.fullRestockOnAssign = fullRestockOnAssign;
                this.isCombatSkill = isCombatSkill;
                this.mustKeyPress = mustKeyPress;
                this.rechargeStock = rechargeStock;
                this.requiredStock = requiredStock;
                this.resetCooldownTimerOnUse = resetCooldownTimerOnUse;
                this.stockToConsume = stockToConsume;
            }

            internal int baseMaxStock;
            internal bool beginSkillCooldownOnSkillEnd;
            internal bool canceledFromSprinting;
            internal bool cancelSprintingOnActivation;
            internal bool forceSprintingDuringState;
            internal bool dontAllowPastMaxStocks;
            internal bool fullRestockOnAssign;
            internal bool isCombatSkill;
            internal bool mustKeyPress;
            internal int rechargeStock;
            internal int requiredStock;
            internal bool resetCooldownTimerOnUse;
            internal int stockToConsume;
        }
    }
}
