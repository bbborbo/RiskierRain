using BepInEx.Configuration;
using RiskierRain.CoreModules;
using R2API;
using RiskierRain.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.Items
{
    class Slungus : ItemBase
    {
        public static BuffDef slungusField;
        public static float slungusWaitTime = 1f;


        public override string ItemName => "Sleepy Fungus";

        public override string ItemLangTokenName => "SLUNGUS";

        public override string ItemPickupDesc => "Standing still generates a field that slows enemies and projectiles.";

        public override string ItemFullDescription => "slungus lol";

        public override string ItemLore => "the monsters see ur so lazy and become lethargic";

        public override ItemTier Tier => ItemTier.Tier2;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
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
            slungusField = ScriptableObject.CreateInstance<BuffDef>();
            {
                slungusField.name = "slungusField";
                slungusField.buffColor = new Color(0.8f, 0.4f, 0f);
                slungusField.canStack = false;
                slungusField.isDebuff = false;
                slungusField.iconSprite = RiskierRainPlugin.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(slungusField);
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                self.AddItemBehavior<SlungusItemBehavior>(GetCount(self));
            }
        }

    }

    public class SlungusItemBehavior : CharacterBody.ItemBehavior
    {
        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                float notMovingStopwatch = this.body.notMovingStopwatch;

                if (stack > 0 && notMovingStopwatch >= Slungus.slungusWaitTime)
                {
                    if (!body.HasBuff(Slungus.slungusField))
                    {
                        this.body.AddBuff(Slungus.slungusField);
                        return;
                    }
                }
                else if (body.HasBuff(Slungus.slungusField))
                {
                    body.RemoveBuff(Slungus.slungusField);
                }
            }
        }

        private void OnDisable()
        {
            this.body.RemoveBuff(Slungus.slungusField);
        }
    }
}
