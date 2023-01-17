using BepInEx.Configuration;
using RiskierRain.CoreModules;
using HG;
using R2API;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Items
{
    class VortexLunar : ItemBase<VortexLunar>
    {
        public static GameObject vortexWormholeProjectile;
        public static BuffDef vortexCooldownDebuff;
        public static float baseCooldown = 7;
        public static float stackCooldown = -1;
        public static float minDamageCoefficient = 3;

        public static float damageCoefficient = 0.5f;
        public static float procCoefficient = 1;
        public static float baseRadius = 15;
        public static float stackRadius = 5;

        public override string ItemName => "Ascended Vase";

        public override string ItemLangTokenName => "LUNARMEATHOOK";

        public override string ItemPickupDesc => "High damage hits pull in ALL nearby enemies AND allies. Recharges over time.";

        public override string ItemFullDescription => $"On hits that deal <style=cIsDamage>more than {Tools.ConvertDecimal(minDamageCoefficient)} damage</style>, " +
            $"create an otherworldly vortex that <style=cIsHealth>pulls in nearby enemies AND allies</style> " +
            $"within {baseRadius}m <style=cStack>(+{stackRadius} per stack)</style>. " +
            $"Recharges every <style=cIsUtility>{baseCooldown}</style> seconds <style=cStack>(-{0 - stackCooldown} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Lunar;
        public override BalanceCategory Category => BalanceCategory.StateOfInteraction;

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Gateway/PickupVase.prefab").WaitForCompletion();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Gateway/texVaseIcon.png").WaitForCompletion();

        public override ItemTag[] ItemTags => new ItemTag[] { };

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += AscendedVortexOnHit;
        }

        private void AscendedVortexOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, RoR2.GlobalEventManager self, RoR2.DamageInfo damageInfo, GameObject victim)
        {
            if(!damageInfo.rejected && damageInfo.procCoefficient != 0 && damageInfo.attacker != null)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody victimBody = victim.GetComponent<CharacterBody>();

                if(attackerBody != null && !damageInfo.procChainMask.HasProc(ProcType.BounceNearby) && !attackerBody.HasBuff(vortexCooldownDebuff))
                {
                    int vortexItemCount = GetCount(attackerBody);
                    float damageCoefficient2 = damageInfo.damage / (attackerBody.damage * minDamageCoefficient);
                    if (vortexItemCount > 0 && damageCoefficient2 >= 1)
                    {
                        float vortexRadius = baseRadius + stackRadius * (vortexItemCount - 1);
                        float vortexDamage = attackerBody.damage * damageCoefficient;
                        float vortexCooldown = Mathf.Max(0.5f, baseCooldown + stackCooldown * (vortexItemCount - 1));
                        attackerBody.AddTimedBuffAuthority(vortexCooldownDebuff.buffIndex, vortexCooldown);

                        ProjectileManager.instance.FireProjectile(vortexWormholeProjectile, 
                            damageInfo.position, Util.QuaternionSafeLookRotation(Vector3.zero), 
                            attackerBody.gameObject, 0f, 0f, false, DamageColorIndex.Default, null, 0);

                        EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova"), new EffectData
                        {
                            origin = damageInfo.position,
                            scale = vortexRadius
                        }, true);

                        #region zomby
                        /*
                        #region hurtbox search
                        List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
                        BullseyeSearch search = new BullseyeSearch();
                        List<HealthComponent> list2 = CollectionPool<HealthComponent, List<HealthComponent>>.RentCollection();

                        // whitelist victim and attacker
                        if (true)
                        {
                            if (attackerBody.healthComponent && false)
                            {
                                list2.Add(attackerBody.healthComponent);
                            }
                            if (victimBody && victimBody.healthComponent)
                            {
                                list2.Add(victimBody.healthComponent);
                            }
                        }

                        BounceOrb.SearchForTargets(search, TeamIndex.None, damageInfo.position, vortexRadius, 100, list, list2);
                        CollectionPool<HealthComponent, List<HealthComponent>>.ReturnCollection(list2);
                        List<HealthComponent> bouncedObjects = new List<HealthComponent>
                                {
                                    victim.GetComponent<HealthComponent>()
                                };
                        #endregion

                        
                        EffectManager.SpawnEffect(Fuse.fuseNovaEffectPrefab, new EffectData
                        {
                            origin = damageInfo.position,
                            scale = vortexRadius
                        }, true);

                        for (int i = 0; i < list.Count; i++)
                        {
                            HurtBox hurtBox3 = list[i];
                            if (hurtBox3)
                            {
                                /*BounceOrb bounceOrb = new BounceOrb();
                                bounceOrb.origin = damageInfo.position;
                                bounceOrb.damageValue = vortexDamage;
                                bounceOrb.isCrit = damageInfo.crit;
                                bounceOrb.teamIndex = TeamIndex.Neutral;
                                bounceOrb.attacker = damageInfo.attacker;
                                bounceOrb.procChainMask = damageInfo.procChainMask;
                                bounceOrb.procChainMask.AddProc(ProcType.BounceNearby);
                                bounceOrb.procCoefficient = procCoefficient;
                                bounceOrb.damageColorIndex = DamageColorIndex.Item;
                                bounceOrb.bouncedObjects = bouncedObjects;
                                bounceOrb.target = hurtBox3;
                                OrbManager.instance.AddOrb(bounceOrb);

                                HealthComponent healthComponent = hurtBox3.healthComponent;
                                if (healthComponent)
                                {
                                    float forceMultiplier = 1;
                                    if(healthComponent.body.characterMotor != null)
                                    {
                                        forceMultiplier = healthComponent.body.characterMotor.mass;
                                    }
                                    else if(healthComponent.body.rigidbody != null)
                                    {
                                        forceMultiplier = healthComponent.body.rigidbody.mass;
                                    }

                                    Vector3 position = hurtBox3.transform.position;
                                    GameObject gameObject = healthComponent.gameObject;
                                    DamageInfo di = new DamageInfo()
                                    {
                                        damage = vortexDamage,
                                        attacker = damageInfo.attacker,
                                        inflictor = null,
                                        force = (position - damageInfo.position).normalized * -15f * forceMultiplier * Mathf.Min(damageCoefficient2, 3),
                                        crit = damageInfo.crit,
                                        procChainMask = damageInfo.procChainMask,
                                        procCoefficient = procCoefficient,
                                        position = position,
                                        damageColorIndex = DamageColorIndex.Item
                                    };
                                    healthComponent.TakeDamage(di);
                                    GlobalEventManager.instance.OnHitEnemy(di, gameObject);
                                    GlobalEventManager.instance.OnHitAll(di, gameObject);
                                }
                            }
                        }

                        //return the collection, not sure what this does
                        CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
                        */
                        #endregion
                    }
                }
            }
            orig(self, damageInfo, victim);
        }

        public override void Init(ConfigFile config)
        {
            return;
            CreateProjectile();
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }

        private void CreateProjectile()
        {
            vortexWormholeProjectile = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/GravSphere").InstantiateClone("VortexWormholeProjectile", true);
            VortexWormholeBehavior wormholeBehavior = vortexWormholeProjectile.AddComponent<VortexWormholeBehavior>();
            wormholeBehavior.projectileController = vortexWormholeProjectile.GetComponent<ProjectileController>();
            wormholeBehavior.projectileSimple = vortexWormholeProjectile.GetComponent<ProjectileSimple>();
            wormholeBehavior.teamFilter = vortexWormholeProjectile.GetComponent<TeamFilter>();
            wormholeBehavior.radialForce = vortexWormholeProjectile.GetComponent<RadialForce>();

            wormholeBehavior.radialForce.radius = baseRadius;
            wormholeBehavior.radialForce.forceMagnitude = -1000 * baseRadius;
        }

        private void CreateBuff()
        {
            vortexCooldownDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                vortexCooldownDebuff.name = "VortexCooldown";
                vortexCooldownDebuff.buffColor = Color.magenta;
                vortexCooldownDebuff.canStack = false;
                vortexCooldownDebuff.isDebuff = true;
                vortexCooldownDebuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("textures/bufficons/texBuffMercExposeIcon");
            };
            Assets.buffDefs.Add(vortexCooldownDebuff);
        }
    }

    public class VortexWormholeBehavior : MonoBehaviour
    {
        internal ProjectileController projectileController;
        internal ProjectileSimple projectileSimple;
        internal TeamFilter teamFilter;
        internal RadialForce radialForce;

        void Awake()
        {
            if (!projectileController)
                projectileController = GetComponent<ProjectileController>();
            if (!projectileSimple)
            {
                projectileSimple = GetComponent<ProjectileSimple>();
            }
            if (!teamFilter)
                teamFilter = GetComponent<TeamFilter>();
            if (!radialForce)
            {
                radialForce = GetComponent<RadialForce>();
            }
            projectileSimple.lifetime = 0.5f;
            radialForce.radius = VortexLunar.baseRadius;
        }

        void Start()
        {
            teamFilter.teamIndex = TeamIndex.Lunar;

            int vortexCount = 0;
            if (!projectileController)
            {
                projectileController = this.gameObject.GetComponent<ProjectileController>();
            }

            if (projectileController)
            {
                GameObject owner = projectileController.owner;
                if (owner)
                {
                    CharacterBody body = owner.GetComponent<CharacterBody>();
                    if (body)
                    {
                        Inventory inv = body.inventory;
                        if (inv)
                        {
                            vortexCount = inv.GetItemCount(VortexLunar.instance.ItemsDef);
                        }
                        else
                        {
                            Debug.Log("No Inventory component found");
                        }
                    }
                    else
                    {
                        Debug.Log("No CharacterBody component found");
                    }
                }
                else
                {
                    Debug.Log("No owner GameObject found");
                }
            }
            else
            {
                Debug.Log("No ProjectileCOntroller component found");
            }

            if(vortexCount > 0)
            {
                radialForce.radius = VortexLunar.baseRadius + (VortexLunar.stackRadius * (vortexCount - 1));
                radialForce.forceMagnitude = -1000 * radialForce.radius;
            }
            else
            {
                Destroy(this.gameObject);
            }
        }
    }
}
