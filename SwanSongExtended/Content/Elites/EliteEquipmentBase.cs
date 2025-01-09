using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static SwanSongExtended.Modules.EliteModule;
using static RoR2.CombatDirector;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Elites
{
    public abstract class EliteEquipmentBase<T> : EliteEquipmentBase where T : EliteEquipmentBase<T>
    {
        public static T instance { get; private set; }

        public EliteEquipmentBase()
        {
            if (instance != null) throw new InvalidOperationException("Singleton class \"" + typeof(T).Name + "\" inheriting EquipmentBoilerplate/Equipment was instantiated twice");
            instance = this as T;
        }
    }

    public abstract class EliteEquipmentBase : SharedBase
    {
        public EliteTierDef[] VanillaTier1()
        {
            List<EliteTierDef> etd = new List<EliteTierDef>();

            foreach (CombatDirector.EliteTierDef tier in EliteAPI.VanillaEliteTiers)//EliteAPI.GetCombatDirectorEliteTiers())
            {
                if (tier.eliteTypes.Contains(RoR2Content.Elites.Fire))
                {
                    etd.Add(tier);
                }
            }

            return etd.ToArray();
        }
        public EliteTierDef[] VanillaTier2()
        {
            List<EliteTierDef> etd = new List<EliteTierDef>();
            

            foreach (CombatDirector.EliteTierDef tier in EliteAPI.VanillaEliteTiers)//EliteAPI.GetCombatDirectorEliteTiers())
            {
                EliteDef[] eliteTypes = new EliteDef[2] { RoR2Content.Elites.Poison, RoR2Content.Elites.Haunted };

                if (tier.eliteTypes.Contains(RoR2Content.Elites.Poison))
                {
                    etd.Add(tier);
                }
            }

            return etd.ToArray();
        }



        public abstract string EliteEquipmentName { get; }
        public abstract string EliteAffixToken { get; }
        public abstract string EliteEquipmentPickupDesc { get; }
        public abstract string EliteEquipmentFullDescription { get; }
        public abstract string EliteEquipmentLore { get; }
        public abstract string EliteModifier { get; }
        public abstract float EliteHealthModifier { get; }
        public abstract float EliteDamageModifier { get; }

        public virtual bool AppearsInSinglePlayer { get; } = true;

        public virtual bool AppearsInMultiPlayer { get; } = true;

        public virtual bool CanDrop { get; } = false;

        public virtual float Cooldown { get; } = 60f;

        public virtual bool EnigmaCompatible { get; } = false;

        public virtual bool IsBoss { get; } = false;

        public virtual bool IsLunar { get; } = false;

        public abstract GameObject EliteEquipmentModel { get; }
        public abstract Sprite EliteEquipmentIcon { get; }

        public EquipmentDef EliteEquipmentDef;

        /// <summary>
        /// Implement before calling CreateEliteEquipment.
        /// </summary>
        public BuffDef EliteBuffDef;

        public abstract Texture2D EliteBuffIcon { get; }

        public virtual Color EliteBuffColor { get; set; } = new Color32(255, 255, 255, byte.MaxValue);

        /// <summary>
        /// If not overriden, the elite cannot spawn in any defined tier. Use EliteTier for vanilla elites.
        /// </summary>
        public virtual EliteTiers EliteTier { get; set; } = EliteTiers.Other;

        /// <summary>
        /// For overlays only.
        /// </summary>
        public virtual Material EliteOverlayMaterial { get; set; } = null;
        public virtual string EliteRampTextureName { get; set; } = null;
        public virtual float DropOnDeathChance { get; set; } = 1/4000;

        public EliteDef EliteDef;
        public virtual ExpansionDef RequiredExpansion { get; } = null;

        public override void Init()
        {
            base.Init();
            CreateEliteEquipment();
            CreateElite();
        }

        public abstract ItemDisplayRuleDict CreateItemDisplayRules();

        public override void Lang()
        {
            LanguageAPI.Add("BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_NAME", EliteEquipmentName);
            LanguageAPI.Add("BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_PICKUP", EliteEquipmentPickupDesc);
            LanguageAPI.Add("BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_DESCRIPTION", EliteEquipmentFullDescription);
            LanguageAPI.Add("BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_LORE", EliteEquipmentLore);
            LanguageAPI.Add("BORBO_ELITE_" + EliteAffixToken + "_MODIFIER", EliteModifier + " {0}");

        }

        protected virtual void CreateEliteEquipment()
        {
            //elite buff
            Sprite iconSprite = null;
            if (EliteBuffIcon != null)
            {
                iconSprite = Sprite.Create(EliteBuffIcon, new Rect(0.0f, 0.0f, EliteBuffIcon.width, EliteBuffIcon.height), new Vector2(0.5f, 0.5f));
            }
            EliteBuffDef = Content.CreateAndAddBuff("bd" + EliteAffixToken, iconSprite, EliteBuffColor, false, false);

            //elite def
            EliteDef = ScriptableObject.CreateInstance<EliteDef>();
            EliteDef.name = "BORBO_ELITE_" + EliteAffixToken;
            EliteDef.modifierToken = "BORBO_ELITE_" + EliteAffixToken + "_MODIFIER";
            EliteDef.color = EliteBuffColor;
            EliteDef.shaderEliteRampIndex = 0;
            Texture2D eliteRamp = CommonAssets.mainAssetBundle.LoadAsset<Texture2D>(CommonAssets.eliteMaterialsPath + EliteRampTextureName + ".png");
            EliteRamp.AddRamp(EliteDef, eliteRamp);

            //elite equipment
            EliteEquipmentDef = ScriptableObject.CreateInstance<EquipmentDef>();
            EliteEquipmentDef.name = "BORBO_ELITE_EQUIPMENT_" + EliteAffixToken;
            EliteEquipmentDef.nameToken = "BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_NAME";
            EliteEquipmentDef.pickupToken = "BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_PICKUP";
            EliteEquipmentDef.descriptionToken = "BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_DESCRIPTION";
            EliteEquipmentDef.loreToken = "BORBO_ELITE_EQUIPMENT_" + EliteAffixToken + "_LORE";
            EliteEquipmentDef.pickupModelPrefab = EliteEquipmentModel;
            EliteEquipmentDef.pickupIconSprite = EliteEquipmentIcon;
            EliteEquipmentDef.appearsInSinglePlayer = AppearsInSinglePlayer;
            EliteEquipmentDef.appearsInMultiPlayer = AppearsInMultiPlayer;
            EliteEquipmentDef.canDrop = CanDrop;
            EliteEquipmentDef.cooldown = Cooldown;
            EliteEquipmentDef.enigmaCompatible = EnigmaCompatible;
            EliteEquipmentDef.isBoss = IsBoss;
            EliteEquipmentDef.isLunar = IsLunar;
            EliteEquipmentDef.dropOnDeathChance = DropOnDeathChance;
            EliteEquipmentDef.requiredExpansion = RequiredExpansion;

            //cross references
            EliteDef.eliteEquipmentDef = EliteEquipmentDef;
            EliteEquipmentDef.passiveBuffDef = EliteBuffDef;
            EliteBuffDef.eliteDef = EliteDef;
            
            ItemAPI.Add(new CustomEquipment(EliteEquipmentDef, CreateItemDisplayRules()));
            Content.AddEliteDef(EliteDef);
            //EliteAPI.Add(new CustomElite(EliteDef, CanAppearInEliteTiers));
            //Assets.equipDefs.Add(EliteEquipmentDef);
            CustomElite customElite = new CustomElite(EliteDef, new EliteTierDef[0]);


            #region BorboEliteDef
            CustomEliteDef BED = GetCustomElite();
            EliteModule.Elites.Add(BED);

            //CustomElite customElite = new CustomElite(EliteModifier, EliteEquipmentDef, EliteBuffColor, EliteAffixToken, EliteAPI.GetCombatDirectorEliteTiers());
            //R2API.EliteAPI.Add(customElite);
            #endregion

            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;

            if (UseTargeting && TargetingIndicatorPrefabBase)
            {
                On.RoR2.EquipmentSlot.Update += UpdateTargeting;
            }

            if (EliteOverlayMaterial)
            {
                On.RoR2.CharacterBody.FixedUpdate += OverlayManager;
            }
        }

        protected virtual CustomEliteDef GetCustomElite()
        {
            CustomEliteDef customElite = ScriptableObject.CreateInstance<CustomEliteDef>();
            customElite.eliteDef = EliteDef;
            customElite.eliteTier = EliteTier;
            customElite.eliteRamp = CommonAssets.mainAssetBundle.LoadAsset<Texture>(CommonAssets.eliteMaterialsPath + EliteRampTextureName + ".png");
            customElite.overlayMaterial = EliteOverlayMaterial;
            customElite.spawnEffect = null;
            return customElite;
        }

        private void OverlayManager(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            if (self.modelLocator && self.modelLocator.modelTransform && self.HasBuff(EliteBuffDef) && !self.GetComponent<EliteOverlayManager>())
            {
                RoR2.TemporaryOverlay overlay = self.modelLocator.modelTransform.gameObject.AddComponent<RoR2.TemporaryOverlay>();
                overlay.duration = float.PositiveInfinity;
                overlay.alphaCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
                overlay.animateShaderAlpha = true;
                overlay.destroyComponentOnEnd = true;
                overlay.originalMaterial = EliteOverlayMaterial;
                overlay.AddToCharacerModel(self.modelLocator.modelTransform.GetComponent<RoR2.CharacterModel>());
                var EliteOverlayManager = self.gameObject.AddComponent<EliteOverlayManager>();
                EliteOverlayManager.Overlay = overlay;
                EliteOverlayManager.Body = self;
                EliteOverlayManager.EliteBuffDef = EliteBuffDef;

                self.modelLocator.modelTransform.GetComponent<CharacterModel>().UpdateOverlays(); //<-- not updating this will cause model.myEliteIndex to not be accurate.
                self.RecalculateStats(); //<-- not updating recalcstats will cause isElite to be false IF it wasnt an elite before.
            }
            orig(self);
        }

        public class EliteOverlayManager : MonoBehaviour
        {
            public TemporaryOverlay Overlay;
            public CharacterBody Body;
            public BuffDef EliteBuffDef;

            public void FixedUpdate()
            {
                if (!Body.HasBuff(EliteBuffDef))
                {
                    UnityEngine.Object.Destroy(Overlay);
                    UnityEngine.Object.Destroy(this);
                }
            }
        }

        protected void CreateElite()
        {
        }

        internal bool IsElite(CharacterBody body, BuffDef buffDef = null)
        {
            if(buffDef == null)
            {
                buffDef = EliteBuffDef;
            }

            return body.HasBuff(buffDef);
        }

        protected bool PerformEquipmentAction(On.RoR2.EquipmentSlot.orig_PerformEquipmentAction orig, RoR2.EquipmentSlot self, EquipmentDef equipmentDef)
        {
            if (equipmentDef == EliteEquipmentDef)
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

            if (self.equipmentIndex == EliteEquipmentDef.equipmentIndex)
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
    }
}
