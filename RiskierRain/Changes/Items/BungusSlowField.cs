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
using UnityEngine.AddressableAssets;
using RoR2.Projectile;

namespace RiskierRain.Items
{
    class Slungus : ItemBase
    {
        public static GameObject slungusSlowFieldPrefab;
        public static BuffDef slungusBuff;
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
            CreateSlowField();
            Hooks();
        }

        private void CreateSlowField()
        {
            GameObject railerSlowField = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerMineAltDetonated.prefab").WaitForCompletion();

            slungusSlowFieldPrefab = railerSlowField.InstantiateClone("SlungusSlowField");

            //this specifically handles slowing down of projectiles, NOT of characters
            SlowDownProjectiles slowDownProjectiles = slungusSlowFieldPrefab.GetComponent<SlowDownProjectiles>();

            //this adds the buff to characterbodies inside its radius for slowing down
            BuffWard buffWard = slungusSlowFieldPrefab.GetComponent<BuffWard>();
        }

        void CreateBuff()
        {
            slungusBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                slungusBuff.name = "slungusField";
                slungusBuff.buffColor = new Color(0.8f, 0.4f, 0f);
                slungusBuff.canStack = false;
                slungusBuff.isDebuff = false;
                slungusBuff.iconSprite = RiskierRainPlugin.mainAssetBundle.LoadAsset<Sprite>("Assets/Textures/Icons/Buff/texBuffCobaltShield.png");
            };
            Assets.buffDefs.Add(slungusBuff);
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
        public GameObject slungusFieldInstance;
        private void FixedUpdate()
        {
            if (NetworkServer.active)
            {
                float notMovingStopwatch = this.body.notMovingStopwatch;

                if (stack > 0 && notMovingStopwatch >= Slungus.slungusWaitTime)
                {
                    if (!body.HasBuff(Slungus.slungusBuff))
                    {
                        EnableSlungus();
                        return;
                    }
                }
                else if (body.HasBuff(Slungus.slungusBuff))
                {
                    DisableSlungus();
                }
            }
        }

        private void EnableSlungus()
        {
            this.body.AddBuff(Slungus.slungusBuff);
            if (!slungusFieldInstance)
            {
                slungusFieldInstance = Instantiate(Slungus.slungusSlowFieldPrefab);
            }
        }

        private void DisableSlungus()
        {
            body.RemoveBuff(Slungus.slungusBuff);
            if (slungusFieldInstance)
            {
                Destroy(slungusFieldInstance);
            }
        }

        private void OnDisable()
        {
            DisableSlungus();
        }
    }
}
