using BepInEx.Configuration;
using EntityStates;
using EntityStates.GrandParent;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using SwanSongExtended.Items;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Changes
{
    public class TpSunula : ReworkBase<TpSunula>
    {
        [AutoConfig("Sun Duration Base", 10)]
        public static float sunDurationBase = 10;
        [AutoConfig("Sun Duration Stack", 5)]
        public static float sunDurationStack = 5;
        [AutoConfig("Burn Duration Friendly", "Vanilla is 5 regardless of team", 2)]
        public static float burnDurationFriendly = 2;
        [AutoConfig("Burn Duration Enemy", "Vanilla is 5 regardless of team", 7.5f)]
        public static float burnDurationEnemy = 7.5f;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_ParentEgg.ParentEgg_asset;

        public override string ItemName => "Planula";

        public override string ItemPickupDesc => "THE UNMATCHED POWER OF THE SUN!";

        public override string ItemFullDesc =>
            $"After beginning the teleporter event, " +
            $"{DamageColor("summon a sun overhead")} that lasts for " +
            $"{DamageColor(sunDurationBase.ToString())} seconds {StackText($"+{sunDurationStack}")}. " +
            $"{HealthColor("All enemies and allies burn near the sun")}.";

        public override void Hooks()
        {
            IL.RoR2.GrandParentSunController.ServerFixedUpdate += FriendlySunulaDamage;
            IL.RoR2.HealthComponent.TakeDamageProcess += FuckPlanula;
            On.RoR2.TeleporterInteraction.IdleToChargingState.OnEnter += OnTeleporterEventPreStart;
            On.RoR2.TeleporterInteraction.ChargingState.OnEnter += OnTeleporterEventStart;
            On.RoR2.TeleporterInteraction.ChargingState.FixedUpdate += OnTeleporterEventUpdate;
        }

        private void FriendlySunulaDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int targetBodyLoc = -1;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out targetBodyLoc),
                x => x.MatchStfld<RoR2.OverheatSystem.OverheatInfo>(nameof(OverheatSystem.OverheatInfo.targetBody)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchStfld<RoR2.OverheatSystem.OverheatInfo>(nameof(OverheatSystem.OverheatInfo.burnTotalDamageScale))
                );
            if(b1 == false)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FriendlySunulaDamage));
                return;
            }
            c.Emit(OpCodes.Ldarg, 0);
            c.Emit(OpCodes.Ldloc, targetBodyLoc);
            c.EmitDelegate<Func<float, GrandParentSunController, CharacterBody, float>>((durationIn, self, targetBody) =>
            {
                PlanulaSunController planula = PlanulaSunController.GetPlanulaSunControllerFromSun(self);
                if (planula == null)
                    return durationIn;
                TeamIndex targetTeam = targetBody.teamComponent?.teamIndex ?? TeamIndex.Neutral;
                if (planula.activatorTeam == targetTeam)
                    return burnDurationFriendly;
                return burnDurationEnemy;
            });
        }

        private void FuckPlanula(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.HealthComponent/ItemCounts", "parentEgg")
                );
            if (!b1)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(FuckPlanula));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        private void OnTeleporterEventPreStart(On.RoR2.TeleporterInteraction.IdleToChargingState.orig_OnEnter orig, RoR2.TeleporterInteraction.IdleToChargingState self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
            int sunulaCount = Util.GetItemCountForTeam(TeamIndex.Player, RoR2Content.Items.ParentEgg.itemIndex, false, false);
            if (sunulaCount > 0)
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

        private void OnTeleporterEventStart(On.RoR2.TeleporterInteraction.ChargingState.orig_OnEnter orig, RoR2.TeleporterInteraction.ChargingState self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
            PlanulaSunController sunController = self.gameObject.GetComponent<PlanulaSunController>();
            if (sunController != null)
            {
                sunController.EndBeamEffect();
                sunController.CreateSun();
            }
        }

        private void OnTeleporterEventUpdate(On.RoR2.TeleporterInteraction.ChargingState.orig_FixedUpdate orig, RoR2.TeleporterInteraction.ChargingState self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
        }
    }

    public class PlanulaSunController : MonoBehaviour
    {
        public static PlanulaSunController GetPlanulaSunControllerFromSun(GrandParentSunController sun)
        {
            if (sun.ownership == null || sun.ownership.ownerObject == null)
                return null;
            if (PlanulaSunController.readOnlyInstancesList == null || PlanulaSunController.readOnlyInstancesList.Count <= 0)
                return null;
            foreach(PlanulaSunController planula in readOnlyInstancesList)
            {
                if (sun.gameObject == planula.sunInstance)
                    return planula;
            }
            return null;
        }
        private static List<PlanulaSunController> instancesList = new List<PlanulaSunController>();
        public static ReadOnlyCollection<PlanulaSunController> readOnlyInstancesList = new ReadOnlyCollection<PlanulaSunController>(instancesList);
        public TeamIndex activatorTeam;
        public GameObject activator;
        public CharacterMaster master;
        public HoldoutZoneController holdoutZoneController;
        Vector3 sunSpawnPosition;
        ParticleSystem beamEffectInstance;
        GameObject sunInstance;
        float sunDuration;
        float sunAge = 0;

        void OnEnable()
        {
            instancesList.Add(this);
            if (activator != null && activator.TryGetComponent(out master))
                activatorTeam = master.teamIndex;
        }
        void OnDisable()
        {
            if (instancesList.Contains(this))
                instancesList.Remove(this);
        }

        public void SetSunDuration(int stack)
        {
            sunDuration = ItemBase.GetStackValue(TpSunula.sunDurationBase, TpSunula.sunDurationStack, stack);
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
            if (NetworkServer.active && sunInstance != null)
            {
                sunAge += Time.fixedDeltaTime;
                if (sunAge >= sunDuration)
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
