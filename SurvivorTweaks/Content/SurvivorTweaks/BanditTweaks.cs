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

namespace SurvivorTweaks.SurvivorTweaks
{
    class BanditTweaks : SurvivorTweakBase<BanditTweaks>
    {
        public static float shotgunDamageCoeff = 0.75f; //1
        public static float rifleDamageCoeff = 2.8f; // 3.3
        public static float rifleSpreadBloom = 0.4f; //0.5f
        public static float reloadBaseDuration = 0.6f; //0.5

        public static float daggerDamageCoeff = 6f; //3.6
        public static float daggerCooldown = 6f; //4 
        public static float daggerSelfForce = 1500f; //0
        public static float shivDamageCoeff = 4f; //2.4
        public static float shivCooldown = 7f; //4
        public static int shivStock = 2; //1

        public static float stealthHopVelocity = 13f; //15
        public static float stealthDuration = 4f; //3
        public static float stealthCooldown = 9f; //6
        public static float stealthAspdBonus = 2f; //0

        public static float lightsOutDamage = 7f; //6
        public static float lightsOutCooldown = 8f; //4
        public static float desperadoDamage = 2.5f; //6
        public static float desperadoCooldown = 3f; //4

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

            CharacterBody.onBodyStartGlobal += RecalculateTokenAmount;
            TeleporterInteraction.onTeleporterFinishGlobal += OnAdvanceStageSaveTokens;
            ShowReport.OnEnter += ResetTokens;

            //On.RoR2.CharacterBody.RecalculateStats += BackstabPassiveCritChance;
            On.RoR2.CharacterBody.Start += BackstabPassiveCritChance;
            LanguageAPI.Add("BANDIT2_PASSIVE_DESCRIPTION", "All attacks from <style=cIsDamage>behind</style> are <style=cIsDamage>Critical Strikes</style>. " +
                "All <style=cIsDamage>Critical Strike Chance</style> is instead converted into <style=cIsDamage>Critical Strike Damage</style>.");
        }

        private void BanditCloakBuff(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.HasBuff(RoR2Content.Buffs.Cloak) && sender.bodyIndex == BodyCatalog.FindBodyIndex("Bandit2Body"))
            {
                args.attackSpeedMultAdd += stealthAspdBonus;
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

        #region Desperado
        public struct BodyDesperadoPair
        {
            public short id;
            public int tokens;
        }
        public static List<BodyDesperadoPair> lastStageDesperadoTokens = new List<BodyDesperadoPair>();

        private void ResetTokens(ShowReport.orig_OnEnter orig, EntityStates.GameOver.ShowReport self)
        {
            lastStageDesperadoTokens = new List<BodyDesperadoPair>();
            orig(self);
        }

        private void OnAdvanceStageSaveTokens(TeleporterInteraction interaction)
        {
            lastStageDesperadoTokens = new List<BodyDesperadoPair>(PlayerCharacterMasterController.instances.Count);

            if (lastStageDesperadoTokens.Capacity == 0)
            {
                Debug.Log("Desperado Token Thing Fail!");
            }

            for (int i = 0; i < lastStageDesperadoTokens.Capacity; i++) 
            {
                PlayerCharacterMasterController instance = PlayerCharacterMasterController.instances[i];
                CharacterBody body = instance.master.GetBody();

                BodyDesperadoPair newPair = new BodyDesperadoPair();
                newPair.id = body.playerControllerId;
                newPair.tokens = body.GetBuffCount(RoR2Content.Buffs.BanditSkull);

                //Debug.Log(i);
                lastStageDesperadoTokens.Add(newPair);
            }
            /*foreach (PlayerCharacterMasterController instance in PlayerCharacterMasterController.instances)
            {
                BodyDesperadoPair newPair = new BodyDesperadoPair();
                newPair.body = instance.master.GetBody();
                newPair.tokens = newPair.body.GetBuffCount(RoR2Content.Buffs.BanditSkull);

                lastStageDesperadoTokens.Add(newPair);
            }*/
        }

        private void RecalculateTokenAmount(CharacterBody body)
        {
            if (!NetworkServer.active)
                return;

            if (body.isPlayerControlled && body.teamComponent.teamIndex == TeamIndex.Player)
            {
                if(lastStageDesperadoTokens.Capacity == 0)
                {
                    lastStageDesperadoTokens = new List<BodyDesperadoPair>(PlayerCharacterMasterController.instances.Count);
                }
                try
                {
                    for (int i = 0; i < lastStageDesperadoTokens.Capacity; i++)
                    {
                        BodyDesperadoPair pair = lastStageDesperadoTokens[i];
                        if (body.playerControllerId == pair.id && pair.tokens > 0)
                        {
                            //Debug.Log("Matching body found in desperado pair!");
                            int currentTokenAmount = body.GetBuffCount(RoR2Content.Buffs.BanditSkull);
                            int newTokenAmount = (int)Mathf.Min(pair.tokens, body.level * 2);

                            if (currentTokenAmount < newTokenAmount)
                            {
                                for (int k = 0; k < newTokenAmount - currentTokenAmount; k++)
                                {
                                    body.AddBuff(RoR2Content.Buffs.BanditSkull);
                                }
                            }
                        }
                    }
                }
                catch
                {
                    //Debug.Log("Error detected when trying to recalculate desperado tokens!");
                }
            }
        }
        #endregion

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
                damageInfo.damageType |= DamageType.NonLethal;
            }

            orig(self, damageInfo);

            if (NetworkServer.active && (self.health <= 0 || !self.alive) && attackerBody != null && attackerBody.bodyIndex == BodyCatalog.FindBodyIndexCaseInsensitive("Bandit2Body"))
            {
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
        }

        #region primaries
        void ChangeVanillaPrimaries(SkillFamily family)
        {
            On.EntityStates.GenericBulletBaseState.OnEnter += ModifyRifleAttacks;
            On.EntityStates.Bandit2.Weapon.Reload.OnEnter += ChangeReloadDuration;

            //shotgun primary
            LanguageAPI.Add("BANDIT2_PRIMARY_DESCRIPTION", $"Fire a shotgun burst for <style=cIsDamage>5x{Tools.ConvertDecimal(shotgunDamageCoeff)} damage</style>. Can hold up to 4 shells.");

            //rifle primary
            LanguageAPI.Add("BANDIT2_PRIMARY_ALT_DESCRIPTION", $"Fire a rifle blast for <style=cIsDamage>{Tools.ConvertDecimal(rifleDamageCoeff)} damage</style>. Can hold up to 4 bullets.");

        }

        private void ModifyRifleAttacks(On.EntityStates.GenericBulletBaseState.orig_OnEnter orig, EntityStates.GenericBulletBaseState self)
        {
            if(self is EntityStates.Bandit2.Weapon.Bandit2FireRifle)
            {
                self.spreadBloomValue = rifleSpreadBloom;
                self.damageCoefficient = rifleDamageCoeff;
            }
            else if(self is EntityStates.Bandit2.Weapon.FireShotgun2)
            {
                self.damageCoefficient = shotgunDamageCoeff;
            }
            orig(self);
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
            LanguageAPI.Add("BANDIT2_SECONDARY_DESCRIPTION", $"Lunge and slash for <style=cIsDamage>{Tools.ConvertDecimal(daggerDamageCoeff)} damage</style>. " +
                $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>.");

            //shiv secondary
            On.EntityStates.Bandit2.Weapon.Bandit2FireShiv.OnEnter += ModifyShivDamage;
            SkillDef shiv = family.variants[1].skillDef;
            shiv.baseRechargeInterval = shivCooldown;
            shiv.baseMaxStock = shivStock;
            shiv.rechargeStock = shivStock;
            shiv.mustKeyPress = true;
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
            family.variants[0].skillDef.baseRechargeInterval = stealthCooldown;

            LanguageAPI.Add("BANDIT2_UTILITY_DESCRIPTION", $"<style=cIsDamage>Stunning</style>. " +
                $"Deal <style=cIsDamage>200% damage</style>, become <style=cIsUtility>invisible</style>, then deal <style=cIsDamage>200% damage</style> again." +
                $"While cloaked, gain +{DamageColor(ConvertDecimal(stealthAspdBonus))} attack speed.");
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
            GetHitBehavior += BanditExecutionOnHit;
            GetMoreStatCoefficients += BanditFinisher;

            //lights out
            On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.ModifyBullet += ModifyLightsOutDamage;
            family.variants[0].skillDef.baseRechargeInterval = lightsOutCooldown;
            special.variants[0].skillDef.keywordTokens = new string[2] { "KEYWORD_SLAYER", SharedUtilsPlugin.executeKeywordToken };
            LanguageAPI.Add("BANDIT2_SPECIAL_DESCRIPTION", $"<style=cIsDamage>Slayer</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{Tools.ConvertDecimal(lightsOutDamage)} damage</style>. " +
                $"Kills <style=cIsUtility>reset all your cooldowns</style>.");

            //desperado
            On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.ModifyBullet += ModifyDesperadoDamage;
            family.variants[1].skillDef.baseRechargeInterval = desperadoCooldown;
            special.variants[1].skillDef.keywordTokens = new string[2] { "KEYWORD_SLAYER", SharedUtilsPlugin.executeKeywordToken };
            LanguageAPI.Add("BANDIT2_SPECIAL_ALT_DESCRIPTION", $"<style=cIsDamage>Slayer</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{Tools.ConvertDecimal(desperadoDamage)} damage</style>. " +
                $"Kills grant <style=cIsDamage>stacking tokens</style> for <style=cIsDamage>10%</style> more Desperado damage.");
        }

        private void BanditExecutionOnHit(CharacterBody attackerBody, DamageInfo damageInfo, CharacterBody victimBody)
        {
            if (NetworkServer.active)
            {
                if (damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill) || (damageInfo.damageType & DamageType.ResetCooldownsOnKill) != 0UL)
                {
                    victimBody.AddTimedBuff(CommonAssets.lightsoutExecutionDebuff, 0.6f);
                }
                if (damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill) || (damageInfo.damageType & DamageType.GiveSkullOnKill) != 0UL)
                {
                    victimBody.AddTimedBuff(CommonAssets.desperadoExecutionDebuff, 0.6f);
                }
            }
        }

        private void BanditFinisher(CharacterBody sender, MoreStatHookEventArgs args)
        {
            bool hasBanditExecutionBuff = sender.HasBuff(CommonAssets.desperadoExecutionDebuff) || sender.HasBuff(CommonAssets.lightsoutExecutionDebuff);
            args.ModifyBaseExecutionThreshold(SharedUtilsPlugin.survivorExecuteThreshold, hasBanditExecutionBuff);
        }

        private void ModifyLightsOutDamage(On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmResetRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damage = lightsOutDamage * self.damageStat;
            //bulletAttack.damageType = bulletAttack.damageType & ~DamageType.BonusToLowHealth;
        }

        private void ModifyDesperadoDamage(On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damage = desperadoDamage * self.damageStat;
            //bulletAttack.damageType = bulletAttack.damageType & ~DamageType.BonusToLowHealth;

            int num = 0;
            if (self.characterBody)
            {
                num = self.characterBody.GetBuffCount(RoR2Content.Buffs.BanditSkull);
            }
            bulletAttack.damage *= 1f + 0.1f * (float)num;
        }
        #endregion
    }
}

