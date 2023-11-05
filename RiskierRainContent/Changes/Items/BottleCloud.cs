using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainContent;
using static RiskierRain.JumpStatHook;
using EntityStates.Bandit2;
using UnityEngine.Networking;
using RiskierRain.CoreModules;
using RoR2.ExpansionManagement;

namespace RiskierRain.Items
{
    class BottleCloud : ItemBase<BottleCloud>
    {
        public override ExpansionDef RequiredExpansion => RiskierRainContent.expansionDef;
        public override string ItemName => "Cloud In A Bottle";

        public override string ItemLangTokenName => "CLOUDBOTTLE";

        public override string ItemPickupDesc => "Gain an extra jump. Double jumping near enemies stuns them.";

        public override string ItemFullDescription => $"Gain an extra jump. Double jumping within {smokeBombRadius}m of an enemy " +
            $"drops a <style=cIsUtility>smoke bomb</style>, stunning them for <style=cIsUtility>{smokeBombProcCoefficient}</style> seconds. " +
            $"Cannot be reactivated for <style=cIsUtility>{CloudBottleBehavior.cooldownDuration}</style> seconds " +
            $"<style=cIsStack>(-{Tools.ConvertDecimal(CloudBottleBehavior.cooldownReductionPerStack)} per stack)</style>.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => Assets.orangeAssetBundle.LoadAsset<GameObject>("Assets/Prefabs/bottle.prefab");

        public override Sprite ItemIcon => Assets.orangeAssetBundle.LoadAsset<Sprite>("Assets/Icons/texIconPickupITEM_CLOUDBOTTLE.png");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
            JumpStatCoefficient += CloudJump;
            On.RoR2.CharacterBody.OnInventoryChanged += AddItemBehavior;
        }

        private void AddItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (self.master)
                {
                    CloudBottleBehavior ringBehavior = self.AddItemBehavior<CloudBottleBehavior>(GetCount(self));
                }
            }
        }

        private void CloudJump(CharacterBody sender, ref int jumpCount)
        {
            if (GetCount(sender) > 0)
            {
                jumpCount += 1;
            }
        }

        public override void Init(ConfigFile config)
        {
            CreateItem();
            CreateLang();
            //CreateBuff();
            Hooks();
        }

        static GameObject novaEffectPrefab = null;// LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");
        internal static float smokeBombRadius = 13f;
        static float smokeBombDamageCoefficient = 1f;
        static float smokeBombProcCoefficient = 1f;
        internal static void CreateNinjaSmokeBomb(CharacterBody self)
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

            if (novaEffectPrefab)
            {
                EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
                {
                    origin = self.transform.position,
                    scale = smokeBombRadius
                }, true);
            }
        }
    }

    public class CloudBottleBehavior : CharacterBody.ItemBehavior
    {
        public static float cooldownDuration = 7;
        public static float cooldownReductionPerStack = 0.2f;
        float cooldownTimer = 0;
        float bombRadiusSqr;

        void Start()
        {
            bombRadiusSqr = BottleCloud.smokeBombRadius * BottleCloud.smokeBombRadius;
            OnJumpEvent += CloudBottleJump;
        }
        void OnDestroy()
        {
            OnJumpEvent -= CloudBottleJump;
        }

        private void CloudBottleJump(CharacterMotor motor)
        {
            if (cooldownTimer > 0)
                return;

            CharacterBody body = motor.body;
            if (body && body.inventory?.GetItemCount(BottleCloud.instance.ItemsDef) <= 0)
                return;

            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;

            if (IsBaseJump(motor, body))
                return;


            TeamIndex teamIndex = this.body.teamComponent.teamIndex;
            int num = 0;
            for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2 += 1)
            {
                bool flag2 = teamIndex2 != teamIndex && teamIndex2 > TeamIndex.Neutral;
                if (flag2)
                {
                    foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(teamIndex2))
                    {
                        bool flag3 = (teamComponent.transform.position - this.body.corePosition).sqrMagnitude <= bombRadiusSqr;
                        if (flag3)
                        {
                            num++;
                            break;
                        }
                    }
                }
                if (num > 0)
                    break;
            }

            if (num > 0)
            {
                cooldownTimer = cooldownDuration * Mathf.Pow(1 - cooldownReductionPerStack, stack - 1);
                BottleCloud.CreateNinjaSmokeBomb(motor.body);
            }
            else if (IsLastJump(motor, body))
            {
                EffectManager.SpawnEffect(StealthMode.smokeBombEffectPrefab, new EffectData
                {
                    origin = body.footPosition
                }, true);
            }
        }

        private void FixedUpdate()
        {
            if(cooldownTimer > 0)
            {
                cooldownTimer -= Time.fixedDeltaTime;
            }
        }
    }
}
