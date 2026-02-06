using BepInEx;
using SurvivorTweaks.Modules;
using On.EntityStates.GameOver;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.Networking;
using static SurvivorTweaks.Modules.Assets;
using static MoreStats.StatHooks;
using static MoreStats.OnHit;
using UnityEngine.AddressableAssets;
using RainrotSharedUtils;
using static R2API.RecalculateStatsAPI;
using static SurvivorTweaks.Modules.Language.Styling;
using SurvivorTweaks.Components;
using MonoMod.Cil;
using EntityStates;
using Mono.Cecil.Cil;
using EntityStates.Bandit2.Weapon;
using EntityStates.Toolbot;
using EntityStates.Huntress.HuntressWeapon;

namespace SurvivorTweaks.SurvivorTweaks
{
    class BanditTweaks : SurvivorTweakBase<BanditTweaks>
    {
        public static bool noFinishersFromSkillSourcedDamage = true;
        public static bool useBanditSkullSurplus = false;
        public static float shotgunDamageCoeff = 0.75f; //1
        public static float rifleDamageCoeff = 2.8f; // 3.3
        public static float rifleSpreadBloom = 0.4f; //0.5f
        public static float reloadEnterBaseDuration = 0.4f; //0.25f
        public static float reloadBaseDuration = 0.6f; //0.3f
        public static float primaryMinDuration = 0.1f;
        public static float primaryAutoDuration = 0.375f;

        public static float daggerDamageCoeff = 6f; //3.6
        public static float daggerCooldown = 6f; //4 
        public static float daggerSelfForce = 1500f; //0
        public static float shivDamageCoeff = 4f; //2.4
        public static float shivCooldown = 7f; //4
        public static int shivStock = 2; //1

        public static float stealthHopVelocity = 13f; //15
        public static float stealthDuration = 4.5f; //3
        public static float stealthCooldown = 6f; //6
        public static float stealthAspdBonus = 0.6f; //0

        public static float lightsOutDamage = 6f; //6
        public static float lightsOutCooldown = 8f; //4
        public static float desperadoDamage = 3f; //6
        public static float desperadoCooldown = 3f; //4
        public static float desperadoDamagePerToken = 0.03f; //0.1f
        public static float desperadoAttackSpeedPerToken = 0.07f; //0f
        public static int desperadoTokensPerLevel = 2;
        public static float revolverDebuffDuration = 1f;//0f
        public static float revolverDrawDuration = 0.8f; //idk
        public static float finisherAimDuration = 10f; //n/a
        public static float revolverBulletRadius = 1.5f;
        public static float revolverHipFireBulletRadius = 3.0f;
        public static float revolverHipFireGraceDuration = 0.25f;

        public static float hemmorageDamageBase = 15;
        public static float hemmorageDamageMin = 0.5f;
        public static float hemmorageDamageMax = 2.5f;

        public override string bodyName => "Bandit2Body";
        public override string survivorName => "Hopoo Bandit";

        public override void Init()
        {
            GetBodyObject();
            GetSkillsFromBodyObject(bodyObject);
            

            ChangeVanillaPrimaries(primary);
            ChangeVanillaSecondaries(secondary);
            ChangeVanillaUtilities(utility);
            ChangeVanillaSpecials(special);

            GetStatCoefficients += BanditCloakBuff;
            On.RoR2.HealthComponent.TakeDamageProcess += BanditTweaksTakeDamage;
            LanguageAPI.Add("KEYWORD_SUPERBLEED", 
                $"<style=cKeywordName>Hemorrhage</style>" +
                $"<style=cSub>Bleed enemies for <style=cIsDamage>{Tools.ConvertDecimal(hemmorageDamageBase * hemmorageDamageMin)}</style> base damage over 15s. " +
                $"Can deal <style=cIsDamage>up to {hemmorageDamageMax / hemmorageDamageMin}x</style> as much damage against healthy enemies. " +
                $"<i>Hemorrhage can stack.</i></style>");

            //CharacterBody.onBodyStartGlobal += RecalculateTokenAmount;
            //TeleporterInteraction.onTeleporterFinishGlobal += OnAdvanceStageSaveTokens;
            //ShowReport.OnEnter += ResetTokens;

            //On.RoR2.CharacterBody.RecalculateStats += BackstabPassiveCritChance;
            On.RoR2.CharacterBody.Start += BackstabPassiveCritChance;
            LanguageAPI.Add("BANDIT2_PASSIVE_DESCRIPTION", "All attacks from <style=cIsDamage>behind</style> are <style=cIsDamage>Critical Strikes</style>. " +
                "All <style=cIsDamage>Critical Strike Chance</style> is instead converted into <style=cIsDamage>Critical Strike Damage</style>.");
        }

        private void BanditCloakBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.bodyIndex == BodyCatalog.FindBodyIndex("Bandit2Body"))
            {
                if(sender.HasBuff(RoR2Content.Buffs.Cloak))
                    args.attackSpeedMultAdd += stealthAspdBonus;

                int baseTokenCount = sender.GetBuffCount(banditSkullBuff);
                int surplusTokenCount = sender.GetBuffCount(banditSkullSurplusBuff);
                int totalTokenCount = baseTokenCount + surplusTokenCount;
                if (totalTokenCount > 0)
                    args.attackSpeedMultAdd += desperadoAttackSpeedPerToken * totalTokenCount;
            }
        }

        private void BackstabPassiveCritChance(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self.canPerformBackstab || self.bodyFlags.HasFlag(CharacterBody.BodyFlags.HasBackstabPassive))
            {
                Inventory inv = self.inventory;
                if (inv)
                {
                    int itemCount = inv.GetItemCountEffective(DLC1Content.Items.ConvertCritChanceToCritDamage);
                    if (itemCount <= 0)
                    {
                        inv.GiveItem(DLC1Content.Items.ConvertCritChanceToCritDamage);
                    }
                }
            }
        }

        private void BanditTweaksTakeDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            CharacterBody attackerBody = null;
            if(damageInfo.attacker)
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

            if (damageInfo.dotIndex == DotController.DotIndex.SuperBleed)
            {
                //float scalingBleedDamage = damageInfo.damage * hemmorageDamageMultiplier * self.combinedHealthFraction;
                //float normalBleedDamage = damageInfo.damage * hemmorageDamageBase;
                float damage2 = damageInfo.damage * Mathf.Lerp(hemmorageDamageMin, hemmorageDamageMax, self.combinedHealthFraction);
                damageInfo.damage = damage2;// scalingBleedDamage + normalBleedDamage;
                damageInfo.damageType.damageType |= DamageType.NonLethal;
            }

            BanditFinisherDebuffOnHit(damageInfo, self.body);
            void BanditFinisherDebuffOnHit(DamageInfo damageInfo, CharacterBody victimBody)
            {
                if (damageInfo == null || victimBody == null || victimBody.healthComponent.alive == false)
                    return;

                bool b = false;
                if (damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill) || (damageInfo.damageType & DamageType.ResetCooldownsOnKill) != 0UL)
                {
                    victimBody.AddTimedBuff(CommonAssets.lightsoutExecutionDebuff, revolverDebuffDuration);
                    b = true;
                }
                if (damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill) || (damageInfo.damageType & DamageType.GiveSkullOnKill) != 0UL)
                {
                    victimBody.AddTimedBuff(CommonAssets.desperadoExecutionDebuff, revolverDebuffDuration);
                    b = true;
                }
                if (b)
                    victimBody.RecalculateStats();
            }

            orig(self, damageInfo);

            if (!NetworkServer.active)
                return;
            if (self.health > 0 || self.alive)
                return;
            if (attackerBody == null)
                return;
            if (attackerBody.bodyIndex != BodyCatalog.FindBodyIndexCaseInsensitive("Bandit2Body"))
                return;
            if (damageInfo.damageType.damageSource != DamageSource.NoneSpecified && noFinishersFromSkillSourcedDamage)
                return;

            if (self.body.HasBuff(CommonAssets.lightsoutExecutionDebuff.buffIndex) && !damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill))
            {
                self.body.RemoveBuff(CommonAssets.lightsoutExecutionDebuff.buffIndex);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2ResetEffect"), new EffectData
                {
                    origin = damageInfo.position
                }, true);
                SkillLocator skillLocator = attackerBody.skillLocator;
                if (skillLocator)
                {
                    skillLocator.ResetSkills();
                }
            }
            if (self.body.HasBuff(CommonAssets.desperadoExecutionDebuff.buffIndex) && !damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill))
            {
                self.body.RemoveBuff(CommonAssets.desperadoExecutionDebuff.buffIndex);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2KillEffect"), new EffectData
                {
                    origin = damageInfo.position
                }, true);
                if (attackerBody)
                {
                    attackerBody.AddBuff(RoR2Content.Buffs.BanditSkull);
                }
            }
        }

        #region primaries
        void ChangeVanillaPrimaries(SkillFamily family)
        {
            IL.RoR2.CharacterBody.OnSkillCooldown += EclipseLiteFix;
            On.EntityStates.GenericBulletBaseState.OnEnter += ModifyRifleAttacks;
            On.EntityStates.GenericBulletBaseState.FixedUpdate += RifleFixedUpdate;
            On.EntityStates.Bandit2.Weapon.Reload.OnEnter += ChangeReloadDuration;
            On.EntityStates.Bandit2.Weapon.Reload.GiveStock += AutoFireOnReload;
            On.EntityStates.Bandit2.Weapon.EnterReload.GetMinimumInterruptPriority += (orig, self) => { return InterruptPriority.Skill; };
            On.EntityStates.Bandit2.Weapon.Reload.GetMinimumInterruptPriority += (orig, self) => { return InterruptPriority.Skill; };
            On.EntityStates.Bandit2.Weapon.EnterReload.OnEnter += ChangeReloadEnterDuration;
            On.EntityStates.Bandit2.Weapon.Bandit2FirePrimaryBase.GetMinimumInterruptPriority += (orig, self) =>
            {
                if (self.fixedAge <= self.minimumDuration)
                {
                    return InterruptPriority.Pain;
                }
                return InterruptPriority.PrioritySkill;
            };

            //shotgun primary
            SkillDef shotgun = family.variants[0].skillDef;
            shotgun.interruptPriority = InterruptPriority.PrioritySkill;
            shotgun.baseRechargeInterval = reloadBaseDuration;
            //shotgun.mustKeyPress = false;
            LanguageAPI.Add("BANDIT2_PRIMARY_DESCRIPTION", 
                $"Fire a shotgun burst for <style=cIsDamage>5x{shotgunDamageCoeff.AsPercent()} damage</style>. " +
                $"Tap to fire faster. Can hold up to 4 shells.");

            //rifle primary
            SkillDef rifle = family.variants[1].skillDef;
            rifle.interruptPriority = InterruptPriority.PrioritySkill;
            rifle.baseRechargeInterval = reloadBaseDuration;
            //rifle.mustKeyPress = false;
            LanguageAPI.Add("BANDIT2_PRIMARY_ALT_DESCRIPTION", 
                $"Fire a rifle blast for <style=cIsDamage>{rifleDamageCoeff.AsPercent()} damage</style>. " +
                $"Tap to fire faster. Can hold up to 4 bullets.");
        }

        private void EclipseLiteFix(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<GenericSkill>("get_rechargeStock")
                );
            if (!b)
            {
                Log.DebugBreakpoint(nameof(EclipseLiteFix));
                return;
            }
            c.EmitDelegate<Func<int, int>>((_) => { return 1; });
        }

        private void AutoFireOnReload(On.EntityStates.Bandit2.Weapon.Reload.orig_GiveStock orig, Reload self)
        {
            bool g = self.hasGivenStock;
            orig(self);
            if (self.hasGivenStock != g)
            {
                self.characterBody.OnSkillCooldown(self.skillLocator.primary, 1);

                if (self.inputBank && self.inputBank.skill1.down)
                {
                    self.skillLocator.primary.ExecuteIfReady();
                }
            }
        }

        private void ModifyRifleAttacks(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if (self is Bandit2FireRifle || self is FireShotgun2)
            {
                if(self is Bandit2FireRifle)
                {
                    self.spreadBloomValue = rifleSpreadBloom;
                    self.damageCoefficient = rifleDamageCoeff;
                }
                else
                {
                    self.damageCoefficient = shotgunDamageCoeff;
                }
                self.baseDuration = primaryAutoDuration;
                (self as Bandit2FirePrimaryBase).minimumBaseDuration = primaryMinDuration;
            }
            orig(self);
        }

        private void RifleFixedUpdate(On.EntityStates.GenericBulletBaseState.orig_FixedUpdate orig, GenericBulletBaseState self)
        {
            if(self is Bandit2FirePrimaryBase state && self.skillLocator && self.skillLocator.primary)
            {
                //if the primary skill is released, exit early
                //otherwise, if the skill is held for long enough, fire again
                bool heldDown = self.inputBank && self.inputBank.skill1.down && state.duration != state.minimumDuration;
                if (!heldDown)
                    state.duration = state.minimumDuration;
                else
                {
                    state.fixedAge += Time.fixedDeltaTime;
                    if (state.fixedAge >= state.duration)
                    {
                        state.skillLocator.primary.ExecuteIfReady();
                    }
                    return;
                }
            }
            orig(self);
        }

        private void ChangeReloadEnterDuration(On.EntityStates.Bandit2.Weapon.EnterReload.orig_OnEnter orig, EnterReload self)
        {
            EnterReload.baseDuration = reloadEnterBaseDuration;
            orig(self);
            EnterReload.baseDuration = reloadEnterBaseDuration;
        }

        private void ChangeReloadDuration(On.EntityStates.Bandit2.Weapon.Reload.orig_OnEnter orig, EntityStates.Bandit2.Weapon.Reload self)
        {
            EntityStates.Bandit2.Weapon.Reload.baseDuration = reloadBaseDuration;
            orig(self);
        }
        #endregion

        #region secondaries
        void ChangeVanillaSecondaries(SkillFamily family)
        {
            //dagger secondary
            On.EntityStates.Bandit2.Weapon.SlashBlade.OnEnter += ModifyDaggerDamage;
            SkillDef dagger = family.variants[0].skillDef;
            dagger.baseRechargeInterval = daggerCooldown;
            dagger.mustKeyPress = true;
            dagger.interruptPriority = InterruptPriority.PrioritySkill;
            LanguageAPI.Add("BANDIT2_SECONDARY_DESCRIPTION", $"Lunge and slash for <style=cIsDamage>{Tools.ConvertDecimal(daggerDamageCoeff)} damage</style>. " +
                $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>.");

            //shiv secondary
            On.EntityStates.Bandit2.Weapon.Bandit2FireShiv.OnEnter += ModifyShivDamage;
            SkillDef shiv = family.variants[1].skillDef;
            shiv.baseRechargeInterval = shivCooldown;
            shiv.baseMaxStock = shivStock;
            shiv.rechargeStock = shivStock;
            shiv.mustKeyPress = true;
            shiv.interruptPriority = InterruptPriority.PrioritySkill;
            LanguageAPI.Add("BANDIT2_SECONDARY_ALT_DESCRIPTION", $"Throw a hidden blade for <style=cIsDamage>{Tools.ConvertDecimal(shivDamageCoeff)} damage</style>. " +
                $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>. " + (shivStock > 1 ? $"Hold up to {shivStock}." : ""));
        }

        private void ModifyDaggerDamage(On.EntityStates.Bandit2.Weapon.SlashBlade.orig_OnEnter orig, EntityStates.Bandit2.Weapon.SlashBlade self)
        {
            EntityStates.Bandit2.Weapon.SlashBlade.selfForceStrength = daggerSelfForce;
            self.damageCoefficient = daggerDamageCoeff;
            orig(self);
        }

        private void ModifyShivDamage(On.EntityStates.Bandit2.Weapon.Bandit2FireShiv.orig_OnEnter orig, EntityStates.Bandit2.Weapon.Bandit2FireShiv self)
        {
            self.damageCoefficient = shivDamageCoeff;
            orig(self);
        }
        #endregion

        #region utilities
        void ChangeVanillaUtilities(SkillFamily family)
        {
            On.EntityStates.Bandit2.StealthMode.FireSmokebomb += ModifySmokeBomb;
            On.EntityStates.Bandit2.StealthMode.OnExit += ReleaseSmokeBombState;
            SkillDef smokeBomb = family.variants[0].skillDef;
            smokeBomb.baseRechargeInterval = stealthCooldown;
            smokeBomb.interruptPriority = InterruptPriority.PrioritySkill;
            smokeBomb.isCooldownBlockedUntilManuallyReset = true;

            LanguageAPI.Add("BANDIT2_UTILITY_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. " +
                $"Deal <style=cIsDamage>200% damage</style>, then become <style=cIsUtility>invisible</style> until your next attack. " +
                $"While invisible, gain {DamageColor($"+{stealthAspdBonus.AsPercent()}")} attack speed.");
        }

        private void ReleaseSmokeBombState(On.EntityStates.Bandit2.StealthMode.orig_OnExit orig, EntityStates.Bandit2.StealthMode self)
        {
            orig(self);
            if(self.skillLocator && self.skillLocator.utility)
            {
                self.skillLocator.utility.SetBlockedCooldownSkillState(false);
            }
        }

        private void ModifySmokeBomb(On.EntityStates.Bandit2.StealthMode.orig_FireSmokebomb orig, EntityStates.Bandit2.StealthMode self)
        {
            EntityStates.Bandit2.StealthMode.duration = stealthDuration;
            EntityStates.Bandit2.StealthMode.shortHopVelocity = stealthHopVelocity;
            orig(self);
        }
        #endregion

        #region specials
        void ChangeVanillaSpecials(SkillFamily family)
        {
            GetMoreStatCoefficients += BanditFinisher;
            On.RoR2.CharacterBody.SetBuffCount += OnDesperadoTokenAdded;

            On.EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState.OnEnter += PrepSidearmRevolverEnter;
            IL.EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState.FixedUpdate += PrepSidearmRevolverFixedUpdate;
            On.EntityStates.Bandit2.Weapon.BaseFireSidearmRevolverState.OnEnter += FireSidearmRevolverEnter;
            On.EntityStates.Bandit2.Weapon.BaseFireSidearmRevolverState.FixedUpdate += FireSidearmRevolverFixedUpdate;
            On.EntityStates.Bandit2.Weapon.BaseSidearmState.GetMinimumInterruptPriority += RevolverInterruptPriority;
            On.EntityStates.EntityState.ModifyNextState += BanditHipFire;

            //lights out
            On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.ModifyBullet += ModifyLightsOutDamage;
            SkillDef lightsOutRevolver = family.variants[0].skillDef;
            lightsOutRevolver.baseRechargeInterval = lightsOutCooldown;
            lightsOutRevolver.stockToConsume = 0;
            lightsOutRevolver.suppressSkillActivation = true;
            lightsOutRevolver.interruptPriority = InterruptPriority.Skill;
            lightsOutRevolver.keywordTokens = new string[] { SharedUtilsPlugin.noAttackSpeedMultiplicativeKeywordToken, SharedUtilsPlugin.executeKeywordToken };
            LanguageAPI.Add(lightsOutRevolver.skillDescriptionToken, $"<style=cIsDamage>Exacting</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{Tools.ConvertDecimal(lightsOutDamage)} damage</style>. " +
                $"Kills <style=cIsUtility>reset all your cooldowns</style>.");

            //desperado
            string tokenKeyword = "2R4R_DESPERADOTOKEN_KEYWORD";
            On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.ModifyBullet += ModifyDesperadoDamage;
            SkillDef desperadoRevolver = family.variants[1].skillDef;
            desperadoRevolver.baseRechargeInterval = desperadoCooldown;
            desperadoRevolver.stockToConsume = 0;
            desperadoRevolver.suppressSkillActivation = true;
            desperadoRevolver.interruptPriority = InterruptPriority.Skill;
            desperadoRevolver.keywordTokens = new string[] { SharedUtilsPlugin.noAttackSpeedMultiplicativeKeywordToken, SharedUtilsPlugin.executeKeywordToken, tokenKeyword };
            LanguageAPI.Add(desperadoRevolver.skillDescriptionToken, $"<style=cIsDamage>Exacting</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{desperadoDamage.AsPercent()} damage</style>. " +
                $"Kills grant <style=cIsDamage>stacking tokens</style> for " +
                $"<style=cIsDamage>{(desperadoDamagePerToken + desperadoAttackSpeedPerToken).AsPercent()}</style> more Desperado damage.");
            LanguageAPI.Add(tokenKeyword, KeywordText("Desperado Tokens", 
                $"Each token held increases Bandit's <style=cIsDamage>attack speed</style> by " +
                $"<style=cIsDamage>+{desperadoAttackSpeedPerToken.AsPercent()}</style>, and increases the damage of <style=cIsUtility>Desperado</style> " +
                $"by an additional <style=cIsDamage>+{desperadoDamagePerToken.AsPercent()} TOTAL damage</style>. " +
                $"Retain up to <style=cIsUtility>{desperadoTokensPerLevel}</style> tokens per level between stages."));
        }

        private void BanditHipFire(On.EntityStates.EntityState.orig_ModifyNextState orig, EntityState self, EntityState nextState)
        {
            orig(self, nextState);
            if(self is BaseFireSidearmRevolverState && nextState is BasePrepSidearmRevolverState prepState)
            {
                prepState.baseDuration = 0f;
            }
            else if (self is BasePrepSidearmRevolverState prepState2 && nextState is BaseFireSidearmRevolverState fireState)
            {
                bool isHipFire = prepState2.fixedAge > prepState2.baseDuration + revolverHipFireGraceDuration;
                fireState.bulletRadius = isHipFire ? revolverHipFireBulletRadius : revolverBulletRadius;
            }
        }

        private InterruptPriority RevolverInterruptPriority(On.EntityStates.Bandit2.Weapon.BaseSidearmState.orig_GetMinimumInterruptPriority orig, BaseSidearmState self)
        {
            if(self is BasePrepSidearmRevolverState || self is BaseFireSidearmRevolverState)
            {
                if (self is BaseFireSidearmRevolverState)
                {
                    if (self.skillLocator && self.skillLocator.special
                        && self.skillLocator.special.stock >= self.skillLocator.special.skillDef.requiredStock)
                        return InterruptPriority.PrioritySkill;
                }
                //else if (self.fixedAge > self.baseDuration)
                //    return InterruptPriority.Pain;
                return InterruptPriority.Skill;
            }
            return orig(self);
        }

        private void FireSidearmRevolverFixedUpdate(On.EntityStates.Bandit2.Weapon.BaseFireSidearmRevolverState.orig_FixedUpdate orig, BaseFireSidearmRevolverState self)
        {
            if (self.isAuthority)
            {
                if (self.characterBody.isSprinting)
                {
                    self.outer.SetNextState(new ExitSidearmRevolver());
                    return;
                }
            }
            orig(self);

        }

        private void FireSidearmRevolverEnter(On.EntityStates.Bandit2.Weapon.BaseFireSidearmRevolverState.orig_OnEnter orig, BaseFireSidearmRevolverState self)
        {
            self.baseDuration = finisherAimDuration;
            if (self.skillLocator && self.skillLocator.special)
            {
                self.characterBody.OnSkillActivated(self.skillLocator.special);
                self.skillLocator.special.DeductStock(1);
            }

            orig(self);

            self.duration = finisherAimDuration;
        }

        private void PrepSidearmRevolverFixedUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<EntityState>("get_fixedAge")
                );
            if (!b)
            {
                Log.DebugBreakpoint(nameof(PrepSidearmRevolverFixedUpdate));
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, BasePrepSidearmRevolverState, float>>((fixedAge, self) =>
            {
                //prevents the skill from firing until the input is released
                if (self.inputBank.skill4.down)
                {
                    if(fixedAge > self.duration)
                    {
                        if(self.duration > 0)
                        {
                            self.duration = 0;

                            string muzzleName = "MuzzlePistol";
                            Util.PlaySound(AimStunDrone.exitSoundString, self.gameObject);
                            GameObject effectPrefab = ChargeArrow.muzzleflashEffectPrefab;
                            if (effectPrefab)
                            {
                                EffectManager.SimpleMuzzleFlash(effectPrefab, self.gameObject, muzzleName, false);
                            }
                        }

                        if (self.inputBank && self.inputBank.skill1.down && !self.inputBank.skill1.wasDown)
                        {
                            self.outer.SetNextState(GetNextState());
                        }
                    }

                    return -1;
                }

                return fixedAge;

                EntityState GetNextState()
                {
                    if (self is FireSidearmResetRevolver)
                        return new PrepSidearmResetRevolver();
                    if (self is FireSidearmSkullRevolver)
                        return new PrepSidearmSkullRevolver();
                    return new ExitSidearmRevolver();
                }
            });
        }

        private void PrepSidearmRevolverEnter(On.EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState.orig_OnEnter orig, EntityStates.Bandit2.Weapon.BasePrepSidearmRevolverState self)
        {
            bool isHipFire = false;
            if (self.inputBank.skill4.down && self.baseDuration <= 0.1f)
            {
                isHipFire = true;
            }
            self.baseDuration = revolverDrawDuration;
            orig(self);
            self.duration = isHipFire ? 0.1f : revolverDrawDuration;
        }

        static BuffIndex banditSkullBuff => RoR2Content.Buffs.BanditSkull?.buffIndex ?? BuffIndex.None;
        static BuffIndex banditSkullSurplusBuff => CommonAssets.desperadoTokenSurplusBuff?.buffIndex ?? BuffIndex.None;
        private void OnDesperadoTokenAdded(On.RoR2.CharacterBody.orig_SetBuffCount orig, CharacterBody self, BuffIndex buffType, int newCount)
        {
            if (buffType != banditSkullBuff)
            {
                orig(self, buffType, newCount);
                return;
            }
            // the following code is only run for desperado tokens!
            int baseTokenCount = newCount;// self.GetBuffCount(banditSkullBuff);
            int surplusTokenCount = useBanditSkullSurplus ? self.GetBuffCount(banditSkullSurplusBuff) : 0;
            int totalTokenCount = baseTokenCount + surplusTokenCount;

            int max = MasterDesperadoTokenTracker.GetMaxPersistentTokenCountFromLevel(self.level);
            if (totalTokenCount > max && useBanditSkullSurplus)
            {
                orig(self, banditSkullBuff, 0);
                orig(self, banditSkullSurplusBuff, totalTokenCount);
            }
            else
            {
                orig(self, banditSkullBuff, totalTokenCount);
                orig(self, banditSkullSurplusBuff, 0);
            }

            if (self.master == null || !NetworkServer.active)
                return;

            if(!self.master.TryGetComponent(out MasterDesperadoTokenTracker tracker))
            {
                tracker = self.master.gameObject.AddComponent<MasterDesperadoTokenTracker>();
                tracker.master = self.master;
            }
            tracker.SetTokenCount(totalTokenCount);
        }


        private void BanditFinisher(CharacterBody sender, MoreStatHookEventArgs args)
        {
            bool hasBanditExecutionBuff = sender.HasBuff(CommonAssets.desperadoExecutionDebuff) || sender.HasBuff(CommonAssets.lightsoutExecutionDebuff);
            args.ModifyBaseExecutionThreshold(SharedUtilsPlugin.GetSurvivorExecuteThreshold(sender.isBoss), hasBanditExecutionBuff);
        }

        private void ModifyLightsOutDamage(On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmResetRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damage = lightsOutDamage * self.damageStat * self.attackSpeedStat;
            bulletAttack.damageType.damageType = bulletAttack.damageType.damageType & ~DamageType.BonusToLowHealth;
        }

        private void ModifyDesperadoDamage(On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            int tokenCount = 0;
            if (self.characterBody)
            {
                tokenCount = self.characterBody.GetBuffCount(RoR2Content.Buffs.BanditSkull) + self.characterBody.GetBuffCount(CommonAssets.desperadoTokenSurplusBuff);
            }
            bulletAttack.damage = desperadoDamage * self.damageStat * (self.attackSpeedStat + (desperadoDamagePerToken * (float)tokenCount));
            bulletAttack.damageType.damageType = bulletAttack.damageType.damageType & ~DamageType.BonusToLowHealth;
        }
        #endregion
    }
}

