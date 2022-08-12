using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace RiskierRain.Equipment
{
    // The directly below is entirely from TILER2 API (by ThinkInvis) specifically the Item module. Utilized to keep instance checking functionality as I migrate off TILER2.
    // TILER2 API can be found at the following places:
    // https://github.com/ThinkInvis/RoR2-TILER2
    // https://thunderstore.io/package/ThinkInvis/TILER2/

    public abstract class EquipmentBase<T> : EquipmentBase where T : EquipmentBase<T>
    {
        public static T instance { get; private set; }

        public EquipmentBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class EquipmentBase
    {
        public static Dictionary<string, EquipmentDef> DefDictionary = new Dictionary<string, EquipmentDef>();

        public abstract string EquipmentName { get; }
        public abstract string EquipmentLangTokenName { get; }
        public abstract string EquipmentPickupDesc { get; }
        public abstract string EquipmentFullDescription { get; }
        public abstract string EquipmentLore { get; }

        public abstract GameObject EquipmentModel { get; }
        public abstract Sprite EquipmentIcon { get; }

        public virtual bool AppearsInSinglePlayer { get; } = true;

        public virtual bool AppearsInMultiPlayer { get; } = true;
        public virtual BalanceCategory Category { get; set; } = BalanceCategory.None;
        public virtual HookType Type { get; set; } = HookType.None;

        public virtual bool CanDrop { get; } = true;

        public virtual float Cooldown { get; } = 60f;

        public virtual bool EnigmaCompatible { get; } = true;

        public virtual bool IsBoss { get; } = false;

        public virtual bool IsLunar { get; } = false;
        public virtual bool IsHidden { get; } = false;
        public virtual ColorCatalog.ColorIndex ColorIndex { get; } = ColorCatalog.ColorIndex.Equipment;

        public EquipmentDef EquipDef;
        public virtual string OptionalDefString { get; set; } = "";


        internal static void CloneVanillaDisplayRules(UnityEngine.Object newDef, UnityEngine.Object vanillaDef)
        {
            return;
            if (newDef != null)
            {
                foreach (GameObject bodyPrefab in BodyCatalog.bodyPrefabs)
                {
                    CharacterModel model = bodyPrefab.GetComponentInChildren<CharacterModel>();
                    if (model)
                    {
                        ItemDisplayRuleSet idrs = model.itemDisplayRuleSet;
                        if (idrs)
                        {
                            // clone the original item display rule

                            Array.Resize(ref idrs.keyAssetRuleGroups, idrs.keyAssetRuleGroups.Length + 1);
                            idrs.keyAssetRuleGroups[idrs.keyAssetRuleGroups.Length - 1].displayRuleGroup = idrs.FindDisplayRuleGroup(vanillaDef);
                            idrs.keyAssetRuleGroups[idrs.keyAssetRuleGroups.Length - 1].keyAsset = newDef;

                            idrs.GenerateRuntimeValues();
                        }
                    }
                }
            }
        }
        public abstract void Init(ConfigFile config);

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        protected void CreateLang()
        {
            LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_NAME", EquipmentName);
            LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_PICKUP", EquipmentPickupDesc);
            LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_DESCRIPTION", EquipmentFullDescription);
            LanguageAPI.Add("EQUIPMENT_" + EquipmentLangTokenName + "_LORE", EquipmentLore);
        }

        protected void CreateEquipment()
        {
            EquipDef = ScriptableObject.CreateInstance<EquipmentDef>();
            {
                EquipDef.name = "EQUIPMENT_" + EquipmentLangTokenName;
                EquipDef.nameToken = "EQUIPMENT_" + EquipmentLangTokenName + "_NAME";
                EquipDef.pickupToken = "EQUIPMENT_" + EquipmentLangTokenName + "_PICKUP";
                EquipDef.descriptionToken = "EQUIPMENT_" + EquipmentLangTokenName + "_DESCRIPTION";
                EquipDef.loreToken = "EQUIPMENT_" + EquipmentLangTokenName + "_LORE";
                EquipDef.pickupModelPrefab = EquipmentModel;
                EquipDef.pickupIconSprite = EquipmentIcon;
                EquipDef.appearsInSinglePlayer = AppearsInSinglePlayer;
                EquipDef.appearsInMultiPlayer = AppearsInMultiPlayer;
                EquipDef.canDrop = CanDrop;
                EquipDef.cooldown = Cooldown;
                EquipDef.enigmaCompatible = EnigmaCompatible;
                EquipDef.isBoss = IsBoss;
                EquipDef.isLunar = IsLunar;
                EquipDef.colorIndex = ColorIndex;
            }
            var itemDisplayRules = CreateItemDisplayRules();
            if (itemDisplayRules == null)
            {
                itemDisplayRules = new ItemDisplayRuleDict();
            }

            if(EquipmentLangTokenName == "GUILLOTINEEQUIPMENT")
            {
                EquipDef.unlockableDef = UnlockableCatalog.GetUnlockableDef("KillElitesMilestone");
            }

            ItemAPI.Add(new CustomEquipment(EquipDef, itemDisplayRules));
            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;

            if (OptionalDefString != "")
            {
                DefDictionary.Add(OptionalDefString, EquipDef);
            }
        }

        private bool PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (equipmentDef == EquipDef)
            {
                return ActivateEquipment(self);
            }
            else
            {
                return orig(self, equipmentDef);
            }
        }

        protected abstract bool ActivateEquipment(EquipmentSlot slot);

        public abstract void Hooks();


        #region Targeting
        public Ray GetAimRay(InputBankTest inputBank)
        {
            return new Ray
            {
                direction = inputBank.aimDirection,
                origin = inputBank.aimOrigin
            };
        }

        public Ray GetAimRay(CharacterBody body)
        {
            if (body.inputBank)
            {
                return new Ray(body.inputBank.aimOrigin, body.inputBank.aimDirection);
            }
            return new Ray(body.transform.position, body.transform.forward);
        }
        #endregion
    }
}
