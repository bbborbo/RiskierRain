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
    public class Slungus : ItemBase<Slungus>
    {
        public static GameObject slungusSlowFieldPrefab;
        public static BuffDef slungusBuff;
        public static float slungusWaitTime = 1f;

        public static float radiusBase = 18f;
        public static float radiusStack = 4f;
        public float projectileSlowCoefficient = 0.2f; //0.1f

        public override string ItemName => "Slumbering Spores";

        public override string ItemLangTokenName => "SLUNGUS";

        public override string ItemPickupDesc => "Standing still slows nearby enemies and projectiles.";

        public override string ItemFullDescription => $"While stationary, create a " +
            $"<style=cIsUtility>stasis field</style> for {radiusBase}m " +
            $"<style=cStack>(+{radiusStack}m per stack)</style> around you, " +
            $"<style=cIsUtility>slowing</style> nearby " +
            //$"enemies by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style> " +
            //$"and projectiles by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style>.";
            $"enemes and projectiles by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style>.";

        public override string ItemLore =>
@"Order: Lay-Z Mushroom Travel Buddy
Tracking Number: 58***********
Estimated Delivery: 09/23/2056
Shipping Method:  Priority/Biological
Shipping Address:444 Slate Drive, Mars
Shipping Details:

Thank you for your purchase!

Directions:
Turn nozzle to ‘open’. Spores will disperse into the air, causing time to warp and pass slower, thus shortening your wait as the rest of reality will be experiencing time faster. It may take some time for the warping effects to occur; leave the spore bottle in one area to maximize spore count. If traveling in a small enclosed space, the spores may eventually fill the entire area. Ventilate regularly to prevent oversaturation.
To end time warp effect, close nozzle and leave or ventilate the affected area. Always close nozzle before leaving affected area; partial bodily exposure to time warpage may have unwanted effects.
Note- you may experience time normally for several seconds or minutes after the warping begins; this is normal. Simply wait for time to slow for you too (the slow-moving objects will resume their normal speed and unaffected objects will appear to speed up) and then happy waiting!

Warnings:
Objects appear to move slower, but carry the same force as they would normally. Do not interact with normally fast moving or forceful objects in an unsafe manner.
The affected area experienced lowered temperature; you may want to wear warm clothes or turn your heater up. Thermometers do not accurately measure temperature in affected area; assume practical temperatures to be up to 50 degrees lower than measured.
(bold text)Do not open bottle. Handle with extreme care. Ventilate regularly.(/bold)

FUN-GUYS Inc. is not liable for any illness, injury, death, extended or permanent change in time perception, spacial warping, mania or lethargy, hallucination, paranoia, acute panic attack, or otherwise dissatisfactory results. All purchases are final.";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist }; //, ItemTag.Damage

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

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
            slowDownProjectiles.slowDownCoefficient = projectileSlowCoefficient;

            //this adds the buff to characterbodies inside its radius for slowing down
            BuffWard buffWard = slungusSlowFieldPrefab.GetComponent<BuffWard>();
            //buffWard.buffDef = RoR2Content.Buffs.Slow60;
            buffWard.expires = false; //true
            buffWard.expireDuration = 10; //10
        }

        void CreateBuff()
        {
            slungusBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                slungusBuff.name = "slungusField";
                slungusBuff.buffColor = new Color(0.9f, 0.9f, 0f);
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
            if (body.hasAuthority)
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
        }

        private void UpdateSlungusRadius()
        {
            Debug.Log(radius);
            BuffWard buffWard = slungusFieldInstance.GetComponent<BuffWard>();
            if (buffWard)
            {
                buffWard.radius = radius;
                Debug.Log(buffWard.radius);
            }
            SphereCollider collider = slungusFieldInstance.GetComponent<SphereCollider>();
            if (collider)
            {
                collider.radius = radius;
                Debug.Log(collider.radius);
            }
        }

        private void DisableSlungus()
        {
            body.RemoveBuff(Slungus.slungusBuff);
            if (slungusFieldInstance)
            {
                Destroy(slungusFieldInstance, 0.5f);
            }
        }

        private void OnDisable()
        {
            DisableSlungus();
        }
    }
}
