using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Changes.Items
{
    class LunarHealthDegen : ItemBase<LunarHealthDegen>
    {
        public static BuffDef lunarLuckBuff;
        public static BuffDef lunarLuckBarrierCooldown;

        public static float luckBase = 1;
        public static float luckstack = 0; //maybe 1?

        public static float healthRegenBase = -5;
        public static float healthRegenStack = -5;

        public static float damageBase = 4;
        public static float damageLevel = 0.6f;

        public override string ItemName => "oofie ouchies";

        public override string ItemLangTokenName => "LUNARHEALTHDEGEN";

        public override string ItemPickupDesc => "Your health degenerates over time. Gain barrier and luck at low health.";

        public override string ItemFullDescription => "holy fucking bingle";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.LowHealth, ItemTag.Utility };

        public override BalanceCategory Category => BalanceCategory.StateOfHealth;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.RecalculateStats += AddBuffStats;
        }

        private void AddBuffStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int itemCount = GetCount(self);

            if (itemCount > 0)
            {
                self.regen += healthRegenBase + (healthRegenStack * itemCount - 1);//health degen

                int buffCount = self.GetBuffCount(lunarLuckBuff.buffIndex);//LUCK/DAMAGE UP
                if (buffCount >= 4)
                {
                    self.damage += (damageBase + damageLevel * (self.level - 1));
                    if (buffCount >= 7)
                    {
                        self.damage += (damageBase + damageLevel * (self.level - 1));//remove once luck works
                                                                                     //ADD LUCK
                    }
                }
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.healthComponent != null)
                {
                    self.AddItemBehavior<LunarHealthDegenBehavior>(GetCount(self));
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
            lunarLuckBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                lunarLuckBuff.name = "PlaceHolder";
                lunarLuckBuff.buffColor = Color.blue;
                lunarLuckBuff.canStack = true;
                lunarLuckBuff.isDebuff = false;
                lunarLuckBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
            };
            lunarLuckBarrierCooldown = ScriptableObject.CreateInstance<BuffDef>();
            {
                lunarLuckBarrierCooldown.name = "PlaceHolder2";
                lunarLuckBarrierCooldown.buffColor = Color.white;
                lunarLuckBarrierCooldown.canStack = false;
                lunarLuckBarrierCooldown.isDebuff = false;
                lunarLuckBarrierCooldown.isCooldown = true;
                lunarLuckBarrierCooldown.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
            };
            Assets.buffDefs.Add(lunarLuckBuff);
            Assets.buffDefs.Add(lunarLuckBarrierCooldown);
        }
    }

    public class LunarHealthDegenBehavior: CharacterBody.ItemBehavior
    {
        HealthComponent healthComponent;
        BuffIndex luckUpBuffIndex = LunarHealthDegen.lunarLuckBuff.buffIndex;
        BuffIndex barrierCooldownBuffIndex = LunarHealthDegen.lunarLuckBarrierCooldown.buffIndex;
        public static int maxBuffCount = 10;//2
        int buffCount = 0;

        public static float barrierFraction = 0.5f;
        public static float barrierCoolDown = 30;

        private void Start()
        {
            healthComponent = body.healthComponent;
            buffCount = body.GetBuffCount(luckUpBuffIndex);
        }

        private void FixedUpdate()
        {
            float missingHealthFraction = 1 - (healthComponent.health + healthComponent.shield) / healthComponent.fullCombinedHealth;
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * (maxBuffCount));
            if (newBuffCount > buffCount && buffCount < maxBuffCount)
            {
                this.body.AddBuff(luckUpBuffIndex);
                buffCount++;
                if (buffCount >= 7 &! body.HasBuff(barrierCooldownBuffIndex))
                {
                    healthComponent.AddBarrier(healthComponent.fullCombinedHealth * barrierFraction);
                    body.AddTimedBuff(barrierCooldownBuffIndex, barrierCoolDown);
                }
            }
            else if (newBuffCount < buffCount && buffCount > 0)
            {
                this.body.RemoveBuff(luckUpBuffIndex);
                buffCount--;
            }//FIX THIS ITS WEIRD
        }
        //void OnDestroy()
        //{
            //while (buffCount > 0)
                //this.body.RemoveBuff(luckUpBuffIndex);
        //}
    }
}
