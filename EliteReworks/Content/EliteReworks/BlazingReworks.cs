using FruityElites.Modules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;
using static MoreStats.StatHooks;
using HG;
using R2API;
using UnityEngine.AddressableAssets;
using System.Collections.ObjectModel;
using System.Linq;
using R2API.Networking.Interfaces;
using R2API.Networking;

namespace FruityElites.EliteReworks
{
    public class SyncBlazingAuraRadius : INetMessage
    {
        GameObject bodyObject;
        float radius;
        public SyncBlazingAuraRadius()
        {
        }
        public SyncBlazingAuraRadius(GameObject bodyObject, float radius)
        {
            this.bodyObject = bodyObject;
            this.radius = radius;
        }

        public void Serialize(NetworkWriter writer)
        {
            writer.Write(this.bodyObject);
            writer.Write((double)this.radius);
        }
        public void Deserialize(NetworkReader reader)
        {
            this.bodyObject = reader.ReadGameObject();
            this.radius = (float)reader.ReadDouble();
        }

        public void OnReceived()
        {
            if (!NetworkServer.active)
                return;

            if (bodyObject.TryGetComponent(out AffixRedBehavior red))
            {
                red.SetAuraRange(radius);
            }
        }
    }

    class BlazingReworks : EliteReworkBase<BlazingReworks>
    {
        public static GameObject flameAuraPrefab;
        public static GameObject flameAuraMaxRangeIndicatorPrefab;
        public static GameObject extinguishImpactEffect;

        public static BuffDef accelerantBuff;
        [AutoConfig("On-Death : Accelerant Buff Duration", "Expressed in seconds. Vanilla is N/A", 8f)]
        public static float accelerantDuration = 8f;
        [AutoConfig("On-Death : Accelerant Attack Speed", "Expressed as a percentage (eg 0.25 is 25%). Vanilla is N/A", 0.25f)]
        public static float accelerantAttackSpeed = 0.25f;
        [AutoConfig("On-Death : Accelerant Movement Speed", "Expressed as a percentage (eg 0.25 is 25%). Vanilla is N/A", 0.0f)]
        public static float accelerantMovementSpeed = 0.0f;
        [AutoConfig("On-Death : Accelerant Ignite Chance", "Expressed as a chance out of 100 (eg 100 is 100%). Vanilla is N/A", 100f)]
        public static float accelerantIgniteChance = 100f;

        [AutoConfig("Passive : Flame Aura Range", "Maximum range of flame aura, added to body radius. Expressed in meters. Vanilla is N/A", 18f)]
        public static float flameAuraRange = 18f;
        [AutoConfig("Passive : Flame Aura Growth Per Second", "Expressed as a fraction of max range (eg 0.25 is 25% is 4 seconds for full range). Vanilla is N/A", 0.25f)]
        public static float flameAuraGrowthPerSecond = 0.25f;
        [AutoConfig("Passive : Flame Aura Damage Interval", "Duration in seconds between ticks of damage. Vanilla is N/A", 0.5f)]
        public static float flameAuraDamageInterval = 0.5f;

        [AutoConfig("Passive : Flame Aura Ignite Damage Base", "Total starting damage of ignite stacks added by flame aura. Vanilla is N/A", 10f)]
        public static float flameAuraIgniteTotalDamageBase = 10f;
        [AutoConfig("Passive : Flame Aura Ignite Damage Level", 
            "Amount to scale ignite stacks added by flame aura. Expressed as a percentage of base value (eg 0.4 is 40% per level). Vanilla is N/A", 0.4f)]
        public static float flameAuraIgniteTotalDamageLevel = 0.4f;


        public static float fireTrailDPS = 80f; //1.5f
        public static float fireTrailBaseRadius = 6f; //3f
        public static float fireTrailLifetime = 100f; //3f
        public override string eliteName => "Blazing";

        public override void Init()
        {
            NetworkingAPI.RegisterMessageType<SyncBlazingAuraRadius>();

            accelerantBuff = Content.CreateAndAddBuff("bdBlazingAccelerant", null, Color.red, false, false);
            EliteReworksPlugin.LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common.bdOnFire_asset, (onFire) =>
            {
                accelerantBuff.iconSprite = onFire.iconSprite;
            });
            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BurnNearby.HelfireController_prefab, CreateFlameAura);
            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_NearbyDamageBonus.NearbyDamageBonusIndicator_prefab, CreateRangeIndicator);
            EliteReworksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2SmokeBombMini_prefab, CreateExtinguishImpactEffect);
            //RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Bandit2.Bandit2SmokeBombMini_prefab
            //RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Common_VFX.MuzzleflashSmokeRing_prefab
            //RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_Toolbot.
            //RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_MiniMushroom.SporeGrenadeGasImpact_prefab
            //RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC3_FriendUnit.FriendUnitImpact_prefab
            base.Init();
        }

        private void CreateExtinguishImpactEffect(GameObject obj)
        {
            extinguishImpactEffect = obj.InstantiateClone("BlazingExtinguishImpactEffect", false);
            Content.CreateAndAddEffectDef(extinguishImpactEffect);
        }

        private void CreateRangeIndicator(GameObject obj)
        {
            flameAuraMaxRangeIndicatorPrefab = obj.InstantiateClone("BlazingRangeIndicator", true);

            Transform radiusSpherical = flameAuraMaxRangeIndicatorPrefab.transform.GetChild(1);
            if (radiusSpherical)
            {
                radiusSpherical.transform.localScale = Vector3.one;

                if (radiusSpherical.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
                {
                    Material mat = UnityEngine.Object.Instantiate(meshRenderer.material);
                    mat.SetColor("_TintColor", new Color32(255, 118, 24, 192));

                    meshRenderer.material = mat;
                }
            }

            Modules.Content.AddNetworkedObjectPrefab(flameAuraMaxRangeIndicatorPrefab);
        }

        private void CreateFlameAura(GameObject helfireAura)
        {
            Transform auraTransform = helfireAura.transform.Find("AuraTransform");
            if(auraTransform == null)
            {
                if(helfireAura.TryGetComponent(out HelfireController helfireController))
                {
                    auraTransform = helfireController.auraEffectTransform;
                }
            }
            if(auraTransform == null)
            {
                Log.Error("FruityElites could not get blazing aura effect");
                return;
            }

            flameAuraPrefab = auraTransform.gameObject.InstantiateClone("BlazingFlameAura", false);

            Material matAura;
            Transform radiusSpherical = flameAuraPrefab.transform.GetChild(1);
            if (radiusSpherical != null && radiusSpherical.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
            {
                matAura = UnityEngine.Object.Instantiate(meshRenderer.material);
                matAura.SetColor("_TintColor", Color.white);
                matAura.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampMagmaWorm_png).WaitForCompletion());
                matAura.SetFloat("_RimPower", 3.98f);
                matAura.SetFloat("_RimStrength", 0.61f);
                meshRenderer.material = matAura;
            }

            Material matParticle;
            Transform worldFire = flameAuraPrefab.transform.GetChild(0);
            worldFire.transform.localScale = Vector3.one * 3;
            if (worldFire.gameObject.TryGetComponent(out ParticleSystemRenderer psr1))
            {
                matParticle = UnityEngine.Object.Instantiate(psr1.material);
                matParticle.SetColor("_TintColor", Color.white);
                matParticle.SetTexture("_RemapTex", Addressables.LoadAssetAsync<Texture>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Common_ColorRamps.texRampMageFire_png).WaitForCompletion());
                matParticle.SetFloat("_Boost", 3.32f); //0.34
                psr1.material = matParticle;

                Transform localFire = flameAuraPrefab.transform.GetChild(2);
                localFire.transform.localScale = Vector3.one * 2;
                if (localFire.gameObject.TryGetComponent(out ParticleSystemRenderer psr2))
                {
                    psr2.material = matParticle;
                }
                Transform localFireSpherical = flameAuraPrefab.transform.GetChild(3);
                localFireSpherical.transform.localScale = Vector3.one * 2;
                if (localFireSpherical.gameObject.TryGetComponent(out ParticleSystemRenderer psr3))
                {
                    psr3.material = matParticle;
                }
                Transform spit = flameAuraPrefab.transform.GetChild(4);
                spit.transform.localScale = Vector3.one * 2;
                if (spit.gameObject.TryGetComponent(out ParticleSystemRenderer psr4))
                {
                    psr4.material = matParticle;
                }
            }
        }

        public override void Hooks()
        {
            On.RoR2.CharacterBody.UpdateFireTrail += BlazingFireTrailChanges;
            On.RoR2.CharacterBody.AddOrRemoveEliteItemBehavior += AddBlazingItemBehavior;
            GetStatCoefficients += AccelerantStats;
            GetMoreStatCoefficients += AccelerantMoreStats;
        }

        private void AccelerantMoreStats(CharacterBody sender, MoreStatHookEventArgs args)
        {
            if (sender.HasBuff(accelerantBuff))
            {
                args.burnChanceOnHit += accelerantIgniteChance;
            }
        }

        private void AccelerantStats(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.HasBuff(accelerantBuff))
            {
                args.attackSpeedMultAdd += accelerantAttackSpeed;
                args.moveSpeedMultAdd += accelerantMovementSpeed;
            }
        }

        private void AddBlazingItemBehavior(On.RoR2.CharacterBody.orig_AddOrRemoveEliteItemBehavior orig, CharacterBody self, BuffDef buffDef, bool add)
        {
            if (buffDef == RoR2Content.Buffs.AffixRed)
            {
                self.AddItemBehavior<AffixRedBehavior>(add ? 1 : 0);
                return;
            }
            orig(self, buffDef, add);
        }

        private void BlazingFireTrailChanges(On.RoR2.CharacterBody.orig_UpdateFireTrail orig, CharacterBody self)
        {

            return;

            if (self.fireTrail)
            {
                self.fireTrail.radius = fireTrailBaseRadius * self.radius;
                self.fireTrail.damagePerSecond = (1 + 0.2f * self.level) * fireTrailDPS;
                //self.fireTrail.pointLifetime = fireTrailLifetime;
            }
        }
    }
    public class AffixRedBehavior : CharacterBody.ItemBehavior, IOnTakeDamageServerReceiver, IOnKilledServerReceiver
    {
        private static List<AffixRedBehavior> instancesList = new List<AffixRedBehavior>();
        public static ReadOnlyCollection<AffixRedBehavior> readOnlyInstancesList = new ReadOnlyCollection<AffixRedBehavior>(AffixRedBehavior.instancesList);

        float flameAuraMaxRange => body.bestFitRadius + BlazingReworks.flameAuraRange;
        float flameAuraGrowthPerSecond => flameAuraMaxRange * BlazingReworks.flameAuraGrowthPerSecond;
        float flameAuraDamageInterval => BlazingReworks.flameAuraDamageInterval / body.attackSpeed;
        private const float rangeIndicatorScale = 2;
        private const float auraScale = 1;
        private const float minRange = 2;
        private float currentRange = 0;
        private float damageStopwatch = 0;
        public GameObject auraInstance;
        public GameObject rangeIndicatorInstance;

        SphereSearch sphereSearch;

        private bool indicatorEnabled
        {
            get
            {
                return this.rangeIndicatorInstance != null;
            }
            set
            {
                if (this.indicatorEnabled == value)
                {
                    return;
                }
                if (value)
                {
                    this.rangeIndicatorInstance = UnityEngine.Object.Instantiate<GameObject>(BlazingReworks.flameAuraMaxRangeIndicatorPrefab, base.body.corePosition, Quaternion.identity);
                    this.rangeIndicatorInstance.transform.localScale = Vector3.one * rangeIndicatorScale * flameAuraMaxRange;
                    this.rangeIndicatorInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
                    return;
                }
                UnityEngine.Object.Destroy(this.rangeIndicatorInstance);
                this.rangeIndicatorInstance = null;
            }
        }

        void Start()
        {
            instancesList.Add(this);
            body?.healthComponent?.AddOnTakeDamageServerReceiver(this);

            indicatorEnabled = true;
            auraInstance = Instantiate(BlazingReworks.flameAuraPrefab, body.transform);
            SetAuraRange(0);

            if (NetworkServer.active)
            {
                this.sphereSearch = new SphereSearch();
                this.sphereSearch.origin = body.corePosition;
                this.sphereSearch.mask = LayerIndex.entityPrecise.mask;
                this.sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;
                this.sphereSearch.radius = currentRange;
            }
        }
        void OnDestroy()
        {
            if (instancesList.Contains(this))
            {
                instancesList.Remove(this);
            }
            body?.healthComponent?.RemoveOnTakeDamageServerReceiver(this);
            Destroy(auraInstance);
            indicatorEnabled = false;
            //if (body.healthComponent.alive == false)
            //    AccelerateNearby(TeamIndex.Player);
        }
        public void OnTakeDamageServer(DamageReport damageReport)
        {
            if(damageReport.damageInfo.damageType.IsDamageSourceSkillBased || damageReport.damageInfo.damageType.damageSource == DamageSource.Equipment && damageReport.attackerBody.isPlayerControlled)
            {
                List<AffixRedBehavior> reds = new List<AffixRedBehavior>(readOnlyInstancesList).Where(x => 
                x.currentRange > minRange
                && ((body.corePosition - x.body.corePosition).sqrMagnitude <= Mathf.Pow(flameAuraMaxRange + 2,2))
                ).ToList();

                foreach(AffixRedBehavior red in reds)
                    red.ServerSetAuraRange(0);
            }
            //if(body.healthComponent.alive == false && body.healthComponent.wasAlive == true && damageReport.attackerTeamIndex != damageReport.victimTeamIndex)
            //{
            //    AccelerateNearby(damageReport.attackerTeamIndex);
            //}
        }

        void Update()
        {
            if ((body.outOfDanger || currentRange >= minRange) && currentRange < flameAuraMaxRange)
            {
                if (currentRange < minRange)
                    damageStopwatch = flameAuraDamageInterval;

                SetAuraRange(MathF.Min(flameAuraMaxRange, currentRange + flameAuraGrowthPerSecond * Time.deltaTime));
            }
        }

        void FixedUpdate()
        {
            if (NetworkServer.active && currentRange >= minRange)
            {
                damageStopwatch -= Time.fixedDeltaTime;
                if (damageStopwatch < 0)
                {
                    //get targets
                    List<HurtBox> enemies = GetNearbyTargets(currentRange, body.teamComponent.teamIndex, true);

                    float totalDamage = BlazingReworks.flameAuraIgniteTotalDamageBase * Tools.GetAmbientLevelScalar(BlazingReworks.flameAuraIgniteTotalDamageLevel);
                    Inventory inv = body.inventory;
                    while (damageStopwatch < 0)
                    {
                        damageStopwatch += flameAuraDamageInterval;

                        foreach(HurtBox target in enemies)
                        {
                            bool isResistAoe = target.healthComponent.body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ResistantToAOE);
                            InflictDotInfo inflictDotInfo = new InflictDotInfo
                            {
                                attackerObject = body.gameObject,
                                victimObject = target.healthComponent.gameObject,
                                totalDamage = new float?(isResistAoe ? totalDamage * 0.33f : totalDamage),
                                damageMultiplier = 1f,
                                dotIndex = DotController.DotIndex.Burn,
                                maxStacksFromAttacker = null
                            };
                            StrengthenBurnUtils.CheckDotForUpgrade(inv, ref inflictDotInfo);
                            DotController.InflictDot(ref inflictDotInfo);
                        }
                    }
                }
            }
        }

        public void OnKilledServer(DamageReport damageReport)
        {
            AccelerateNearby(damageReport.attackerTeamIndex);
        }
        void AccelerateNearby(TeamIndex targetTeam)
        {
            //get targets
            List<HurtBox> enemies = GetNearbyTargets(flameAuraMaxRange + 2, targetTeam, false);

            //buff targets/send buff orb
            foreach (HurtBox target in enemies)
            {
                CharacterBody body = target.healthComponent.body;
                target.healthComponent.body.AddTimedBuff(BlazingReworks.accelerantBuff, BlazingReworks.accelerantDuration);
            }
        }

        List<HurtBox> GetNearbyTargets(float radius, TeamIndex targetTeam, bool invertTeam)
        {
            List<HurtBox> candidates = new List<HurtBox>();
            sphereSearch.origin = this.transform.position;
            sphereSearch.radius = radius;

            TeamMask mask = default(TeamMask);
            if (invertTeam)
                mask = TeamMask.GetEnemyTeams(targetTeam);
            else
                mask.AddTeam(targetTeam);

            sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(mask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(candidates);
            return candidates;
        }
        private void ServerSetAuraRange(float newRange)
        {
            if (NetworkServer.active)
            {
                SetAuraRange(newRange);
                new SyncBlazingAuraRadius(body.gameObject, newRange).Send(NetworkDestination.Clients);
            }
        }

        internal void SetAuraRange(float newRange)
        {
            currentRange = newRange;
            bool shouldBeActive = currentRange > minRange;
            bool isActive = auraInstance.activeSelf;
            if(isActive != shouldBeActive)
            {
                if(shouldBeActive == false)
                {
                    //EffectManager.SimpleImpactEffect(BlazingReworks)
                    EffectManager.SpawnEffect(BlazingReworks.extinguishImpactEffect,
                        new EffectData
                        {
                            origin = body.corePosition,
                            scale = 1,
                            rotation = UnityEngine.Random.rotation
                        }, false);
                    damageStopwatch = flameAuraDamageInterval;
                }
                auraInstance.SetActive(shouldBeActive);
            }
            if (shouldBeActive)
            {
                auraInstance.transform.localScale = Vector3.one * auraScale * currentRange;
            }
        }
    }
}
