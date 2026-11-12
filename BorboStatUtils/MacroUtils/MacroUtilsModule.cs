using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static RainrotSharedUtils.MacroUtils.MacroHooks;

namespace RainrotSharedUtils.MacroUtils
{
    public static class MacroUtilsModule
    {
        public class MasterMacroStats
        {
            public void AddPrint()
            {
                if (NetworkServer.active == false)
                    return;
                printCountThisStage++;
            }
            public void ResetPrints()
            {
                if (NetworkServer.active == false)
                    return;
                printCountThisStage = 0;
            }
            public int printCountThisStage { get; private set; }

            public bool hasUsedShopAccess { get; internal set; }
            public bool canBuyUsedShopsDirectly { get; internal set; }
            public float usedShopAccessRange { get; internal set; }
        }
        public static FixedConditionalWeakTable<CharacterMaster, MasterMacroStats> masterMacroStats = new FixedConditionalWeakTable<CharacterMaster, MasterMacroStats>();
        public static MasterMacroStats GetMacroStatsFromMaster(CharacterMaster master)
        {
            if (master == null)
                return null;
            return masterMacroStats.GetOrCreateValue(master);
        }

        public static event MacroInventoryUpdateEventHandler GetMacroStats;
        public delegate void MacroInventoryUpdateEventHandler(CharacterMaster sender, MacroInventoryEventArgs args);
        public class MacroInventoryEventArgs
        {
            /// <summary>
            /// Allows viewing used shops, but not direct purchasing. Hacking can still buy.
            /// </summary>
            public int usedShopKeyCount = 0;
            /// <summary>
            /// Allows buying from used shops directly.
            /// </summary>
            public int usedShopPurchaseCount = 0;
            public float usedShopKeyRangeAdd = 0f;
        }
        internal static void GetStatMods(CharacterMaster master)
        {
            if (master == null)
                return;
            MacroInventoryEventArgs StatMods = new MacroInventoryEventArgs();
            if (GetMacroStats != null)
            {
                GetMacroStats.Invoke(master, StatMods);
            }
            MasterMacroStats macroStats = GetMacroStatsFromMaster(master);
            if(macroStats != null)
            {
                macroStats.canBuyUsedShopsDirectly = StatMods.usedShopPurchaseCount > 0;
                macroStats.hasUsedShopAccess = StatMods.usedShopKeyCount > 0 || macroStats.canBuyUsedShopsDirectly;
                macroStats.usedShopAccessRange = ShopTerminalMacroHandler.reopenDistance + StatMods.usedShopKeyRangeAdd;
            }
        }

        public static void Init()
        {
            On.RoR2.CharacterMaster.OnInventoryChanged += CharacterMaster_OnInventoryChanged;
            RoR2.Stage.onServerStageComplete += PrintCountReset;
            On.RoR2.Items.MultiShopCardUtils.OnNonMoneyPurchase += MacroOnNonMoneyPurchase;
        }

        private static bool _UseMultiShopMacro;
        public static bool UseMultiShopMacro
        {
            get
            {
                return _UseMultiShopMacro;
            }
            set
            {
                if (_UseMultiShopMacro)
                    InitMultiShopMacro();
                _UseMultiShopMacro = value;
            }
        }
        private static void InitMultiShopMacro()
        {
            On.RoR2.PurchaseInteraction.GetInteractability += PurchaseInteraction_GetInteractability;
            On.RoR2.MultiShopController.CreateTerminals += MultiShopController_CreateTerminals;
            On.RoR2.DroneVendorMultiShopController.CreateTerminals += DroneVendorMultiShopController_CreateTerminals;
        }
    }
}
