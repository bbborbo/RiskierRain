using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static MoreStats.StatHooks;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    class LunarHealthDegen : ItemBase<LunarHealthDegen>
    {
        public override string ConfigName => "Reworks : Corpsebloom";
        public override AssetBundle assetBundle => null;

        public static BuffDef lunarLuckBuff;
        public static BuffDef lunarLuckBarrierCooldown;
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();


        public static int luckBase = 1;
        public static int luckStack = 1; //maybe 1?

        public static float healthRegenBase = -2;
        public static float healthRegenStack = -2;
        public static float healthRegenLevelBase = -0.3f;
        public static float healthRegenLevelStack = -0.3f;

        public static float damageBase = 4;
        public static float damageLevel = 0.6f;

        public override string ItemName => "Corpsebloom";

        public override string ItemLangTokenName => "LUNARHEALTHDEGEN";

        public override string ItemPickupDesc => "Your health degenerates over time. Gain barrier and luck at low health.";

        public override string ItemFullDescription => "holy fucking bingle";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.LowHealth, ItemTag.Utility };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/RepeatHeal/PickupCorpseflower.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/RepeatHeal/texCorpseflowerIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }
        public override void Init()
        {
            Log.Warning("Corpsebloom rework not fully implemented");
            base.Init();
            SwanSongPlugin.RetierItem(nameof(RoR2Content.Items.RepeatHeal));

            lunarLuckBuff = Content.CreateAndAddBuff(
                "bdLunarFlowerLuckBuff",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.blue,
                true, false
                );
            lunarLuckBarrierCooldown = Content.CreateAndAddBuff(
                "bdLunarFlowerBarrierCooldown",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.white,
                false, false
                );
            lunarLuckBarrierCooldown.isCooldown = true;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.RecalculateStats += AddBuffStats;

            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.RepeatHeal);
            GetMoreStatCoefficients += ElegyLuck;
        }

        private void ElegyLuck(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.GetBuffCount(lunarLuckBuff.buffIndex) >= 7)
            {
                args.luckAdd += luckBase + luckStack * (GetCount(sender) - 1);
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
                    self.damage += (damageBase + (damageLevel * (self.level - 1)));
                    if (buffCount >= 7)
                    {
                        degenMod = 0.5f;
                    }
                }
                self.regen += (healthRegenBase + (healthRegenStack * (itemCount - 1))) * degenMod;//health degen
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
