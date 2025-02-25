using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Orbs;
using SwanSongExtended.Modules;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Equipment
{
    class TeslaEquipment : EquipmentBase<TeslaEquipment>
    {
        public override AssetBundle assetBundle => null;
        #region config
        public override string ConfigName => "Reworks : Unstable Tesla Coil";
        [AutoConfig("Base Shield Gain", 0.2f)]
        float extraShields = 0.20f;

        [AutoConfig("Buff Duration", 10)]
        float buffDuration = 10;
        [AutoConfig("Max Health Fraction Cap For Shield Restoration", 0.05f)]
        float healthFraction = 0.05f;
        [AutoConfig("Max Shield Fraction To Restore As Shields", 0.15f)]
        float shieldFraction = 0.15f;
        [AutoConfig("Zap Damage Coefficient", 2f)]
        float zapDamageCoefficient = 2f;
        [AutoConfig("Total Zaps", 4)]
        float totalZaps = 4;
        #endregion

        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

        public override string EquipmentName => "Remarkably Stable Tesla Coil";

        public override string EquipmentLangTokenName => "TESLAEQUIPMENT";

        public override string EquipmentPickupDesc => "Zap nearby enemies and restore Shields on kill.";

        public override string EquipmentFullDescription => $"{UtilityColor("Electrify")} yourself for {UtilityColor($"{buffDuration} seconds")}. " +
            $"Kills while {UtilityColor("Electrified")} will " +
            $"zap {DamageColor(totalZaps.ToString())} nearby enemies for {DamageValueText(zapDamageCoefficient)} " +
            $"and {HealingColor($"restore {Tools.ConvertDecimal(shieldFraction)} of your shields")}. " +
            $"Passively gain a {HealingColor("shield")} equal to " +
            $"{HealingColor($"{Tools.ConvertDecimal(extraShields)} of your maximum health")}.";

        public override string EquipmentLore => $"<style=cMono>Tesla Presentation Software v1.14" +
            $"\nPowering on..." +
            $"\n10...</style>" +
            $"\nIssuing welcome statement..." +
            $"\n<style=cMono>9...</style>" +
            $"\nWelcome one and all!" +
            $"\n<style=cMono>8...</style>" +
            $"\nPlease take a seat." +
            $"\n<style=cMono>7...</style>" +
            $"\nMake sure those behind you can see." +
            $"\n<style=cMono>6...</style>" +
            $"\nThe presentation will start shortly." +
            $"\n<style=cMono>5...</style>" +
            $"\nPlease obey the staff for your safety." +
            $"\n<style=cMono>4..." +
            $"\nInitiating room mood lighting..." +
            $"\n3...</style>" +
            $"\nGet ready to behold..." +
            $"\n<style=cMono>2...</style>" +
            $"\nThe marvelous wonders..." +
            $"\n<style=cMono>1...</style>" +
            $"\nOf electricity!" +
            $"\n<style=cMono>Power anomaly detected..." +
            $"\nInitiating reboot procedure in {buffDuration}...</style>";

        public override GameObject EquipmentModel => LegacyResourcesAPI.Load<GameObject>("prefabs/pickupmodels/PickupTeslaCoil");

        public override Sprite EquipmentIcon => LegacyResourcesAPI.Load<Sprite>("textures/itemicons/texTeslaCoilIcon");

        public override float BaseCooldown => 45;
        public override bool EnigmaCompatible => false;
        public override bool CanBeRandomlyActivated => false;
        //public override string OptionalDefString { get; set; } = "BorboTesla";

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public override void Hooks()
        {
            BodyCatalog.availability.onAvailable += () => CloneVanillaDisplayRules(instance.EquipDef, RoR2Content.Items.ShockNearby);
            On.RoR2.GlobalEventManager.OnCharacterDeath += GrantShieldOnKill;
            GetStatCoefficients += AddBonusShield;
            SwanSongPlugin.RetierItem(Addressables.LoadAssetAsync<ItemDef>("RoR2/Base/ShockNearby/ShockNearby.asset").WaitForCompletion());
        }

        private void AddBonusShield(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.equipmentSlot != null)
            {
                if(sender.equipmentSlot.equipmentIndex == this.EquipDef.equipmentIndex)
                {
                    args.baseShieldAdd = sender.maxHealth * extraShields;
                }
            }
        }

        private void GrantShieldOnKill(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            CharacterBody attacker = damageReport.attackerBody;
            CharacterBody victim = damageReport.victimBody;
            if(attacker != null)
            {
                if (attacker.HasBuff(RoR2Content.Buffs.TeslaField))
                {
                    HealthComponent hc = attacker.healthComponent;
                    if(hc != null)
                    {
                        float healthFractionToRestore = attacker.maxHealth * this.healthFraction;
                        float maxShieldToRestore = attacker.maxShield * this.shieldFraction;

                        float shieldToRestore = Mathf.Max(healthFractionToRestore, maxShieldToRestore);

                        hc.RechargeShield(shieldToRestore);

                        List<HealthComponent> targets = new List<HealthComponent>();
                        for(int i = 0; i < totalZaps; i++)
                        {
                            LightningOrb lightningOrb = new LightningOrb
                            {
                                origin = damageReport.damageInfo.position,
                                damageValue = attacker.damage * zapDamageCoefficient,
                                isCrit = Util.CheckRoll(attacker.crit),
                                bouncesRemaining = 0,
                                teamIndex = attacker.teamComponent.teamIndex,
                                attacker = attacker.gameObject,
                                procCoefficient = 0.5f,
                                bouncedObjects = targets,
                                lightningType = LightningOrb.LightningType.Tesla,
                                damageColorIndex = DamageColorIndex.Item,
                                range = 50f
                            };
                            HurtBox hurtBox = lightningOrb.PickNextTarget(damageReport.damageInfo.position);
                            if (hurtBox)
                            {
                                targets.Add(hurtBox.healthComponent);
                                lightningOrb.target = hurtBox;
                                OrbManager.instance.AddOrb(lightningOrb);
                            }
                        }
                    }
                }
            }
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            bool b = false;

            CharacterBody activator = slot.characterBody;
            if(activator != null)
            {
                activator.AddTimedBuffAuthority(RoR2Content.Buffs.TeslaField.buffIndex, buffDuration);
                b = true;
            }

            return b;
        }
    }
}
