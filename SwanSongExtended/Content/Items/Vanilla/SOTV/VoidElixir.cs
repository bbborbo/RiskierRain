using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SwanSongExtended.Components;
using static SwanSongExtended.Modules.Language.Styling;
using UnityEngine.AddressableAssets;
using RainrotSharedUtils.Components;
using UnityEngine.Networking;
using RoR2.ExpansionManagement;

namespace SwanSongExtended.Items
{
    class VoidElixir : ItemBase<VoidElixir>
    {
        public override ExpansionDef RequiredExpansion => SotvExpansionDef();
        public static GameObject infernoAuraPrefab;
        public override bool isEnabled => false;

        [AutoConfig("Inferno Duration", 20f)]
        public static float infernoDuration = 20f;
        [AutoConfig("Inferno Radius Base", 25f)]
        public static float infernoRadiusBase = 25f;
        [AutoConfig("Inferno Radius Stack", 5f)]
        public static float infernoRadiusStack = 5f;

        [AutoConfig("Inferno Tick Interval", 0.5f)]
        public static float infernoTickInterval = 0.5f;
        [AutoConfig("Inferno Tick Damage Coefficient", 1f)]
        public static float infernoTickDamageCoeff = 1f;
        [AutoConfig("Inferno Ignite Damage Coefficient", 2f)]
        public static float infernoIgniteDamageCoeff = 2f;
        [AutoConfig("Inferno Ignite Duration", 3f)]
        public static float infernoIgniteDuration = 3f;

        [AutoConfig("Consumed Regen Bonus", 2f)]
        public static float regenBuff = 2f;
        [AutoConfig("Consumed Armor Bonus", 8)]
        public static int armorBuff = 8;
        public override string ItemName => "Hadal Brine or whatevber";

        public override string ItemLangTokenName => "INFERNOPOTION";

        public override string ItemPickupDesc => "At low health, summon a ring of fire. Usable once per stage.";

        public override string ItemFullDescription =>
            $"Taking damage to below " +
            $"{RedText(Tools.ConvertDecimal(0.25f) + " health")} " +
            $"{UtilityColor("consumes")} this item, " +
            $"summoning a ring of fire for {infernoDuration} seconds, " +
            $"which continuously ignites enemies within {infernoRadiusBase}m (+Z per stack). " +
            $"Each empty bottle increases {HealingColor("armor")} by {HealingColor(armorBuff.ToString())} " +
            $"and {HealingColor("base health regeneration")} by {HealingColor(regenBuff.ToString() + " hp/s")}. " +
            $"Regenerates at the start of each stage.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Damage, ItemTag.LowHealth, ItemTag.OnStageBeginEffect };

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.UpdateLastHitTime += ElixirHook;
        }
        private void ElixirHook(On.RoR2.HealthComponent.orig_UpdateLastHitTime orig, RoR2.HealthComponent self,
           float damageValue, Vector3 damagePosition, bool damageIsSilent, GameObject attacker,
           bool delayedDamage, bool firstHitOfDelayedDamage)
        {
            CharacterBody body = self.body;
            if (NetworkServer.active && body && damageValue > 0f)
            {
                int count = GetCount(body);
                if (count > 0 && self.isHealthLow)
                {
                    Util.CleanseBody(body, true, false, true, true, true, true);

                    TransformPotions(count, body);

                    EffectData effectData = new EffectData
                    {
                        origin = self.transform.position
                    };
                    effectData.SetNetworkedObjectReference(self.gameObject);
                    EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/HealingPotionEffect"), effectData, true);
                }
            }
            orig(self, damageValue, damagePosition, damageIsSilent, attacker, delayedDamage, firstHitOfDelayedDamage);
        }

        private void TransformPotions(int count, CharacterBody body)
        {
            Inventory inv = body.inventory;
            inv.RemoveItem(instance.ItemsDef, count);
            inv.GiveItem(Elixir2Consumed.instance.ItemsDef, count);

            CharacterMasterNotificationQueue.SendTransformNotification(
                body.master, instance.ItemsDef.itemIndex,
                VoidElixirConsumed.instance.ItemsDef.itemIndex,
                CharacterMasterNotificationQueue.TransformationType.Default);
        }

        public override void Init()
        {
            CreateInfernoAura();

            base.Init();
        }

        private static void CreateInfernoAura()
        {
            infernoAuraPrefab = Addressables.LoadAssetAsync<GameObject>("RoR2/Base/Icicle/IcicleAura.prefab").WaitForCompletion().InstantiateClone("InfernoPotionAura", true);
            Content.AddNetworkedObjectPrefab(infernoAuraPrefab);

            DotWard dotWard = null;
            BuffWard buffWard = infernoAuraPrefab.GetComponent<BuffWard>();
            if (buffWard)
            {
                UnityEngine.Object.Destroy(buffWard);
                dotWard = infernoAuraPrefab.AddComponent<DotWard>();
                //NEED TO SET OWNER (?)
                dotWard.dotIndex = DotController.DotIndex.Burn;
                dotWard.damageCoefficient = infernoIgniteDamageCoeff;

                dotWard.rangeIndicator = null;//determined by aura controller
                dotWard.radius = 0; //determined by aura controller
                dotWard.expireDuration = 0; //determined by aura controller
                dotWard.interval = infernoTickInterval; //determined by aura controller
                dotWard.buffDuration = infernoIgniteDuration;
                dotWard.buffTimer = infernoTickInterval;
                dotWard.shape = BuffWard.BuffWardShape.Sphere;
                dotWard.invertTeamFilter = true;
                dotWard.requireGrounded = false;
            }

            IcicleAuraController auraController = infernoAuraPrefab.GetComponent<IcicleAuraController>();
            if (auraController)
            {
                auraController.icicleBaseRadius = infernoRadiusBase;
                auraController.icicleRadiusPerIcicle = infernoRadiusStack;
                auraController.icicleDuration = infernoDuration;
                auraController.baseIcicleMax = 1;
                auraController.icicleMaxPerStack = 0;
                auraController.buffWard = dotWard;
            }

            //particle effects
        }
    }
}
