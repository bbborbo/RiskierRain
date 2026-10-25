using RoR2;
using SwanSongExtended.Items;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

[assembly: HG.Reflection.SearchableAttribute.OptIn]
namespace RiskierRain.Changes
{
    public static partial class DifficultyChanges
    {
        #region trial drop tables
        public static string[] bannedTrialItemPaths = new string[]
        {
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_HealingPotion.HealingPotion_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_ShieldBooster.ShieldBooster_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_CritAtLowerElevation.CritAtLowerElevation_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Talisman.Talisman_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_PermanentDebuffOnHit.PermanentDebuffOnHit_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC1_OutOfCombatArmor.OutOfCombatArmor_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_ArmorPlate.ArmorPlate_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC3_Items_JumpDamageStrike.JumpDamageStrike_asset,
            RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StunChanceOnHit.StunChanceOnHit_asset
        };
        public static ItemDef[] bannedTrialItemDefs;
        public static ExplicitPickupDropTable safeTrialItemDefs;
        [SystemInitializer(typeof(ItemCatalog), typeof(PickupCatalog))]
        private static void InitTrialBanList()
        {
            bannedTrialItemDefs = new ItemDef[]
            {
                RoR2Content.Items.Talisman,
                RoR2Content.Items.ArmorPlate,
                RoR2Content.Items.StunChanceOnHit,
                DLC1Content.Items.HealingPotion,
                DLC1Content.Items.PermanentDebuffOnHit,
                DLC1Content.Items.OutOfCombatArmor,
                DLC3Content.Items.ShieldBooster,
                DLC3Content.Items.CritAtLowerElevation,
                DLC3Content.Items.JumpDamageStrike
            };
            safeTrialItemDefs = ScriptableObject.CreateInstance<ExplicitPickupDropTable>();
            safeTrialItemDefs.pickupEntries = new ExplicitPickupDropTable.PickupDefEntry[]
            {
                GetPickupEntry(ManaFlower.instance?.ItemsDef ?? null, 0.01f),
                GetPickupEntry(DLC2Content.Items.LowerPricedChests, 0.2f),
                GetPickupEntry(DLC1Content.Items.FreeChest, 0.2f),
                GetPickupEntry(DLC1Content.Items.AttackSpeedAndMoveSpeed),
                GetPickupEntry(RoR2Content.Items.Feather, 0.2f),
                GetPickupEntry(RoR2Content.Items.IgniteOnKill),
                GetPickupEntry(RoR2Content.Items.Hoof),
                GetPickupEntry(RoR2Content.Items.Syringe)
            };

        }
        private static ExplicitPickupDropTable.PickupDefEntry GetPickupEntry(ItemDef itemDef, float weight = 1)
        {
            ExplicitPickupDropTable.PickupDefEntry entry = new ExplicitPickupDropTable.PickupDefEntry();
            entry.pickupDef = itemDef;
            entry.pickupWeight = weight;
            return entry;
        }
        public static void ChangeTrialDropTables()
        {
            On.RoR2.ItemSpawnPoint.Awake += ChangeDisabledTrialItems;

            RiskierRainPlugin.LoadAsync<ExplicitPickupDropTable>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_sadism_sad1.dtSad1RentalsExplicit_OpenChests_asset, (dropTable) =>
            {
                dropTable.pickupEntries[1] = GetPickupEntry(RoR2Content.Items.Squid, 0.3f);//stun grenade -> squid polyp
            });
            RiskierRainPlugin.LoadAsync<ExplicitPickupDropTable>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_DLC4_sadism_sad1.dtSad1RentalsExplicit_Money3_asset, (dropTable) =>
            {
                dropTable.pickupEntries[0] = GetPickupEntry(RoR2Content.Items.Thorns, 0.3f);//guillotine -> razorwire
                dropTable.pickupEntries[2].pickupDef = RoR2Content.Items.ExplodeOnDeath;//willowisp
            });
        }

        private static void ChangeDisabledTrialItems(On.RoR2.ItemSpawnPoint.orig_Awake orig, RoR2.ItemSpawnPoint self)
        {
            if (self.useExplicitItemDef)
            {
                bool banned = false;
                if (self._explicitItemDefAddress.RuntimeKeyIsValid())
                {
                    if (bannedTrialItemPaths.Contains(self._explicitItemDefAddress.AssetGUID))
                    {
                        banned = true;
                    }
                }
                else if (self._explicitItemDefReference != null)
                {
                    if(bannedTrialItemDefs != null && bannedTrialItemDefs.Contains(self._explicitItemDefReference))
                    {
                        banned = true;
                    }
                }

                if (banned)
                {
                    self.useExplicitItemDef = false;
                    self.dropTable = safeTrialItemDefs;
                }
            }
            orig(self);
        }

        #endregion
        #region trial of greed
        public static uint trialGreedRequiredMoney = 175;
        public static void ChangeTrialOfGreed()
        {
            On.RoR2.Quests.TrialQuests.TrialQuestSadismMoney.OnEnter += TrialQuestSadismMoney_OnEnter;
        }

        private static void TrialQuestSadismMoney_OnEnter(On.RoR2.Quests.TrialQuests.TrialQuestSadismMoney.orig_OnEnter orig, RoR2.Quests.TrialQuests.TrialQuestSadismMoney self)
        {
            self.requiredMoneyBase = trialGreedRequiredMoney;
            orig(self);
        }
        #endregion
    }
}
