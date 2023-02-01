using BepInEx;
using EntityStates.GoldGat;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

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

        #region enemy use equip

        public void MakeEnemiesuseEquipment()
        {
            On.RoR2.EquipmentSlot.FixedUpdate += TryUseEquip;
        }

        private void TryUseEquip(On.RoR2.EquipmentSlot.orig_FixedUpdate orig, EquipmentSlot self)
        {
            orig(self);
            if (!self.characterBody.isPlayerControlled)
            {
                    if (!self.characterBody.outOfCombat)
                    {
                        //self.ExecuteIfReady(EquipmentCatalog.GetEquipmentDef(self.equipmentIndex));
                        bool isEquipmentActivationAllowed = self.characterBody.isEquipmentActivationAllowed;
                        if (isEquipmentActivationAllowed /**&& self.hasEffectiveAuthority*/)
                        {
                            if (NetworkServer.active)
                            {
                                self.ExecuteIfReady();
                                return;
                            }
                            self.CallCmdExecuteIfReady();
                        }
                    }
             
            }
        }


        #endregion

        #region crunder
        public float crunderFunnyMoneyProcChance = 10;
        void CrowdfunderFunny()
        {
            On.RoR2.GlobalEventManager.OnHitEnemy += CrunderFunnyMoney;
            LanguageAPI.Add("EQUIPMENT_GOLDGAT_PICKUP", "Toggle to fire. Costs gold per bullet. Passively has a chance to gain gold on hit.");
            LanguageAPI.Add("EQUIPMENT_GOLDGAT_DESC", 
                $"Fires a continuous barrage that deals <style=cIsDamage>100% damage per bullet</style>. " +
                $"Costs $1 per bullet. Hitting enemies has a {crunderFunnyMoneyProcChance}% chance to refund the cost. Cost increases over time.");
        }

        private void CrunderFunnyMoney(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            if (!damageInfo.rejected && damageInfo.procCoefficient > 0 && damageInfo.attacker)
            {
                CharacterBody attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (attackerBody)
                {
                    Inventory inv = attackerBody.inventory;
                    if(inv && inv.currentEquipmentIndex == RoR2Content.Equipment.GoldGat._equipmentIndex)
                    {
                        if (Util.CheckRoll(crunderFunnyMoneyProcChance * damageInfo.procCoefficient, attackerBody.master))
                        {
                            uint goldAmount = (uint)((float)GoldGatFire.baseMoneyCostPerBullet * 
                                (1f + (TeamManager.instance.GetTeamLevel(attackerBody.master.teamIndex) - 1f) * 0.25f));
                            GoldOrb goldOrb = new GoldOrb();
                            goldOrb.origin = damageInfo.position;
                            goldOrb.target = attackerBody.mainHurtBox;
                            goldOrb.goldAmount = goldAmount;
                            OrbManager.instance.AddOrb(goldOrb);
                            //EffectManager.SimpleImpactEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/CoinImpact"), damageInfo.position, Vector3.up, true);
                        }
                    }
                }
            }
            orig(self, damageInfo, victim);
        }
        #endregion
    }
}