using BepInEx.Configuration;
using RiskierRain.Components;
using RiskierRain.CoreModules;
using EntityStates.TeleporterHealNovaController;
using MonoMod.Cil;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RiskierRain.CoreModules.EliteModule;
using static EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse;
using static RoR2.CombatDirector;
using RiskierRain.States.LeechingHealNovaController;

namespace RiskierRain.Equipment
{
    class LeechingAspect : EliteEquipmentBase<LeechingAspect>
    {
        public static float healPulseRadius = 25.1354563f;
        public static float healFraction = 0.1f;
        public static float maxHealFraction = 2f;
        public static GameObject pulsePrefab;

        public override string EliteEquipmentName => "N\u2019Kuhana\u2019s Respite";

        public override string EliteAffixToken => "AFFIX_LEECH";

        public override string EliteEquipmentPickupDesc => "Become an aspect of eternity.";

        public override string EliteEquipmentFullDescription => "";

        public override string EliteEquipmentLore => "";

        public override string EliteModifier => "Serpentine";

        public override GameObject EliteEquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EliteEquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");


        public override Sprite EliteBuffIcon => RoR2Content.Equipment.AffixHaunted.passiveBuffDef.iconSprite;
        public override Color EliteBuffColor => Color.magenta;

        //public override Material EliteOverlayMaterial { get; set; } = LegacyResourcesAPI.Load<Material>("materials/matElitePoisonOverlay");
        public override Material EliteOverlayMaterial { get; set; } = RiskierRainPlugin.assetBundle.LoadAsset<Material>(RiskierRainPlugin.assetsPath + "matLeeching.mat");
        public override string EliteRampTextureName { get; set; } = "texRampLeeching";
        public override EliteTiers EliteTier { get; set; } = EliteTiers.Tier2;

        public override bool CanDrop { get; } = false;

        public override float Cooldown { get; } = 0f; 


        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += LeechingOnHit;
            //On.EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse.OnEnter += LeechingHealingPulse;
            On.EntityStates.TeleporterHealNovaController.TeleporterHealNovaWindup.OnEnter += LeechingHealingPulseIntercept;
            On.RoR2.CharacterBody.OnInventoryChanged += LeechingPulseRangeIndicator;
        }

        private void LeechingPulseRangeIndicator(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            self.AddItemBehavior<AffixSerpentineBehavior>(IsElite(self) ? 1 : 0);
        }

        public override void Init(ConfigFile config)
        {
            /*Material mat = LegacyResourcesAPI.Load<Material>("materials/matEliteHauntedOverlay");
            mat.color = Color.magenta;
            EliteMaterial = mat;*/

            CanAppearInEliteTiers = VanillaTier2();

            CreatePulsePrefab();
            CreateEliteEquipment();
            CreateLang();
            CreateElite();
            Hooks();
        }

        private void CreatePulsePrefab()
        {
            pulsePrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/networkedobjects/teleporterhealnovapulse").InstantiateClone("LeechingHealNovaPulse", true);
            LeechingHealingPulseComponent LHP = pulsePrefab.AddComponent<LeechingHealingPulseComponent>();
            Assets.entityStates.Add(typeof(LeechingHealNovaPulse));

            Transform PP = pulsePrefab.transform.Find("PP");
            if (PP == null)
            {
                Debug.Log("No PP found");
                PostProcessDuration ppd = pulsePrefab.GetComponentInChildren<PostProcessDuration>();
                if(ppd != null)
                {
                    PP = ppd.transform;
                }
            }
            if (PP != null)
            {
                GameObject.Destroy(PP.gameObject);
            }
            else
            {
            }
        }

        private void LeechingOnHit(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (damageInfo.attacker && victim && NetworkServer.active && damageInfo.procCoefficient > 0)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody vBody = victim.GetComponent<CharacterBody>();

                if (aBody && vBody)
                {
                    if (IsElite(aBody))
                    {
                        CreatePulse(aBody, damageInfo);
                    }
                }
            }

            orig(self, damageInfo, victim);
        }
        protected void CreatePulse(CharacterBody body, DamageInfo damageInfo)
        {
            TeamIndex team = body.teamComponent.teamIndex;
            Transform transform = damageInfo.attacker.transform;

            GameObject gameObject = UnityEngine.Object.Instantiate(pulsePrefab, transform.position, transform.rotation);
            //GameObject gameObject = UnityEngine.Object.Instantiate(TeleporterHealNovaGeneratorMain.pulsePrefab, transform.position, transform.rotation);
            gameObject.GetComponent<TeamFilter>().teamIndex = team;

            LeechingHealingPulseComponent LHP = gameObject.GetComponent<LeechingHealingPulseComponent>();
            if(LHP == null)
            {
                Debug.Log("LHP null!");
                LHP = gameObject.AddComponent<LeechingHealingPulseComponent>();
            }
            LHP.procCoefficient = damageInfo.procCoefficient;
            LHP.maxHealth = body.healthComponent.fullCombinedHealth;
            LHP.bodyRadius = body.radius;

            NetworkServer.Spawn(gameObject);
        }

        private void LeechingHealingPulseIntercept(On.EntityStates.TeleporterHealNovaController.TeleporterHealNovaWindup.orig_OnEnter orig, TeleporterHealNovaWindup self)
        {
            orig(self);
            LeechingHealingPulseComponent lhcp = self.gameObject.GetComponent<LeechingHealingPulseComponent>();
            if (lhcp)
            {
                //EffectManager.SimpleEffect(TeleporterHealNovaWindup.chargeEffectPrefab, self.transform.position, Quaternion.identity, false);
                if (self.isAuthority)
                {
                    self.outer.SetNextState(new LeechingHealNovaPulse() 
                    { 
                        leechingHealingPulseComponent = lhcp
                    });
                }
            }
        }

        private void LeechingHealingPulse(On.EntityStates.TeleporterHealNovaController.TeleporterHealNovaPulse.orig_OnEnter orig, TeleporterHealNovaPulse self)
        {
            orig(self);
            LeechingHealingPulseComponent LHP = self.gameObject.GetComponent<LeechingHealingPulseComponent>();
            if (LHP != null)
            {
                self.radius = healPulseRadius;
                self.healPulse.finalRadius = healPulseRadius;
                self.healPulse.healFractionValue = 0;

                TeamFilter teamFilter = self.GetComponent<TeamFilter>();
                TeamIndex teamIndex = teamFilter ? teamFilter.teamIndex : TeamIndex.None;
                float healMax = LHP.maxHealth * maxHealFraction;
                float procCoeff = LHP.procCoefficient;

                SphereSearch sphereSearch = new SphereSearch
                {
                    mask = LayerIndex.entityPrecise.mask,
                    origin = self.transform.position,
                    queryTriggerInteraction = QueryTriggerInteraction.Collide,
                    radius = healPulseRadius
                };
                TeamMask teamMask = default(TeamMask);
                List<HurtBox> hurtBoxesList = new List<HurtBox>();
                List<HealthComponent> healedTargets = new List<HealthComponent>();

                teamMask.AddTeam(teamIndex);
                sphereSearch.RefreshCandidates().FilterCandidatesByHurtBoxTeam(teamMask).FilterCandidatesByDistinctHurtBoxEntities().GetHurtBoxes(hurtBoxesList);

                for (int i = 0; i < hurtBoxesList.Count; i++)
                {
                    HealthComponent healthComponent = hurtBoxesList[i].healthComponent;
                    if (!healedTargets.Contains(healthComponent))
                    {
                        healedTargets.Add(healthComponent);
                        if (!IsElite(healthComponent.body))
                        {
                            float baseHeal = healthComponent.fullHealth * healFraction;

                            float endHeal = Mathf.Max(baseHeal, healMax) * procCoeff;

                            healthComponent.Heal(endHeal, default(ProcChainMask));
                        }
                    }
                }
            }
        }

        void AssignEliteTier()
        {
            foreach (CombatDirector.EliteTierDef etd in CombatDirector.eliteTiers)
            {
                EliteDef[] eliteTypes = new EliteDef[] { RoR2Content.Elites.Poison, RoR2Content.Elites.Haunted };

                if (etd.eliteTypes == eliteTypes)
                {
                    CanAppearInEliteTiers = new EliteTierDef[1] { etd };
                }
            }
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            return false;
        }
    }

    public class AffixSerpentineBehavior : CharacterBody.ItemBehavior
    {
        private const float scaleMultiplier = 1.9f;
        private GameObject affixLeechingWard;
        private void FixedUpdate()
        {
            if (!NetworkServer.active)
            {
                return;
            }
            bool flag = this.stack > 0;
            if (this.affixLeechingWard != flag)
            {
                if (flag)
                {
                    affixLeechingWard = Instantiate<GameObject>(LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/NearbyDamageBonusIndicator"), body.transform);
                    affixLeechingWard.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject);
                    affixLeechingWard.transform.Find("Radius, Spherical").localScale = Vector3.one * (LeechingHealNovaPulse.baseRadius + body.radius) * scaleMultiplier;
                    return;
                }
                UnityEngine.Object.Destroy(this.affixLeechingWard);
                this.affixLeechingWard = null;
            }
        }

        private void OnDisable()
        {
            if (this.affixLeechingWard)
            {
                UnityEngine.Object.Destroy(this.affixLeechingWard);
            }
        }

    }
}
