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
        public static float secondsPerCost = 0.5f;

        public override string EquipmentName => "Executive Card";

        public override string EquipmentLangTokenName => throw new NotImplementedException();

        public override string EquipmentPickupDesc => throw new NotImplementedException();

        public override string EquipmentFullDescription => throw new NotImplementedException();

        public override string EquipmentLore => throw new NotImplementedException();

        public override GameObject EquipmentModel => throw new NotImplementedException();

        public override Sprite EquipmentIcon => throw new NotImplementedException();
        public override float Cooldown { get; } = 90f;

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
            bool isCard = targetingEquipmentIndex == ExeCardRework.instance.EquipDef.equipmentIndex;
            if (!isCard || !userShouldAnticipateTarget)
            {
                orig(self, targetingEquipmentIndex, userShouldAnticipateTarget);
                return;
            }
            float maxDistance = 150;
            float maxAngle = 4;
            var minDot = Mathf.Cos(Mathf.Clamp(maxAngle, 0f, 180f) * Mathf.PI / 180f);

            float camAdjust;
            Ray aim = CameraRigController.ModifyAimRayIfApplicable(self.GetAimRay(), self.characterBody.gameObject, out camAdjust);
            Collider[] results = Physics.OverlapSphere(aim.origin, maxDistance + camAdjust, Physics.AllLayers, QueryTriggerInteraction.Collide);


            bool currentTargetValid = false;
            GameObject currentTargetObject = self.currentTarget.rootObject;
            if (currentTargetObject != null)
            {
                PurchaseInteraction currentTargetInteraction = currentTargetObject?.GetComponent<PurchaseInteraction>();
                if (currentTargetInteraction != null)
                    currentTargetValid = HackingMainState.PurchaseInteractionIsValidTarget(currentTargetInteraction) && currentTargetInteraction.available;

                if (!currentTargetValid
                    || (currentTargetObject.transform.position - self.gameObject.transform.position).sqrMagnitude > maxDistance * maxDistance
                    || Vector3.Dot(aim.direction, (currentTargetObject.transform.position - aim.origin).normalized) < minDot)
                    self.InvalidateCurrentTarget();
            }

            bool validTarget = false;
            PurchaseInteraction targetPurchase = null;
            foreach (Collider collider in results)
            {
                var vdot = Vector3.Dot(aim.direction, (collider.transform.position - aim.origin).normalized);
                if (vdot < minDot) 
                    continue;

                PurchaseInteraction component = collider.GetComponent<EntityLocator>()?.entity.GetComponent<PurchaseInteraction>();
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
                    else if (targetPurchase == null && component.available)
                    {
                        targetPurchase = component;
                    }
                }
            }

            if(targetPurchase != null)
            {
                if (validTarget && userShouldAnticipateTarget)
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
            else
            {
                self.InvalidateCurrentTarget();
                self.targetIndicator.active = false;
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

            LanguageAPI.Add("EQUIPMENT_MULTISHOPCARD_PICKUP", "Hack a targeted interactable. Hacked Multishops remain open.");
            LanguageAPI.Add("EQUIPMENT_MULTISHOPCARD_DESC", $"Target an interactable to <style=cIsUtility>hack</style> it, unlocking its contents for <style=cIsUtility>free</style>. " +
                $"If the target is a <style=cIsUtility>multishop</style> terminal, the other terminals will <style=cIsUtility>remain open</style>.");
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
                    //int difficultyScaledCost = Run.instance.GetDifficultyScaledCost(HackingInProgressState.baseGoldForBaseDuration, Stage.instance.entryDifficultyCoefficient);
                    //float cost = (float)(targetPurchase.cost / difficultyScaledCost);
                    //slot.inventory?.DeductActiveEquipmentCooldown(-cost * secondsPerCost);

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

                    slot.InvalidateCurrentTarget();
                    b = true;
                }
            }
            return b;
        }
    }
}
