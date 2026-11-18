using BepInEx;
using EntityStates.Treebot.Weapon;
using R2API;
using R2API.Utils;
using RainrotSharedUtils;
using RainrotSharedUtils.Components;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using SurvivorTweaks.Modules;
using System;
using UnityEngine;
using static MoreStats.StatHooks;
using static RainrotSharedUtils.Styling;
using static R2API.RecalculateStatsAPI;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using UnityEngine.Networking;

namespace SurvivorTweaks.SurvivorTweaks
{
    class RexTweaks : SurvivorTweakBase<RexTweaks>
    {
        [AutoConfig(": Base Max Health", "Scales 30% per level. Vanilla is 130", 110f)]
        public static float baseMaxHealth = 110f;//130
        [AutoConfig(": Base Armor", "Vanilla is 20", 0f)]
        public static float baseArmor = 0f;//130

        [AutoConfig("Keywords (Armored) : Max Health Increase", 0.2f)]
        public static float roboSkillMaxHealth = 0.2f;

        [AutoConfig("Primary (DIRECTIVE Inject) : Damage Coefficient Per Projectile", "Expressed as a percentage (eg 0.8 is 80%). Vanilla is 0.8", 0.8f)]
        public static float syringeDamageCoefficient = 0.8f; // 0.8f
        [AutoConfig("Primary (DIRECTIVE Inject) : Heal Fraction (Final Projectile)", "Expressed as a percentage (eg 0.3 is 30%). Vanilla is 0.6", 0.3f)]
        public static float syringeHealFraction = 0.3f; // 0.6f

        [AutoConfig("Secondary (Seed Barrage) : Damage Coefficient", "Expressed as a percentage (eg 6.0 is 60%). Vanilla is 4.5", 6.0f)]
        public static float mortarDamageCoeff = 6f;//4.5f
        [AutoConfig("Secondary (Seed Barrage) : Base Cooldown", "Expressed in seconds. Vanilla is 0.5", 0.5f)]
        public static float mortarCooldown = 0.5f;//0.5f
        [AutoConfig("Secondary (DIRECTIVE Drill) : Base Cooldown", "Expressed in seconds. Vanilla is 6", 4f)]
        public static float drillCooldown = 4f;//6f
        [AutoConfig("Secondary (DIRECTIVE Drill) : Base Max Stock", "Vanilla is 1", 1)]
        public static int drillMaxStock = 1; //1

        [AutoConfig("Utility (Bramble Volley) : Heal Fraction", "Expressed as a percentage (eg 0.07 is 7%). Vanilla is 0.1", 0.07f)]
        public float brambleHealFraction = 0.07f; // 0.1f
        [AutoConfig("Utility (Proliferate) : Base Cooldown", "Expressed in seconds. Vanilla is 5.5", 7f)]
        public float proliferateCooldown = 7f; // 5.5f

        [AutoConfig("Special (DIRECTIVE Harvest) : Projectile Blast Radius", "Expressed in meters. Vanilla is 2", 8f)]
        public float harvestBlastRadius = 8f; // 2f
        [AutoConfig("Special (DIRECTIVE Harvest) : Base Endlag Duration", "Expressed in seconds. Vanilla is 1.5", 0.3f)]
        public float harvestEndlag = 0.3f; // 1.5
        [AutoConfig("Special (DIRECTIVE Harvest) : Base Cooldown", "Expressed in seconds. Vanilla is 6", 12f)]
        public float harvestCooldown = 12f; // 6
        [AutoConfig("Special (DIRECTIVE Harvest) : Fruiting Debuff Duration", "Expressed in seconds. Vanilla is 10", 4f)]
        public float harvestDebuffDuration = 4f; // 10
        [AutoConfig("Special (DIRECTIVE Harvest) : Fruit Heal Fraction", "Expressed as a fraction of maximum health. Vanilla is 0.25", 0.05f)]
        public float harvestFruitHeal = 0.1f; // 6
        [AutoConfig("Special (DIRECTIVE Harvest) : Fruits Per Body Size (Executed)", "Multiplied by the size of the victim's body.", 1f)]
        public float harvestFruitCountPerSizeKilled = 1.33f; // 6
        [AutoConfig("Special (DIRECTIVE Harvest) : Fruits Max", 8)]
        public int harvestFruitCountMax = 8; // 8
        [AutoConfig("Special (DIRECTIVE Harvest) : Fruits Per Body Size (Debuff Expired)", "Multiplied by the size of the victim's body.", 0.5f)]
        public float harvestFruitCountPerSizeExpired = 0.67f; // 6

        public override string bodyName => "TreebotBody";

        public override string survivorName => "REX";

        public override void Init()
        {
            base.Init();
            CommonAssets.AddRexReworkAssets();
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBody_prefab, (result) =>
            {
                bodyObject = result;
                GetSkillsFromBodyObject(bodyObject);
                bodyObject.AddComponent<ElementCounter>();
                skillLocator.passiveSkill.keywordToken = "";

                CharacterBody body = bodyObject.GetComponent<CharacterBody>();
                body.baseMaxHealth = baseMaxHealth;
                body.levelMaxHealth = baseMaxHealth * 0.3f;
                body.baseArmor = baseArmor;

                RemoveTectonicBlast(utility, "TREEBOT_UTILITY_ALT1_NAME");
            });

            string passiveName = "Hybrid Protocol";
            LanguageAPI.Add("TREEBOT_DESCRIPTION", 
                "REX is a half robot / half plant that uses HP to cast devastating skills from a distance. The plant nor the robot could survive this planet alone \u2013 but thankfully they have each other. " +
                $"<style=cSub>\r\n\r\n< ! > Weaken is important to reducing the strength of strong enemies for both you and your allies." +
                "\r\n\r\n< ! > Seed Barrage can be used from any range and has a very low cooldown. Just make sure to watch your HP!" +
                "\r\n\r\n< ! > DIRECTIVE: Disperse can be used to knock enemies away. It can also push flying monsters into walls to kill them." +
                "\r\n\r\n< ! > A well-aimed Tangling Growth can keep REX in the fight forever. Rooting multiple monsters will guarantee you recover the initial HP costs. <style=cSub>\r\n");
            LanguageAPI.Add("TREEBOT_PASSIVE_NAME", passiveName);
            LanguageAPI.Add("TREEBOT_PASSIVE_DESCRIPTION", $"Robotics abilities {HealingColor("increase your maximum health")} by {HealingColor($"+{roboSkillMaxHealth.AsPercent()} per ability.")} Plant abilities {HealthColor("cost health to use.")}");

            ChangePrimaries();
            ChangeSecondaries();
            ChangeUtilities();
            ChangeSpecials();

            GetStatCoefficients += ProtocolHealthBoost;
        }

        private void ProtocolHealthBoost(CharacterBody sender, StatHookEventArgs args)
        {
            if(sender.bodyIndex == RoR2Content.BodyPrefabs.TreebotBody.bodyIndex)
            {
                ElementCounter.Power roboPower = ElementCounter.GetPowerLevelFromBody(sender.gameObject, ElementCounter.CounterElement.Robo);
                if (roboPower > 0)
                {
                    args.healthMultAdd += roboSkillMaxHealth * (int)roboPower;
                }
            }
        }

        #region primary
        private void ChangePrimaries()
        {
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodyFireSyringe_asset, (syringe) =>
            {
                ElementCounter.RegisterSkillElement(syringe, ElementCounter.CounterElement.Robo);
                syringe.keywordTokens = new string[] { CommonAssets.RexRoboHealthKeywordToken, "KEYWORD_WEAK" };
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.SyringeProjectileHealing_prefab, (syringeLast) =>
            {
                if (syringeLast.TryGetComponent(out ProjectileHealOwnerOnDamageInflicted phoodi))
                {
                    phoodi.fractionOfDamage = syringeHealFraction;
                }
            });
            On.EntityStates.Treebot.Weapon.FireSyringe.OnEnter += NerfSyringe;
            LanguageAPI.Add("TREEBOT_PRIMARY_DESCRIPTION",
                $"{HealingColor(CommonAssets.RexRoboHealthKeyword)}. " +
                $"Fire 3 syringes for <style=cIsDamage>3x{Tools.ConvertDecimal(syringeDamageCoefficient)} damage</style>. " +
                $"The last syringe <style=cIsDamage>Weakens</style> and <style=cIsHealing>heals for {Tools.ConvertDecimal(syringeHealFraction)} of damage dealt</style>.");
        }

        private void NerfSyringe(On.EntityStates.Treebot.Weapon.FireSyringe.orig_OnEnter orig, FireSyringe self)
        {
            FireSyringe.damageCoefficient = syringeDamageCoefficient;
            orig(self);
        }
        #endregion

        #region secondary
        private void ChangeSecondaries()
        {
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodyAimMortar2_asset, (mortar) =>
            {
                mortar.baseRechargeInterval = mortarCooldown;
            });
            On.EntityStates.Treebot.Weapon.FireMortar2.OnEnter += (orig, self) =>
            {
                FireMortar2.damageCoefficient = mortarDamageCoeff;
                orig(self);
            };
            LanguageAPI.Add("TREEBOT_SECONDARY_DESCRIPTION",
                $"<style=cIsHealth>15% HP</style>. " +
                $"Launch a mortar into the sky for <style=cIsDamage>{Tools.ConvertDecimal(mortarDamageCoeff)} damage</style>.");

            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodyAimMortarRain_asset, (drill) =>
            {
                ElementCounter.RegisterSkillElement(drill, ElementCounter.CounterElement.Robo);
                drill.baseRechargeInterval = drillCooldown;
                drill.baseMaxStock = drillMaxStock;
                drill.keywordTokens = new string[] { CommonAssets.RexRoboHealthKeywordToken };
            });
            LanguageAPI.Add("TREEBOT_SECONDARY_ALT1_DESCRIPTION",
                $"{HealingColor(CommonAssets.RexRoboHealthKeyword)}. " +
                $"Launch a series of seed bullets into the sky, raining down for <style=cIsDamage>450% damage per second</style>.");
        }
        #endregion

        #region utility
        private void ChangeUtilities()
        {
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodySonicBoom_asset, (boop) =>
            {
                ElementCounter.RegisterSkillElement(boop, ElementCounter.CounterElement.Robo);
                boop.keywordTokens = new string[] { CommonAssets.RexRoboHealthKeywordToken, "KEYWORD_SONIC_BOOM", "KEYWORD_WEAK" };
            });

            On.EntityStates.Treebot.Weapon.FirePlantSonicBoom.OnEnter += NerfBrambleVolley;
            LanguageAPI.Add("TREEBOT_UTILITY_DESCRIPTION",
                $"{HealingColor(CommonAssets.RexRoboHealthKeyword)}. " +
                $"Fire a <style=cIsUtility>Sonic Boom</style> that <style=cIsDamage>Weakens</style> all enemies hit.");
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodyProliferation_asset, (proliferate) =>
            {
                ElementCounter.RegisterSkillElement(proliferate, ElementCounter.CounterElement.Plant);
                proliferate.baseRechargeInterval = proliferateCooldown;
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.ProliferationFlowerTurretBody_prefab, (plantBody) =>
            {
                if(plantBody.TryGetComponent(out CharacterBody body))
                {
                    body.bodyFlags |= CharacterBody.BodyFlags.UsesAmbientLevel;
                }
            });
        }

        private void NerfBrambleVolley(On.EntityStates.Treebot.Weapon.FirePlantSonicBoom.orig_OnEnter orig, FirePlantSonicBoom self)
        {
            FirePlantSonicBoom.healthFractionPerHit = brambleHealFraction;
            FirePlantSonicBoom.healthCostFraction = 0.2f;
            orig(self);
        }
        #endregion

        #region special
        private static GameObject fruitPack;
        private void ChangeSpecials()
        {
            string idk = "KEYWORD_FRUITS";
            SurvivorTweaksPlugin.LoadAsync<SkillDef>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotBodyFireFruitSeed_asset, (harvest) =>
            {
                ElementCounter.RegisterSkillElement(harvest, ElementCounter.CounterElement.Robo);
                harvest.keywordTokens = new string[] { CommonAssets.RexRoboHealthKeywordToken, SharedUtilsPlugin.executeKeywordToken, idk };
                harvest.baseRechargeInterval = harvestCooldown;
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotFruitSeedProjectile_prefab, (harvestProjectile) =>
            {
                if (harvestProjectile.TryGetComponent(out ProjectileImpactExplosion pie))
                {
                    pie.blastRadius = harvestBlastRadius;
                    pie.preserveExplosionScale = false;
                }
            });
            SurvivorTweaksPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_Treebot.TreebotFruitPack_prefab, (fruit) =>
            {
                fruitPack = fruit;
                Transform pickupTrigger = fruit.transform.Find("PickupTrigger");
                if (pickupTrigger && pickupTrigger.TryGetComponent(out HealthPickup healthPickup))
                {
                    healthPickup.fractionalHealing = harvestFruitHeal;
                }
                if (pickupTrigger && pickupTrigger.TryGetComponent(out GravitatePickup gravPickup))
                {
                    gravPickup.gravitateAtFullHealth = false;
                }
            });
            GetMoreStatCoefficients += HarvestFinisher;
            On.EntityStates.Treebot.TreebotFireFruitSeed.OnEnter += FireFruitEnter;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += FruitCountOnDeath;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += FruitDurationOnHit;
            On.RoR2.CharacterBody.OnBuffFinalStackLost += FruitCountOnExpiry;
            LanguageAPI.Add("TREEBOT_SPECIAL_ALT1_DESCRIPTION",
                $"{HealingColor(CommonAssets.RexRoboHealthKeyword)}. " +
                $"{HealthColor("Finisher")}. " +
                $"Deploy a harvester that deals <style=cIsDamage>330% damage</style>. " +
                $"<style=cIsHealing>Harvested</style> enemies drop multiple " +
                $"fruits that heal for {HealingColor(harvestFruitHeal.AsPercent() + " HP")}."
                );
            LanguageAPI.Add(idk, KeywordText("Harvested", $"Afflicted enemies are {HealingColor("Fruiting")} for {harvestDebuffDuration} seconds. " +
                $"Upon expiry, drop up to {HealingColor($"{harvestFruitCountMax} fruits")}. " +
                $"Slain enemies drop {HealingColor((harvestFruitCountPerSizeKilled / harvestFruitCountPerSizeExpired).ToString())} more.")
                );
        }

        private void FruitCountOnExpiry(On.RoR2.CharacterBody.orig_OnBuffFinalStackLost orig, CharacterBody self, BuffDef buffDef)
        {
            orig(self, buffDef);
            if (buffDef != RoR2Content.Buffs.Fruiting)
                return;
            if (self.healthComponent.alive == false)
                return;

            EffectManager.SpawnEffect(LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/TreebotFruitDeathEffect"), new EffectData
            {
                origin = self.corePosition,
                rotation = UnityEngine.Random.rotation
            }, true);
            int num2 = Mathf.Min(Math.Max(1, (int)(self.bestFitRadius * harvestFruitCountPerSizeExpired)), harvestFruitCountMax);
            GameObject original = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/TreebotFruitPack");
            for (int i = 0; i < num2; i++)
            {
                GameObject gameObject4 = UnityEngine.Object.Instantiate<GameObject>(original, self.corePosition + UnityEngine.Random.insideUnitSphere * self.radius * 0.5f, UnityEngine.Random.rotation);
                TeamFilter component4 = gameObject4.GetComponent<TeamFilter>();
                if (component4)
                {
                    component4.teamIndex = TeamIndex.Player;
                }
                gameObject4.GetComponentInChildren<HealthPickup>();
                gameObject4.transform.localScale = new Vector3(1f, 1f, 1f);
                NetworkServer.Spawn(gameObject4);
            }
        }

        private void FruitDurationOnHit(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld((typeof(RoR2Content.Buffs)).FullName, nameof(RoR2Content.Buffs.Fruiting)),
                x => x.MatchLdcR4(out _)
                );
            if (b1 == false)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(FruitDurationOnHit));
                return;
            }
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, harvestDebuffDuration);
        }

        private void FruitCountOnDeath(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld((typeof(RoR2Content.Buffs)).FullName, nameof(RoR2Content.Buffs.Fruiting)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_bestFitRadius"),
                x => x.MatchLdcR4(out _)
                );
            if (b1 == false)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(FruitCountOnDeath), 1);
                return;
            }
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, harvestFruitCountPerSizeKilled);

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcI4(out _),
                x => x.MatchCallOrCallvirt(typeof(Mathf).FullName, nameof(Mathf.Min))
                );
            if (b2 == false)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(FruitCountOnDeath), 2);
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, harvestFruitCountMax);
            c.Index--;

            bool b3 = c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcI4(1)
                );
            if (b3 == false)
            {
                RainrotSharedUtils.SharedUtilsPlugin.DebugBreakpoint(nameof(FruitCountOnDeath), 3);
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldc_I4, 2);
        }

        private void HarvestFinisher(CharacterBody sender, MoreStatHookEventArgs args)
        {
            bool hasRexHarvestBuff = sender.HasBuff(RoR2Content.Buffs.Fruiting);
            args.ModifyBaseExecutionThreshold(SharedUtilsPlugin.GetSurvivorExecuteThreshold(sender.isBoss), hasRexHarvestBuff);
        }

        private void FireFruitEnter(On.EntityStates.Treebot.TreebotFireFruitSeed.orig_OnEnter orig, EntityStates.Treebot.TreebotFireFruitSeed self)
        {
            self.baseDuration = harvestEndlag;
            orig(self);
        }
        #endregion
    }
}
