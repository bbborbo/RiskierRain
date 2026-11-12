using System;
using System.Collections.Generic;
using System.Text;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RainrotSharedUtils.MacroUtils.MacroUtilsModule;

namespace RainrotSharedUtils.MacroUtils
{
    public static class MacroHooks
    {
        public static void CharacterMaster_OnInventoryChanged(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            orig(self);
            GetStatMods(self);
        }
        #region shop
        internal static void DroneVendorMultiShopController_CreateTerminals(On.RoR2.DroneVendorMultiShopController.orig_CreateTerminals orig, DroneVendorMultiShopController self)
        {
            orig(self);
            if(self.TryGetComponent(out EntityStateMachine esm))
            {
                foreach (DroneVendorTerminalBehavior terminal in self._terminals)
                {
                    ShopTerminalMacroHandler terminalBehavior = terminal.gameObject.AddComponent<ShopTerminalMacroHandler>();
                    terminalBehavior.isDroneVendor = true;
                    terminalBehavior.droneShopController = self;
                    terminalBehavior.shopStateMachine = esm;
                }
            }
        }
        internal static void MultiShopController_CreateTerminals(On.RoR2.MultiShopController.orig_CreateTerminals orig, MultiShopController self)
        {
            orig(self);
            foreach (GameObject terminalObject in self.terminalGameObjects)
            {
                ShopTerminalMacroHandler terminalBehavior = terminalObject.AddComponent<ShopTerminalMacroHandler>();
                terminalBehavior.shopController = self;
            }
        }

        internal static RoR2.Interactability PurchaseInteraction_GetInteractability(On.RoR2.PurchaseInteraction.orig_GetInteractability orig, RoR2.PurchaseInteraction self, RoR2.Interactor activator)
        {
            Interactability interactability = orig(self, activator);
            if (interactability == Interactability.Available)
            {
                if(self.TryGetComponent(out ShopTerminalMacroHandler macro) && activator.TryGetComponent(out CharacterBody activatorBody))
                {
                    if (macro.BodyHasAccess(activatorBody) == false)
                        interactability = Interactability.ConditionsNotMet;
                }
            }
            return interactability;
        }
        #endregion
        #region printer
        public static void PrintCountReset(Stage obj)
        {
            foreach (CharacterMaster master in CharacterMaster.instancesList)
            {
                MasterMacroStats cpc = GetMacroStatsFromMaster(master);
                if (cpc != null) cpc.ResetPrints();
            }
        }


        public static void MacroOnNonMoneyPurchase(On.RoR2.Items.MultiShopCardUtils.orig_OnNonMoneyPurchase orig, CostTypeDef.PayCostContext context)
        {
            MacroOnPrinterPurchase(context);
            orig(context);
        }
        public static void MacroOnPrinterPurchase(CostTypeDef.PayCostContext context)
        {
            if (context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.WhiteItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.GreenItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.RedItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.BossItem)
                    && context.costTypeDef != CostTypeCatalog.GetCostTypeDef(CostTypeIndex.LunarItemOrEquipment)
                    )
                return;

            MasterMacroStats cpc = GetMacroStatsFromMaster(context.activatorMaster);
            if (cpc != null)
            {
                cpc.AddPrint();
            }
        }
        #endregion
    }
}
