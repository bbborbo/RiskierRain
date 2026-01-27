using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Artifacts;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SwanSongExtended.Artifacts.QuickStartArtifact;

namespace SwanSongExtended.Equipment
{
    class QuickStartEquipment : EquipmentBase<QuickStartEquipment>
    {
        public override bool lockEnabled => true;
        public override string EquipmentName => "Stillborn Prayer";

        public override string EquipmentLangTokenName => "QUICKSTARTEQUIPMENT";

        public override string EquipmentPickupDesc => "Grants a single wish...";

        public override string EquipmentFullDescription => $"Grants an {GetRarityName()} item of your choice.";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => LoadDropPrefab();

        public override Sprite EquipmentIcon => LoadItemIcon("texIconQuickStartEquipment");
        public override float BaseCooldown => 0;
        public override bool EnigmaCompatible => false;
        public override bool CanBeRandomlyActivated => false;
        public override bool CanDrop => false;

        public override string ConfigName => "Equipment : Quick Start (Stillborn Prayer)";

        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            Transform origin = slot.characterBody.gameObject.transform;
            UniquePickup pickupIndex = new UniquePickup(PickupCatalog.itemTierToPickupIndex[ItemTier.Tier2/*(ItemTier)wishPickupIndex*/]); //common = 0, uncommon = 1, rare = 2

            GameObject commandCube = UnityEngine.Object.Instantiate<GameObject>(CommandArtifactManager.commandCubePrefab, origin.position, origin.rotation);
            commandCube.GetComponent<PickupIndexNetworker>().NetworkpickupState = pickupIndex;
            commandCube.GetComponent<PickupPickerController>().SetOptionsFromPickupForCommandArtifact(pickupIndex);
            NetworkServer.Spawn(commandCube);

            slot.inventory.SetEquipmentIndex(EquipmentIndex.None, false);
            return true;
        }
    }
}
