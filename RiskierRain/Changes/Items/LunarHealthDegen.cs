using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static BorboStatUtils.BorboStatUtils;

namespace RiskierRain.Changes.Items
{
    class LunarHealthDegen : ItemBase<LunarHealthDegen>
    {
        public static BuffDef lunarLuckBuff;
        public static BuffDef lunarLuckBarrierCooldown;
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();


        public static float luckBase = 1;
        public static float luckStack = 1; //maybe 1?

        public static float healthRegenBase = -2;
        public static float healthRegenStack = -2;
        public static float healthRegenLevelBase = -0.3f;
        public static float healthRegenLevelStack = -0.3f;

        public static float damageBase = 4;
        public static float damageLevel = 0.6f;

        public override string ItemName => "Elegy of Extinction";

        public override string ItemLangTokenName => "LUNARHEALTHDEGEN";

        public override string ItemPickupDesc => "Your health degenerates over time. Gain barrier and luck at low health.";

        public override string ItemFullDescription => "holy fucking bingle";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.LowHealth, ItemTag.Utility };

        public override BalanceCategory Category => BalanceCategory.StateOfHealth;

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/LunarPortalOnUse/PickupLunarPortalOnUse.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/LunarPortalOnUse/texLunarPortalOnUseIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public static void GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Equipment.LunarPortalOnUse);
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.RecalculateStats += AddBuffStats;
            On.RoR2.BodyCatalog.Init += GetDisplayRules; // i tink this doesnt work :s
            ModifyLuckStat += ElegyLuck;
        }

        private void ElegyLuck(CharacterBody sender, ref float luck)
        {
            if (sender.GetBuffCount(lunarLuckBuff.buffIndex) >= 7)
            {
                luck += luckBase + (luckStack * GetCount(sender));
            }
        }

        private void AddBuffStats(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int itemCount = GetCount(self);

            if (itemCount > 0)
            {
                float degenMod = 1;
                int buffCount = self.GetBuffCount(lunarLuckBuff.buffIndex);//LUCK/DAMAGE UP
                if (buffCount >= 4)
                {
                    self.damage += (damageBase + damageLevel * (self.level - 1));
                    if (buffCount >= 7)
                    {
                        degenMod = 0.5f;
                    }
                }
                self.regen += (healthRegenBase + healthRegenStack * (itemCount - 1)) * degenMod;//health degen
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
        public static int maxBuffCount = 10;
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
            while (newBuffCount > buffCount && buffCount < maxBuffCount)
            {
                this.body.AddBuff(luckUpBuffIndex);
                buffCount++;
                if (buffCount >= 7 &! body.HasBuff(barrierCooldownBuffIndex))
                {
                    healthComponent.AddBarrier(healthComponent.fullCombinedHealth * barrierFraction);
                    body.AddTimedBuff(barrierCooldownBuffIndex, barrierCoolDown);
                }
            }
            while (newBuffCount < buffCount && buffCount > 0)
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
