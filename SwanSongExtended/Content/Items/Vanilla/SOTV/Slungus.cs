using BepInEx.Configuration;
using R2API;
using SwanSongExtended.Items;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using RoR2.Projectile;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;

namespace SwanSongExtended.Items
{
    public class Slungus : ItemBase<Slungus>
    {
        public static GameObject slungusSlowFieldPrefab;
        public static BuffDef slungusBuff;
        public static float slungusWaitTime = 0.2f;
        public float movespeedIncreaseBase = 0.2f;
        public float movespeedIncreaseStack = 0.3f;

        public static float radiusBase = 24f;
        public static float radiusStack = 0f;
        public float projectileSlowCoefficient = 0.2f; //0.1f

        public override ExpansionDef RequiredExpansion => SotvExpansionDef();
        public override string ItemName => "Slumbering Spores";

        public override string ItemLangTokenName => "SLUNGUS";

        public override string ItemPickupDesc => "Standing still increases you damage while slowing nearby enemies and projectiles.";

        public override string ItemFullDescription => $"While stationary, create a " +
            $"<style=cIsUtility>stasis field</style> for {radiusBase}m around you, " +
            $"<style=cIsUtility>slowing</style> nearby " +
            //$"enemies by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style> " +
            //$"and projectiles by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style>.";
            $"enemes and projectiles by <style=cIsUtility>{Tools.ConvertDecimal(1 - projectileSlowCoefficient)}</style>. " +
            $"Deal {Tools.ConvertDecimal(movespeedIncreaseBase)} more damage " +
            $"<style=cStack>({Tools.ConvertDecimal(movespeedIncreaseStack)} per stack) until the next time you get hit.";

        public override string ItemLore =>
@"Order: Lay-Z Mushroom Travel Buddy
Tracking Number: 58***********
Estimated Delivery: 09/23/2056
Shipping Method:  Priority/Biological
Shipping Address: 444 Slate Drive, Mars
Shipping Details:

Thank you for your purchase!

Directions:
Turn nozzle to ‘open’. Spores will disperse into the air, causing time to warp and pass slower, thus shortening your wait as the rest of reality will be experiencing time faster. It may take some time for the warping effects to occur; leave the spore bottle in one area to maximize spore count. If traveling in a small enclosed space, the spores may eventually fill the entire area. Ventilate regularly to prevent oversaturation.
To end time warp effect, close nozzle and leave or ventilate the affected area. Always close nozzle before leaving affected area; partial bodily exposure to time warpage may have unwanted effects.
Note- you may experience time normally for several seconds or minutes after the warping begins; this is normal. Simply wait for time to slow for you too (the slow-moving objects will resume their normal speed and unaffected objects will appear to speed up) and then happy waiting!

Warnings:
Objects appear to move slower, but carry the same force as they would normally. Do not interact with normally fast moving or forceful objects in an unsafe manner.
The affected area experiences lowered temperature; you may want to wear warm clothes or turn your heater up. Thermometers do not accurately measure temperature in affected area; assume practical temperatures to be up to 50 degrees lower than measured.
<style=cIsHealth>Do not open bottle. Handle with extreme care. Ventilate regularly.</style>

FUN-GUYS Inc. is not liable for any illness, injury, death, extended or permanent change in time perception, spacial warping, mania or lethargy, hallucination, paranoia, acute panic attack, or otherwise dissatisfactory results. All purchases are final.";

        public override ItemTier Tier => ItemTier.Tier2;
        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.CannotCopy }; //, ItemTag.Damage

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlSlungus.prefab");

        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_SLUNGUS.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            slungusBuff = Content.CreateAndAddBuff(
                "bdSlungusActive",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(), //replace me
                new Color(0.9f, 0.9f, 0f),
                false, false
                );
            CreateSlowField();
            base.Init();
        }
        public override void Hooks()
        {
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            R2API.RecalculateStatsAPI.GetStatCoefficients += SlungusDamage;
            On.RoR2.HealthComponent.TakeDamageProcess += RemoveSlungusBuff;
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

        private void RemoveSlungusBuff(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (!damageInfo.rejected)
            {
                CharacterBody body = self.body;
                if (body.HasBuff(slungusBuff) && body.hasAuthority)
                {
                    body.RemoveBuff(slungusBuff);
                }
            }
            orig(self, damageInfo);
        }

        private void SlungusDamage(CharacterBody sender, RecalculateStatsAPI.StatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv && sender.HasBuff(slungusBuff))
            {
                int slungusCount = GetCount(sender);
                args.moveSpeedMultAdd += movespeedIncreaseBase + movespeedIncreaseStack * (slungusCount - 1);
            }
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
        public static float slungusBuffReapplicationTime = 1;
        float buffTimer = 0;
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
                    EnableSlungus();
                    return;
                }
                else
                {
                    DisableSlungus();
                }
            }
        }

        private void EnableSlungus()
        {
            if (!body.HasBuff(Slungus.slungusBuff))
            {
                buffTimer -= Time.fixedDeltaTime;
                if(buffTimer <= 0)
                {
                    buffTimer = slungusBuffReapplicationTime;
                    GrantSlungusBuff();
                }
            }
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
                if (!body.HasBuff(Slungus.slungusBuff))
                {
                    buffTimer = slungusBuffReapplicationTime;
                    GrantSlungusBuff();
                }
            }
        }

        private void GrantSlungusBuff()
        {
            if (body.hasAuthority)
            {
                body.AddBuff(Slungus.slungusBuff);
            }
        }

        private void UpdateSlungusRadius()
        {
            BuffWard buffWard = slungusFieldInstance.GetComponent<BuffWard>();
            if (buffWard)
            {
                buffWard.radius = radius;
            }
            SphereCollider collider = slungusFieldInstance.GetComponent<SphereCollider>();
            if (collider)
            {
                collider.radius = radius;
            }
        }

        private void DisableSlungus()
        {
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
