using BepInEx.Configuration;
using EntityStates;
using EntityStates.GrandParent;
using R2API;
using RoR2;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Items
{
    class Planula : ItemBase<Planula>
    {
        public override string ConfigName => "Reworks : Planula";
        [AutoConfig("Sun Duration Base", 10f)]
        public static float sunDurationBase = 10;
        [AutoConfig("Sun Duration Base", 5f)]
        public static float sunDurationStack = 5;

        public override AssetBundle assetBundle => null;
        public override string ItemName => "Planula";

        public override string ItemLangTokenName => "TPSUNULA";

        public override string ItemPickupDesc => "Summon a sun during the teleporter event.";

        public override string ItemFullDescription => $"After beginning the teleporter event, " +
            $"{DamageColor("summon a sun overhead")} that lasts for " +
            $"{DamageColor(sunDurationBase.ToString())} seconds {StackText($"+{sunDurationStack}")}. " +
            $"{HealthColor("All enemies and allies burn near the sun")}.";

        public override string ItemLore => "Placeholder";

        public override ItemTier Tier => ItemTier.Boss;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.AIBlacklist, ItemTag.Damage, ItemTag.HoldoutZoneRelated };

        public override GameObject ItemModel => Addressables.LoadAssetAsync<GameObject>("RoR2/Base/ParentEgg/PickupParentEgg.prefab").WaitForCompletion();
        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>("RoR2/Base/ParentEgg/texParentEggIcon.png").WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Init()
        {
            SwanSongPlugin.RetierItem(nameof(RoR2Content.Items.ParentEgg));
            base.Init();
        }
        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.ItemsDef, RoR2Content.Items.ParentEgg);

            On.RoR2.TeleporterInteraction.IdleToChargingState.OnEnter += OnTeleporterEventPreStart;
            On.RoR2.TeleporterInteraction.ChargingState.OnEnter += OnTeleporterEventStart;
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += OnTeleporterEventUpdate;
        }

        private void OnTeleporterEventPreStart(On.RoR2.TeleporterInteraction.IdleToChargingState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            int sunulaCount = Util.GetItemCountForTeam(TeamIndex.Player, ItemsDef.itemIndex, false, false);
            if(sunulaCount > 0)
            {
                TeleporterInteraction tpInteraction = (self as TeleporterInteraction.BaseTeleporterState).teleporterInteraction;
                GameObject activator = tpInteraction.chargeActivatorServer;
                PlanulaSunController sunController = tpInteraction.gameObject.AddComponent<PlanulaSunController>();

                Debug.Log(sunController != null);
                if (sunController != null)
                {
                    sunController.holdoutZoneController = tpInteraction.holdoutZoneController;
                    Transform effectOrigin = self.transform.Find("FireworkOrigin");
                    if (effectOrigin == null)
                        effectOrigin = tpInteraction.transform;
                    sunController.activator = activator;
                    sunController.CreateBeamEffect(effectOrigin);
                    sunController.SetSunDuration(sunulaCount);
                }
            }
        }

        private void OnTeleporterEventStart(On.RoR2.TeleporterInteraction.ChargingState.orig_OnEnter orig, BaseState self)
        {
            orig(self);
            PlanulaSunController sunController = self.gameObject.GetComponent<PlanulaSunController>();
            if (sunController != null)
            {
                sunController.EndBeamEffect();
                sunController.CreateSun();
            }
        }

        private void OnTeleporterEventUpdate(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, BaseState self)
        {
            orig(self);
        }
    }

    public class PlanulaSunController : MonoBehaviour
    {
        public GameObject activator;
        public HoldoutZoneController holdoutZoneController;
        Vector3 sunSpawnPosition;
        ParticleSystem beamEffectInstance;
        GameObject sunInstance;
        float sunDuration;
        float sunAge = 0;

        public void SetSunDuration(int stack)
        {
            sunDuration = ItemBase.GetStackValue(Planula.sunDurationBase, Planula.sunDurationStack, stack);
        }
        public void CreateBeamEffect(Transform parent)
        {
            if (parent)
            {
                sunSpawnPosition = FindSunSpawnPosition(parent.position);

                ChildLocator component = UnityEngine.Object.Instantiate<GameObject>(ChannelSunStart.beamVfxPrefab, parent).GetComponent<ChildLocator>();
                component.FindChild("EndPoint").SetPositionAndRotation(sunSpawnPosition, Quaternion.identity);
                Transform transform2 = component.FindChild("BeamParticles");
                beamEffectInstance = transform2.GetComponent<ParticleSystem>();
                return;
            }
            beamEffectInstance = null;
        }

        public void EndBeamEffect()
        {
            if (beamEffectInstance != null)
            {
                ParticleSystem.MainModule main = beamEffectInstance.main;
                main.loop = false;
                beamEffectInstance.Stop();
            }
        }
        public static Vector3 FindSunSpawnPosition(Vector3 searchOrigin)
        {
            Vector3 vector = searchOrigin;
            if (vector != null)
            {
                float num = ChannelSun.sunPlacementIdealAltitudeBonus;
                float num2 = ChannelSun.sunPrefabDiameter * 0.5f;
                RaycastHit raycastHit;
                if (Physics.Raycast(vector, Vector3.up, out raycastHit, num + num2, LayerIndex.world.mask, QueryTriggerInteraction.Ignore))
                {
                    num = Mathf.Clamp(raycastHit.distance - num2, 0f, num);
                }
                vector.y += num;
                return vector;
            }
            return vector;
        }

        void FixedUpdate()
        {
            if(NetworkServer.active && sunInstance != null)
            {
                sunAge += Time.fixedDeltaTime;
                if(sunAge >= sunDuration)
                {
                    DestroySun();
                }
            }
        }

        public void CreateSun()
        {
            if (NetworkServer.active)
            {
                sunInstance = UnityEngine.Object.Instantiate<GameObject>(ChannelSun.sunPrefab, sunSpawnPosition, Quaternion.identity);
                sunInstance.GetComponent<GenericOwnership>().ownerObject = activator;
                NetworkServer.Spawn(sunInstance);
            }
        }

        private void DestroySun()
        {
            if (this.sunInstance)
            {
                UnityEngine.Object.Destroy(this.sunInstance);
                this.sunInstance = null;
            }
        }
    }
}
