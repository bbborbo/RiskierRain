using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.DamageAPI;

namespace RainrotSharedUtils.Frost
{
    public static class FrostUtilsModule
    {
        public static int maxIceExplosionsPerSecond = 4;
        public static int iceExplosionsThisSecond = 0;
        public const int chillStacksMax = 6;
        public const float chillProcDuration = 6f;
        private static float iceExplosionTrackerTimer = 0;
        public static GameObject iceExplosion => Assets.iceDelayBlastPrefab;
        public static void Init()
        {
            FixSnapfreeze();
        }
        public static void FixedUpdate()
        {
            if (maxIceExplosionsPerSecond <= 0 || iceExplosionsThisSecond <= 0)
            {
                iceExplosionsThisSecond = 0;
                iceExplosionTrackerTimer = 1 / maxIceExplosionsPerSecond;
                return;
            }

            iceExplosionTrackerTimer -= Time.fixedDeltaTime;
            while(iceExplosionTrackerTimer < 0)
            {
                iceExplosionTrackerTimer += 1 / maxIceExplosionsPerSecond;
                iceExplosionsThisSecond--;
            }
        }
        public static void FixSnapfreeze()
        {
            //mageicewallpillarprojectile.prefab
            GameObject iceWallPillarPrefab = Addressables.LoadAssetAsync<GameObject>("65d14128d015b6946b0dec7981dfe63a").WaitForCompletion();
            ProjectileImpactExplosion pie = iceWallPillarPrefab.GetComponentInChildren<ProjectileImpactExplosion>();
            if (pie)
            {
                pie.destroyOnEnemy = false;
            }
        }

        #region interface
        public static void CreateIceBlast(CharacterBody attackerBody, float baseForce, float damage, float procCoefficient, float radius, bool crit, Vector3 blastPosition, bool isStrongBlast = false, DamageSource damageSource = DamageSource.NoneSpecified)
        {
            if (NetworkServer.active)
            {
                //EffectManager.SimpleEffect(this.iceExplosionEffectPrefab, base.transform.position + Vector3.up, Util.QuaternionSafeLookRotation(Vector3.forward), true);
                EffectManager.SpawnEffect(GetIceBlastEffect(isStrongBlast), new EffectData
                {
                    origin = blastPosition,
                    scale = radius
                }, true);

                BlastAttack blastAttack = new BlastAttack();
                blastAttack.radius = radius;
                blastAttack.procCoefficient = procCoefficient;
                blastAttack.position = blastPosition;
                blastAttack.attacker = attackerBody.gameObject;
                blastAttack.crit = crit;
                blastAttack.baseDamage = damage;
                blastAttack.falloffModel = BlastAttack.FalloffModel.None;
                blastAttack.baseForce = baseForce;
                blastAttack.teamIndex = attackerBody.teamComponent.teamIndex;
                blastAttack.attackerFiltering = AttackerFiltering.NeverHitSelf;
                blastAttack.damageType = new DamageTypeCombo(DamageTypeExtended.Generic, DamageTypeExtended.Frost, damageSource);
                blastAttack.Fire();

                //return;
                //RainrotSharedUtils.Frost.FrostUtilsModule.ApplyChillSphere(blastPosition, radius, attackerBody.teamComponent.teamIndex);
                //
                //GameObject blast = UnityEngine.Object.Instantiate<GameObject>(iceExplosion, blastPosition, Quaternion.identity);
                //blast.transform.localScale = new Vector3(radius, radius, radius);
                //DelayBlast delay = blast.GetComponent<DelayBlast>();
                //delay.maxTimer += UnityEngine.Random.Range(-0.1f, 0.1f);
                //delay.position = position;
                //delay.baseDamage = attackerBody.damage * novaBaseDamage;// attacker.damage * (1 + 0.5f * (int)icePowerToUse);
                //delay.procCoefficient = 0.8f - (0.1f * (int)icePowerToUse);//0.5f;//
                //delay.attacker = attackerBody.gameObject;
                //delay.radius = radius;
                //delay.damageType = DamageType.Frost;
                //delay.falloffModel = BlastAttack.FalloffModel.SweetSpot;
                //blast.GetComponent<TeamFilter>().teamIndex = attackerBody.teamComponent.teamIndex;

                //NetworkServer.Spawn(blast);
            }
        }

        public static GameObject GetIceBlastEffect(bool isStrongBlast)
        {
            if (isStrongBlast)
                return Assets.iceNovaEffectStrong;

            iceExplosionsThisSecond++;

            if (iceExplosionsThisSecond < maxIceExplosionsPerSecond)
                return Assets.iceNovaEffectWeak;
            return Assets.iceNovaEffectLowPriority;
        }

        public static void ApplyChillSphere(Vector3 origin, float radius, TeamIndex teamIndex, float duration = chillProcDuration, float chillCount = 3)
        {
            if (!NetworkServer.active)
                return;
            SphereSearch chillSphere = new SphereSearch();
            chillSphere.origin = origin;
            chillSphere.mask = LayerIndex.entityPrecise.mask;
            chillSphere.radius = radius;
            chillSphere.RefreshCandidates();
            chillSphere.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(teamIndex));
            chillSphere.FilterCandidatesByDistinctHurtBoxEntities();
            chillSphere.OrderCandidatesByDistance();
            List<HurtBox> hurtboxBuffer = new List<HurtBox>();
            chillSphere.GetHurtBoxes(hurtboxBuffer);
            chillSphere.ClearCandidates();

            for (int i = 0; i < hurtboxBuffer.Count; i++)
            {
                HurtBox hurtBox = hurtboxBuffer[i];
                CharacterBody vBody = hurtBox.healthComponent?.body;
                if (vBody)
                {
                    bool freezeImmune = vBody.HasBuff(DLC2Content.Buffs.FreezeImmune);
                    bool isInFrozenState = vBody.healthComponent.isInFrozenState;
                    if (!freezeImmune && !isInFrozenState)
                        ApplyChillStacks(vBody, 100, chillCount, duration);
                }
            }
            hurtboxBuffer.Clear();
        }

        public static void ApplyChillStacks(CharacterMaster attackerMaster, CharacterBody vBody, float procChance, float chillCount = 1, float chillDuration = chillProcDuration)
        {
            ApplyChillStacks(vBody, procChance, chillCount, chillDuration, attackerMaster ? attackerMaster.luck : 1);
        }
        public static void ApplyChillStacks(CharacterBody vBody, float procChance, float totalChillToApply = 1, float chillDuration = chillProcDuration, float luck = 1)
        {
            //the current chill stack on the victim
            int chillCount = vBody.GetBuffCount(DLC2Content.Buffs.Frost);

            int chillLimitCount = 0;// vBody.GetBuffCount(ChillLimitBuff);
            //the cap on chill stacks, 10 by default but reduced by 3 per chill limit on the victim
            int chillCap = 10 - 3 * chillLimitCount;
            //if the current chill stacks is more than the cap, dont worry about applying more
            if (chillCount > chillCap)
                return;

            //cap the chill stacks applied to the difference between chill cap and chill count
            totalChillToApply = Mathf.Min(totalChillToApply, chillCap - chillCount);
            for (int i = 0; i < totalChillToApply; i++)
            {
                if (Util.CheckRoll(procChance, luck))
                {
                    vBody.AddTimedBuff(DLC2Content.Buffs.Frost.buffIndex, chillDuration);
                }
            }

            //i made this super unreadable because its funny
            /*if (chillCount <= 0)
                return;
            if (Util.CheckRoll(procChance, attackerMaster))
            {
                vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, chillDuration);
            }
            ApplyChillStacks(attackerMaster, vBody, procChance, chillCount--, chillDuration);*/
        }
        #endregion
    }
}
