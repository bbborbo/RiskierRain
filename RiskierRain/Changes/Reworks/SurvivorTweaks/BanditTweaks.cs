using BepInEx;
using RiskierRain.CoreModules;
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
using static RiskierRain.CoreModules.StatHooks;
using static BorboStatUtils.BorboStatUtils;
using UnityEngine.AddressableAssets;

namespace RiskierRain.SurvivorTweaks
{
    class BanditTweaks : SurvivorTweakModule
    {
        public static float shotgunDamageCoeff = 0.75f; //1
        public static float rifleDamageCoeff = 2.8f; // 3.3
        public static float rifleSpreadBloom = 0.4f; //0.5f
        public static float reloadBaseDuration = 0.7f; //0.5

        public static float daggerDamageCoeff = 6f; //3.6
        public static float daggerCooldown = 6f; //4 
        public static float daggerSelfForce = 1500f; //0
        public static float shivDamageCoeff = 4f; //2.4
        public static float shivCooldown = 6f; //4

        public static float stealthHopVelocity = 13f; //15
        public static float stealthDuration = 4f; //3
        public static float stealthCooldown = 9f; //6

        public static float lightsOutDamage = 7f; //6
        public static float lightsOutCooldown = 8f; //4
        public static float desperadoDamage = 2.5f; //6
        public static float desperadoCooldown = 3f; //4

        public static float hemmorageDamageBase = 20;
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

        private void BackstabPassiveCritChance(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (self.canPerformBackstab || self.bodyFlags.HasFlag(CharacterBody.BodyFlags.HasBackstabPassive))
            {
                Inventory inv = self.inventory;
                if (inv)
                {
                    int itemCount = inv.GetItemCount(DLC1Content.Items.ConvertCritChanceToCritDamage);
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
            bool isAlreadyDead = (self.health <= 0 || !self.alive);
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

            if(!isAlreadyDead)
            {
                if (damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill))
                {
                    self.body.AddTimedBuffAuthority(lightsoutExecutionDebuff.buffIndex, 0.5f);
                }
                if (damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill))
                {
                    self.body.AddTimedBuffAuthority(desperadoExecutionDebuff.buffIndex, 0.5f);
                }
            }

            orig(self, damageInfo);

            if((self.health <= 0 || !self.alive) && attackerBody != null && !isAlreadyDead)
            {
                if (self.body.HasBuff(lightsoutExecutionDebuff.buffIndex) && !damageInfo.damageType.damageType.HasFlag(DamageType.ResetCooldownsOnKill))
                {
                    self.body.RemoveBuff(lightsoutExecutionDebuff.buffIndex);

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
                if (self.body.HasBuff(desperadoExecutionDebuff.buffIndex) && !damageInfo.damageType.damageType.HasFlag(DamageType.GiveSkullOnKill))
                {
                    self.body.RemoveBuff(desperadoExecutionDebuff.buffIndex);

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
            family.variants[0].skillDef.baseRechargeInterval = daggerCooldown;
            LanguageAPI.Add("BANDIT2_SECONDARY_DESCRIPTION", $"Lunge and slash for <style=cIsDamage>{Tools.ConvertDecimal(daggerDamageCoeff)} damage</style>. " +
                $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>.");

            //shiv secondary
            On.EntityStates.Bandit2.Weapon.Bandit2FireShiv.OnEnter += ModifyShivDamage;
            family.variants[1].skillDef.baseRechargeInterval = shivCooldown;
            LanguageAPI.Add("BANDIT2_SECONDARY_ALT_DESCRIPTION", $"Throw a hidden blade for <style=cIsDamage>{Tools.ConvertDecimal(shivDamageCoeff)} damage</style>. " +
                $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>.");
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

            //LanguageAPI.Add("BANDIT2_UTILITY_DESCRIPTION", $"Throw a hidden blade for <style=cIsDamage>{Tools.ConvertDecimal(shivDamageCoeff)}</style>. " +
            //    $"Critical Strikes also cause <style=cIsHealth>hemorrhaging</style>.");
        }

        private void ModifySmokeBomb(On.EntityStates.Bandit2.StealthMode.orig_FireSmokebomb orig, EntityStates.Bandit2.StealthMode self)
        {
            EntityStates.Bandit2.StealthMode.duration = stealthDuration;
            EntityStates.Bandit2.StealthMode.shortHopVelocity = stealthHopVelocity;
            orig(self);
        }
        #endregion

        #region specials
        public static BuffDef desperadoExecutionDebuff;
        public static BuffDef lightsoutExecutionDebuff;
        void ChangeVanillaSpecials(SkillFamily family)
        {
            GetExecutionThreshold += BanditFinisher;
            AddBanditExecutionDebuff();

            //lights out
            On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.ModifyBullet += ModifyLightsOutDamage;
            family.variants[0].skillDef.baseRechargeInterval = lightsOutCooldown;
            special.variants[0].skillDef.keywordTokens = new string[2] { "KEYWORD_SLAYER", CoreModules.Assets.executeKeywordToken };
            LanguageAPI.Add("BANDIT2_SPECIAL_DESCRIPTION", $"<style=cIsDamage>Slayer</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{Tools.ConvertDecimal(lightsOutDamage)} damage</style>. " +
                $"Kills <style=cIsUtility>reset all your cooldowns</style>.");

            //desperado
            On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.ModifyBullet += ModifyDesperadoDamage;
            family.variants[1].skillDef.baseRechargeInterval = desperadoCooldown;
            special.variants[1].skillDef.keywordTokens = new string[2] { "KEYWORD_SLAYER", CoreModules.Assets.executeKeywordToken };
            LanguageAPI.Add("BANDIT2_SPECIAL_ALT_DESCRIPTION", $"<style=cIsDamage>Slayer</style>. <style=cIsHealth>Finisher</style>. " +
                $"Fire a revolver shot for <style=cIsDamage>{Tools.ConvertDecimal(desperadoDamage)} damage</style>. " +
                $"Kills grant <style=cIsDamage>stacking tokens</style> for <style=cIsDamage>10%</style> more Desperado damage.");
        }

        private void AddBanditExecutionDebuff()
        {
            desperadoExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                desperadoExecutionDebuff.buffColor = Color.black;
                desperadoExecutionDebuff.canStack = false;
                desperadoExecutionDebuff.isDebuff = true;
                desperadoExecutionDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
                desperadoExecutionDebuff.name = "DesperadoExecutionDebuff";
                desperadoExecutionDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffCrippleIcon.tif").WaitForCompletion();
            }
            CoreModules.Assets.buffDefs.Add(desperadoExecutionDebuff);
            lightsoutExecutionDebuff = ScriptableObject.CreateInstance<BuffDef>();
            {
                lightsoutExecutionDebuff.buffColor = Color.black;
                lightsoutExecutionDebuff.canStack = false;
                lightsoutExecutionDebuff.isDebuff = true;
                lightsoutExecutionDebuff.flags |= BuffDef.Flags.ExcludeFromNoxiousThorns;
                lightsoutExecutionDebuff.name = "LightsOutExecutionDebuff";
                lightsoutExecutionDebuff.iconSprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Common/texBuffCrippleIcon.tif").WaitForCompletion();
            }
            CoreModules.Assets.buffDefs.Add(lightsoutExecutionDebuff);
        }

        private void BanditFinisher(CharacterBody sender, ref float executeThreshold)
        {
            bool hasBanditExecutionBuff = sender.HasBuff(desperadoExecutionDebuff) || sender.HasBuff(lightsoutExecutionDebuff);
            executeThreshold = ModifyExecutionThreshold(executeThreshold, survivorExecuteThreshold, hasBanditExecutionBuff);
        }

        private void ModifyLightsOutDamage(On.EntityStates.Bandit2.Weapon.FireSidearmResetRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmResetRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damage = lightsOutDamage * self.damageStat;
            bulletAttack.damageType = bulletAttack.damageType & ~DamageType.BonusToLowHealth;
        }

        private void ModifyDesperadoDamage(On.EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver.orig_ModifyBullet orig, EntityStates.Bandit2.Weapon.FireSidearmSkullRevolver self, BulletAttack bulletAttack)
        {
            orig(self, bulletAttack);
            bulletAttack.damage = desperadoDamage * self.damageStat;
            bulletAttack.damageType = bulletAttack.damageType & ~DamageType.BonusToLowHealth;

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

