using BepInEx.Configuration;
using R2API;
using RoR2;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Equipment
{
    class TeslaEquipment : EquipmentBase<TeslaEquipment>
    {
        float extraShields = 0.20f;

        float buffDuration = 10;
        float healthFraction = 0.03f;
        float shieldFraction = 0.15f;
        float zapDamageCoefficient = 2f;
        float totalZaps = 4;

        static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

        public override string EquipmentName => "Remarkably Stable Tesla Coil";

        public override string EquipmentLangTokenName => "TESLAEQUIPMENT";

        public override string EquipmentPickupDesc => "Zap nearby enemies and restore Shields on kill.";

        public override string EquipmentFullDescription => $"<style=cIsUtility>Electrify</style> yourself for <style=cIsUtility>{buffDuration} seconds</style>. " +
            $"Kills while <style=cIsUtility>Electrified</style> will " +
            $"zap <style=cIsDamage>{totalZaps}</style> nearby enemies for <style=cIsDamage>{Tools.ConvertDecimal(zapDamageCoefficient)} damage</style> " +
            $"and <style=cIsHealing>restore {Tools.ConvertDecimal(shieldFraction)} of your shields.</style> " +
            $"Passively gain a <style=cIsHealing>shield</style> equal to " +
            $"<style=cIsHealing>{Tools.ConvertDecimal(extraShields)} of your maximum health.</style> ";

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
        public override BalanceCategory Category { get; set; } = BalanceCategory.StateOfHealth;
        public override HookType Type { get; set; } = HookType.Shield;

        public override bool CanDrop { get; } = true;

        public override float Cooldown { get; } = 45f;
        public override bool IsHidden => false;
        public override string OptionalDefString { get; set; } = "BorboTesla";

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return IDR;
        }

        public static void GetDisplayRules(On.RoR2.BodyCatalog.orig_Init orig)
        {
            orig();
            CloneVanillaDisplayRules(TeslaEquipment.instance.EquipDef, RoR2Content.Items.ShockNearby);
        }

        public override void Hooks()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += GrantShieldOnKill;
            GetStatCoefficients += AddBonusShield;
            RiskierRainPlugin.RetierItem(nameof(RoR2Content.Items.ShockNearby), ItemTier.NoTier);
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
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

        public override void Init(ConfigFile config)
        {
            On.RoR2.BodyCatalog.Init += GetDisplayRules;
            CreateEquipment();
            CreateLang();
            Hooks();
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
