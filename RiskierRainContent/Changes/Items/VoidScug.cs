using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RiskierRainContent.CoreModules;
using RoR2;
using RoR2.ExpansionManagement;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Items
{
    class VoidScug : ItemBase<VoidScug>
    {
        public static GameObject scugNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef scugBuff;
        public static float radiusBase = 16;
        public static float radiusStack = 4;

        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Cautious scug";

        public override string ItemLangTokenName => "VOIDSCUG";

        public override string ItemPickupDesc => "Chill enemies when hit. Recharges when out of danger.";

        public override string ItemFullDescription => "fucking     SCUUUUUUUUUUUUUGGGGG";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlScug.prefab");
        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_VOIDSCUG.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += ScugTakeDamage;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }
        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    ScugBehavior scugBehavior = self.AddItemBehavior<ScugBehavior>(GetCount(self));
                }
            }
        }
        private void ScugTakeDamage(On.RoR2.HealthComponent.orig_TakeDamage orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            CharacterBody body = self.body;
            int scugItemCount = GetCount(body);
            orig(self, damageInfo);

            if (scugItemCount > 0 && body.HasBuff(scugBuff))
            {
                ScugBlast(body, scugItemCount);
            }

        }
        void ScugBlast(CharacterBody body, int itemCount)
        {
            float currentRadius = radiusBase + radiusStack * (itemCount - 1);

            EffectManager.SpawnEffect(scugNovaEffectPrefab, new EffectData
            {
                origin = body.transform.position,
                scale = currentRadius
            }, true);
            ApplyChillSphere(body, itemCount);
            BlastAttack scugNova = new BlastAttack()
            {
                baseDamage = body.damage,
                radius = currentRadius,
                procCoefficient = 0.5f,
                position = body.transform.position,
                attacker = body.gameObject,
                crit = Util.CheckRoll(body.crit, body.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.Generic,
                teamIndex = TeamComponent.GetObjectTeam(body.gameObject)
            };
            scugNova.Fire();
        }
        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            CreateBuff();
            Hooks();
        }
        private void CreateBuff()
        {
            scugBuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                scugBuff.name = "ScugBuff";
                scugBuff.iconSprite = LegacyResourcesAPI.Load<Sprite>("texbuffelementalringsreadyicon");
                scugBuff.buffColor = new Color(0.9f, 0.8f, 0.0f);
                scugBuff.canStack = false;
                scugBuff.isDebuff = false;
                scugBuff.isHidden = true;
            };
            Assets.buffDefs.Add(scugBuff);
        }

        static void ApplyChillSphere(CharacterBody body, int itemCount)
        {
            Vector3 corePosition = body.corePosition;
            scugSphereSearch.origin = corePosition;
            scugSphereSearch.mask = LayerIndex.entityPrecise.mask;
            scugSphereSearch.radius = radiusBase + radiusStack * (itemCount - 1);
            scugSphereSearch.RefreshCandidates();
            scugSphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(body.teamComponent.teamIndex));
            scugSphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
            scugSphereSearch.OrderCandidatesByDistance();
            scugSphereSearch.GetHurtBoxes(scugOnKillHurtBoxBuffer);
            scugSphereSearch.ClearCandidates();
            
            for (int i = 0; i < scugOnKillHurtBoxBuffer.Count; i++)
            {
                HurtBox hurtBox = scugOnKillHurtBoxBuffer[i];
                if (hurtBox.healthComponent)
                {
                    hurtBox.healthComponent.body.AddTimedBuff(RoR2Content.Buffs.Slow80, 10);
                }
            }
            scugOnKillHurtBoxBuffer.Clear();
        }



        private static readonly SphereSearch scugSphereSearch = new SphereSearch();
        private static readonly List<HurtBox> scugOnKillHurtBoxBuffer = new List<HurtBox>();

        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.HealWhileSafe, //consumes slug
                itemDef2 = VoidScug.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }

    }
    class ScugBehavior : CharacterBody.ItemBehavior
    {
        
        private void FixedUpdate()
        {
            this.SetProvidingBuff(body.outOfDanger);
        }
        private void SetProvidingBuff(bool shouldProvideBuff)
        {
            if (shouldProvideBuff == providingBuff)
            {
                return;
            }
            providingBuff = shouldProvideBuff;
            if (providingBuff)
            {
                body.AddBuff(VoidScug.scugBuff);
                return;
            }
            body.RemoveBuff(VoidScug.scugBuff);
        }
        private bool providingBuff;
    }
}
