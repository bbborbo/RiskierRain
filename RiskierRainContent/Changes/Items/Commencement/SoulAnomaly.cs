using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class SoulAnomaly : ItemBase<SoulAnomaly>
    {
        public static BuffDef spiritBuff;
        public static float baseMoveSpeedAdd = 0.2f;
        public static float maxMoveMultAdd = 1.3f;
        public static float maxAttackMultAdd = 1.3f;
        public override string ItemName => "Relic of Soul";

        public override string ItemLangTokenName => "SOULANOMALY";

        public override string ItemPickupDesc => "Move and attack faster at low health.";

        public override string ItemFullDescription => $"Gain {Tools.ConvertDecimal(baseMoveSpeedAdd)} movement speed. " +
            $"For every missing <style=cIsHealth>{100 / (float)SoulAnomalyBehavior.maxBuffCount}% of max health</style>, " +
            $"increase movement speed by <style=cIsDamage>{Tools.ConvertDecimal(maxMoveMultAdd / SoulAnomalyBehavior.maxBuffCount)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(maxMoveMultAdd / SoulAnomalyBehavior.maxBuffCount)} per stack)</style> " +
            $"and attack speed by <style=cIsDamage>{Tools.ConvertDecimal(maxAttackMultAdd / SoulAnomalyBehavior.maxBuffCount)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(maxAttackMultAdd / SoulAnomalyBehavior.maxBuffCount)} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Boss;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override ItemTag[] ItemTags { get; } = new ItemTag[] { ItemTag.BrotherBlacklist, ItemTag.WorldUnique, ItemTag.CannotSteal };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetStatCoefficients += SpiritSpeedBoosts;
        }

        private void SpiritSpeedBoosts(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if(itemCount > 0)
            {
                float buffFraction = (float)sender.GetBuffCount(SoulAnomaly.spiritBuff) / (float)SoulAnomalyBehavior.maxBuffCount;
                //Debug.Log(buffFraction);

                args.attackSpeedMultAdd += buffFraction * maxAttackMultAdd * itemCount;
                args.moveSpeedMultAdd += (buffFraction * maxMoveMultAdd * itemCount) + baseMoveSpeedAdd;
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.healthComponent != null)
                {
                    self.AddItemBehavior<SoulAnomalyBehavior>(GetCount(self));
                }
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateBuff()
        {
            spiritBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                spiritBuff.name = "SpiritSpeedBuff";
                spiritBuff.buffColor = Color.cyan;
                spiritBuff.canStack = true;
                spiritBuff.isDebuff = false;
                spiritBuff.iconSprite = Resources.Load<Sprite>("textures/bufficons/texMovespeedBuffIcon");
            };
            Assets.buffDefs.Add(spiritBuff);
        }
    }
    public class SoulAnomalyBehavior : CharacterBody.ItemBehavior
    {
        HealthComponent healthComponent;
        BuffIndex buffIndex = SoulAnomaly.spiritBuff.buffIndex;
        public static int maxBuffCount = 10;
        int buffCount = 0;

        private void Start()
        {
            healthComponent = body.healthComponent;
            buffCount = body.GetBuffCount(buffIndex);
        }
        private void FixedUpdate()
        {
            float missingHealthFraction = 1 - (healthComponent.health + healthComponent.shield) / healthComponent.fullCombinedHealth;
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * maxBuffCount);
            if (newBuffCount > buffCount && buffCount < maxBuffCount)
            {
                this.body.AddBuff(buffIndex);
                buffCount++;
            }
            else if (newBuffCount < buffCount && buffCount > 0)
            {
                this.body.RemoveBuff(buffIndex);
                buffCount--;
            }
        }
    }
}
