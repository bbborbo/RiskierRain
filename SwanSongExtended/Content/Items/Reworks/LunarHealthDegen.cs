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
using static R2API.RecalculateStatsAPI;
using SwanSongExtended.Modules;
using SwanSongExtended.Artifacts;

namespace SwanSongExtended.Items
{
    class LunarHealthDegen : ItemBase<LunarHealthDegen>
    {
        public override string ConfigName => "Reworks : Corpsebloom";
        public override AssetBundle assetBundle => null;

        public static BuffDef lunarLuckBuff;
        public static BuffDef lunarLuckBarrierCooldown;
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();


        public static int luckBase = 2;
        public static int luckStack = 2; //maybe 1?

        public static float healthRegenBase = -2;
        public static float healthRegenStack = -2;
        public static float healthRegenLevelBase = -0.3f;
        public static float healthRegenLevelStack = -0.3f;

        public static float damageBase = 4;
        public static float damageStack = 2;

        public override string ItemName => "Corpsebloom";

        public override string ItemLangTokenName => "LUNARHEALTHDEGEN";

        public override string ItemPickupDesc => "Your health degenerates over time. Gain barrier and luck at low health.";

        public override string ItemFullDescription => $"Gain Reduce base health regeneration by {healthRegenBase} hp/s (-{healthRegenStack} per stack). " +
            $"While below 60% max health, increase base damage by {damageBase} (+{damageStack} per stack). " +
            $"While below 30% max health, gain {Tools.ConvertDecimal(LunarHealthDegenBehavior.barrierFraction)} barrier " +
            $"and increase Luck by {luckBase} (+{luckStack} per stack).";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Cleansable, ItemTag.LowHealth, ItemTag.Utility, FreeLunarArtifact.FreeLunarBlacklist };

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
            GetStatCoefficients += ElegyStats;
        }

        private void ElegyStats(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);

            if (itemCount > 0)
            {
                float degenMod = 1;
                int stackMod = itemCount - 1;
                float levelMult = (1 + 0.2f * sender.level);
                int buffCount = sender.GetBuffCount(lunarLuckBuff.buffIndex);//LUCK/DAMAGE UP
                if (buffCount >= 4)
                {
                    //sender.damage += (damageBase + (damageLevel * (sender.level - 1)));
                    args.baseDamageAdd += (damageBase + damageStack * stackMod) * levelMult;
                    if (buffCount >= 7)
                    {
                        degenMod = 0.5f;
                    }
                }
                //sender.regen += (healthRegenBase + (healthRegenStack * (itemCount - 1))) * degenMod;//health degen
                args.baseRegenAdd += (healthRegenBase + healthRegenStack * stackMod) * degenMod * levelMult;//health degen
            }
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
            float missingHealthFraction = 1 - ((healthComponent.health + healthComponent.shield) / healthComponent.fullCombinedHealth);
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
