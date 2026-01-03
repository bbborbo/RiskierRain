using BepInEx.Configuration;
using FruityElites.Modules;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace FruityElites.Equipment
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
        public override string TOKEN_PREFIX => "EQUIPMENT_";
        public abstract string EquipmentName { get; }
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
            LanguageAPI.Add(BASE_TOKEN + "_NAME", EquipmentName);
            LanguageAPI.Add(BASE_TOKEN + "_PICKUP", EquipmentPickupDesc);
            LanguageAPI.Add(BASE_TOKEN + "_DESCRIPTION", EquipmentFullDescription);
            LanguageAPI.Add(BASE_TOKEN + "_LORE", EquipmentLore);
        }

        protected void CreateEquipment()
        {
            EquipDef = ScriptableObject.CreateInstance<EquipmentDef>();
            {
                EquipDef.name = BASE_TOKEN;
                EquipDef.nameToken = BASE_TOKEN + "_NAME";
                EquipDef.pickupToken = BASE_TOKEN + "_PICKUP";
                EquipDef.descriptionToken = BASE_TOKEN + "_DESCRIPTION";
                EquipDef.loreToken = BASE_TOKEN + "_LORE";
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

            if (UseTargeting && TargetingIndicatorPrefabBase)
            {
                On.RoR2.EquipmentSlot.Update += UpdateTargeting;
            }
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


        #region Targeting Setup
        //Targeting Support
        public virtual bool UseTargeting { get; } = false;
        public GameObject TargetingIndicatorPrefabBase = null;
        public enum TargetingType
        {
            Enemies,
            Friendlies,
        }
        public virtual TargetingType TargetingTypeEnum { get; } = TargetingType.Enemies;

        //Based on MysticItem's targeting code.
        protected void UpdateTargeting(On.RoR2.EquipmentSlot.orig_Update orig, EquipmentSlot self)
        {
            orig(self);

            if (self.equipmentIndex == EquipDef.equipmentIndex)
            {
                var targetingComponent = self.GetComponent<TargetingControllerComponent>();
                if (!targetingComponent)
                {
                    targetingComponent = self.gameObject.AddComponent<TargetingControllerComponent>();
                    targetingComponent.VisualizerPrefab = TargetingIndicatorPrefabBase;
                }

                if (self.stock > 0)
                {
                    switch (TargetingTypeEnum)
                    {
                        case (TargetingType.Enemies):
                            targetingComponent.ConfigureTargetFinderForEnemies(self);
                            break;
                        case (TargetingType.Friendlies):
                            targetingComponent.ConfigureTargetFinderForFriendlies(self);
                            break;
                    }
                }
                else
                {
                    targetingComponent.Invalidate();
                    targetingComponent.Indicator.active = false;
                }
            }
        }

        public class TargetingControllerComponent : MonoBehaviour
        {
            public GameObject TargetObject;
            public GameObject VisualizerPrefab;
            public Indicator Indicator;
            public BullseyeSearch TargetFinder;
            public Action<BullseyeSearch> AdditionalBullseyeFunctionality = (search) => { };

            public void Awake()
            {
                Indicator = new Indicator(gameObject, null);
            }

            public void OnDestroy()
            {
                Invalidate();
            }

            public void Invalidate()
            {
                TargetObject = null;
                Indicator.targetTransform = null;
            }

            public void ConfigureTargetFinderBase(EquipmentSlot self)
            {
                if (TargetFinder == null) TargetFinder = new BullseyeSearch();
                TargetFinder.teamMaskFilter = TeamMask.allButNeutral;
                TargetFinder.teamMaskFilter.RemoveTeam(self.characterBody.teamComponent.teamIndex);
                TargetFinder.sortMode = BullseyeSearch.SortMode.Angle;
                TargetFinder.filterByLoS = true;
                float num;
                Ray ray = CameraRigController.ModifyAimRayIfApplicable(self.GetAimRay(), self.gameObject, out num);
                TargetFinder.searchOrigin = ray.origin;
                TargetFinder.searchDirection = ray.direction;
                TargetFinder.maxAngleFilter = 10f;
                TargetFinder.viewer = self.characterBody;
            }

            public void ConfigureTargetFinderForEnemies(EquipmentSlot self)
            {
                ConfigureTargetFinderBase(self);
                TargetFinder.teamMaskFilter = TeamMask.GetUnprotectedTeams(self.characterBody.teamComponent.teamIndex);
                TargetFinder.RefreshCandidates();
                TargetFinder.FilterOutGameObject(self.gameObject);
                AdditionalBullseyeFunctionality(TargetFinder);
                PlaceTargetingIndicator(TargetFinder.GetResults());
            }

            public void ConfigureTargetFinderForFriendlies(EquipmentSlot self)
            {
                ConfigureTargetFinderBase(self);
                TargetFinder.teamMaskFilter = TeamMask.none;
                TargetFinder.teamMaskFilter.AddTeam(self.characterBody.teamComponent.teamIndex);
                TargetFinder.RefreshCandidates();
                TargetFinder.FilterOutGameObject(self.gameObject);
                AdditionalBullseyeFunctionality(TargetFinder);
                PlaceTargetingIndicator(TargetFinder.GetResults());

            }

            public void PlaceTargetingIndicator(IEnumerable<HurtBox> TargetFinderResults)
            {
                HurtBox hurtbox = TargetFinderResults.Any() ? TargetFinderResults.First() : null;

                if (hurtbox)
                {
                    TargetObject = hurtbox.healthComponent.gameObject;
                    Indicator.visualizerPrefab = VisualizerPrefab;
                    Indicator.targetTransform = hurtbox.transform;
                }
                else
                {
                    Invalidate();
                }
                Indicator.active = hurtbox;
            }
        }

        #endregion Targeting Setup


        public static GameObject LoadDropPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if (EliteReworksPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = EliteReworksPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DropPrefabs/Item/{prefabName}.prefab");
            }

            if (prefab == null)
                prefab = Resources.Load<GameObject>("prefabs/NullModel");
            return prefab;
        }

        public static GameObject LoadDisplayPrefab(string prefabName = "")
        {
            GameObject prefab = null;
            if (EliteReworksPlugin.mainAssetBundle && prefabName != "")
            {
                prefab = EliteReworksPlugin.mainAssetBundle.LoadAsset<GameObject>($"Assets/Models/DisplayPrefabs/Item/{prefabName}.prefab"); ;
            }
            return prefab;
        }

        public static Sprite LoadItemIcon(string spriteName = "")
        {
            Sprite icon = null;
            if (EliteReworksPlugin.mainAssetBundle && spriteName != "")
            {
                icon = EliteReworksPlugin.mainAssetBundle.LoadAsset<Sprite>($"Assets/Textures/Icons/Item/{spriteName}.png");
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
