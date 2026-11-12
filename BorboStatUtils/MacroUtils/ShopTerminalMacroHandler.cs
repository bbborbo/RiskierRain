using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RainrotSharedUtils.MacroUtils.MacroUtilsModule;

namespace RainrotSharedUtils.MacroUtils
{
    class ShopTerminalMacroHandler : MonoBehaviour
    {
        public static float reopenDistance = 20;
        public static float pollInterval = 0.5f;
        private float pollCountdown = 0;
        private PurchaseInteraction purchaseInteraction;
        public bool isDroneVendor = false;
        #region drone
        private ShopTerminalBehavior shopTerminal;
        public MultiShopController shopController;
        #endregion
        #region shop
        private DroneVendorTerminalBehavior droneTerminal;
        public DroneVendorMultiShopController droneShopController;
        public EntityStateMachine shopStateMachine;
        public EntityStateMachine terminalStateMachine;
        #endregion

        private bool hasBeenPurchased = false;
        private bool isReopened = false;
        private bool shouldClose = true;
        internal UniquePickup savedPickup;
        internal bool hidden;
        private static MasterMacroStats cachedMacroStats;

        private void OnPurchase(CostTypeDef.PayCostContext context, CostTypeDef.PayCostResults results)
        {
            hasBeenPurchased = true;

            if (isDroneVendor)
            {
                UpdateReopening(droneShopController.doCloseOnTerminalPurchase, droneShopController._terminals.Select(x => x.gameObject).ToArray());
            }
            else
            {
                UpdateReopening(shopController.doCloseOnTerminalPurchase, shopController.terminalGameObjects);
            }

            void UpdateReopening(bool[] doCloseOnTerminalPurchase, UnityEngine.Object[] others)
            {
                foreach (bool shouldClose in doCloseOnTerminalPurchase)
                {
                    foreach (GameObject shopObject in others)
                    {
                        if (shopObject.TryGetComponent(out ShopTerminalMacroHandler macro))
                        {
                            if (macro.hasBeenPurchased)
                                continue;
                            macro.isReopened = !shouldClose;
                            macro.shouldClose = shouldClose;
                        }
                    }
                    break;
                }
            }
        }

        void Awake()
        {
            if (NetworkServer.active == false)
                Destroy(this);
        }
        void Start()
        {
            purchaseInteraction = GetComponent<PurchaseInteraction>();
            purchaseInteraction.onDetailedPurchaseServer.AddListener(OnPurchase);
            if(isDroneVendor == false)
            {
                shopTerminal = GetComponent<ShopTerminalBehavior>();
                savedPickup = shopTerminal.pickup;
                hidden = shopTerminal.hidden;
            }
            else
            {
                terminalStateMachine = GetComponent<EntityStateMachine>();
                droneTerminal = GetComponent<DroneVendorTerminalBehavior>();
                savedPickup = droneTerminal.currentPickup;
                hidden = droneTerminal.hidden;
            }
        }

        void FixedUpdate()
        {
            if (hasBeenPurchased == true)
            {
                return;
            }

            pollCountdown -= Time.fixedDeltaTime;
            if (pollCountdown > 0)
                return;
            pollCountdown = pollInterval;

            bool keyHolderNearby = GetKeyholderNearby(transform.position);
            if(keyHolderNearby != isReopened)
            {
                if (isReopened)
                    CloseTerminal();
                else
                    OpenTerminal();
            }
        }
        internal static bool GetKeyholderNearby(Vector3 origin)
        {
            foreach (PlayerCharacterMasterController masterController in PlayerCharacterMasterController.instances)
            {
                if (masterController.master == null || masterController.master.IsDeadAndOutOfLivesServer())
                    continue;

                if (MasterHasKey(masterController.master) == false)
                    continue;

                float reopenDistance = cachedMacroStats.usedShopAccessRange;
                if (ShopTerminalMacroHandler.reopenDistance == -1 || Vector3.Distance(masterController.master.GetBody().corePosition, origin) <= reopenDistance)
                {
                    return true;
                }
            }
            return false;
        }

        private static bool MasterHasKey(CharacterMaster master, bool justLooking = true)
        {
            if (master == null)
                return false;
            cachedMacroStats = MacroUtilsModule.GetMacroStatsFromMaster(master);
            if (cachedMacroStats != null)
            {
                return justLooking ? cachedMacroStats.hasUsedShopAccess : cachedMacroStats.canBuyUsedShopsDirectly;
            }
            return false;
        }

        public bool BodyHasAccess(CharacterBody body, bool justLooking = false)
        {
            if (shouldClose == false || isReopened == false)
                return true;
            return MasterHasKey(body.master, justLooking);
        }


        void OpenTerminal()
        {
            if (purchaseInteraction.available)
                return;

            if (isDroneVendor == false)
            {
                if (shopTerminal.hasBeenPurchased)
                    return;
                isReopened = true;
                shopController.Networkavailable = true;
                Util.PlaySound("Play_UI_tripleChestShutter", gameObject);

                purchaseInteraction.SetAvailable(true);
                if (NetworkServer.active)
                {
                    shopTerminal.SetPickup(savedPickup, hidden);
                    shopTerminal.UpdatePickupDisplayAndAnimations();
                }
                return;
            }

            if (!NetworkServer.active || (shopStateMachine.state is EntityStates.Idle == false) || droneTerminal.hasBeenPurchased)
                return;
            isReopened = true;
            purchaseInteraction.SetAvailable(true);
            if (terminalStateMachine && terminalStateMachine.state is EntityStates.Idle)
            {
                droneShopController.Networkavailable = true;
            }
            droneTerminal.SetPickup(savedPickup);
        }
        void CloseTerminal()
        {
            if (isReopened == false || shouldClose == false)
                return;
            isReopened = false;

            if (!NetworkServer.active)
                return;

            if (isDroneVendor == false)
            {
                if (shopTerminal.hasBeenPurchased)
                    return;
                purchaseInteraction.SetAvailable(false);
                shopController.Networkavailable = false;
                shopTerminal.SetNoPickup();
                return;
            }

            if (droneTerminal.hasBeenPurchased)
                return;
            purchaseInteraction.SetAvailable(false);
            droneShopController.Networkavailable = false;
            droneTerminal.SetNoPickup();
        }
    }
}
