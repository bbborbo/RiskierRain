using BepInEx.Configuration;
using RiskierRainContent.CoreModules;
using EntityStates.Toolbot;
using R2API;
using RoR2;
using RoR2.Artifacts;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RiskierRainContent.CoreModules.EliteModule;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Equipment
{
    class VolatileAspect : T1EliteEquipmentBase<VolatileAspect>
    {
        public static float volatileMortarDamageCoefficient = 3f;
        public static GameObject volatileMortarPrefab;
        // this is the exact damage the landmine does, but it scales with team level
        public static float volatileLandmineDamage = 10f;
        public static GameObject volatileLandminePrefab;
        public override string EliteEquipmentName => "Bava\u2019s Refrain";

        public override string EliteAffixToken => "AFFIX_EXPLOSIVE";

        public override string EliteEquipmentPickupDesc => "Become an aspect of force.";

        public override string EliteEquipmentFullDescription => "All attacks are explosive. Periodically fire a mortar.";

        public override string EliteEquipmentLore => "";

        public override string EliteModifier => "Volatile";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        //public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.mainAssetBundle.LoadAsset<Material>(RiskierRainPlugin.eliteMaterialsPath + "matVolatile.mat");
        public override string EliteRampTextureName { get; set; } = "texRampVolatile";

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f;

        public override Texture2D EliteBuffIcon => Addressables.LoadAssetAsync<Texture2D>("RoR2/Base/EliteIce/texBuffAffixWhite.tif").WaitForCompletion();
        public override Color EliteBuffColor => new Color(1.0f, 0.6f, 0.0f, 1.0f);

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitAll += VolatileOnHit;
            //On.RoR2.CharacterBody.FixedUpdate += VolatileMortar;
            On.RoR2.CharacterBody.AddBuff_BuffIndex += VolatileBuffCheck;
            On.RoR2.GlobalEventManager.OnCharacterDeath += SpiteOnDeath;
        }

        private void SpiteOnDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            CharacterBody victimBody = damageReport.victimBody;
            CharacterMaster victimMaster = damageReport.victimMaster;
            if (victimBody)
            {
                if (victimBody.HasBuff(EliteBuffDef))
                {
                    Vector3 spawnPosition = Util.GetCorePosition(victimBody);
                    ProjectileManager.instance.FireProjectile(volatileLandminePrefab, spawnPosition, Util.QuaternionSafeLookRotation(Vector3.up), 
                        victimBody.gameObject, (1 + 0.3f * victimBody.level) * volatileLandmineDamage, 400, Util.CheckRoll(victimBody.crit, victimBody.master));

                    /*List<BombArtifactManager.BombRequest> bombRequests = new List<BombArtifactManager.BombRequest>();

                    int num = Mathf.CeilToInt(Mathf.Min(Mathf.CeilToInt(victimBody.bestFitRadius * BombArtifactManager.extraBombPerRadius * BombArtifactManager.cvSpiteBombCoefficient.value),
                        BombArtifactManager.maxBombCount) / 2);
                    for (int i = 0; i < num; i++)
                    {
                        Vector3 b = UnityEngine.Random.insideUnitSphere * 
                            (BombArtifactManager.bombSpawnBaseRadius + victimBody.bestFitRadius * BombArtifactManager.bombSpawnRadiusCoefficient);
                        BombArtifactManager.BombRequest item = new BombArtifactManager.BombRequest
                        {
                            spawnPosition = spawnPosition,
                            raycastOrigin = spawnPosition + b,
                            bombBaseDamage = victimBody.damage * BombArtifactManager.bombDamageCoefficient,
                            attacker = victimBody.gameObject,
                            teamIndex = damageReport.victimTeamIndex,
                            velocityY = UnityEngine.Random.Range(5f, 25f)
                        };
                        bombRequests.Add(item);
                    }

                    Ray ray = new Ray(spawnPosition + new Vector3(0f, BombArtifactManager.maxBombStepUpDistance, 0f), Vector3.down);
                    float maxDistance = BombArtifactManager.maxBombStepUpDistance + BombArtifactManager.maxBombFallDistance;

                    RaycastHit raycastHit;
                    if (Physics.Raycast(ray, out raycastHit, maxDistance, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                    {
                        float groundY = raycastHit.point.y;
                        if (spawnPosition.y < groundY + 4f)
                        {
                            spawnPosition.y = groundY + 4f;
                        }
                        foreach(BombArtifactManager.BombRequest bombRequest in bombRequests)
                        {
                            Vector3 raycastOrigin = bombRequest.raycastOrigin;
                            raycastOrigin.y = groundY;
                            GameObject gameObject = UnityEngine.Object.Instantiate<GameObject>(BombArtifactManager.bombPrefab, spawnPosition, UnityEngine.Random.rotation);
                            SpiteBombController component = gameObject.GetComponent<SpiteBombController>();
                            DelayBlast delayBlast = component.delayBlast;
                            TeamFilter component2 = gameObject.GetComponent<TeamFilter>();
                            component.bouncePosition = raycastOrigin;
                            component.initialVelocityY = bombRequest.velocityY;
                            delayBlast.position = spawnPosition;
                            delayBlast.baseDamage = bombRequest.bombBaseDamage;
                            delayBlast.baseForce = 2300f;
                            delayBlast.attacker = bombRequest.attacker;
                            delayBlast.radius = BombArtifactManager.bombBlastRadius;
                            delayBlast.crit = false;
                            delayBlast.procCoefficient = 0.75f;
                            delayBlast.maxTimer = BombArtifactManager.bombFuseTimeout;
                            delayBlast.timerStagger = 0f;
                            delayBlast.falloffModel = BlastAttack.FalloffModel.None;
                            component2.teamIndex = bombRequest.teamIndex;
                            NetworkServer.Spawn(gameObject);
                        }
                    }*/
                }
            }
            orig(self, damageReport);
        }

        private void VolatileBuffCheck(On.RoR2.CharacterBody.orig_AddBuff_BuffIndex orig, CharacterBody self, BuffIndex buffType)
        {
            orig(self, buffType);
            if (buffType == EliteBuffDef.buffIndex)
            {
                VolatileMortarAttachment VMA = self.gameObject?.GetComponent<VolatileMortarAttachment>();
                if (VMA == null)
                {
                    self.gameObject.AddComponent<VolatileMortarAttachment>();
                }
            }
        }

        private void VolatileMortar(On.RoR2.CharacterBody.orig_FixedUpdate orig, CharacterBody self)
        {
            orig(self);
            if (self.HasBuff(EliteBuffDef))
            {
                VolatileMortarAttachment VMA = self.gameObject.GetComponent<VolatileMortarAttachment>();
                if(VMA == null)
                {
                    self.gameObject.AddComponent<VolatileMortarAttachment>();
                }
            }
        }

        private void VolatileOnHit(On.RoR2.GlobalEventManager.orig_OnHitAll orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();

                if (aBody)
                {
                    if (IsElite(aBody, EliteBuffDef))
                    {
                        if (!damageInfo.procChainMask.HasProc(ProcType.Behemoth) && damageInfo.procCoefficient != 0f)
                        {
                            float radius = 4 * damageInfo.procCoefficient;
                            float damageCoefficient = 0.1f;
                            float baseDamage = Util.OnHitProcDamage(damageInfo.damage, aBody.damage, damageCoefficient);
                            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/OmniEffect/OmniExplosionVFXQuick"), new EffectData
                            {
                                origin = damageInfo.position,
                                scale = radius,
                                rotation = Util.QuaternionSafeLookRotation(damageInfo.force)
                            }, true);
                            BlastAttack blastAttack = new BlastAttack();
                            blastAttack.position = damageInfo.position;
                            blastAttack.baseDamage = baseDamage;
                            blastAttack.baseForce = (damageInfo.force.magnitude) * 3f + 200f;
                            blastAttack.radius = radius;
                            blastAttack.attacker = damageInfo.attacker;
                            blastAttack.inflictor = null;
                            blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                            blastAttack.crit = damageInfo.crit;
                            blastAttack.procChainMask = damageInfo.procChainMask;
                            blastAttack.procCoefficient = 0f;
                            blastAttack.damageColorIndex = DamageColorIndex.Item;
                            blastAttack.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                            blastAttack.damageType = damageInfo.damageType;
                            blastAttack.Fire();
                        }
                    }
                }
            }

            orig(self, damageInfo, victim);
        }

        public override void Init(ConfigFile config)
        {
            //CanAppearInEliteTiers = VanillaTier1();
            CreateProjectile();

            CreateEliteEquipment();
            CreateLang();
            CreateElite();
            Hooks();
            VolatileMortarAttachment.buffDef = EliteBuffDef;
        }

        private void CreateProjectile()
        {
            volatileMortarPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/ToolbotGrenadeLauncherProjectile").InstantiateClone("BorboVolatileMortar", true);
            if(volatileMortarPrefab != null)
            {
                GameObject mortarGhost = LegacyResourcesAPI.Load<GameObject>("prefabs/projectileghosts/ToolbotGrenadeGhost").InstantiateClone("BorboVolatileMortarGhost", false);
                volatileMortarPrefab.transform.localScale = Vector3.one * 0.2f;

                ProjectileController pc = volatileMortarPrefab.GetComponent<ProjectileController>();
                ProjectileGhostController pgc = mortarGhost.GetComponent<ProjectileGhostController>();
                pc.ghost = pgc;

                Material material1 = CoreModules.Assets.mainAssetBundle.LoadAsset<Material>(CoreModules.Assets.eliteMaterialsPath + "matVolatile.mat");
                //Material material2 = new Material(LegacyShaderAPI.Find("Standard"));
                Tools.GetMaterial(mortarGhost, "Mesh", Color.red, ref material1);
                Tools.GetMaterial(mortarGhost, "GameObject (1)", Color.green, ref material1);// material2);
                Tools.GetParticle(mortarGhost, "Fire", Color.blue);
                //Tools.DebugMaterial(projectileGhost);
                //Tools.DebugParticleSystem(projectileGhost);

                ProjectileDamage pd = volatileMortarPrefab.GetComponent<ProjectileDamage>();
                pd.force = 1500;

                ProjectileSimple scrapPs = volatileMortarPrefab.GetComponent<ProjectileSimple>();
                scrapPs.desiredForwardSpeed = 30f;

                Rigidbody scrapRb = volatileMortarPrefab.GetComponent<Rigidbody>();
                scrapRb.useGravity = true;
                scrapRb.freezeRotation = false;

                AntiGravityForce scrapAntiGravity = volatileMortarPrefab.GetComponent<AntiGravityForce>();
                if (scrapAntiGravity == null)
                {
                    scrapAntiGravity = volatileMortarPrefab.AddComponent<AntiGravityForce>();
                }
                scrapAntiGravity.rb = scrapRb;
                scrapAntiGravity.antiGravityCoefficient = 0f;

                ProjectileImpactExplosion pie = volatileMortarPrefab.GetComponent<ProjectileImpactExplosion>();
                pie.blastProcCoefficient = 0;
                pie.blastRadius = 15f;
                pie.bonusBlastForce = Vector3.up * 1000;

                CoreModules.Assets.projectilePrefabs.Add(volatileMortarPrefab);
            }

            volatileLandminePrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiMine.prefab").WaitForCompletion().InstantiateClone("BorboVolatileLandmine", true);
            if (volatileLandminePrefab != null)
            {
                GameObject landmineGhost = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Engi/EngiMineGhost.prefab").WaitForCompletion().InstantiateClone("BorboVolatileLandmineGhost", false);
                //landmineGhost.transform.localScale = Vector3.one * 1f;

                ProjectileDeployToOwner pdto = volatileLandminePrefab.GetComponent<ProjectileDeployToOwner>();
                if (pdto)
                    UnityEngine.Object.Destroy(pdto);

                Deployable deployableComponent = volatileLandminePrefab.GetComponent<Deployable>();
                if (deployableComponent)
                    UnityEngine.Object.Destroy(deployableComponent);

                ProjectileController pc = volatileLandminePrefab.GetComponent<ProjectileController>();
                ProjectileGhostController pgc = landmineGhost.GetComponent<ProjectileGhostController>();
                pc.ghost = pgc;

                Material material1 = CoreModules.Assets.mainAssetBundle.LoadAsset<Material>(CoreModules.Assets.eliteMaterialsPath + "matVolatile.mat");
                //Material material2 = new Material(LegacyShaderAPI.Find("Standard"));
                Tools.GetMaterial(landmineGhost, "Mesh", Color.red, ref material1);
                Tools.GetMaterial(landmineGhost, "GameObject (1)", Color.green, ref material1);// material2);
                Tools.GetParticle(landmineGhost, "Fire", Color.blue);
                //Tools.DebugMaterial(projectileGhost);
                //Tools.DebugParticleSystem(projectileGhost);

                ProjectileDamage pd = volatileLandminePrefab.GetComponent<ProjectileDamage>();
                pd.force = 2500;

                ProjectileSphereTargetFinder pstf = volatileLandminePrefab.GetComponent<ProjectileSphereTargetFinder>();
                if (pstf)
                {
                    pstf.targetSearchInterval = 0.2f;
                }

                /*EntityStateMachine[] stateMachines = volatileLandminePrefab.GetComponents<EntityStateMachine>();
                foreach(EntityStateMachine esm in stateMachines)
                {
                    if(esm.customName == "Main")
                    {

                    }
                    if(esm.customName == "Arming")
                    {

                    }
                }*/

                CoreModules.Assets.projectilePrefabs.Add(volatileLandminePrefab);
            }
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }

    class VolatileMortarAttachment : MonoBehaviour
    {
        public static GameObject projectilePrefab => VolatileAspect.volatileMortarPrefab;
        public static float mortarDamageCoefficient => VolatileAspect.volatileMortarDamageCoefficient;
        public static BuffDef buffDef;

        public CharacterBody body;
        static float mortarTimer = 0f;
        static float mortarInterval = 6f;
        static float maxYawSpread = 60f;

        void Start()
        {
            if(body == null)
            {
                body = this.gameObject.GetComponent<CharacterBody>();
            }
        }

        void FixedUpdate()
        {
            //Debug.Log("AHHH!!");
            if(body != null && body.healthComponent != null)
            {
                HealthComponent hc = body.healthComponent;
                if (body.HasBuff(buffDef) && hc.alive && !hc.isInFrozenState)// && (!body.outOfDanger || !body.outOfCombat))
                {
                    mortarTimer += Time.fixedDeltaTime * body.attackSpeed;
                    if (mortarTimer >= mortarInterval)
                    {
                        mortarTimer = 0f;
                        Vector3 forward = body.inputBank.aimDirection;
                        forward.y = 0;
                        float projectileCount = Mathf.Ceil(body.radius * 1f) + 1;
                        float yawPerProjectile = (maxYawSpread * 2) / (projectileCount + 1);

                        for (int i = 0; i < projectileCount; i++)
                        {
                            //float currentYaw = (yawPerProjectile - (i)) * maxYawSpread;
                            float currentYaw = (projectileCount == 1) ? 0 : (yawPerProjectile * (i + 1)) - maxYawSpread;
                            Vector3 forward2 = (projectileCount == 1) ? forward : Util.ApplySpread(forward, 2, 10, 1f, 1f, currentYaw, 0);
                            Vector3 fireDirection = forward2 + Vector3.up * 1.2f;
                            ProjectileManager.instance.FireProjectile(projectilePrefab, body.corePosition,
                                Util.QuaternionSafeLookRotation(fireDirection), body.gameObject, body.damage * mortarDamageCoefficient, 0f,
                                Util.CheckRoll(body.crit, body.master), DamageColorIndex.Default, null, -1f);
                        }
                    }
                }
                else
                {
                    mortarTimer = 0f;
                }
            }
        }
    }
}