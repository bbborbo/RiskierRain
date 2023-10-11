using BepInEx.Configuration;
using EntityStates;
using EntityStates.Bandit2;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace RiskierRain.Equipment
{
    class NinjaGear : EquipmentBase<NinjaGear>
    {
        static GameObject novaEffectPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");

        static bool createSmokeBomb = true;
        static float smokeBombRadius = 13f;
        static float smokeBombDamageCoefficient = 1f;

        static float dashVelocityVertical = 1.5f;
        static float dashVelocityHorizontal = 25;

        public override string EquipmentName => "Master Ninja Gear";

        public override string EquipmentLangTokenName => "NINJAGEAR";

        public override string EquipmentPickupDesc => "Have a chance to dodge incoming attacks. Activate to dash and drop smoke bombs.";

        public override string EquipmentFullDescription => $"Activate to <style=cIsUtility>dash forward {dashVelocityHorizontal}m,</style> " +
            $"and gain a <style=cIsHealing>20% chance</style> to <style=cIsHealing>dodge</style> incoming attacks. " +
            $"Dashing and dodging will drop a <style=cIsDamage>smoke bomb,</style> <style=cIsUtility>stunning enemies</style> " +
            $"within {smokeBombRadius}m for <style=cIsUtility>1 second.</style>";

        public override string EquipmentLore => "";

        public override GameObject EquipmentModel => RiskierRainPlugin.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/mdlNinjaGear.prefab");

        public override Sprite EquipmentIcon => RiskierRainPlugin.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupEQUIPMENT_NINJAGEAR.png");
        public override BalanceCategory Category => BalanceCategory.StateOfDefenseAndHealing;

        public override bool CanDrop { get; } = true;

        public override float Cooldown { get; } = 7f;

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            On.RoR2.HealthComponent.TakeDamage += NinjaGearDodge;
        }

        private void NinjaGearDodge(On.RoR2.HealthComponent.orig_TakeDamage orig, HealthComponent self, DamageInfo damageInfo)
        {
            if(self.body.equipmentSlot)
            {
                if (self.body.equipmentSlot.equipmentIndex == EquipDef.equipmentIndex)
                {
                    if(Util.CheckRoll(20, 0f, null))
                    {
                        EffectData effectData = new EffectData
                        {
                            origin = damageInfo.position,
                            rotation = Util.QuaternionSafeLookRotation((damageInfo.force != Vector3.zero) ? damageInfo.force : UnityEngine.Random.onUnitSphere)
                        };
                        EffectManager.SpawnEffect(HealthComponent./*private*/AssetReferences.bearEffectPrefab, effectData, true);
                        //Util.PlaySound(StealthMode.enterStealthSound, self.gameObject);
                        damageInfo.rejected = true;
                        CreateNinjaSmokeBomb(self.body);
                    }
                }
            }
            orig(self, damageInfo);
        }

        public override void Init(ConfigFile config)
        {
            CreateEquipment();
            CreateLang();
            Hooks();
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
                blastAttack.procCoefficient = 1;
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
