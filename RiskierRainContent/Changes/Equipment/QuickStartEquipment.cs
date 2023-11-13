using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Artifacts;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRainContent.Equipment
{
    class QuickStartEquipment : EquipmentBase<QuickStartEquipment>
    {
        public override string EquipmentName => "Stillborn prayer";

        public override string EquipmentLangTokenName => "QUICKSTARTEQUIPMENT";

        public override string EquipmentPickupDesc => "Grants a single wish";

        public override string EquipmentFullDescription => "Grants an Uncommon item of your choice";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite EquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");
        public override float Cooldown => 0; 
        public override bool EnigmaCompatible => false; 
        public override bool ForceEnable => true; 
        public override bool CanDrop => false; 

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {

        }

        public override void Init(ConfigFile config)
        {
            CreateEquipment();
            CreateLang();
            Hooks();
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            Transform origin = slot.characterBody.gameObject.transform;
            /*PickupIndex pickupIndex = PickupCatalog.FindPickupIndex(RoR2Content.Items.);
            PickupDropletController.CreatePickupDroplet(pickupIndex, dropletOrigin.position, Vector3.zero);*/
            PickupIndex pickupIndex = new PickupIndex(3);//common  = 2, uncommon = 3, rare = 4
            GameObject commandCube = UnityEngine.Object.Instantiate<GameObject>(CommandArtifactManager.commandCubePrefab, origin.position, origin.rotation);
            commandCube.GetComponent<PickupIndexNetworker>().NetworkpickupIndex = pickupIndex;
            commandCube.GetComponent<PickupPickerController>().SetOptionsFromPickupForCommandArtifact(pickupIndex);
            NetworkServer.Spawn(commandCube);
            slot.inventory.SetEquipmentIndex(EquipmentIndex.None);
            return true;
        }
    }
}
