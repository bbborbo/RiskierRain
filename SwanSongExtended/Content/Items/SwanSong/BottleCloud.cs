using BepInEx.Configuration;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using EntityStates.Bandit2;
using UnityEngine.Networking;
using RoR2.ExpansionManagement;
using JumpRework;
using static MoreStats.StatHooks;
using static MoreStats.JumpAPI;
using SwanSongExtended.Modules;
using UnityEngine.AddressableAssets;
using RoR2.Items;
using MoreStats;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class BottleCloud : ItemBase<BottleCloud>
    {
        public static bool GetBottleCloudConfig()
        {
            return SwanSongPlugin.GetConfigBool(true, "Items : Cloud In A Bottle", "Also enables Quarantined Contaminant"); 
            //instance.Bind(true, "Should This Content Be Enabled", "Also enables Quarantined Contaminant");
        }
        public override bool forcePrerequisites => true;
        public override bool GetPrerequisites()
        {
            return BottleCloud.GetBottleCloudConfig();
        }
        public override string ConfigName => "Items : Cloud In A Bottle";
        public static float verticalBonusOnCloudJump = 0.15f;
        static GameObject novaEffectPrefab = null;// LegacyResourcesAPI.Load<GameObject>("prefabs/effects/JellyfishNova");
        public static BuffDef cloudReadyBuff;
        public static BuffDef cloudNotReadyBuff;
        public static bool cloudReadyBuffHidden = false;
        public static bool cloudNotReadyBuffHidden = false;
        internal static float smokeBombRadius = 9f;
        internal static float smokeBombRadiusStack = 3f;
        static float smokeBombDamageCoefficient = 1f;
        static float smokeBombStunDuration = 2f;
        static float smokeBombProcCoefficient = 1f;
        static int cloudCooldownOnStun = 5;
        static int cloudCooldown = 15;
        public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;
        public override string ItemName => "Cloud In A Bottle";

        public override string ItemLangTokenName => "CLOUDBOTTLE";

        public override string ItemPickupDesc => "Double jump near enemies to stun them!";

        public override string ItemFullDescription => $"Gain an air jump charge. <style=cIsUtility>{cloudCooldown}</style>s cooldown." +
            $"While ready, air jumping within <style=cIsUtility>{smokeBombRadius}m</style> " +
            $"<style=cStack>(+{smokeBombRadius}m per stack)</style> of any enemy " +
            $"drops a <style=cIsUtility>smoke bomb</style>, <style=cIsDamage>stunning</style> them " +
            $"for <style=cIsDamage>{smokeBombProcCoefficient}s</style> and reduces the cooldown by " +
            $"<style=cIsUtility>{cloudCooldown - cloudCooldownOnStun}s.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier1;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility };

        public override GameObject ItemModel => LoadDropPrefab("mdlBottleCloud");

        public override Sprite ItemIcon => LoadItemIcon("texIconBottleCloud");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }
        public override void Init()
        {
            cloudReadyBuff = Content.CreateAndAddBuff(
                "bdCloudReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.white, false, false);
            cloudNotReadyBuff = Content.CreateAndAddBuff(
                "bdCloudNotReady",
                Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffGenericShield.tif").WaitForCompletion(),
                Color.grey, true, true);
            cloudReadyBuff.isHidden = cloudReadyBuffHidden;
            cloudNotReadyBuff.isHidden = cloudNotReadyBuffHidden;
            base.Init();
        }

        public override void Hooks()
        {
            OnConditionalJumpUrgent += CloudOnNearby;
            OnConditionalJumpLast += CloudOnLast;
        }

        private bool CloudOnLast(CharacterMotor sender, bool jumpIgnoredRequirements)
        {
            if (jumpIgnoredRequirements)
                return false;
            CharacterBody body = sender.body;
            if (!GetCloudReady(body))
                return false;

            JumpAPI.SetJumpPowerForCurrentJump(vBonus: BottleCloud.verticalBonusOnCloudJump);

            SetCloudCooldown(body, cloudCooldown);

            EffectManager.SpawnEffect(StealthMode.smokeBombEffectPrefab, new EffectData
            {
                origin = body.footPosition
            }, true);

            return true;
        }

        private bool CloudOnNearby(CharacterMotor sender, bool jumpIgnoredRequirements)
        {
            CharacterBody body = sender.body;
            if (!GetCloudReady(body))
                return false;

            float radius = GetSmokeBombRadius(body);
            List<HurtBox> hurtboxBuffer = BottleCloud.GetEnemiesWithinRadius(body.teamComponent.teamIndex, radius, body.corePosition);
            if (hurtboxBuffer.Count <= 0)
                return false;

            JumpAPI.SetJumpPowerForCurrentJump(vBonus: BottleCloud.verticalBonusOnCloudJump);

            SetCloudCooldown(body, cloudCooldownOnStun);

            CreateNinjaSmokeBomb(body, radius, hurtboxBuffer);

            return true;
        }

        private static float GetSmokeBombRadius(CharacterBody body)
        {
            int stack = body.inventory.GetItemCountEffective(BottleCloud.instance.ItemsDef);

            return smokeBombRadius + smokeBombRadiusStack * (stack - 1);
        }

        public static List<HurtBox> GetEnemiesWithinRadius(TeamIndex attackerTeam, float radius, Vector3 searchPos)
        {
            //float radiusSqr = radius * radius;
            //int enemyCountWithinRadius = 0;
            //for (TeamIndex teamIndex2 = TeamIndex.Neutral; teamIndex2 < TeamIndex.Count; teamIndex2 += 1)
            //{
            //    if (teamIndex2 == attackerTeam)
            //        continue;
            //
            //    foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(teamIndex2))
            //    {
            //        bool flag3 = (teamComponent.transform.position - searchPos).sqrMagnitude <= radiusSqr;
            //        if (flag3)
            //        {
            //            enemyCountWithinRadius++;
            //            return true;
            //        }
            //    }   
            //    if (enemyCountWithinRadius > 0)
            //        return true;
            //}
            //return false;

            SphereSearch chillSphere = new SphereSearch();
            chillSphere.origin = searchPos;
            chillSphere.mask = LayerIndex.entityPrecise.mask;
            chillSphere.radius = radius;
            chillSphere.RefreshCandidates();
            chillSphere.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(attackerTeam));
            chillSphere.FilterCandidatesByDistinctHurtBoxEntities();
            chillSphere.OrderCandidatesByDistance();
            List<HurtBox> hurtboxBuffer = new List<HurtBox>();
            chillSphere.GetHurtBoxes(hurtboxBuffer);
            chillSphere.ClearCandidates();

            return hurtboxBuffer;
            
            //for (int i = 0; i < hurtboxBuffer.Count; i++)
            //{
            //    HurtBox hurtBox = hurtboxBuffer[i];
            //    CharacterBody vBody = hurtBox.healthComponent?.body;
            //    if (vBody)
            //    {
            //        bool freezeImmune = vBody.HasBuff(DLC2Content.Buffs.FreezeImmune);
            //        bool isInFrozenState = vBody.healthComponent.isInFrozenState;
            //        if (!freezeImmune && !isInFrozenState)
            //            ApplyChillStacks(vBody, 100, chillCount, duration);
            //    }
            //}
            //hurtboxBuffer.Clear();
        }

        private static bool GetCloudReady(CharacterBody body)
        {
            if (body.inventory == null || body.inventory.GetItemCountEffective(BottleCloud.instance.ItemsDef) <= 0)
                return false;
            return body.HasBuff(cloudReadyBuff);
        }

        private static void SetCloudCooldown(CharacterBody body, int duration)
        {
            if (!NetworkServer.active)
                return;

            if(body.HasBuff(cloudReadyBuff))
                body.RemoveBuff(cloudReadyBuff);
            if (body.HasBuff(cloudNotReadyBuff))
                body.ClearTimedBuffs(cloudNotReadyBuff.buffIndex);

            for(int i = 1; i <= duration; i++)
            {
                body.AddTimedBuff(cloudNotReadyBuff.buffIndex, i);
            }
        }

        internal static void CreateNinjaSmokeBomb(CharacterBody attacker, float radius, List<HurtBox> hurtboxBuffer)
        {
            //BlastAttack blastAttack = new BlastAttack();
            //blastAttack.radius = smokeBombRadius;
            //blastAttack.procCoefficient = smokeBombProcCoefficient;
            //blastAttack.position = attacker.transform.position;
            //blastAttack.attacker = attacker.gameObject;
            //blastAttack.crit = Util.CheckRoll(attacker.crit, attacker.master);
            //blastAttack.baseDamage = attacker.damage * smokeBombDamageCoefficient;
            //blastAttack.falloffModel = BlastAttack.FalloffModel.None;
            //blastAttack.damageType = DamageType.Stun1s;
            //blastAttack.baseForce = StealthMode.blastAttackForce;
            //blastAttack.teamIndex = TeamComponent.GetObjectTeam(blastAttack.attacker);
            //blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
            //blastAttack.Fire();

            for (int i = 0; i < hurtboxBuffer.Count; i++)
            {
                HurtBox hurtBox = hurtboxBuffer[i];
                CharacterBody vBody = hurtBox.healthComponent?.body;
                if (vBody)
                {

                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = attacker.damage * smokeBombDamageCoefficient;
                    damageInfo.procCoefficient = smokeBombProcCoefficient;
                    damageInfo.attacker = attacker.gameObject;
                    damageInfo.crit = Util.CheckRoll(attacker.crit, attacker.master);
                    damageInfo.damageType = new DamageTypeCombo(DamageType.Stun1s, DamageTypeExtended.Generic, DamageSource.NoneSpecified);

                    vBody.healthComponent.TakeDamage(damageInfo);

                    SetStateOnHurt component = vBody.healthComponent.GetComponent<SetStateOnHurt>();
                    if (component != null)
                    {
                        component.OverrideStun(smokeBombStunDuration);
                    }
                }
            }
            hurtboxBuffer.Clear();


            EffectManager.SpawnEffect(StealthMode.smokeBombEffectPrefab, new EffectData
            {
                origin = attacker.footPosition
            }, true);

            if (novaEffectPrefab)
            {
                EffectManager.SpawnEffect(novaEffectPrefab, new EffectData
                {
                    origin = attacker.transform.position,
                    scale = radius
                }, true);
            }
        }
    }

    public class CloudBottleBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        private static ItemDef GetItemDef() => BottleCloud.instance.ItemsDef;

        void OnEnable()
        {
            this.body.AddBuff(BottleCloud.cloudReadyBuff);
        }
        void OnDisable()
        {
            if (body.HasBuff(BottleCloud.cloudNotReadyBuff))
            {
                body.ClearTimedBuffs(BottleCloud.cloudNotReadyBuff);
            }
            if (body.HasBuff(BottleCloud.cloudReadyBuff))
            {
                body.RemoveBuff(BottleCloud.cloudReadyBuff);
            }
        }
        private void FixedUpdate()
        {
            bool isBuffed = this.body.HasBuff(BottleCloud.cloudReadyBuff);
            bool isDebuffed = this.body.HasBuff(BottleCloud.cloudNotReadyBuff);
            bool isNeither = !isBuffed && !isDebuffed;
            if (isNeither)
            {
                this.body.AddBuff(BottleCloud.cloudReadyBuff);
            }
            bool isBoth = isBuffed && isDebuffed;
            if (isBoth)
            {
                this.body.RemoveBuff(BottleCloud.cloudReadyBuff);
            }
        }
    }
}
