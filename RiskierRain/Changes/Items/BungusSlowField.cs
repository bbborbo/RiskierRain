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

        public static float radiusBase = 10f;
        public static float radiusStack = 2f;

        public override string ItemName => "Slumbering Spores";

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
            buffWard.expires = false; //true
            buffWard.expireDuration = 10; //10
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
        public float radius
        {
            get
            {
                return Slungus.radiusBase + Slungus.radiusStack * (stack - 1);
            }
        } 
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
                    //UpdateSlungusRadius();
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
                slungusFieldInstance = Instantiate(Slungus.slungusSlowFieldPrefab, body.transform);

                TeamComponent teamFilter = body.GetComponent<TeamComponent>();
                TeamIndex teamIndex = teamFilter.teamIndex;
                slungusFieldInstance.GetComponent<TeamFilter>().teamIndex = teamIndex;

                ProjectileController projectileController = slungusFieldInstance.GetComponent<ProjectileController>();
                if (projectileController)
                {
                    projectileController.Networkowner = body.gameObject;
                }
                UpdateSlungusRadius();
                NetworkServer.Spawn(slungusFieldInstance);
            }
        }

        private void UpdateSlungusRadius()
        {
            Debug.Log(radius);
            BuffWard buffWard = gameObject.GetComponent<BuffWard>();
            if (buffWard)
            {
                buffWard.radius = radius;
            }
            SphereCollider collider = gameObject.GetComponent<SphereCollider>();
            if (collider)
            {
                collider.radius = radius;
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
