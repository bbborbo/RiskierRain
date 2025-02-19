using BepInEx.Configuration;
using EntityStates;
using EntityStates.Bandit2;
using R2API;
using RoR2;
using RoR2.ExpansionManagement;
using SwanSongExtended.Modules;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using static SwanSongExtended.Modules.Language.Styling;

namespace SwanSongExtended.Equipment
{
    class NinjaGear : EquipmentBase<NinjaGear>
    {
        public override AssetBundle assetBundle => SwanSongPlugin.orangeAssetBundle;
        static GameObject novaEffectPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");

        #region config
        public override string ConfigName => "Equipment : Master Ninja Gear";

        static bool createSmokeBomb = true;
        [AutoConfig("Smoke Bomb Radius", 13f)]
        static float smokeBombRadius = 13f;
        [AutoConfig("Smoke Bomb Damage Coefficient", 1f)]
        static float smokeBombDamageCoefficient = 1f;
        [AutoConfig("Smoke Bomb Proc Coefficient", 1f)]
        static float smokeBombProcCoefficient = 1f;

        [AutoConfig("Vertical Dash Velocity", 1.5f)]
        static float dashVelocityVertical = 1.5f;
        [AutoConfig("Horizontal Dash Velocity", 25f)]
        static float dashVelocityHorizontal = 25;

        [AutoConfig("Dodge Chance", 20)]
        static int dodgeChance = 20;
        #endregion

        public override string EquipmentName => "Master Ninja Gear";

        public override string EquipmentLangTokenName => "NINJAGEAR";

        public override string EquipmentPickupDesc => "Have a chance to dodge incoming attacks. Activate to dash and drop smoke bombs.";

        public override string EquipmentFullDescription => $"Activate to {UtilityColor($"dash forward {dashVelocityHorizontal}m")}" +
            (dodgeChance > 0 ? $", and gain a {HealingColor($"{dodgeChance}% chance")} to {HealingColor("dodge")} incoming attacks. " : ". ") +
            $"Dashing and dodging will drop a {DamageColor("smoke bomb")}, {UtilityColor("stunning enemies")} " +
            $"within {smokeBombRadius}m for {UtilityColor($"{smokeBombProcCoefficient} second")}.";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => assetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlNinjaGear.prefab");

        public override Sprite EquipmentIcon => assetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupEQUIPMENT_NINJAGEAR.png");

        public override float BaseCooldown => 7f;
        public override bool EnigmaCompatible => true;
        public override bool CanBeRandomlyActivated => false;

        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamageProcess += NinjaGearDodge;
        }

        private void NinjaGearDodge(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if(self.body.equipmentSlot)
            {
                if (self.body.equipmentSlot.equipmentIndex == EquipDef.equipmentIndex)
                {
                    if(Util.CheckRoll(dodgeChance, 0f, null))
                    {
                        EffectData effectData = new EffectData
                        {
                            origin = damageInfo.position,
                            rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                        };
                        EffectManager.SpawnEffect(HealthComponent./*private*/AssetReferences.bearEffectPrefab, effectData, true);
                        //Util.PlaySound(StealthMode.enterStealthSound, self.gameObject);
                        damageInfo.rejected = true;
                    }

                    if (damageInfo.rejected && NetworkServer.active)
                    {
                        CreateNinjaSmokeBomb(self.body);
                    }
                }
            }
            orig(self, damageInfo);
        }

        protected override bool ActivateEquipment(EquipmentSlot slot)
        {
            bool returnValue = false;

            CharacterMotor motor = slot.characterBody.characterMotor;
            if (motor != null)
            {
                float num = slot.characterBody.acceleration * motor.airControl;
                float hV = 15;
                float vV = dashVelocityVertical;
                if(slot.characterBody.moveSpeed > 0 && num > 0)
                {
                    float num2 = Mathf.Sqrt(dashVelocityHorizontal / num);
                    float num3 = slot.characterBody.moveSpeed / num;
                    hV = (num2 + num3) / num3;
                    Debug.Log(hV);
                }

                motor.velocity *= 0.1f;
                motor.Motor.ForceUnground();
                GenericCharacterMain.ApplyJumpVelocity(motor, slot.characterBody, hV, vV);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/BoostJumpEffect"), new EffectData
                {
                    origin = slot.characterBody.footPosition,
                    rotation = Util.QuaternionSafeLookRotation(motor.velocity)
                }, true);

                if(NetworkServer.active)
                    CreateNinjaSmokeBomb(slot.characterBody);

                returnValue = true;
            }

            return returnValue;
        }

        private static void CreateNinjaSmokeBomb(CharacterBody self)
        {
            if (createSmokeBomb)
            {
                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = smokeBombRadius;
                blastAttack.procCoefficient = smokeBombProcCoefficient;
                blastAttack.position = self.transform.position;
                blastAttack.attacker = self.gameObject;
                blastAttack.crit = Util.CheckRoll(self.crit, self.master);
                blastAttack.baseDamage = self.damage * smokeBombDamageCoefficient;
                blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                blastAttack.damageType = DamageType.Stun1s;
                blastAttack.baseForce = StealthMode.blastAttackForce;
                blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
                blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
                blastAttack.Fire();

                EffectManager.SpawnEffect(StealthMode.smokeBombEffectPrefab, new EffectData
                {
                    origin = self.footPosition
                }, true);
                EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = smokeBombRadius
                }, true);
            }
        }
    }
}
