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
        /// <summary>
        /// skull surplus is a cosmetic buff that helps indicate when bandit is above his transferrable token limit;
        /// having this set to false just means that the regular buff is used. it does not affect the token transference mechanic
        /// </summary>
        [AutoConfig("Ability Tweaks (Special) : Desperado : Use Token Surplus Buff", 
            "If true, Desperado tokens past Bandit's transferable limit will be indicated with a separate buff. " +
            "This is purely cosmetic and does not affect the token transference mechanic.", false)]
        public static bool useBanditSkullSurplus = false;

        [AutoConfig("Bandit : Base Max Health", "Scales 30% per level. Vanilla is 110", 90f)]
        public static float baseMaxHealth = 90f;//110
        [AutoConfig("Keywords : Hemorrhage : Base Damage Coefficient", "Total damage of the DOT. Expressed as a percentage (eg 7.5 is 750%). Vanilla is 20", 7.5f)]
        public static float hemorrhageDamageBase = 7.5f;
        [AutoConfig("Keywords : Hemorrhage : Damage Multiplier To Full Health Enemies", "Vanilla is 1", 5f)]
        public static float hemorrhageDamageMaxMultiplier = 5f;
        [AutoConfig("Keywords : Hemorrhage : Nonlethality", "Set to true for Hemorrhage to be nonlethal. Vanilla is false", true)]
        public static bool hemorrhageNonLethality = true;
        public static float hemorrhageDamageMax => Mathf.Max(0, hemorrhageDamageMaxMultiplier - 1);

        [AutoConfig("Ability Tweaks (Passive) : Backstab : Use Crit Conversion", "If true, all crit chance will be converted to crit damage (like Railgunner)", false)]
        public static bool useBanditCritConversion = false;
        [AutoConfig("Ability Tweaks (Passive) : Stealth : Attack Speed While Stealthed", "Expressed as a percentage (eg 0.6 is 60%). Vanilla is 0", 0.6f)]
        public static float stealthAspdBonus = 0.6f; //0

        [AutoConfig("Ability Tweaks (Primary) : Burst (Shotgun) : Damage Coefficient Per Bullet", "Expressed as a percentage (eg 0.7 is 70%). Vanilla is 1", 0.7f)]
        public static float shotgunDamageCoeff = 0.7f; //1 //times 5
        [AutoConfig("Ability Tweaks (Primary) : Blast (Rifle) : Damage Coefficient", "Expressed as a percentage (eg 2.8 is 280%). Vanilla is 3.3", 2.8f)]
        public static float rifleDamageCoeff = 2.8f; // 3.3
        [AutoConfig("Ability Tweaks (Primary) : Blast (Rifle) : Spread Bloom", "Vanilla is 0.5", 0.3f)]
        public static float rifleSpreadBloom = 0.3f; //0.5f
        [AutoConfig("Ability Tweaks (Primary) : Base Reload Delay", "Duration to delay reloading after firing. Expressed in seconds. Vanilla is 0.25", 0.4)]
        public static float reloadEnterBaseDuration = 0.4f; //0.25f
        [AutoConfig("Ability Tweaks (Primary) : Base Reload Duration", "Duration between reloading bullets. Expressed in seconds. Vanilla is 0.3", 0.5f)]
        public static float reloadBaseDuration = 0.5f; //0.3f
        [AutoConfig("Ability Tweaks (Primary) : Base Attack Duration (Minimum/Tap Shot)", "Minimum duration while mashing primary attack. Expressed in seconds. Vanilla is 0", 0.1f)]
        public static float primaryMinDuration = 0.1f;
        [AutoConfig("Ability Tweaks (Primary) : Base Attack Duration (Held/Auto)", "Duration to auto fire while holding primary attack. Expressed in seconds. Vanilla is N/A", 0.325f)]
        public static float primaryAutoDuration = 0.325f;

        [AutoConfig("Ability Tweaks (Secondary) : Serrated Dagger : Damage Coefficient", "Expressed as a percentage (eg 6.0 is 600%). Vanilla is 3.6", 6f)]
        public static float daggerDamageCoeff = 6f; //3.6
        [AutoConfig("Ability Tweaks (Secondary) : Serrated Dagger : Base Cooldown", "Expressed in seconds. Vanilla is 4", 6f)]
        public static float daggerCooldown = 6f; //4 
        [AutoConfig("Ability Tweaks (Secondary) : Serrated Dagger : Lunge Force", "Vanilla is 0", 1500f)]
        public static float daggerSelfForce = 1500f; //0

        [AutoConfig("Ability Tweaks (Secondary) : Serrated Shiv : Damage Coefficient", "Expressed as a percentage (eg 4.0 is 400%). Vanilla is 2.4", 4f)]
        public static float shivDamageCoeff = 4f; //2.4
        [AutoConfig("Ability Tweaks (Secondary) : Serrated Shiv : Base Cooldown", "Expressed in seconds. Vanilla is 4", 7f)]
        public static float shivCooldown = 7f; //4
        [AutoConfig("Ability Tweaks (Secondary) : Serrated Shiv : Base Max Stock", "Vanilla is 1", 2)]
        public static int shivStock = 2; //1

        [AutoConfig("Ability Tweaks (Utility) : Smoke Bomb : Enter/Exit Hop Velocity", "Vanilla is 15", 13f)]
        public static float stealthHopVelocity = 13f; //15
        [AutoConfig("Ability Tweaks (Utility) : Smoke Bomb : Stealth Duration", "Expressed in seconds. Vanilla is 3", 3f)]
        public static float stealthDuration = 3f; //3
        [AutoConfig("Ability Tweaks (Utility) : Smoke Bomb : Base Cooldown", "Expressed in seconds. Vanilla is 6", 4f)]
        public static float stealthCooldown = 4f; //6

        [AutoConfig("Ability Tweaks (Special) : Lights Out : Damage Coefficient", "Expressed as a percentage (eg 4.5 is 450%). Vanilla is 6", 4.5f)]
        public static float lightsOutDamage = 4.5f; //6
        [AutoConfig("Ability Tweaks (Special) : Lights Out : Base Cooldown", "Expressed in seconds. Vanilla is 4", 8f)]
        public static float lightsOutCooldown = 8f; //4

        [AutoConfig("Ability Tweaks (Special) : Desperado : Damage Coefficient", "Expressed as a percentage (eg 3.0 is 300%). Vanilla is 6", 3.0f)]
        public static float desperadoDamage = 3f; //6
        [AutoConfig("Ability Tweaks (Special) : Desperado : Base Cooldown", "Expressed in seconds. Vanilla is 4", 3f)]
        public static float desperadoCooldown = 3f; //4
        [AutoConfig("Ability Tweaks (Special) : Desperado : Desperado Damage Multiplier Per Token", 
            "Additive with bonuses from Exacting. Expressed as a percentage (eg 0.075 is 7.5%). Vanilla is 0.1", 0.075f)]
        public static float desperadoDamagePerToken = 0.075f; //0.1f
        [AutoConfig("Ability Tweaks (Special) : Desperado : Desperado Attack Speed Per Token",
            "Additive with direct damage bonus. Expressed as a percentage (eg 0.025 is 2.5%). Vanilla is 0", 0.025f)]
        public static float desperadoAttackSpeedPerToken = 0.025f; //0f
        [AutoConfig("Ability Tweaks (Special) : Desperado : Token Transference Rate Per Level", "Vanilla is 0", 2)]
        public static int desperadoTokensPerLevel = 2;

        [AutoConfig("Ability Tweaks (Special) : Finisher Debuff Duration", "Expressed in seconds. Vanilla is 0", 1.6f)]
        public static float revolverDebuffDuration = 1.6f;//0f
        [AutoConfig("Ability Tweaks (Special) : Revolver Wind-Up Duration", "Minimum time to cast revolver Special. Expressed in seconds. Vanilla is idk", 1.6f)]
        public static float revolverDrawDuration = 0.8f; //idk
        [AutoConfig("Ability Tweaks (Special) : Revolver Max Aim Duration", "Maximum time to cast revolver Special. Expressed in seconds. Vanilla is N/A", 1.6f)]
        public static float finisherAimDuration = 5f; //n/a
        [AutoConfig("Ability Tweaks (Special) : Revolver Hardscope Bullet Width", "Affects aim assist. Expressed in seconds. Vanilla is idk", 1.5f)]
        public static float revolverBulletRadius = 1.5f;
        [AutoConfig("Ability Tweaks (Special) : Revolver Hipfire Bullet Width", "Affects aim assist. Expressed in seconds. Vanilla is N/A", 3f)]
        public static float revolverHipFireBulletRadius = 3.0f;
        [AutoConfig("Ability Tweaks (Special) : Revolver Hipfire Grace Duration", "Maximum window after drawing revolver Special to gain hip fire bonus. Expressed in seconds. Vanilla is N/A", 0.25f)]
        public static float revolverHipFireGraceDuration = 0.25f;

        public override string bodyName => "Bandit2Body";
        public override string survivorName => "Bandit";

        public override void Init()
        {
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Bandit2.Bandit2Body_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);
                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMaxHealth = baseMaxHealth;
                body.levelMaxHealth = baseMaxHealth * 0.3f;

                GetSkillsFromBodyObject(bodyObject);
                ChangeVanillaPrimaries(primary);
                ChangeVanillaSecondaries(secondary);
                ChangeVanillaUtilities(utility);
                ChangeVanillaSpecials(special);
            });


            GetStatCoefficients += BanditCloakBuff;
            On.RoR2.HealthComponent.TakeDamageProcess += BanditTweaksTakeDamage;
            GlobalEventManager.onCharacterDeathGlobal += BanditOnKill;
            LanguageAPI.Add("KEYWORD_SUPERBLEED",
                $"<style=cKeywordName>Hemorrhage</style>" +
                $"<style=cSub>Bleed enemies for <style=cIsDamage>{Tools.ConvertDecimal(hemorrhageDamageBase)}</style> base damage over 15s. " +
                $"Can deal <style=cIsDamage>up to {hemorrhageDamageMax}x</style> as much damage against healthy enemies. " +
                $"<i>Hemorrhage can stack.</i></style>");

            //CharacterBody.onBodyStartGlobal += RecalculateTokenAmount;
            //TeleporterInteraction.onTeleporterFinishGlobal += OnAdvanceStageSaveTokens;
            //ShowReport.OnEnter += ResetTokens;

            //On.RoR2.CharacterBody.RecalculateStats += BackstabPassiveCritChance;
            if (useBanditCritConversion)
            {
                On.RoR2.CharacterBody.Start += BackstabPassiveCritChance;
                LanguageAPI.Add("BANDIT2_PASSIVE_DESCRIPTION", "All attacks from <style=cIsDamage>behind</style> are <style=cIsDamage>Critical Strikes</style>. " +
                    "All <style=cIsDamage>Critical Strike Chance</style> is instead converted into <style=cIsDamage>Critical Strike Damage</style>.");
            }
        }

        private void BanditOnKill(DamageReport damageReport)
        {
            if (!NetworkServer.active)
                return;
            if (damageReport.damageInfo.damageType.damageSource != DamageSource.NoneSpecified && noFinishersFromSkillSourcedDamage)
                return;

            if (damageReport.attackerBody == null || damageReport.victimBody == null || damageReport.attackerBody.bodyIndex != BodyCatalog.FindBodyIndexCaseInsensitive("Bandit2Body"))
                return;
            HealthComponent victimHealthComponent = damageReport.victimBody.healthComponent;
            if (victimHealthComponent.health > 0 || victimHealthComponent.alive)
                return;

            if (damageReport.victimBody.HasBuff(CommonAssets.lightsoutExecutionDebuff.buffIndex) && !damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill))
            {
                damageReport.victimBody.RemoveBuff(CommonAssets.lightsoutExecutionDebuff.buffIndex);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2ResetEffect"), new EffectData
                {
                    origin = damageReport.damageInfo.position
                }, true);
                SkillLocator skillLocator = damageReport.attackerBody.skillLocator;
                if (skillLocator)
                {
                    skillLocator.ResetSkills();
                }
            }
            if (damageReport.victimBody.HasBuff(CommonAssets.desperadoExecutionDebuff.buffIndex) && !damageReport.damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill))
            {
                damageReport.victimBody.RemoveBuff(CommonAssets.desperadoExecutionDebuff.buffIndex);

                EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/ImpactEffects/Bandit2KillEffect"), new EffectData
                {
                    origin = damageReport.damageInfo.position
                }, true);
                if (damageReport.attackerBody)
                {
                    damageReport.attackerBody.AddBuff(RoR2Content.Buffs.BanditSkull);
                }
            }
        }

        private void BanditCloakBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.bodyIndex == BodyCatalog.FindBodyIndex("Bandit2Body"))
            {
                if (sender.HasBuff(RoR2Content.Buffs.Cloak))
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
            if (damageInfo.attacker)
                attackerBody = damageInfo.attacker.GetComponent<CharacterBody>();

            if (damageInfo.dotIndex == DotController.DotIndex.SuperBleed)
            {
                //float scalingBleedDamage = damageInfo.damage * hemmorageDamageMultiplier * self.combinedHealthFraction;
                //float normalBleedDamage = damageInfo.damage * hemmorageDamageBase;
                float multiplier = 1 + hemorrhageDamageMax * self.combinedHealthFraction;
                float damage2 = damageInfo.damage * multiplier;
                damageInfo.damage = damage2;// scalingBleedDamage + normalBleedDamage;
                if(hemorrhageNonLethality == true)
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
            if (self.hasGivenStock != g && self.hasGivenStock == true)
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
                if (self is Bandit2FireRifle)
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
            if (self is Bandit2FirePrimaryBase state && self.skillLocator && self.skillLocator.primary)
            {
                //if the primary skill is released, exit early
                //otherwise, if the skill is held for long enough, fire again
                bool heldDown = self.inputBank && self.inputBank.skill1.down && state.duration != state.minimumDuration && state.skillLocator.primary.stock > 0;
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
            if (self.skillLocator && self.skillLocator.utility)
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
            lightsOutRevolver.keywordTokens = new string[] { SharedUtilsPlugin.noAttackSpeedMultiplicativeKeywordToken, "KEYWORD_SLAYER", SharedUtilsPlugin.executeKeywordToken };
            LanguageAPI.Add(lightsOutRevolver.skillDescriptionToken, $"<style=cIsDamage>Exacting</style>. <style=cIsDamage>Slayer</style>. <style=cIsHealth>Finisher</style>. " +
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
            if (self is BaseFireSidearmRevolverState && nextState is BasePrepSidearmRevolverState prepState)
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
            if (self is BasePrepSidearmRevolverState || self is BaseFireSidearmRevolverState)
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
                    if (fixedAge > self.duration)
                    {
                        if (self.duration > 0)
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

            if (!self.master.TryGetComponent(out MasterDesperadoTokenTracker tracker))
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
            //bulletAttack.damageType.damageType = bulletAttack.damageType.damageType & ~DamageType.BonusToLowHealth;
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

