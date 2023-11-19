using BepInEx.Configuration;
using EntityStates.CaptainSupplyDrop;
using On.RoR2.Items;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRainContent.Equipment
{
    class ExeCardRework : EquipmentBase<ExeCardRework>
    {
        public override string EquipmentName => "Executive Card";

        public override string EquipmentLangTokenName => throw new NotImplementedException();

        public override string EquipmentPickupDesc => throw new NotImplementedException();

        public override string EquipmentFullDescription => throw new NotImplementedException();

        public override string EquipmentLore => throw new NotImplementedException();

        public override GameObject EquipmentModel => throw new NotImplementedException();

        public override Sprite EquipmentIcon => throw new NotImplementedException();
        public override float Cooldown { get; } = 75f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.Items.MultiShopCardUtils.OnPurchase += FreezeCard;
            On.RoR2.EquipmentSlot.UpdateTargets += CardTargetInteractables;
        }

        private void CardTargetInteractables(On.RoR2.EquipmentSlot.orig_UpdateTargets orig, EquipmentSlot self, EquipmentIndex targetingEquipmentIndex, bool userShouldAnticipateTarget)
        {
            bool isGuillotine = targetingEquipmentIndex == ExeCardRework.instance.EquipDef.equipmentIndex;
            if (!isGuillotine)
            {
                orig(self, targetingEquipmentIndex, userShouldAnticipateTarget);
                return;
            }

            float maxAngle = 15;
            float maxDistance = 60;
            float camAdjust;
            Ray aim = CameraRigController.ModifyAimRayIfApplicable(self.GetAimRay(), self.characterBody.gameObject, out camAdjust);
            Collider[] results = Physics.OverlapSphere(aim.origin, maxDistance + camAdjust, Physics.AllLayers, QueryTriggerInteraction.Collide);
            var minDot = Mathf.Cos(Mathf.Clamp(maxAngle, 0f, 180f) * Mathf.PI / 180f);

            bool validTarget = false;
            PurchaseInteraction targetPurchase = null;
            foreach (Collider collider in results)
            {
                var vdot = Vector3.Dot(aim.direction, (collider.transform.position - aim.origin).normalized);
                if (vdot < minDot) 
                    continue;

                PurchaseInteraction component = collider.GetComponent<EntityLocator>().entity.GetComponent<PurchaseInteraction>();
                if (component)
                {
                    //using hack beacon criteria
                    validTarget = HackingMainState.PurchaseInteractionIsValidTarget(component);

                    if (validTarget)
                    {
                        validTarget = true;
                        targetPurchase = component;
                        break;
                    }
                    else if (targetPurchase == null)
                    {
                        targetPurchase = component;
                    }
                }
            }

            if(targetPurchase != null)
            {
                if (validTarget)
                {
                    //valid indicator
                    self.targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/RecyclerIndicator");
                }
                else
                {
                    //invalid indicator
                    self.targetIndicator.visualizerPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/RecyclerBadIndicator");
                }

                self.currentTarget = new EquipmentSlot.UserTargetInfo
                {
                    transformToIndicateAt = targetPurchase.gameObject.transform,
                    pickupController = null,
                    hurtBox = null,
                    rootObject = targetPurchase.gameObject,
                };
                self.targetIndicator.active = true;
                self.targetIndicator.targetTransform = self.currentTarget.transformToIndicateAt;
            }

        }

        private void FreezeCard(MultiShopCardUtils.orig_OnPurchase orig, CostTypeDef.PayCostContext context, int moneyCost)
        {
            // :)
        }

        public override void Init(ConfigFile config)
        {
            ExeCardRework.instance.EquipDef = Addressables.LoadAssetAsync<EquipmentDef>("RoR2/DLC1/MultiShopCard/MultiShopCard.asset").WaitForCompletion();
            ExeCardRework.instance.EquipDef.cooldown = Cooldown;

            On.RoR2.EquipmentSlot.PerformEquipmentAction += PerformEquipmentAction;
            Hooks();
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            bool b = false;
            GameObject targetObject = slot.currentTarget.rootObject;
            if(targetObject != null)
            {
                PurchaseInteraction targetPurchase = targetObject.GetComponent<PurchaseInteraction>();
                if (targetPurchase != null && HackingMainState.PurchaseInteractionIsValidTarget(targetPurchase))
                {
                    targetPurchase.Networkcost = 0;

                    ShopTerminalBehavior terminalBehavior = targetObject.GetComponent<ShopTerminalBehavior>();
                    if (terminalBehavior)
                    {
                        terminalBehavior.serverMultiShopController.SetCloseOnTerminalPurchase(targetPurchase.GetComponent<PurchaseInteraction>(), false);
                    }

                    Interactor interactor = slot.characterBody?.GetComponent<Interactor>();
                    if (interactor)
                    {
                        interactor.AttemptInteraction(targetObject);
                    }

                    b = true;
                }
            }
            return b;
        }
    }
}
