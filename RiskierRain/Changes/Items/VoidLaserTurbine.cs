using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.Skills;
using RiskierRain.States;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class VoidLaserTurbine : ItemBase<VoidLaserTurbine>
    {
        public static BuffDef turbineChargeBuff;
        public static BuffDef turbineReadyBuff;
        public static float secondsOfChargeRequired = 60;
        public override string ItemName => "Super Solenoid Engine"; //Super Solonoid Organ

        public override string ItemLangTokenName => "VOIDLASERTURBINE";

        public override string ItemPickupDesc => "Using skills charges a devastating laser primary attack. " +
            "<style=cIsVoid>Corrupts all Brilliant Behemoths and Resonance Discs.</style>";

        public override string ItemFullDescription => $"Using your skills builds charge. " +
            $"After {secondsOfChargeRequired} seconds worth of charge has accumulated, " +
            $"prime 1 use <style=cStack>(+1 per stack)</style> " +
            $"of <style=cIsVoid>{VoidLaserTurbineSkill._SkillName}</style>, replacing your Primary attack. " +
            $"Firing <style=cIsVoid>{VoidLaserTurbineSkill._SkillName}</style> " +
            $"deals <style=cIsDamage>{Tools.ConvertDecimal(VoidLaserBeam.damageCoefficient)} damage</style>, " +
            $"piercing ALL enemies and terrain. " +
            $"<style=cIsVoid>Corrupts all Brilliant Behemoths and Resonance Discs.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier3;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlS2Engine.prefab");

        public override Sprite ItemIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_VOIDLASERTURBINE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    VoidTurbineBehavior ringBehavior = self.AddItemBehavior<VoidTurbineBehavior>(GetCount(self));
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuffs();
            Hooks();
        }
        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation1 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.Behemoth, //consumes brilliant behemoth
                itemDef2 = VoidLaserTurbine.instance.ItemsDef
            };
            ItemDef.Pair transformation2 = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.LaserTurbine, //consumes resonance disc
                itemDef2 = VoidLaserTurbine.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] 
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation1);
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] 
                = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation2);
            orig();
        }

        private void CreateBuffs()
        {
            turbineChargeBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                turbineChargeBuff.name = "VoidTurbineChargeBuff";
                turbineChargeBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("texbuffelementalringsreadyicon");
                turbineChargeBuff.buffColor = new Color(0.5f, 0.0f, 0.4f);
                turbineChargeBuff.canStack = true;
                turbineChargeBuff.isDebuff = false;
            };
            Assets.buffDefs.Add(turbineChargeBuff);
            turbineReadyBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                turbineReadyBuff.name = "VoidTurbineReadyBuff";
                turbineReadyBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("texbuffelementalringsreadyicon");
                turbineReadyBuff.buffColor = new Color(0.9f, 0.2f, 0.8f);
                turbineReadyBuff.canStack = false;
                turbineReadyBuff.isDebuff = false;
            };
            Assets.buffDefs.Add(turbineReadyBuff);
        }
    }
    public class VoidTurbineBehavior : CharacterBody.ItemBehavior
    {
        GenericSkill primarySkill;
        GenericSkill overriddenSkill;
        SkillDef primaryOverride => VoidLaserTurbineSkill.instance.SkillDef;
        void Start()
        {
            SkillLocator skillLocator = body.skillLocator;
            primarySkill = skillLocator ? skillLocator.primary : null;

            if (primarySkill)
            {
                primarySkill.onSkillChanged += this.TryOverrideSkill;
            }
            body.onSkillActivatedServer += OnSkillActivated;
        }

        private void OnSkillActivated(GenericSkill skill)
        {
            if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
                return;

            if(skill.baseRechargeInterval > 0 && skill.rechargeStock > 0)
            {
                float effectiveCooldown = skill.baseRechargeInterval;
                if (skill.rechargeStock > 1)
                    effectiveCooldown /= skill.rechargeStock;

                int buffsToGrant = (int)Mathf.Floor(effectiveCooldown * (100 / VoidLaserTurbine.secondsOfChargeRequired));
                if(buffsToGrant > 0)
                {
                    for(int i = 0; i < buffsToGrant; i++)
                    {
                        body.AddBuff(VoidLaserTurbine.turbineChargeBuff);
                    }
                }
            }
        }

        void OnDestroy()
        {
            if (primarySkill)
            {
                if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
                    body.RemoveBuff(VoidLaserTurbine.turbineReadyBuff);

                primarySkill.onSkillChanged -= this.TryOverrideSkill;
                TryOverrideSkill(primarySkill);
            }
            body.onSkillActivatedServer -= OnSkillActivated;
        }
        void FixedUpdate()
        {
            if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
            {
                if(overriddenSkill != null && overriddenSkill.stock > 0)
                {
                    return;
                }
                UnreadyTurbineSkill();
            }
            int chargeCount = body.GetBuffCount(VoidLaserTurbine.turbineChargeBuff);
            if(chargeCount >= 100)
            {
                while (chargeCount > 0)
                {
                    body.RemoveBuff(VoidLaserTurbine.turbineChargeBuff);
                    chargeCount--;
                }
                ReadyTurbineSkill();
            }
        }

        private void UnreadyTurbineSkill()
        {
            body.RemoveBuff(VoidLaserTurbine.turbineReadyBuff);

            if (primarySkill)
            {
                this.TryOverrideSkill(primarySkill);
            }
        }

        private void ReadyTurbineSkill()
        {
            body.AddBuff(VoidLaserTurbine.turbineReadyBuff);

            if (primarySkill)
            {
                this.TryOverrideSkill(primarySkill);
            }
        }

        private void TryOverrideSkill(GenericSkill skill)
        {
            if (skill)
            {
                if (body.HasBuff(VoidLaserTurbine.turbineReadyBuff))
                {
                    if (this.overriddenSkill == null && !skill.HasSkillOverrideOfPriority(GenericSkill.SkillOverridePriority.Contextual))
                    {
                        this.overriddenSkill = skill;
                        this.overriddenSkill.SetSkillOverride(this, this.primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
                        this.overriddenSkill.maxStock = stack;
                        this.overriddenSkill.stock = stack;
                    }
                }
                else
                {
                    if (this.overriddenSkill)
                    {
                        overriddenSkill.UnsetSkillOverride(this, this.primaryOverride, GenericSkill.SkillOverridePriority.Contextual);
                        overriddenSkill = null;
                    }
                }
            }
        }
    }
}
