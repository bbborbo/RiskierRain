using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RainrotSharedUtils
{
    public static partial class Extensions
    {
        public static float GetStageStopwatch(this Run instance, out bool isFirstStage)
        {
            isFirstStage = true;
            if (instance == null)
                return 0;
            isFirstStage = instance.stageClearCount == 0;
            float stopwatch = instance.GetRunStopwatch();
            float entryStopwatchValue = 0;
            if (Stage.instance)
                entryStopwatchValue = Mathf.Floor(Stage.instance.entryStopwatchValue);
            return stopwatch - entryStopwatchValue;
        }
        public static bool IsPot(this CharacterBody self)
        {
            return self.bodyIndex == BodyCatalog.FindBodyIndex("ExplosivePotBody");
        }
        public static bool IsInvincible(this CharacterBody self)
        {
            return
                self.HasBuff(RoR2Content.Buffs.HiddenInvincibility)
                || self.HasBuff(RoR2Content.Buffs.Immune)
                || self.HasBuff(RoR2Content.Buffs.Intangible)
                || self.HasBuff(DLC2Content.Buffs.HiddenRejectAllDamage)
                ;
        }
        public static bool IsInvincibleOrInvisible(this CharacterBody self)
        {
            return self.IsInvincible()
                || self.IsInvisible()
                ;
        }
        public static bool IsInvisible(this CharacterBody self)
        {
            return self.HasBuff(RoR2Content.Buffs.Cloak)
                || (self.TryGetComponent(out CharacterModel model) == true && model.invisibilityCount > 0)
                ;
        }
        /// <summary>
        /// if true, this interactable can trigger fireworks, squolyps, and your custom on-interaction effect
        /// </summary>
        /// <param name="disallowPickups">set to false to allow non-duplicated items to trigger your thing</param>
        /// <returns></returns>
        public static bool InteractableIsPermittedForSpawn(this GameObject interactableObject, bool disallowPickups = true)
        {
            if (interactableObject == null)
                return false;

            if (interactableObject.TryGetComponent(out InteractionProcFilter interactionProcFilter))
            {
                return interactionProcFilter.shouldAllowOnInteractionBeginProc;
            }
            if (interactableObject.TryGetComponent(out PurchaseInteraction purchaseInteraction))
            {
                return purchaseInteraction.disableSpawnOnInteraction == false;
            }
            if (interactableObject.TryGetComponent(out DelusionChestController delusionController))
            {
                if (interactableObject.TryGetComponent(out PickupPickerController pickupPickerController) && pickupPickerController.enabled)
                {
                    return false;
                }
                return true;
            }
            if ((bool)interactableObject.TryGetComponent(out GenericPickupController pickupController))
            {
                return disallowPickups || pickupController.Duplicated;
            }
            if ((bool)interactableObject.GetComponent<VehicleSeat>())
            {
                return false;
            }
            if ((bool)interactableObject.GetComponent<NetworkUIPromptController>())
            {
                return false;
            }
            if (interactableObject.TryGetComponent(out PowerPedestal powerPedestal))
            {
                return powerPedestal.CanTriggerFireworks;
            }
            if (interactableObject.TryGetComponent(out AccessCodesNodeController accessNodeController))
            {
                return accessNodeController.CheckInteractionOrder();
            }
            return true;
        }
        public static GameObject FixItemModel(this GameObject prefab)
        {
            if (prefab == null)
                prefab = Resources.Load<GameObject>("prefabs/NullModel");

            ModelPanelParameters parameters = prefab.AddComponent<ModelPanelParameters>();

            parameters.minDistance = 1;
            parameters.maxDistance = 15;

            Transform t = prefab.transform.Find("FocusPos");
            if (t == null)
            {
                GameObject focusPoint = new GameObject("FocusPos");
                t = focusPoint.transform;
                t.parent = prefab.transform;
                t.localPosition = Vector3.zero;
            }
            parameters.focusPointTransform = t;

            Transform c = prefab.transform.Find("CameraPos");
            if (c == null)
            {
                GameObject cameraPos = new GameObject("CameraPos");
                c = cameraPos.transform;
                c.parent = prefab.transform;
                c.SetPositionAndRotation(t.position + Vector3.forward * -7 + Vector3.right * -1, c.rotation);
            }
            parameters.cameraPositionTransform = c;

            return prefab;
        }
        public static Sprite AsSprite(this Texture2D tex)
        {
            return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
        }
    }
}
