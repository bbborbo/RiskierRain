using BepInEx;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain
{
    partial class RiskierRainPlugin : BaseUnityPlugin
    {
        #region blaclist
        public static EquipmentDef[] scavBlacklistedEquips = new EquipmentDef[]
        {
            RoR2Content.Equipment.PassiveHealing,
            RoR2Content.Equipment.Fruit,
            RoR2Content.Equipment.LifestealOnHit
        };
        void ChangeEnigmaBlacklists()
        {
            ChangeEquipmentEnigma(nameof(RoR2Content.Equipment.CrippleWard), true);
            ChangeEquipmentEnigma(nameof(RoR2Content.Equipment.Jetpack), true);
        }
        void ChangeEquipmentBlacklists()
        {
            On.RoR2.Inventory.SetEquipmentIndex += BlacklistEquipmentFromScavengers;
        }

        private void BlacklistEquipmentFromScavengers(On.RoR2.Inventory.orig_SetEquipmentIndex orig, Inventory self, EquipmentIndex newEquipmentIndex)
        {
            CharacterBody body = self.gameObject.GetComponent<CharacterBody>();
            if (body != null && body.bodyIndex == BodyCatalog.FindBodyIndex("ScavBody"))
            {
                bool flag = false;
                foreach (EquipmentDef def in scavBlacklistedEquips)
                {
                    if (newEquipmentIndex == def.equipmentIndex)
                    {
                        flag = true;
                    }
                }

                if (flag)
                {
                    Debug.Log("A scavenger almost spawned with a healing equipment! But they didnt ;)");
                    newEquipmentIndex = EquipmentIndex.None;
                }
            }

            orig(self, newEquipmentIndex);
        }
        #endregion

        #region helfire
        void TinctureIgnoreArmor()
        {
            On.RoR2.HealthComponent.TakeDamage += MakeTinctureIgnoreArmor;
        }

        private void MakeTinctureIgnoreArmor(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (damageInfo.dotIndex.HasFlag(DotController.DotIndex.Helfire))
            {
                damageInfo.damageType |= DamageType.BypassArmor;
            }
            orig(self, damageInfo);
        }
        #endregion
    }
}