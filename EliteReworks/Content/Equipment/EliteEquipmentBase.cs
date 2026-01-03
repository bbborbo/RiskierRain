using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static FruityElites.Modules.EliteModule;
using static RoR2.CombatDirector;
using RoR2.ExpansionManagement;
using FruityElites.Modules;
using FruityElites.Equipment;

namespace FruityElites.Elites
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

    public abstract class EliteEquipmentBase : EquipmentBase
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



        public override string TOKEN_PREFIX => "AFFIX_";
        public abstract string EliteModifier { get; }
        public abstract float EliteHealthModifier { get; }
        public abstract float EliteDamageModifier { get; }

        public override bool CanBeRandomlyActivated => false;
        public override bool EnigmaCompatible => false;

        public EquipmentDef EliteEquipmentDef => EquipDef;

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

        public override void Init()
        {
            base.Init();
            CreateEliteEquipment();
            CreateElite();
        }

        public override void Lang()
        {
            base.Lang();
            LanguageAPI.Add(BASE_TOKEN + "_MODIFIER", EliteModifier + " {0}");
        }

        protected virtual void CreateEliteEquipment()
        {
            //elite buff
            Sprite iconSprite = null;
            if (EliteBuffIcon != null)
            {
                iconSprite = Sprite.Create(EliteBuffIcon, new Rect(0.0f, 0.0f, EliteBuffIcon.width, EliteBuffIcon.height), new Vector2(0.5f, 0.5f));
            }
            EliteBuffDef = Content.CreateAndAddBuff("bd" + TOKEN_IDENTIFIER, iconSprite, EliteBuffColor, false, false);

            //elite def
            EliteDef = ScriptableObject.CreateInstance<EliteDef>();
            EliteDef.name = "ed" + TOKEN_IDENTIFIER;
            EliteDef.modifierToken = BASE_TOKEN + "_MODIFIER";
            EliteDef.color = EliteBuffColor;
            EliteDef.shaderEliteRampIndex = 0;
            Texture2D eliteRamp = CommonAssets.mainAssetBundle.LoadAsset<Texture2D>(CommonAssets.eliteMaterialsPath + EliteRampTextureName + ".png");
            if(eliteRamp != null)
            {
                EliteRamp.AddRamp(EliteDef, eliteRamp);
            }

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

            if (EliteOverlayMaterial)
            {
                On.RoR2.CharacterBody.FixedUpdate += OverlayManager;
            }
        }

        protected void CreateElite()
        {
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

        internal bool IsElite(CharacterBody body, BuffDef buffDef = null)
        {
            if(buffDef == null)
            {
                buffDef = EliteBuffDef;
            }

            return body.HasBuff(buffDef);
        }
    }
}
