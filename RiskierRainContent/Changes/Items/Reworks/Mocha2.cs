using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static RiskierRainContent.CoreModules.StatHooks;
using RoR2.EntitlementManagement;
using RoR2.ExpansionManagement;

namespace RiskierRainContent.Items
{
    class Mocha2 : ItemBase<Mocha2>
    {
        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();
        public static BuffDef mochaBuffActive;
        public static BuffDef mochaBuffInactive;
        public static Sprite mochaCustomSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texmovespeedbufficon"); //Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texMovespeedBuffIcon.png").WaitForCompletion(); //

        public static int stageDuration = 80;
        public static int pickupDuration = 20;

        public static float aspdBoostBase = 0.20f;
        public static float aspdBoostStack = 0.0f;
        public static float mspdBoostBase = 0.25f;
        public static float mspdBoostStack = 0.25f;
        public override string ItemName => "Morning Mocha";

        public override string ItemLangTokenName => "LEGALLYDISTINCTCOFFEE";

        public override string ItemPickupDesc => "Gain a temporary speed boost after beginning a stage.";

        public override string ItemFullDescription => $"For <style=cIsUtility>{stageDuration}</style> seconds after entering any stage, " +
            $"or <style=cIsUtility>{pickupDuration}</style> seconds after picking up a new copy of the item, increase " +
            $"<style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(aspdBoostBase)}</style> and " +
            $"<style=cIsDamage>movement speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(mspdBoostBase)}</style> " +
            $"<style=cStack>(+{Tools.ConvertDecimal(mspdBoostStack)} per stack)</style>, " +
            $"and reduce <style=cIsUtility>skill cooldowns</style> by <style=cIsUtility>{Tools.ConvertDecimal(aspdBoostBase)}</style>.";

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

        public static void GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.AttackSpeedAndMoveSpeed);
        }

        public override void Hooks()
        {
            RiskierRainContent.RetierItem(nameof(DLC1Content.Items.AttackSpeedAndMoveSpeed));
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += MochaExpiredBuff;
            On.RoR2.CharacterBody.RecalculateStats += MochaCDR;
            GetStatCoefficients += MochaSpeed;
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
            RoR2Application.onLoad += YoinkMochaAssets;
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
            int oldItemCount = GetCount(self);
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    int itemCount = GetCount(self);
                    BorboMochaBehavior mochaBehavior = self.AddItemBehavior<BorboMochaBehavior>(itemCount);
                    if (mochaBehavior && oldItemCount < itemCount)
                        mochaBehavior.SetMochaBuff(30);
                }
            }
        }

        private void MochaSpeed(CharacterBody sender, StatHookEventArgs args)
        {
            //Debug.Log("dsfjhgbds");
            int mochaCount = GetCount(sender);
            if (mochaCount > 0 && sender.HasBuff(mochaBuffActive))
            {
                args.moveSpeedMultAdd += mspdBoostBase + mspdBoostStack * (mochaCount - 1);

                float aspdBoost = aspdBoostBase + aspdBoostStack * (mochaCount - 1);
                args.attackSpeedMultAdd += aspdBoost;
            }
        }

        private void MochaCDR(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            int mochaCount = GetCount(self);
            if (mochaCount > 0 && self.HasBuff(mochaBuffActive))
            {
                //float cdrBoost = 1 / (1 + aspdBoostBase + aspdBoostStack * (mochaCount - 1));
                float cdrBoost = 1 - aspdBoostBase;
                if (mochaCount > 1)
                    cdrBoost *= Mathf.Pow(1 - aspdBoostStack, mochaCount - 1);

                SkillLocator skillLocator = self.skillLocator;
                if (skillLocator != null)
                {
                    ApplyCooldownScale(skillLocator.primary, cdrBoost);
                    ApplyCooldownScale(skillLocator.secondary, cdrBoost);
                    ApplyCooldownScale(skillLocator.utility, cdrBoost);
                    ApplyCooldownScale(skillLocator.special, cdrBoost);
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
            string baseName = "BorboMochaBoost";
            mochaBuffActive = ScriptableObject.CreateInstance<BuffDef>();
            {
                mochaBuffActive.name = baseName + "Active";
                mochaBuffActive.iconSprite = mochaCustomSprite;
                mochaBuffActive.buffColor = new Color(0.6f, 0.3f, 0.1f);
                mochaBuffActive.canStack = true;
                mochaBuffActive.isDebuff = false;
            }
            mochaBuffInactive = ScriptableObject.CreateInstance<BuffDef>();
            {
                mochaBuffInactive.name = baseName + "Active";
                mochaBuffInactive.iconSprite = mochaCustomSprite;
                mochaBuffInactive.buffColor = new Color(0.1f, 0.1f, 0.2f);
                mochaBuffInactive.canStack = false;
                mochaBuffInactive.isDebuff = false;
            };
            Assets.buffDefs.Add(mochaBuffActive);
            Assets.buffDefs.Add(mochaBuffInactive);
        }
    }

    public class BorboMochaBehavior : CharacterBody.ItemBehavior
    {
        float remainingTime;
        float durationPerBuff = 1; //in seconds

        private void Start()
        {
            remainingTime = Mocha2.stageDuration;
            SetMochaBuff(Mocha2.stageDuration);
        }

        public void SetMochaBuff(int targetCount)
        {
            int currentBuffCount = body.GetBuffCount(Mocha2.mochaBuffActive);
            float startingBuffCount = targetCount / durationPerBuff;
            if (startingBuffCount > currentBuffCount)
            {
                for (int i = currentBuffCount; i < startingBuffCount; i++)
                    AddMochaBuff((i + 1) * durationPerBuff);
            }
        }

        private void AddMochaBuff(float duration)
        {
            body.AddTimedBuff(Mocha2.mochaBuffActive, duration);
        }
    }
}
