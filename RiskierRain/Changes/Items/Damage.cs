using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using RoR2.Items;
using RoR2.Orbs;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;
using static RiskierRain.RiskierRainPlugin;
using static R2API.RecalculateStatsAPI;
using UnityEngine.Networking;
using MoreStats;
using RoR2.Projectile;

namespace RiskierRain.Changes
{
    public static partial class ItemChanges
    {
        #region bands
        // could go with 6 base, 1 total
        static float runaldBaseDamage = 6f;
        static float runaldTotalDamage = 1f; //3f
        static float kjaroBaseDamage = 6;
        static float kjaroTotalDamage = 1f; //3f
        static string runaldTotal = Tools.ConvertDecimal(runaldTotalDamage);
        static string kjaroTotal = Tools.ConvertDecimal(kjaroTotalDamage);
        public static void ChangeRunald()
        {
            //IL.RoR2.GlobalEventManager.ProcessHitEnemy += CooldownBuff;

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += (il) => BandNerf(il, runaldTotalDamage, runaldBaseDamage, "IceRing");

            LanguageAPI.Add("ITEM_ICERING_DESC",
                $"Hits from <style=cIsUtility>skills or equipment</style> " +
                $"that deal <style=cIsDamage>more than 400% damage</style> also blast enemies with a " +
                $"<style=cIsDamage>runic ice blast</style>, " +
                $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(runaldBaseDamage)}</style> BASE damage, " +
                $"plus <style=cIsDamage>{runaldTotal}</style> <style=cStack>(+{runaldTotal} per stack)</style> TOTAL damage. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
        }
        public static void ChangeKjaro()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += (il) => BandNerf(il, runaldTotalDamage, runaldBaseDamage, "FireRing");
            LanguageAPI.Add("ITEM_FIRERING_DESC",
                $"Hits from <style=cIsUtility>skills or equipment</style> " +
                $"that deal <style=cIsDamage>more than 400% damage</style> also blast enemies with a " +
                $"<style=cIsDamage>runic flame tornado</style>, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(kjaroBaseDamage)}</style> BASE damage, " +
                $"plus <style=cIsDamage>{kjaroTotal}</style> <style=cStack>(+{kjaroTotal} per stack)</style> TOTAL damage over time. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
        }


        private static void CooldownBuff(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 51;
            int cooldownTrackerLocation = 51;

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "IceRing")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdcI4(1),
                x => x.MatchStloc(out cooldownTrackerLocation)
                );

            // % CDR (alien head, brainstalks)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(cooldownTrackerLocation),
                x => x.MatchConvR4()
                );
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((cooldown, self) =>
            {
                float multiplier = 1;
                if (self.skillLocator.special)
                {
                    float scale = self.skillLocator.special.cooldownScale;
                    multiplier *= scale;

                    if (self.skillLocator.special.flatCooldownReduction < 9)
                    {
                    }
                    else
                    {
                        //multiplier = 0.5f / 10;
                    }
                }

                return cooldown * multiplier;
            });

            // flat CDR (purity)
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(cooldownTrackerLocation),
                x => x.MatchConvR4(),
                x => x.MatchLdcR4(out _)
                );
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((seconds, self) =>
            {
                float flat = 0;
                if (self.skillLocator.special)
                {
                    //flat = self.skillLocator.special.flatCooldownReduction;
                }

                return Mathf.Max(seconds - flat, 1);
            });
        }

        private static void BandNerf(ILContext il, float totalDamage, float baseDamage, string bandNameInternal)
        {
            ILCursor c = new ILCursor(il);

            int itemCountLocation = 80;
            int totalDamageMultiplierLocation = 85;

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", bandNameInternal))
            && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out itemCountLocation)
                )
            && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _), //original damage multiplier, to be replaced
                x => x.MatchLdloc(itemCountLocation),
                x => x.MatchConvR4(),
                x => x.MatchMul(),
                x => x.MatchStloc(out totalDamageMultiplierLocation)
                );

            if (!b1)
            {
                DebugBreakpoint(nameof(BandNerf) + $"/{bandNameInternal}", 1);
                return;
            }
            //c.Next.Operand = runaldTotalDamage;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, totalDamage);

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(totalDamageMultiplierLocation),
                x => x.MatchCallOrCallvirt(out _)
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(BandNerf) + $"/{bandNameInternal}", 2);
                return;
            }
            //c.Index--;
            c.Emit(OpCodes.Ldloc_0);
            c.EmitDelegate<Func<float, CharacterBody, float>>((damage, attackerBody) =>
            {
                float dam = attackerBody.baseDamage * baseDamage;

                return damage + dam;
            });
        }
        #endregion

        #region glasses
        static float glassesNewCritChance = 10f;
        private static void NerfCritGlasses()
        {
            IL.RoR2.CharacterBody.RecalculateStats += GlassesNerf;
            LanguageAPI.Add("ITEM_CRITGLASSES_DESC",
                $"Your attacks have a <style=cIsDamage>{glassesNewCritChance}%</style> " +
                $"<style=cStack>(+{glassesNewCritChance}% per stack)</style> chance to " +
                $"'<style=cIsDamage>Critically Strike</style>', dealing <style=cIsDamage>double damage</style>.");
        }
        private static void GlassesNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //if we start to use these changes again, pls fix to use trygotonext, too lazy rn
            int countLoc = -1;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "CritGlasses"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out countLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(countLoc),
                x => x.MatchConvR4()
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, glassesNewCritChance);
        }
        #endregion
        #region death mark fix
        public static bool deathMarkedStacking = true; //false
        public static bool deathMarkedHidden = true; //false
        public static float deathMarkedBonusDamageBase = 0.3f; //0.5f
        public static float deathMarkedBonusDamageStack = 0.3f; //0.5f
        public static float deathMarkDamagePerDebuffBase = 0.1f; //0f
        public static int deathMarkMaxDebuffs = 3;
        //death mark fix :)
        public static void ChangeDeathMark()
        {
            LoadAsync<BuffDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_DeathMark.bdDeathMark_asset, (bd) =>
            {
                bd.canStack = deathMarkedStacking;
                bd.isHidden = deathMarkedHidden;
            });
            IL.RoR2.GlobalEventManager.ProcDeathMark += DeathMarkFix_Stacking;
            IL.RoR2.HealthComponent.TakeDamageProcess += DeathMarkFix_Damage;
            LanguageAPI.Add("ITEM_DEATHMARK_DESC",
                $"Enemies take <style=cIsDamage>{Tools.ConvertDecimal(deathMarkDamagePerDebuffBase)}</style> " +
                //$"<style=cStack>(+{Tools.ConvertDecimal(deathMarkDamagePerDebuffStack)} per stack)</style> " +
                $"more damage per unique debuff applied. " +
                $"Enemies with <style=cIsDamage>4</style> or more debuffs are " +
                $"<style=cIsDamage>marked for death</style>, further increasing damage taken by " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(deathMarkedBonusDamageBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(deathMarkedBonusDamageStack)} per stack)</style> " +
                $"from all sources for <style=cIsUtility>7</style> seconds.");
        }


        private static void DeathMarkFix_Stacking(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int deathMarkCountLocation = -1;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "DeathMark"),
                x => x.MatchLdcR4(out _),
                x => x.MatchLdloc(out deathMarkCountLocation)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(DeathMarkFix_Stacking));
                return;
            }
            c.Index++;
            c.Remove();
            c.Remove();
            c.EmitDelegate<Action<CharacterBody, BuffDef, float, float>>((body, buffDef, duration, itemCount) =>
            {
                int currentDebuffCount = body.GetBuffCount(RoR2Content.Buffs.DeathMark);
                int buffsNeeded = 0;

                if (currentDebuffCount < (int)itemCount)
                {
                    buffsNeeded = (int)itemCount - currentDebuffCount;
                }

                for (float i = 0; i < buffsNeeded; i++)
                {
                    body.AddTimedBuff(buffDef, duration);
                }
            });
        }

        private static void DeathMarkFix_Damage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "DeathMark"),
                x => x.MatchCallOrCallvirt<RoR2.CharacterBody>(nameof(CharacterBody.HasBuff))
                )
            && c.TryGotoNext(MoveType.After,
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(DeathMarkFix_Damage));
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((damageMultiplierIn, hc) =>
            {
                CharacterBody body = hc.body;
                float damageMultiplierOut = 1;
                if(body.inventory != null)
                {
                    int deathMarkCount = body.inventory.GetItemCountEffective(RoR2Content.Items.DeathMark);
                    if(deathMarkCount > 0)
                    {
                        int uniqueBuffCount = GetUniqueBuffCount(body, deathMarkMaxDebuffs);
                        if (uniqueBuffCount > 0)
                            damageMultiplierOut += uniqueBuffCount * deathMarkDamagePerDebuffBase;// + deathMarkedBonusDamageStack * (deathMarkCount - 1));
                    }
                }

                int buffCount = body.GetBuffCount(RoR2Content.Buffs.DeathMark);
                if(buffCount > 0)
                    damageMultiplierOut += deathMarkedBonusDamageBase + deathMarkedBonusDamageStack * (buffCount - 1);
                return damageMultiplierOut;
            });

            int GetUniqueBuffCount(CharacterBody body, int max = int.MaxValue)
            {
                int num = 0;
                foreach (BuffIndex buffType in BuffCatalog.debuffBuffIndices)
                {
                    if (body.HasBuff(buffType))
                    {
                        num++;
                    }
                    if (num >= max)
                        return max;
                }
                DotController dotController = DotController.FindDotController(body.gameObject);
                if (dotController)
                {
                    for (DotController.DotIndex dotIndex = DotController.DotIndex.Bleed; dotIndex < DotController.DotIndex.Count; dotIndex++)
                    {
                        if (dotController.HasDotActive(dotIndex))
                        {
                            num++;
                        }
                        if (num >= max)
                            return max;
                    }
                }
                return num;
            }
        }
        #endregion

        #region polylute
        public static float luteDamageCoefficient = 0.4f; //0.6f; //ukulele 0.8f
        public static void ChangePolylute()
        {
            if (ucrLoaded)
                return;
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += PolyluteDamage;

            LanguageAPI.Add("ITEM_CHAINLIGHTNINGVOID_DESC",
                $"<style=cIsDamage>25%</style> chance " +
                $"to fire <style=cIsDamage>lightning</style> " +
                $"for <style=cIsDamage>{Tools.ConvertDecimal(luteDamageCoefficient)}</style> TOTAL damage " +
                $"up to <style=cIsDamage>3</style> <style=cStack>(+3 per stack)</style> " +
                $"times. <style=cIsVoid>Corrupts all Ukuleles</style>.");
        }

        private static void PolyluteDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int luteLoc = 14;
            int dmgLoc = 14;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "ChainLightningVoid"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out luteLoc)
                )
            && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out luteLoc),
                x => x.MatchLdcI4(0),
                x => x.MatchBle(out _)
                )
            && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out dmgLoc)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(PolyluteDamage));
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, luteDamageCoefficient);
        }
        #endregion

        #region shuriken
        static GameObject shurikenProjectilePrefab;
        public static float shurikenBaseDamage = 0.8f; //3f
        public static float shurikenStackDamage = 0.0f; //1f
        public static float shurikenProcCoefficient = 2f; //1f
        public static void ReworkShuriken()
        {
            shurikenProjectilePrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Projectiles/ShurikenProjectile");
            if (shurikenProjectilePrefab != null)
            {
                ProjectileController pc = shurikenProjectilePrefab.GetComponent<ProjectileController>();
                if (pc)
                {
                    pc.procCoefficient = shurikenProcCoefficient;
                }
                ProjectileDamage pd = shurikenProjectilePrefab.GetComponent<ProjectileDamage>();
                if (pd)
                {
                    pd.damageType |= DamageType.BleedOnHit;
                    pd.damageType.damageSource = DamageSource.Primary;
                }
            }

            //On.RoR2.PrimarySkillShurikenBehavior.FireShuriken += ModifyShurikenAttack;
            IL.RoR2.PrimarySkillShurikenBehavior.FireShuriken += ModifyFireShuriken;

            LanguageAPI.Add("ITEM_PRIMARYSKILLSHURIKEN_PICKUP",
                "Activating your Primary skill also throws a shuriken that bleeds enemies. Recharges over time.");
            LanguageAPI.Add("ITEM_PRIMARYSKILLSHURIKEN_DESC",
                $"Activating your <style=cIsUtility>Primary skill</style> " +
                $"also throws a <style=cIsDamage>shuriken</style> that " +
                $"deals <style=cIsDamage>{Tools.ConvertDecimal(shurikenBaseDamage)}</style> base damage " +
                $"and <style=cIsDamage>bleeds</style> enemies struck for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(shurikenProcCoefficient * 2.4f)}</style> base damage. " +
                $"You can hold up to <style=cIsUtility>3</style> " +
                $"<style=cStack>(+1 per stack)</style> " +
                $"<style=cIsDamage>shurikens</style> which all reload over <style=cIsUtility>10</style> seconds.");
        }

        private static void ModifyFireShuriken(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_damage"),
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ModifyFireShuriken));
                return;
            }
            c.Index++;
            c.Next.Operand = shurikenBaseDamage - shurikenStackDamage;
            c.Index++;
            c.Next.Operand = shurikenStackDamage;
        }

        private static void ModifyShurikenAttack(On.RoR2.PrimarySkillShurikenBehavior.orig_FireShuriken orig, PrimarySkillShurikenBehavior self)
        {
            Ray aimRay = self.GetAimRay();
            ProjectileManager.instance.FireProjectileWithoutDamageType(
                self.projectilePrefab, aimRay.origin,
                Util.QuaternionSafeLookRotation(aimRay.direction) * self.GetRandomRollPitch(),
                self.gameObject, self.body.damage * (shurikenBaseDamage), 0f,
                Util.CheckRoll(self.body.crit, self.body.master), DamageColorIndex.Item, null, -1f);
        }
        #endregion
        #region box of dynamite
        public static float dynamiteDamageBase = 3.5f; //2.4f
        public static float dynamiteDamageStack = 2.8f; //0.85f //what the fuck !?
        public static void ChangeBoxOfDynamite()
        {
            IL.RoR2.Items.DroneDynamiteBehaviour.FixedUpdate += IncreaseBoxOfDynamiteDamage;

            LanguageAPI.Add("ITEM_DRONESDROPDYNAMITE_DESC",
                $"Gain <style=cIsDamage>Lt. Droneboy</style>. " +
                $"While in combat, your drones drop sticks of dynamite " +
                $"that detonate for <style=cIsDamage>{dynamiteDamageBase.AsPercent()} damage " +
                $"<style=cStack>(+{dynamiteDamageStack.AsPercent()} per stack)</style></style>" +
                $", <style=cIsDamage>Stunning</style> enemies. " +
                $"Recharges after <style=cIsUtility>10</style> seconds.");
        }

        private static void IncreaseBoxOfDynamiteDamage(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<FireProjectileInfo>(nameof(FireProjectileInfo.damage)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdfld<BaseItemBodyBehavior>(nameof(BaseItemBodyBehavior.stack)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _)
                );
            if (!b)
            {
                DebugBreakpoint(nameof(IncreaseBoxOfDynamiteDamage));
                return;
            }
            c.Next.Operand = dynamiteDamageBase;
            c.Index++;
            c.Next.Operand = dynamiteDamageStack;
        }
        #endregion
        #region luminous shot

        public static float luminousTotalDamageBase = 1.75f;//1.75f
        public static float luminousTotalDamageStack = 1.0f;//0.5f
        public static void ChangeLuminousShot()
        {
            IL.RoR2.GlobalEventManager.ProcessHitEnemy += ChangeLuminousShotStats;

            LanguageAPI.Add("ITEM_INCREASEPRIMARYDAMAGE_DESC",
                $"Activating <style=cIsUtility>Secondary skill</style> stores " +
                $"up to <style=cIsUtility>5 charges</style> <style=cStack>(+1 per stack)</style>. " +
                $"Requires <style=cIsUtility>3 charges</style> for your " +
                $"<style=cIsUtility>Primary skill</style> to fire lightning strikes, " +
                $"dealing <style=cIsDamage>{luminousTotalDamageBase.AsPercent()} TOTAL damage</style> " +
                $"<style=cStack>(+{luminousTotalDamageStack.AsPercent()} per stack)</style> each. " +
                $"<style=cIsUtility>Reduces Secondary skill cooldown by 20%</style>."
                );
        }

        private static void ChangeLuminousShotStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC2Content/Items", nameof(DLC2Content.Items.IncreasePrimaryDamage)))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _)
                );
            if (!b1)
            {
                DebugBreakpoint(nameof(ChangeLuminousShotStats), 1);
                return;
            }

            c.Next.Operand = luminousTotalDamageBase;

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchMul()
                );
            if (!b2)
            {
                DebugBreakpoint(nameof(ChangeLuminousShotStats), 2);
                return;
            }

            c.Next.Operand = luminousTotalDamageStack;
        }
        #endregion
        #region genesis loop
        public static float genesisLoopDamageCoeff = 30; //60
        public static float genesisLoopProcCoeff = 0.75f; //1.0f
        public static void ChangeGenesisLoop()
        {
            EntityStates.VagrantNovaItem.DetonateState.blastProcCoefficient = 0.3f;
            EntityStates.VagrantNovaItem.DetonateState.blastDamageCoefficient = genesisLoopDamageCoeff;
            LanguageAPI.Add("ITEM_NOVAONLOWHEALTH_DESC",
                $"Falling below <style=cIsHealth>25% health</style> causes you to explode, " +
                $"dealing <style=cIsDamage>{Tools.ConvertDecimal(genesisLoopDamageCoeff)} base damage</style>. " +
                $"Recharges every <style=cIsUtility>30 / (2 <style=cStack>+1 per stack</style>) seconds</style>.");

            On.EntityStates.VagrantNovaItem.ChargeState.OnEnter += (orig, self) =>
            {
                orig(self);
                self.duration = 3;
            };
        }
        #endregion

        #region justice
        static float justiceMinDamageCoeff = 8f;
        public static void ChangeJustice()
        {
            On.RoR2.GlobalEventManager.ProcessHitEnemy += JusticeBuff;
            LanguageAPI.Add("ITEM_ARMORREDUCTIONONHIT_PICKUP",
                "Reduce the armor of enemies after repeatedly striking them or on massive hits.");
            LanguageAPI.Add("ITEM_ARMORREDUCTIONONHIT_DESC",
                $"After hitting an enemy <style=cIsDamage>5</style> times, or dealing " +
                $"<style=cIsDamage>more than {Tools.ConvertDecimal(justiceMinDamageCoeff)} damage</style> to them in a single hit, " +
                $"reduce their <style=cIsDamage>armor</style> by <style=cIsDamage>60</style> " +
                $"for <style=cIsDamage>8</style><style=cStack> (+8 per stack)</style> seconds.");
        }
        private static void JusticeBuff(On.RoR2.GlobalEventManager.orig_ProcessHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            orig(self, damageInfo, victim);
            if (damageInfo.attacker && damageInfo.procCoefficient > 0f)
            {
                CharacterBody component = damageInfo.attacker.GetComponent<CharacterBody>();
                CharacterBody component2 = victim.GetComponent<CharacterBody>();
                if (component)
                {
                    CharacterMaster master = component.master;
                    if (master)
                    {
                        int justiceCount = 0;
                        Inventory inventory = master.inventory;
                        if (inventory)
                        {
                            justiceCount = inventory.GetItemCountEffective(RoR2Content.Items.ArmorReductionOnHit);
                        }

                        if (component2 != null && justiceCount > 0)
                        {
                            BuffDef buffIndex = RoR2Content.Buffs.PulverizeBuildup;
                            BuffDef buffType = RoR2Content.Buffs.Pulverized;
                            if (damageInfo.damage / component.damage >= justiceMinDamageCoeff && !component2.HasBuff(buffType))
                            {
                                component2.ClearTimedBuffs(buffIndex);
                                component2.AddTimedBuff(buffType, 8f * (float)justiceCount);
                                ProcChainMask procChainMask2 = damageInfo.procChainMask;
                            }
                        }
                    }
                }
            }
        }
        #endregion

        #region pauldron
        private static float pauldronDamageMultiplier = 1f;
        private static float pauldronAspdMultiplier = 0.5f;

        public static void ChangePauldron()
        {
            GetStatCoefficients += WarCryDamage;
            IL.RoR2.CharacterBody.RecalculateStats += RemovePauldronAttackSpeed;
            LanguageAPI.Add("ITEM_WARCRYONMULTIKILL_DESC",
                $"<style=cIsDamage>Killing 3 enemies</style> within <style=cIsDamage>1</style> second " +
                $"sends you into a <style=cIsDamage>frenzy</style> for <style=cIsDamage>6s</style> <style=cStack>(+4s per stack)</style>. " +
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>50%</style>, " +
                $"<style=cIsDamage>damage</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronDamageMultiplier)}</style>, " +
                $"and <style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronAspdMultiplier)}</style>.");
            LanguageAPI.Add("EQUIPMENT_TEAMWARCRY_DESC",
                $"All allies enter a <style=cIsDamage>frenzy</style> for <style=cIsDamage>7s</style>. " +
                $"Increases <style=cIsUtility>movement speed</style> by <style=cIsUtility>50%</style>, " +
                $"<style=cIsDamage>damage</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronDamageMultiplier)}</style>, " +
                $"and <style=cIsDamage>attack speed</style> by <style=cIsDamage>{Tools.ConvertDecimal(pauldronAspdMultiplier)}</style>.");
        }

        public static void WarCryDamage(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(RoR2Content.Buffs.WarCryBuff) || sender.HasBuff(RoR2Content.Buffs.TeamWarCry))
                args.damageMultAdd += pauldronDamageMultiplier;
        }

        private static void RemovePauldronAttackSpeed(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int attackSpeedMultiplierLocation = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld("RoR2.CharacterBody", "baseAttackSpeed"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld("RoR2.CharacterBody", "levelAttackSpeed")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out attackSpeedMultiplierLocation),
                x => x.MatchLdloc(attackSpeedMultiplierLocation)
                );


            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Buffs", "WarCryBuff"),
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.HasBuff))
                );
            c.GotoNext(MoveType.After,
                x => x.MatchLdloc(attackSpeedMultiplierLocation),
                x => x.MatchLdcR4(1f)
                );
            c.Index--;
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, pauldronAspdMultiplier);

            //Debug.Log(il.ToString());
        }
        #endregion
        #region lost seers lenses
        public static void FixLostSeers()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += FixLostSeersDamageImmunity;
        }

        private static void FixLostSeersDamageImmunity(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.DLC1Content/Items", "CritGlassesVoid"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, HealthComponent, int>>((lensCount, hc) => {
                CharacterBody cb = hc.body;
                if (cb.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToVoidDeath))
                {
                    return 0;
                }
                return lensCount;
            });
        }
        #endregion

        #region sticky bomb
        public static float stickyDamageCoeffBase = 3.2f; //3.2 is 8 stacks to beat atg, 4.0 is 6 stacks
        public static float stickyDamageCoeffStack = 0.4f;
        public static void ChangeStickyBomb()
        {
            RetierItemAsync(RoR2BepInExPack.GameAssetPathsBetter.RoR2_Base_StickyBomb.StickyBomb_asset, ItemTier.Tier2);

            IL.RoR2.GlobalEventManager.ProcessHitEnemy += StickyBombRework;
            LanguageAPI.Add("ITEM_STICKYBOMB_DESC",
                $"<style=cIsDamage>5%</style> <style=cStack>(+5% per stack)</style> chance " +
                $"on hit to attach a <style=cIsDamage>bomb</style> to an enemy, detonating for " +
                $"<style=cIsDamage>{Tools.ConvertDecimal(stickyDamageCoeffBase)}</style> " +
                $"<style=cStack>(+{Tools.ConvertDecimal(stickyDamageCoeffStack)} per stack)</style> TOTAL damage.");

            GameObject stickyPrefab = LegacyResourcesAPI.Load<GameObject>("prefabs/projectiles/StickyBomb");
            ProjectileImpactExplosion pie = stickyPrefab.GetComponent<ProjectileImpactExplosion>();
        }

        private static void StickyBombRework(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int stickyLoc = 14;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "StickyBomb"),
                x => x.MatchCallOrCallvirt<RoR2.Inventory>(nameof(RoR2.Inventory.GetItemCountEffective)),
                x => x.MatchStloc(out stickyLoc)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt("RoR2.Util", nameof(RoR2.Util.OnHitProcDamage))
                );
            c.Emit(OpCodes.Ldloc, stickyLoc);
            c.EmitDelegate<Func<float, int, float>>((damageCoefficient, itemCount) =>
            {
                float damageOut = stickyDamageCoeffBase + (stickyDamageCoeffStack * (itemCount - 1));
                return damageOut;
            });
        }
        #endregion
    }
}
