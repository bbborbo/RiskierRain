using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;
using System.Collections;
using SwanSongExtended.Modules;
using static SwanSongExtended.Modules.Language.Styling;
using On.RoR2.Items;

namespace SwanSongExtended.Items
{
    class Mocha2 : ItemBase<Mocha2>
    {
        #region config
        public override string ConfigName => "Reworks : Mocha";

        [AutoConfig("Seconds Of Boost On Stage Start", 60)]
        public static int stageDuration = 60;
        [AutoConfig("Seconds Of Boost On Item Pickup", 30)]
        public static int pickupDuration = 30;
        [AutoConfig("Seconds Of Boost On Interactable Use", 10)]
        public static int interactDuration = 10;

        [AutoConfig("Free Movement/Atk Speed Bonus", 0.05f)]
        public static float spdBoostFree = 0.05f;
        [AutoConfig("Buffed Movement/Atk Speed Bonus", 0.20f)]
        public static float spdBoostBuff = 0.20f;
        [AutoConfig("Free Cooldown Reduction Bonus", 0.00f)]
        public static float cdrBoostFree = 0.00f;
        [AutoConfig("Buffed Cooldown Reduction Bonus", 0.12f)]
        public static float cdrBoostBuff = 0.12f;
        #endregion

        public override AssetBundle assetBundle => null;
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();
        public static BuffDef mochaBuffActive;
        public static BuffDef mochaBuffInactive;
        public static Sprite mochaCustomSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.tif").WaitForCompletion();
        public override string ItemName => "Morning Mocha";

        public override string ItemLangTokenName => "LEGALLYDISTINCTCOFFEE";

        public override string ItemPickupDesc => "Gain a temporary speed boost after beginning a stage.";

        public override string ItemFullDescription => $"For <style=cIsUtility>{stageDuration}</style> seconds after entering any stage, " +
            $"increase {DamageColor("attack speed")} and {DamageColor("movement speed")} " +
            $"by {DamageColor(Tools.ConvertDecimal(spdBoostBuff))} {StackText($"+{Tools.ConvertDecimal(spdBoostBuff)}")}, " +
            $"and reduce {UtilityColor("skill cooldowns")} by " +
            $"{UtilityColor($"-{Tools.ConvertDecimal(cdrBoostBuff)}")} {StackText($"-{Tools.ConvertDecimal(cdrBoostBuff)}")}" +
            $"Using {UtilityColor("any interactable")} while this buff is active will extend the duration of the buff " +
            $"by {UtilityColor($"{interactDuration} seconds")}.";

        public override string ItemLore => "Order: To-Go Coffee Cup, 16 ounces" +
            "\r\nTracking Number: 32******" +
            "\\r\nEstimated Delivery: 05/04/2058" +
            "\r\nShipping Method:  Standard" +
            "\r\nShipping Address: Museum of Natural History, Ninten Island, Earth" +
            "\r\nShipping Details:" +
            "\nMy finest brew. Hope it doesn't spoil during transit. " +
            "Remember to heat it back up to 176.23 degrees... that's when it's freshest. " +
            "See you soon... Coo.";

        public override ItemTier Tier => ItemTier.Tier1;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/AttackSpeedAndMoveSpeed/PickupCoffee.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/AttackSpeedAndMoveSpeed/texCoffeeIcon.png").WaitForCompletion();
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public void GetDisplayRules()
        {
            CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.AttackSpeedAndMoveSpeed);
        }

        public override void Init()
        {
            mochaBuffActive = Content.CreateAndAddBuff(
                "bdCoffeeActive",
                mochaCustomSprite,
                new Color(0.6f, 0.3f, 0.1f),
                true, false
                );
            mochaBuffInactive = Content.CreateAndAddBuff(
                "bdCoffeeInctive",
                mochaCustomSprite,
                new Color(0.1f, 0.1f, 0.2f),
                false, false
                );
            base.Init();
        }
        public override void Hooks()
        {
            SwanSongPlugin.RetierItem(nameof(DLC1Content.Items.AttackSpeedAndMoveSpeed));
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += MochaExpiredBuff;
            On.RoR2.CharacterBody.RecalculateStats += MochaCDR;
            On.RoR2.Items.MultiShopCardUtils.OnPurchase += MochaExtend;
            GetStatCoefficients += MochaSpeed;
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.AttackSpeedAndMoveSpeed);
            RoR2Application.onLoad += YoinkMochaAssets;
        }

        private void MochaExtend(MultiShopCardUtils.orig_OnPurchase orig, CostTypeDef.PayCostContext context, int moneyCost)
        {
            orig(context, moneyCost);

            CharacterBody body = context.activatorBody;
            if (!body)
                return;

            int buffCount = body.GetBuffCount(mochaBuffActive);
            if (buffCount <= 0)
                return;

            float newBuffCount = Mathf.Min(buffCount + interactDuration, stageDuration - 1);
            for(int i = buffCount; i < newBuffCount; i++)
            {
                body.AddTimedBuffAuthority(mochaBuffActive.buffIndex, i + 1);
            }
        }

        private void YoinkMochaAssets()
        {
            if (CheckDLC1Entitlement())
            {
                Sprite mochaSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/AttackSpeedAndMoveSpeed/texCoffeeIcon.png").WaitForCompletion();
                instance.ItemsDef.pickupIconSprite = mochaSprite;
                GameObject mochaModel = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/AttackSpeedAndMoveSpeed/DisplayCoffee.prefab").WaitForCompletion();
                instance.ItemsDef.pickupModelPrefab = mochaModel;
            }
        }

        private void MochaExpiredBuff(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            if (buffDef == mochaBuffActive)
            {
                self.AddBuff(mochaBuffInactive);
            }
            orig(self, buffDef);
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                int itemCount = GetCount(self);
                    
                BorboMochaBehavior mochaBehavior = self.GetComponent<BorboMochaBehavior>(); 
                if (mochaBehavior && mochaBehavior.stack < itemCount)
                {
                    mochaBehavior.UpdateTime(pickupDuration);
                }
                self.AddItemBehavior<BorboMochaBehavior>(itemCount);
            }
        }

        private void MochaSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            //Debug.Log("dsfjhgbds");
            int mochaCount = GetCount(sender);
            float spdBuff = spdBoostFree;
            if (mochaCount > 0 && sender.HasBuff(mochaBuffActive))
            {
                spdBuff += spdBoostBuff;
            }
            args.moveSpeedMultAdd += spdBuff * mochaCount;
            args.attackSpeedMultAdd += spdBuff * mochaCount;
        }

        private void MochaCDR(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int mochaCount = GetCount(self);
            if (mochaCount > 0)
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = Mathf.Pow(1 - cdrBoostFree, mochaCount);
                if (self.HasBuff(mochaBuffActive))
                    cdrBoost *= Mathf.Pow(1 - cdrBoostBuff, mochaCount);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    Tools.ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.utility, cdrBoost);
                    Tools.ApplyCooldownScale(skillLocator.special, cdrBoost);
                }
            }
        }
    }

    public class BorboMochaBehavior : CharacterBody.ItemBehavior
    {
        bool addingBuffs = false;
        public int remainingTime = 0;
        float durationPerBuff = 1; //in seconds

        private void Start()
        {
            if(remainingTime < Mocha2.stageDuration)
                remainingTime = Mocha2.stageDuration;
            SetMochaTime(remainingTime);
        }
        
        public void UpdateTime(int newTime)
        {
            remainingTime = newTime;
            SetMochaTime(newTime);
        }

        private void SetMochaTime(int targetCount)
        {
            int startingBuffCount = 0;

            if (body.HasBuff(Mocha2.mochaBuffInactive))
                body.RemoveBuff(Mocha2.mochaBuffInactive);
            else
                startingBuffCount = body.GetBuffCount(Mocha2.mochaBuffActive);

            float endBuffCount = targetCount / durationPerBuff;
            if (endBuffCount > startingBuffCount)
            {
                for (int i = startingBuffCount; i < endBuffCount; i++)
                    AddMochaBuff((i + 1) * durationPerBuff);
            }
        }

        private void AddMochaBuff(float duration)
        {
            body.AddTimedBuff(Mocha2.mochaBuffActive, duration);
        }
    }
}
