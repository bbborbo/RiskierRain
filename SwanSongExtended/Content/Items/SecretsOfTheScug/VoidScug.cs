using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;

namespace SwanSongExtended.Items
{
    class VoidScug : ItemBase<VoidScug>
    {
        public static GameObject scugNovaEffectPrefab = Resources.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef scugBuff;
        public static float radiusBase = 28;
        public static float durationBase = 6;
        public static float durationStack = 3;
        public static float damageBase = 3;
        public static float damageStack = .5f;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Curious Scug";

        public override string ItemLangTokenName => "VOIDSCUG";

        public override string ItemPickupDesc => "Chill nearby enemies when hit. Recharges outside of danger.";

        public override string ItemFullDescription => $"When hit, <style=cIsUtility>Chill</style> all enemies within " +
            $"<style=cIsUtility>{radiusBase}m</style> for " +
            $"<style=cIsUtility>{durationBase}</style> <style=cStack>(+{durationStack} per stack)</style> seconds. " +
            $"Recharges outside of danger.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility};

        public override GameObject ItemModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlScug.prefab");
        public override Sprite ItemIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_VOIDSCUG.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            scugBuff = Content.CreateAndAddBuff(
                "bdScugBurstReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ElementalRings/texBuffElementalRingsReadyIcon.tif").WaitForCompletion(),
                new Color(0.9f, 0.8f, 0.0f),
                true, false);
            scugBuff.isHidden = true;
            base.Init();
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += ScugTakeDamage;
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
        private void ScugTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, RoR2.HealthComponent self, RoR2.DamageInfo damageInfo)
        {
            CharacterBody body = self.body;
            int scugItemCount = GetCount(body);
            int scugBuffCount = body.GetBuffCount(scugBuff);
            orig(self, damageInfo);

            if (scugItemCount <= 0 || body.GetBuffCount(scugBuff) <= 0)
                return;//return if no scug item or no scug buff
            if (body.GetBuffCount(scugBuff) >= 2)
            {
                ScugBlast(body, scugItemCount);
                return;//blast if more than 1 buff
            }
            if (self.combinedHealthFraction <= .5f)
            {
                ScugBlast(body, scugItemCount);
                //blast if 1 buff AND under 50% hp
            }

        }

        void ScugBlast(CharacterBody body, int itemCount)
        {
            EffectManager.SpawnEffect(scugNovaEffectPrefab, new EffectData
            {
                origin = body.transform.position,
                scale = radiusBase
            }, true);
            ChillRework.ChillRework.ApplyChillSphere(body.corePosition, radiusBase, body.teamComponent.teamIndex, durationBase + durationStack * (itemCount - 1));
            BlastAttack scugNova = new BlastAttack()
            {
                baseDamage = body.damage * (damageBase + damageStack * (itemCount - 1)),
                radius = radiusBase,
                procCoefficient = 0f,
                position = body.transform.position,
                attacker = body.gameObject,
                baseForce = 900,
                crit = Util.CheckRoll(body.crit, body.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.Generic,
                teamIndex = TeamComponent.GetObjectTeam(body.gameObject)
            };
            scugNova.Fire();
            body.RemoveBuff(scugBuff);
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
        private void SetProvidingBuff(bool outOfDanger)
        {
            if (outOfDanger)
            {
                body.SetBuffCount(VoidScug.scugBuff.buffIndex, 2);
            }
        }

    }
}
