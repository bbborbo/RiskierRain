using BepInEx.Configuration;
using SwanSongExtended.Equipment.Zapinator;
using EntityStates.Captain.Weapon;
using R2API;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using RiskierRainContent.CoreModules;
using RoR2.ExpansionManagement;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Equipment
{
    class BrokenZapinator : EquipmentBase
    {
        public override AssetBundle assetBundle => null;

        #region config
        public override string ConfigName => "Equipment : Zapinator";
        #endregion
        BasicPickupDropTable zapinatorDropTable;
        public float itemChance = 1;

        float maxAimDistance = 500;

        static GameObject supplyDropMuzzleFlash = Resources.Load<GameObject>("prefabs/effects/muzzleflashes/CaptainAirstrikeMuzzleEffect");

        float freeChargeBaseChance = 5;
        float bonusMultiplierBaseChance = 25;

        float tinyDamageDivider = 2f;
        float bigDamageMultiplier = 2;

        float bigProcMultiplier = 5;
        float bigAoeMultiplier = 3;

        float bigForceMultiplier = 3;
        float selfForceAmt = 0.6f;

        float slowVelocityMultiplier = 0.3f;
        float fastVelocityMultiplier = 1.5f;
        float backwardsVelocityMultiplier = -0.5f;

        float badAccuracy = 0.8f;
        float multiShotAccuracy = 0.7f;
        int multiShotBonus = 3;


        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDef;
        public override string EquipmentName => "Orange Zapinator";

        public override string EquipmentLangTokenName => "BROKENZAPINATOR";

        public override string EquipmentPickupDesc => UtilityColor("I can't believe we left this in the game!");

        public override string EquipmentFullDescription => "Fires a projectile.";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => SwanSongPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlZappinator.prefab");

        public override Sprite EquipmentIcon => SwanSongPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupEQUIPMENT_BROKENZAPINATOR.png");
        public override float BaseCooldown => 35f;
        public override bool EnigmaCompatible => true;
        public override bool CanBeRandomlyActivated => true;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            ZapinatorProjectileCatalog.Init();
        }
        public override void Init()
        {
            zapinatorDropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            zapinatorDropTable.tier1Weight = 1;
            zapinatorDropTable.tier2Weight = 1;
            zapinatorDropTable.tier3Weight = 1;
            zapinatorDropTable.voidTier1Weight = 1;
            zapinatorDropTable.voidTier2Weight = 1;
            zapinatorDropTable.voidTier3Weight = 1;
            zapinatorDropTable.equipmentWeight = 1;
            zapinatorDropTable.lunarItemWeight = 1;
            zapinatorDropTable.lunarEquipmentWeight = 1;
            zapinatorDropTable.bossWeight = 1;

            base.Init();
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            bool b = false;

            CharacterBody ownerBody = slot.characterBody;
            if (ownerBody != null)
            {
                if(Util.CheckRoll(itemChance, 0))
                {
                    PickupIndex pickupIndex = PickupIndex.none;
                    if (zapinatorDropTable)
                    {
                        pickupIndex = zapinatorDropTable.GenerateDrop(ownerBody.equipmentSlot.rng);
                        Ray aimRay = GetAimRay(ownerBody);
                        aimRay.direction += Vector3.up * 0.25f;
                        PickupDropletController.CreatePickupDroplet(pickupIndex, aimRay.origin, aimRay.direction * 25);
                        ownerBody.characterMotor.ApplyForce(aimRay.direction * -1500, true, false);
                        b = true;
                    }
                }
                else
                {
                    int projectileIndex = UnityEngine.Random.Range(0, ZapinatorProjectileCatalog.zapinatorProjectileCount);
                    ZapinatorProjectileData projectileData = ZapinatorProjectileCatalog.GetProjectileDataFromIndex(projectileIndex);
                    ZapinatorRollData modifierRoll = RollModifiersFromProjectileData(projectileData, ownerBody);

                    bool crit = Util.CheckRoll(ownerBody.crit, ownerBody.master);

                    if ((projectileData.type & ZapinatorProjectileType.Captain) > 0)
                    {
                        b = CreateSupplyDrop(slot, ownerBody, projectileData, modifierRoll, crit);
                    }
                    else if ((projectileData.type & ZapinatorProjectileType.Defensive) > 0)
                    {

                    }
                    else
                    {
                        b = CreateOrdinaryProjectile(slot, ownerBody, projectileData, modifierRoll, crit);
                    }
                }

                // rolling to just not consume a charge for the lols
                if (Util.CheckRoll(freeChargeBaseChance, ownerBody.master))
                {
                    b = false;
                }
            }
            return b;
        }

        private bool CreateOrdinaryProjectile(EquipmentSlot slot, CharacterBody ownerBody, ZapinatorProjectileData projectileData, ZapinatorRollData roll, bool crit)
        {
            bool requiresSurface = false;
            bool requiresTarget = false;
            Vector3 rotationDir = Vector3.zero;

            var accuracy = UnityEngine.Random.insideUnitSphere * (1 - roll.accuracy);
            Ray aimRay = GetAimRay(ownerBody);
            RaycastHit hit = CheckPlacementFromAimRay(aimRay, ownerBody.gameObject);

            #region FPI bs
            /*
            public DamageType? damageTypeOverride;
            public ProcChainMask procChainMask;
            public DamageColorIndex damageColorIndex;
            public bool crit;
            public float force;
            public float damage;
            [NonSerialized]
            public float _fuseOverride;
            [NonSerialized]
            public float _speedOverride;
            public bool useSpeedOverride;
            public GameObject target;
            public Quaternion rotation;
            public Vector3 position;
            public bool useFuseOverride;

            public float speedOverride { get; set; }
            public float fuseOverride { get; set; }*/
            #endregion

            GameObject projectile = ProcessProjectile(UnityEngine.Object.Instantiate<GameObject>(projectileData.prefab), roll);

            FireProjectileInfo fireProjectileInfo = new FireProjectileInfo()
            {
                projectilePrefab = projectile,
                owner = ownerBody.gameObject,
                crit = crit,
                force = roll.forceMultiplier,
                damage = roll.damageCoefficient * ownerBody.damage,
                position = ownerBody.transform.position
            };
            rotationDir = aimRay.direction;

            if ((projectileData.type & ZapinatorProjectileType.RequiresSurface) > 0)
            {
                // requires surface means it comes from where you aim
                if (hit.point == Vector3.negativeInfinity)
                    return false;

                requiresSurface = true;
                fireProjectileInfo.position = hit.point;
                rotationDir = Vector3.up;
            }
            else if ((projectileData.type & ZapinatorProjectileType.RequiresTarget) > 0)
            {
                if (hit.point == Vector3.negativeInfinity)
                    return false;

                requiresTarget = true;
                fireProjectileInfo.target = hit.collider.gameObject;
                rotationDir = Vector3.up;
            }

            if (ownerBody.characterMotor && !requiresSurface)
            {
                ownerBody.characterMotor.ApplyForce(aimRay.direction * (-roll.forceMultiplier * roll.selfForceMultiplier), false, false);
            }

            for (int i = 0; i < roll.totalProjectiles; i++)
            {
                fireProjectileInfo.rotation = Util.QuaternionSafeLookRotation(rotationDir + UnityEngine.Random.insideUnitSphere * (1 - roll.accuracy));
                ProjectileManager.instance.FireProjectile(fireProjectileInfo);
            }
            return true;
        }

        private GameObject ProcessProjectile(GameObject projectile, ZapinatorRollData roll)
        {
            if (projectile != null)
            {
                ProjectileSimple projectileSimple = projectile.GetComponent<ProjectileSimple>();
                if (projectileSimple != null)
                {
                    projectileSimple.desiredForwardSpeed *= roll.velocityMultiplier;
                }
                ProjectileDamage projectileDamage = projectile.GetComponent<ProjectileDamage>();
                if (projectileDamage != null)
                {
                    projectileDamage.damageType |= roll.damageTypes;
                }
                ProjectileImpactExplosion impactExplosion = projectile.GetComponent<ProjectileImpactExplosion>();
                if (impactExplosion != null)
                {
                    impactExplosion.blastRadius *= roll.aoeSizeMultiplier;
                    impactExplosion.blastProcCoefficient = roll.procCoefficient;

                    if (impactExplosion.childrenCount > 0 && impactExplosion.fireChildren == true)
                    {
                        impactExplosion.childrenProjectilePrefab = ProcessProjectile(impactExplosion.childrenProjectilePrefab, roll);
                    }
                }
                ProjectileOverlapAttack overlapAttack = projectile.GetComponent<ProjectileOverlapAttack>();
                if (overlapAttack != null)
                {
                    overlapAttack.damageCoefficient = roll.damageCoefficient / 5;
                    overlapAttack.overlapProcCoefficient = roll.procCoefficient / 5;
                }
                ProjectileDotZone dotZone = projectile.GetComponent<ProjectileDotZone>();
                if (dotZone != null)
                {
                    dotZone.damageCoefficient = roll.damageCoefficient / 5;
                    dotZone.overlapProcCoefficient = roll.procCoefficient / 5;
                }
            }

            return projectile;
        }

        private bool CreateSupplyDrop(EquipmentSlot slot, CharacterBody ownerBody, ZapinatorProjectileData projectileData, ZapinatorRollData roll, bool crit)
        {
            bool b = false;
            SetupSupplyDrop.PlacementInfo placementInfo = new SetupSupplyDrop.PlacementInfo();
            if (slot.hasAuthority)
            {
                placementInfo = SetupSupplyDrop.GetPlacementInfo(GetAimRay(ownerBody), ownerBody.gameObject);
            }
            if (placementInfo.ok)
            {
                EffectManager.SimpleMuzzleFlash(supplyDropMuzzleFlash, ownerBody.gameObject, CallSupplyDropBase.muzzleString, false);
                if (NetworkServer.active)
                {
                    GameObject projectile = UnityEngine.Object.Instantiate<GameObject>(projectileData.prefab, placementInfo.position, placementInfo.rotation);
                    projectile.GetComponent<TeamFilter>().teamIndex = ownerBody.teamComponent.teamIndex;
                    projectile.GetComponent<GenericOwnership>().ownerObject = ownerBody.gameObject;

                    ProjectileDamage pd = projectile.GetComponent<ProjectileDamage>();
                    pd.damageColorIndex = DamageColorIndex.Default;

                    pd.crit = crit;
                    pd.damage = ownerBody.damage * roll.damageCoefficient;
                    pd.force = CallSupplyDropBase.impactDamageForce * roll.forceMultiplier;
                    pd.damageType = DamageType.Generic | roll.damageTypes;

                    NetworkServer.Spawn(projectile);
                }
                b = true;
            }

            return b;
        }

        ZapinatorRollData RollModifiersFromProjectileData(ZapinatorProjectileData data, CharacterBody body)
        {
            ZapinatorRollData mod = new ZapinatorRollData();

            ZapinatorModifiers[] modifierPool = data.possibleModifiers;
            int rolls = data.maxModifiers;

            if(Util.CheckRoll(bonusMultiplierBaseChance, body.master))
            {
                rolls += data.bonusModifiers;
            }

            for (int i = 0; i < rolls; i++)
            {
                int modifierRoll = UnityEngine.Random.Range(0, modifierPool.Length);
                ZapinatorModifiers newModifier = modifierPool[modifierRoll];

                switch (newModifier)
                {
                    case ZapinatorModifiers.TinyDamage:
                        mod.damageCoefficient /= tinyDamageDivider;
                        break;
                    case ZapinatorModifiers.BigDamage:
                        mod.damageCoefficient *= bigDamageMultiplier;
                        break;
                    case ZapinatorModifiers.BigProcs:
                        mod.procCoefficient *= bigProcMultiplier;
                        break;
                    case ZapinatorModifiers.BigAoe:
                        mod.aoeSizeMultiplier *= bigAoeMultiplier;
                        break;

                    case ZapinatorModifiers.BigKnockback:
                        mod.forceMultiplier *= bigForceMultiplier;
                        break;
                    case ZapinatorModifiers.SelfKnockback:
                        mod.selfForceMultiplier += selfForceAmt;
                        break;
                    case ZapinatorModifiers.NegativeKnockback:
                        mod.damageCoefficient *= -1;
                        break;

                    case ZapinatorModifiers.VerySlow:
                        mod.velocityMultiplier *= slowVelocityMultiplier;
                        break;
                    case ZapinatorModifiers.VeryFast:
                        mod.damageCoefficient *= fastVelocityMultiplier;
                        break;
                    case ZapinatorModifiers.Backwards:
                        mod.velocityMultiplier *= backwardsVelocityMultiplier;
                        mod.selfForceMultiplier *= -1;
                        break;

                    case ZapinatorModifiers.BadAccuracy:
                        mod.accuracy *= badAccuracy;
                        break;
                    case ZapinatorModifiers.MultiShot:
                        mod.accuracy *= multiShotAccuracy;
                        mod.totalProjectiles += multiShotBonus;
                        break;

                    case ZapinatorModifiers.Debuff:
                        mod.damageTypes |= GetRandomDamageType();
                        mod.damageTypes |= GetRandomDamageType();
                        mod.damageTypes |= GetRandomDamageType();
                        break;
                }
            }

            return mod;
        }

        private DamageType GetRandomDamageType()
        {
            DamageType type = DamageType.Generic;
            int i = UnityEngine.Random.Range(1, 16);
            switch (i)
            {
                case 1:
                    type = DamageType.ApplyMercExpose;
                    break;
                case 2:
                    type = DamageType.BleedOnHit;
                    break;
                case 3:
                    type = DamageType.BlightOnHit;
                    break;
                case 4:
                    type = DamageType.BonusToLowHealth;
                    break;
                case 5:
                    type = DamageType.BypassArmor;
                    break;
                case 6:
                    type = DamageType.ClayGoo;
                    break;
                case 7:
                    type = DamageType.CrippleOnHit;
                    break;
                case 8:
                    type = DamageType.Freeze2s;
                    break;
                case 9:
                    type = DamageType.IgniteOnHit;
                    break;
                case 10:
                    type = DamageType.Nullify;
                    break;
                case 11:
                    type = DamageType.PoisonOnHit;
                    break;
                case 12:
                    type = DamageType.Shock5s;
                    break;
                case 13:
                    type = DamageType.SlowOnHit;
                    break;
                case 14:
                    type = DamageType.Stun1s;
                    break;
                case 15:
                    type = DamageType.WeakOnHit;
                    break;
            }

            return type;
        }
        private RaycastHit CheckPlacementFromAimRay(Ray ray, GameObject obj)
        {
            float num = maxAimDistance;
            float num2 = 0f;
            Ray aimRay = ray;
            RaycastHit raycastHit = new RaycastHit();
            raycastHit.point = Vector3.negativeInfinity;
            if (Physics.Raycast(CameraRigController.ModifyAimRayIfApplicable(ray, obj, out num2), out raycastHit, num + num2, LayerIndex.CommonMasks.bullet))
            {

            }

            return raycastHit;
        }
    }

    public static class ZapinatorProjectileCatalog
    {
        public static List<ZapinatorProjectileData> projectileData = new List<ZapinatorProjectileData>();

        public static int zapinatorProjectileCount
        {
            get
            {
                return ZapinatorProjectileCatalog.projectileData.Count;
            }
        }

        public static ZapinatorProjectileData GetProjectileDataFromIndex(int index)
        {
            if (index > zapinatorProjectileCount)
            {
                Debug.Log("How do you break what's already broken?");

                var errorProjectile = new ZapinatorProjectileData()
                {
                    prefab = Resources.Load<GameObject>("prefabs/projectiles/BeetleQueenSpit"),
                    type = ZapinatorProjectileType.MovesFromSpawn,

                    possibleModifiers = new ZapinatorModifiers[]
                    {
                        ZapinatorModifiers.SelfKnockback,
                        ZapinatorModifiers.SelfKnockback,
                        ZapinatorModifiers.SelfKnockback,
                        ZapinatorModifiers.SelfKnockback,
                        ZapinatorModifiers.BigKnockback,
                        ZapinatorModifiers.BadAccuracy
                    },
                    maxModifiers = 3,
                    bonusModifiers = 2
                };

                return errorProjectile;
            }
            else
                return projectileData[index];
        }


        /// <summary>
        /// Used as the delegate type for the GetStatCoefficients event.
        /// </summary>
        /// <param name="sender">The CharacterBody which RecalculateStats is being called for.</param>
        /// <param name="args">An instance of StatHookEventArgs, passed to each subscriber to this event in turn for modification.</param>
        public delegate void ZapinatorProjectileEventHandler(List<ZapinatorProjectileData> projectileData);

        /// <summary>
        /// Subscribe to this event to modify one of the stat hooks which TILER2.StatHooks covers (see StatHookEventArgs). Fired during CharacterBody.RecalculateStats.
        /// </summary>
        public static event ZapinatorProjectileEventHandler GetZapinatorProjectiles;

        internal static void Init()
        {
            List<ZapinatorProjectileData> zapinatorProjectiles = new List<ZapinatorProjectileData>();
            GetZapinatorProjectiles?.Invoke(zapinatorProjectiles);

            projectileData = zapinatorProjectiles;

            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/BellBall"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BadAccuracy
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/CommandoGrenadeProjectile"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.BigProcs,
                    ZapinatorModifiers.NegativeKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.SelfKnockback
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/CrocoDiseaseProjectile"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigProcs,
                    ZapinatorModifiers.Debuff
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/EngiMine"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.NegativeKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigProcs
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/LoaderPylon"),
                type = ZapinatorProjectileType.MovesFromSpawn | ZapinatorProjectileType.Turret,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/LunarWispTrackingBomb"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/MageFirebolt"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.Nothing,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/Sawmerang"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigProcs,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.Backwards,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/SpiderMine"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.Backwards,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/SyringeProjectileHealing"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.Nothing,
                    ZapinatorModifiers.BigProcs
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/CryoCanisterProjectile"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/TreebotFlowerSeed"),
                type = ZapinatorProjectileType.MovesFromSpawn | ZapinatorProjectileType.Turret,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.VerySlow
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/TreebotMortar2"),
                type = ZapinatorProjectileType.Stationary | ZapinatorProjectileType.RequiresSurface,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/TreebotMortarRain"),
                type = ZapinatorProjectileType.Stationary | ZapinatorProjectileType.RequiresSurface,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.NegativeKnockback,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.Debuff,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigAoe
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/VagrantTrackingBomb"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.Debuff
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/TarSeeker"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.NegativeKnockback,
                    ZapinatorModifiers.Backwards
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, EquipmentRestock"),
                type = ZapinatorProjectileType.Captain,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.NegativeKnockback
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            /*
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/ElectricOrbProjectile"),
                type = ZapinatorProjectileType.MovesFromSpawn | ZapinatorProjectileType.RequiresSurface,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/TarBall"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigProcs,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.NegativeKnockback,
                    ZapinatorModifiers.MultiShot
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/ToolbotGrenadeLauncherProjectile"),
                type = ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.SelfKnockback,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigAoe,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.Backwards,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/projectiles/EngiBubbleShield"),
                type = ZapinatorProjectileType.Defensive | ZapinatorProjectileType.MovesFromSpawn,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.MultiShot,
                    ZapinatorModifiers.VeryFast,
                    ZapinatorModifiers.VerySlow,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.BadAccuracy,
                    ZapinatorModifiers.Backwards,
                    ZapinatorModifiers.Nothing,
                    ZapinatorModifiers.Nothing
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Hacking"),
                type = ZapinatorProjectileType.Captain,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.NegativeKnockback
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Healing"),
                type = ZapinatorProjectileType.Captain | ZapinatorProjectileType.Defensive,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.Debuff
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });
            projectileData.Add(new ZapinatorProjectileData()
            {
                prefab = Resources.Load<GameObject>("prefabs/networkedobjects/captainsupplydrops/CaptainSupplyDrop, Shocking"),
                type = ZapinatorProjectileType.Captain | ZapinatorProjectileType.Defensive | ZapinatorProjectileType.Turret,

                possibleModifiers = new ZapinatorModifiers[]
                {
                    ZapinatorModifiers.TinyDamage,
                    ZapinatorModifiers.BigDamage,
                    ZapinatorModifiers.BigKnockback,
                    ZapinatorModifiers.Debuff
                },
                maxModifiers = 3,
                bonusModifiers = 2
            });*/
        }
    }
}
