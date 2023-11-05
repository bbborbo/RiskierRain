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
    class FrozenShell : ItemBase<FrozenShell>
    {
        internal static BuffDef frozenShellArmorBuff;
        internal static int freeArmor = 10;
        internal static int maxBonusArmor = 50; //(100 / 3)
        public static int maxBuffCount = 10;

        public override string ItemName => "Frozen Turtle Shell";

        public override string ItemLangTokenName => "FROZENSHELL";

        public override string ItemPickupDesc => "Reduce incoming damage while at low health.";

        public override string ItemFullDescription => $"<style=cIsHealing>Increase armor</style> by " +
            $"<style=cIsHealing>{freeArmor}</style> <style=cStack>(+{freeArmor} per stack)</style>. " +
            $"For every missing <style=cIsHealth>{Mathf.RoundToInt(100 / (float)maxBuffCount)}% of max health</style>, " +
            $"gain <style=cIsHealing>{Mathf.RoundToInt(maxBonusArmor / maxBuffCount)}</style> " +
            $"<style=cStack>(+{Mathf.RoundToInt(maxBonusArmor / maxBuffCount)} per stack)</style> additional armor.";//outdated fix later

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/frozenTurtleShell.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texFrozenShellIcon.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            GetStatCoefficients += this.GiveBonusArmor;
        }

        private void GiveBonusArmor(CharacterBody sender, StatHookEventArgs args)
        {
            int itemCount = GetCount(sender);
            if (itemCount > 0)
            {
                int buffCount = sender.GetBuffCount(frozenShellArmorBuff);
                float fraction = buffCount / maxBuffCount;
                int buffArmor = Mathf.RoundToInt(maxBonusArmor * fraction);
                args.armorAdd += itemCount * (freeArmor + buffArmor * buffCount);
            }
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.healthComponent != null)
                {
                    self.AddItemBehavior<FrozenShellBehavior>(GetCount(self));
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

        void CreateBuff()
        {
            frozenShellArmorBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                frozenShellArmorBuff.name = "IceBarrier";
	            frozenShellArmorBuff.buffColor = Color.cyan;
	            frozenShellArmorBuff.canStack = true;//false
	            frozenShellArmorBuff.isDebuff = false;
                frozenShellArmorBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffGenericShield");
            };
            Assets.buffDefs.Add(frozenShellArmorBuff);
        }
    }
    public class FrozenShellBehavior : CharacterBody.ItemBehavior
    {
        HealthComponent healthComponent;
        BuffIndex iceBarrierBuffIndex = FrozenShell.frozenShellArmorBuff.buffIndex;
        //bool hasBuff = false;
        //new version
        int buffCount = 0;

        private void Start()
        {
            healthComponent = body.healthComponent;
            //hasBuff = body.HasBuff(iceBarrierBuffIndex);
        }
        private void FixedUpdate()
        {
            float combinedHealthFraction = healthComponent.combinedHealthFraction;
            /*if (hasBuff)
            {
                if (combinedHealthFraction > 0.5f)
                {
                    this.body.RemoveBuff(iceBarrierBuffIndex);
                    hasBuff = false;
                }
            }
            else if (combinedHealthFraction <= 0.5f)
            {
                this.body.AddBuff(iceBarrierBuffIndex);
                hasBuff = true;
            }*/
            //new version
            float missingHealthFraction = (1 - combinedHealthFraction);
            int newBuffCount = Mathf.CeilToInt(missingHealthFraction * (FrozenShell.maxBuffCount));
            while (newBuffCount > buffCount && buffCount < FrozenShell.maxBuffCount)
            {
                this.body.AddBuff(iceBarrierBuffIndex);
                buffCount++;                
            }
            while (newBuffCount < buffCount && buffCount > 0)
            {
                this.body.RemoveBuff(iceBarrierBuffIndex);
                buffCount--;
            }
        }
        void OnDestroy()
        {
            //if(hasBuff)
                //this.body.RemoveBuff(iceBarrierBuffIndex);
            while (buffCount > 0)//this might crash the game lol
            {
                this.body.RemoveBuff(iceBarrierBuffIndex);
                buffCount--;
            }
        }
    }
}
