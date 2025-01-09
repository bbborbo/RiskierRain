using BepInEx.Configuration;
using SwanSongExtended.Modules;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace SwanSongExtended.Equipment
{
    public abstract class EquipmentBase<T> : EquipmentBase where T : EquipmentBase<T>
    {
        public static T instance { get; private set; }

        public EquipmentBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting ItemBoilerplate/Item was instantiated twice");
            instance = this as T;
        }
    }
    public abstract class EquipmentBase : SharedBase
    {
        public abstract string EquipmentName { get; }
        public abstract string EquipmentLangTokenName { get; }
        public abstract string EquipmentPickupDesc { get; }
        public abstract string EquipmentFullDescription { get; }
        public abstract string EquipmentLore { get; }

        public abstract GameObject EquipmentModel { get; }
        public abstract Sprite EquipmentIcon { get; }

        public virtual bool AppearsInSinglePlayer { get; } = true;

        public virtual bool AppearsInMultiPlayer { get; } = true;

        public virtual bool CanDrop { get; } = true;

        public virtual bool IsBoss { get; } = false;

        public virtual bool IsLunar { get; } = false;
        public virtual ColorCatalog.ColorIndex ColorIndex { get; } = ColorCatalog.ColorIndex.Equipment;

        public EquipmentDef EquipDef;
        public virtual ExpansionDef RequiredExpansion { get; } = null;

        public abstract float BaseCooldown { get; }
        public abstract bool EnigmaCompatible { get; }
        public abstract bool CanBeRandomlyActivated { get; }

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

        public override void Init()
        {
            base.Init();
            CreateEquipment();
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        public override void Lang()
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
                EquipDef.cooldown = Bind(BaseCooldown, "Base Cooldown");
                EquipDef.enigmaCompatible = Bind(EnigmaCompatible, "Enigma-Compatible");
                EquipDef.canBeRandomlyTriggered = Bind(CanBeRandomlyActivated, "Bottled Chaos-Compatible");
                EquipDef.isBoss = IsBoss;
                EquipDef.isLunar = IsLunar;
                EquipDef.colorIndex = ColorIndex;
                EquipDef.requiredExpansion = RequiredExpansion;
            }
            var itemDisplayRules = CreateItemDisplayRules();
            if (itemDisplayRules == null)
            {
                itemDisplayRules = new ItemDisplayRuleDict();
            }

            ItemAPI.Add(new CustomEquipment(EquipDef, itemDisplayRules));
            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;
        }

        internal bool PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, EquipmentSlot self, EquipmentDef equipmentDef)
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


        public static GameObject LoadDropPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if (SwanSongPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = SwanSongPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DropPrefabs/Item/{prefabName}.prefab");
            }

            if (prefab == null)
                prefab = Resources.Load<GameObject>("prefabs/NullModel");
            return prefab;
        }

        public static GameObject LoadDisplayPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if (SwanSongPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = SwanSongPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DisplayPrefabs/Item/{prefabName}.prefab"); ;
            }
            return prefab;
        }

        public static Sprite LoadItemIcon(string spriteName = "")
        {
            Sprite icon = null;
            if (SwanSongPlugin.mainAssetBundle && spriteName != "")
            {
                icon = SwanSongPlugin.mainAssetBundle.LoadAsset<Sprite>($"Assets/Textures/Icons/Item/{spriteName}.png");
            }

            if (icon == null)
                icon = Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
            return icon;
        }
        public static ExpansionDef SotvExpansionDef()
        {
            return Addressables.LoadAssetAsync<ExpansionDef>("RoR2/DLC1/Common/DLC1.asset").WaitForCompletion();
        }
    }
}
