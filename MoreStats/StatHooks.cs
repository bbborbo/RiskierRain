using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using UnityEngine;

namespace MoreStats
{
    public class MoreStatCoefficients
    {
        public bool barrierDecayFrozen = false;
        public float barrierGenRate = 0;
        public float barrierDrainRate = 0;
        public float barrierDecayMult = 1;
        /// <summary>
        /// DEPRECATED
        /// </summary>
        public float barrierDecayDynamicHalfLife = 0;

        public float luckFromBody = 0;
        public float luckFromMaster = 0;
        public float burnChance = 0;

        public bool shieldRechargeReady = true;
        public float shieldRechargeDelay = BaseStats.BaseShieldDelaySeconds;
        public float healthToShieldConversionReversed = 0;

        public float selfExecutionThresholdAdd = 0;
        public float selfExecutionThresholdBase = Mathf.NegativeInfinity;

        public float healingMult = 1;

        public int bodyScrapWhiteCount = 0;
        public int bodyScrapGreenCount = 0;
        public int bodyScrapRedCount = 0;
        public int bodyScrapYellowCount = 0;

        #region health gate
        public int maxHealthGateCount = 0;
        public float lowestCombinedHealthFraction = 1f;
        internal float GetNextThresholdHealthFraction()
        {
            if (maxHealthGateCount == 0)
                return -1;
            int count = maxHealthGateCount + 1;
            //ensure that the calculated value is always below (not equal to) the lowest health fraction
            return FloorOrSubAndMult(lowestCombinedHealthFraction, count) / count;

            float FloorOrSubAndMult(float value, float multiplier)
            {
                return Mathf.Ceil(value * multiplier) - 1f;
                float truncated = (int)value;
                return ((value == truncated) ? value - 1 : truncated) * multiplier;
            }
        }
        internal float GetHealthFractionSize()
        {
            if (maxHealthGateCount == 0)
                return -1;
            return 1 / (maxHealthGateCount + 1);
        }
        #endregion

        public bool preventHitStun = false;
        public float hitStunThresholdScale = 1f;

        /// <summary>
        /// Does not reset luckFromMaster
        /// </summary>
        internal void ResetStats()
        {
            barrierDecayFrozen = false;
            barrierDecayDynamicHalfLife = 0;
            barrierGenRate = 0;

            luckFromBody = 0;

            burnChance = 0;
            //chillChance = 0;     

            shieldRechargeReady = true;
            shieldRechargeDelay = BaseStats.BaseShieldDelaySeconds;

            selfExecutionThresholdAdd = 0;
            selfExecutionThresholdBase = 0;

            healingMult = 1;

            bodyScrapWhiteCount = 0;
            bodyScrapGreenCount = 0;
            bodyScrapRedCount = 0;
            bodyScrapYellowCount = 0;

            maxHealthGateCount = 0;
        }
    }

    /// <summary>
    /// add to the event GetMoreStatCoefficients to modify stats like RecalculateStatsAPI's GetStatCoefficients
    /// call GetMoreStatsFromBody(CharacterBody) if you need to retrieve processed stat information 
    /// </summary>
    public static class StatHooks
    {
        /// <summary>
        /// For reading processed stats
        /// Please do not edit these stats directly; use GetMoreStatCoefficients
        /// </summary>
        /// <param name="body"></param>
        /// <returns></returns>
        public static MoreStatCoefficients GetMoreStatsFromBody(CharacterBody body)
        {
            if (body == null)
                return null;
            return characterCustomStats.GetOrCreateValue(body);
        }
        public static event MoreStatHookEventHandler GetMoreStatCoefficients;
        public static event HealthGateTriggeredEventHandler OnBodyHealthGateTriggeredGlobal;

        static bool initialized = false;

        internal static void Init()
        {
            if (initialized)
                return;
            initialized = true;

            // Get Stat Coefficients
            IL.RoR2.CharacterBody.RecalculateStats += RecalculateMoreStats;
            // Continuously Update Shield Ready
            On.RoR2.CharacterBody.UpdateOutOfCombatAndDanger += UpdateDangerMoreStats;
            On.RoR2.Util.CheckRoll_float_float_CharacterMaster += RoundLuckInCheckRoll;

            // Barrier Decay And Shield Recharge
            IL.RoR2.HealthComponent.ServerFixedUpdate += HookHealthComponentUpdate;
            IL.RoR2.HealthComponent.GetBarrierDecayRate += HookBarrierDecayRate;

            // Execution
            IL.RoR2.HealthComponent.TakeDamageProcess += InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;

            // Healing
            IL.RoR2.HealthComponent.Heal += ModifyHealing;

            // Health Gating
            IL.RoR2.HealthComponent.TakeDamageProcess += InsertHealthGates;
            //IL.RoR2.HealthComponent.GetHealthBarValues += DisplayHealthGates;
            GlobalEventManager.onServerDamageDealt += RecordHealthFractionServer;
            GlobalEventManager.onClientDamageNotified += RecordHealthFractionClient;
            CharacterBody.onBodyStartGlobal += RecordHealthFractionBody;

            // Hit Stun
            On.RoR2.SetStateOnHurt.GetShouldHitStun += OverrideHitStun;

            // Body Scrap Count
            On.RoR2.DrifterTrashToTreasureController.OnInventoryChanged += DrifterUpdateScrapCounts;
        }

        private static bool OverrideHitStun(On.RoR2.SetStateOnHurt.orig_GetShouldHitStun orig, SetStateOnHurt self, HealthComponent healthComponent, float trueDamage)
        {
            if (healthComponent.body)
            {
                MoreStatCoefficients moreStatCoefficients = GetMoreStatsFromBody(healthComponent.body);
                if (moreStatCoefficients.preventHitStun == true)
                    return false;

                trueDamage *= moreStatCoefficients.hitStunThresholdScale;
            }

            return orig(self, healthComponent, trueDamage);
        }

        private static void HookHealthComponentUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ModifyBarrierDecayRate_ServerFixedUpdate(c);
            ModifyShieldRechargeReady(c);
        }

        private static void ModifyHealing(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            //go to the moment the healing value is saved as a local variable to be applied as healing
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchStloc(out _)
                );
            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ModifyHealing));
                return;
            }

            //inject our healing multiplier at the last moment, then store it and re load the value
            c.Index++;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((healAmt, healthComponent) =>
            {
                CharacterBody body = healthComponent.body;
                MoreStatCoefficients msc = GetMoreStatsFromBody(body);
                if (msc.healingMult > 0)
                {
                    return healAmt * msc.healingMult;
                }
                return healAmt;
            });
            c.Emit(OpCodes.Starg, 1);
            c.Emit(OpCodes.Ldarg_1);
        }

        #region events
        public static FixedConditionalWeakTable<CharacterBody, MoreStatCoefficients> characterCustomStats = new FixedConditionalWeakTable<CharacterBody, MoreStatCoefficients>();
        public class MoreStatHookEventArgs
        {
            #region barrier
            /// <summary>
            /// Tally of barrier freeze sources. If over 0, barrier will not decay.
            /// </summary>
            public int barrierFreezeCount = 0;

            /// <summary>
            /// MULTIPLY to increase decay. Can be multiplied by values less than 1 if you want classic cooldown reduction style reduction.
            /// BARRIER_DECAY_RATE = 
            /// ([BASE_DECAY_RATE] + [barrierDecayPerSecondFlat])
            /// * ([barrierDecayIncreaseMultiplier] / [barrierDecayDecreaseDivisor]) + barrierGenPerSecondFlat
            /// </summary>
            public float barrierDecayRatePercentIncreaseMult = 1;
            /// <summary>
            /// MULTIPLY to reduce decay.
            /// BARRIER_DECAY_RATE = 
            /// ([BASE_DECAY_RATE] + [barrierDecayPerSecondFlat])
            /// * ([barrierDecayIncreaseMultiplier] / [barrierDecayDecreaseDivisor]) + barrierGenPerSecondFlat
            /// </summary>
            public float barrierDecayRatePercentDecreaseDiv = 1;
            public float barrierDecayMultiplier 
            {
                get
                {
                    if (barrierDecayRatePercentDecreaseDiv <= 0 || barrierDecayRatePercentIncreaseMult <= 0)
                        return 0;
                    return barrierDecayRatePercentIncreaseMult / barrierDecayRatePercentDecreaseDiv;
                }
            }

            //flats
            /// <summary>
            /// ADD to reduce barrier decay rate. Generates barrier if above barrier decay rate; not affected by multipliers.
            /// BARRIER_DECAY_RATE = 
            /// ([BASE_DECAY_RATE] + [barrierDecayPerSecondFlat])
            /// * ([barrierDecayIncreaseMultiplier] / [barrierDecayDecreaseDivisor]) + barrierGenPerSecondFlat
            /// </summary>
            public float barrierGenerationRateAddPostMult = 0;
            /// <summary>
            /// ADD to increase barrier decay rate.
            /// BARRIER_DECAY_RATE = 
            /// ([BASE_DECAY_RATE] + [barrierDecayPerSecondFlat])
            /// * ([barrierDecayIncreaseMultiplier] / [barrierDecayDecreaseDivisor]) + barrierGenPerSecondFlat
            /// </summary>
            public float barrierDecayRateAddPreMult = 0;
            #endregion

            #region jumps
            public int jumpCountAdd = 0;
            //public float jumpVerticalIncreaseMultiplier = 1;
            //public float jumpVerticalDecreaseDivisor = 1;
            //public float jumpHorizontalIncreaseMultiplier = 1;
            //public float jumpHorizontalDecreaseMultiplier = 1;
            #endregion

            #region on hit
            /// <summary>
            /// Out of 100, ie +20 is 20% chance to ignite
            /// </summary>
            public float burnChanceOnHit = 0;
            #endregion

            #region shield
            /// <summary>
            /// MULTIPLY by a value between 0 and 1 to increase shield to health conversion
            /// Represents the ratio of max health to shield in the conversion
            /// Expressed as a reversed decimal, i.e. 0.5 is 50% and 0 is 100%
            /// Lowest is 0 (100%), in which case the health stat will always be 1.
            /// </summary>
            public float shieldHealthConversionFractionReversedMult = 1;
            /// <summary>
            /// SUBTRACT to reduce delay, ADD to increase
            /// SHIELD_DELAY = ([baseShieldDelay] + [shieldDelaySecondsIncreaseAddPreMult]) 
            /// * ([shieldDelayPercentIncreaseMult] / [shieldDelayPercentDecreaseDiv]) + shieldDelaySecondsIncreaseAddPostMult
            /// </summary>
            public float shieldDelaySecondsIncreaseAddPreMult = 0f;
            /// <summary>
            /// SUBTRACT to reduce delay, ADD to increase
            /// SHIELD_DELAY = ([baseShieldDelay] + [shieldDelaySecondsIncreaseAddPreMult]) 
            /// * ([shieldDelayPercentIncreaseMult] / [shieldDelayPercentDecreaseDiv]) + shieldDelaySecondsIncreaseAddPostMult
            /// </summary>
            public float shieldDelaySecondsIncreaseAddPostMult = 0f;
            /// <summary>
            /// MULTIPLY to increase delay. Can be multiplied by values less than 1 if you want classic cooldown reduction style reduction
            /// SHIELD_DELAY = ([baseShieldDelay] + [shieldDelaySecondsIncreaseAddPreMult]) 
            /// * ([shieldDelayPercentIncreaseMult] / [shieldDelayPercentDecreaseDiv]) + shieldDelaySecondsIncreaseAddPostMult
            /// </summary>
            public float shieldDelayPercentIncreaseMult = 1f;
            /// <summary>
            /// MULTIPLY to reduce delay.
            /// SHIELD_DELAY = ([baseShieldDelay] + [shieldDelaySecondsIncreaseAddPreMult]) 
            /// * ([shieldDelayPercentIncreaseMult] / [shieldDelayPercentDecreaseDiv]) + shieldDelaySecondsIncreaseAddPostMult
            /// </summary>
            public float shieldDelayPercentDecreaseDiv = 1f;

            public float shieldDelayMultiplier
            {
                get
                {
                    if (shieldDelayPercentDecreaseDiv <= 0 || shieldDelayPercentIncreaseMult <= 0)
                        return 0;
                    return shieldDelayPercentIncreaseMult / shieldDelayPercentDecreaseDiv;
                }
            }
            #endregion

            #region luck
            public float luckAdd = 0;
            #endregion

            #region execution
            /// <summary>
            /// Vanilla sources of execution are mutually exclusive and use the highest threshold rather than adding. Consider this a modded synergy. 
            /// Expressed out of 1, ie 0.15 is +15% max health execution
            /// </summary>
            public float selfExecutionThresholdAdd = 0;
            public float selfExecutionThresholdBase { get; private set; } = Mathf.NegativeInfinity;
            /// <summary>
            /// Mimics vanilla sources of execution, which are mutually exclusive. Uses the highest applicable threshold.
            /// Expressed out of 1, ie 0.15 is 15% max health execution
            /// </summary>
            /// <param name="newThreshold">The execution threshold from your source</param>
            /// <param name="condition">The condition your source needs to meet for the threshold to apply, i.e if the characterbody has the required buff</param>
            public float ModifyBaseExecutionThreshold(float newThreshold, bool condition)
            {
                if (newThreshold <= 0 || selfExecutionThresholdBase >= 1)
                    return selfExecutionThresholdBase;

                if (condition && newThreshold > selfExecutionThresholdBase)
                {
                    selfExecutionThresholdBase = newThreshold;
                }
                return selfExecutionThresholdBase;
            }
            #endregion

            #region healing
            /// <summary>
            /// Just multiply this.
            /// HEALING = [HEAL_AMT_IN] * [healingPercentIncreaseMult]
            /// </summary>
            public float healingPercentIncreaseMult = 1f;
            #endregion

            #region scrap
            /// <summary>
            /// This stat is used for calculating stat bonuses from Scrap - NOT used with printers
            /// </summary>
            public int scrapWhiteCountAdd = 0;
            /// <summary>
            /// This stat is used for calculating stat bonuses from Scrap - NOT used with printers
            /// </summary>
            public int scrapGreenCountAdd = 0;
            /// <summary>
            /// This stat is used for calculating stat bonuses from Scrap - NOT used with printers
            /// </summary>
            public int scrapRedCountAdd = 0;
            /// <summary>
            /// This stat is used for calculating stat bonuses from Scrap - NOT used with printers
            /// </summary>
            public int scrapYellowCountAdd = 0;
            #endregion

            #region health gate
            /// <summary>
            /// Vanilla sources of execution are mutually exclusive and use the highest threshold rather than adding. Consider this a modded synergy. 
            /// Expressed out of 1, ie 0.15 is +15% max health execution
            public int maxHealthGateCount { get; private set; } = 0;
            /// <summary>
            /// Mimics vanilla sources of execution, which are mutually exclusive. Uses the highest applicable threshold.
            /// Expressed out of 1, ie 0.15 is 15% max health execution
            /// </summary>
            /// <param name="newThreshold">The execution threshold from your source</param>
            /// <param name="condition">The condition your source needs to meet for the threshold to apply, i.e if the characterbody has the required buff</param>
            public int ModifyHealthGateCount(int newCount, bool condition = true)
            {
                if (newCount <= 0 || selfExecutionThresholdBase >= 1)
                    return maxHealthGateCount;

                if (condition && newCount > maxHealthGateCount)
                {
                    maxHealthGateCount = newCount;
                }
                return maxHealthGateCount;
            }
            #endregion

            #region
            /// <summary>
            /// Tally of hit stun prevention sources. If over 0, this character will not stagger.
            /// </summary>
            public int preventHitStunCount = 0;
            /// <summary>
            /// MULTIPLY to increase hit stun threshold. >1 values mean the character will take more damage before staggering.
            /// </summary>
            public float hitStunThresholdScaleMult = 1f;
            #endregion
        }
        public delegate void MoreStatHookEventHandler(CharacterBody sender, MoreStatHookEventArgs args);

        public delegate void HealthGateTriggeredEventHandler(CharacterBody sender);
        #endregion

        static MoreStatHookEventArgs StatMods;
        static MoreStatCoefficients CustomStats;
        private static void RecalculateMoreStats(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>(GetStatMods);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<CharacterBody>>((body) =>
            {
                //get stats
                CustomStats = GetMoreStatsFromBody(body);
                if (body.master)
                {
                    body.master.luck -= CustomStats.luckFromBody;
                }
                CustomStats.ResetStats();

                if (body.master)
                {
                    body.master.luck += StatMods.luckAdd;
                    CustomStats.luckFromBody = StatMods.luckAdd;
                }

                CustomStats.burnChance = StatMods.burnChanceOnHit;
                //CustomStats.chillChance = StatMods.chillChanceOnHit;
                CustomStats.healingMult = StatMods.healingPercentIncreaseMult;

                //process barrier decay
                #region barrier
                CustomStats.barrierDecayFrozen = StatMods.barrierFreezeCount > 0;
                CustomStats.barrierDrainRate = StatMods.barrierDecayRateAddPreMult;
                CustomStats.barrierDecayMult = StatMods.barrierDecayMultiplier;
                CustomStats.barrierGenRate = StatMods.barrierGenerationRateAddPostMult;
                #endregion

                //process shield recharge delay
                #region shield delay
                float shieldDelay = (BaseStats.BaseShieldDelaySeconds + StatMods.shieldDelaySecondsIncreaseAddPreMult) 
                    * StatMods.shieldDelayMultiplier + StatMods.shieldDelaySecondsIncreaseAddPostMult;

                CustomStats.shieldRechargeDelay = Mathf.Max(BaseStats.MinShieldDelaySeconds, shieldDelay);
                UpdateShieldRechargeReady(body, CustomStats);
                #endregion

                CustomStats.healthToShieldConversionReversed = 1 - Mathf.Clamp01(StatMods.shieldHealthConversionFractionReversedMult);

                CustomStats.selfExecutionThresholdAdd = StatMods.selfExecutionThresholdAdd;
                CustomStats.selfExecutionThresholdBase = StatMods.selfExecutionThresholdBase;

                CustomStats.maxHealthGateCount = StatMods.maxHealthGateCount;
                CustomStats.hitStunThresholdScale = StatMods.hitStunThresholdScaleMult;
                CustomStats.preventHitStun = StatMods.preventHitStunCount > 0;
            });

            ProcessLuck(c);
            ProcessMaxJumpCount(c);
            ProcessScrapCounts(c);
            if(BaseStats.ApplyShieldConversionHook == true)
                ProcessShieldConversion(c);
        }

        private static void GetStatMods(CharacterBody body)
        {
            StatMods = new MoreStatHookEventArgs();
            if(GetMoreStatCoefficients != null)
            {
                GetMoreStatCoefficients.Invoke(body, StatMods);
            }
        }

        #region luck
        private static void ProcessLuck(ILCursor c)
        {
            c.Index = 0;

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterMaster>("set_luck")
                );

            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessLuck));
                return;
            }

            c.EmitDelegate<Func<float>>(() => StatMods.luckAdd);
            c.Emit(OpCodes.Add);
        }
        #endregion

        #region barrier
        private static void HookBarrierDecayRate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchLdcR4(out _)
                );
            if (ILFound)
            {
                c.Remove();
                c.Remove();
                c.Emit(OpCodes.Ldc_R4, BaseStats.BarrierLowDecayFactor);
                c.Emit(OpCodes.Ldc_R4, BaseStats.BarrierHighDecayFactor);
            }
            else
            {
                Debug.LogError("MORE STATS DYNAMIC BARRIER DECAY HOOK FAILED!!!!");
            }

            bool ILFound2 = c.TryGotoNext(MoveType.Before, 
                x => x.MatchLdcR4(out _),
                x => x.MatchDiv()
                );
            if (ILFound2)
            {
                c.Remove();
                c.Emit(OpCodes.Ldc_R4, BaseStats.BarrierDecayStaticMaxHealthTime);
            }
            else
            {
                Debug.LogError("MORE STATS STATIC BARRIER DECAY HOOK FAILED!!!!");
            }
        }

        private static void ModifyBarrierDecayRate_ServerFixedUpdate(ILCursor c)
        {
            c.Index = 0;

            bool ILFound = c.TryGotoNext(MoveType.After, x => x.MatchCallOrCallvirt<HealthComponent>(nameof(HealthComponent.GetBarrierDecayRate)));

            if(ILFound)
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<float, HealthComponent, float>>((barrierDecayRatePerSecond, healthComponent) =>
                {
                    MoreStatCoefficients stats = GetMoreStatsFromBody(healthComponent.body);
                    if (stats == null)
                        return barrierDecayRatePerSecond;

                    if (!stats.barrierDecayFrozen)
                    {
                        barrierDecayRatePerSecond += stats.barrierDrainRate;
                        barrierDecayRatePerSecond *= stats.barrierDecayMult;
                    }
                    else
                        barrierDecayRatePerSecond = 0;
                    barrierDecayRatePerSecond -= stats.barrierGenRate;

                    return barrierDecayRatePerSecond;
                });
            }
            else
            {
                Debug.LogError("MORE STATS BARRIER DECAY HOOK FAILED!!!!");
            }
        }
        #endregion

        #region jumps
        private static void ProcessMaxJumpCount(ILCursor c)
        {
            c.Index = 0;

            int featherCountLoc = 0;
            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Feather"))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out featherCountLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpCount)),
                x => x.MatchLdloc(featherCountLoc)
                );
            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessMaxJumpCount));
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, CharacterBody, int>>((featherCount, self) =>
            {
                int jumpCount = 0;
                MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                if (featherCount > 0)
                {
                    jumpCount += BaseStats.FeatherJumpCountBase + BaseStats.FeatherJumpCountStack * (featherCount - 1);
                }
                jumpCount += StatMods.jumpCountAdd;

                return Mathf.Max(jumpCount, 0);
            });
        }
        #endregion

        private static bool RoundLuckInCheckRoll(On.RoR2.Util.orig_CheckRoll_float_float_CharacterMaster orig, float percentChance, float luck, CharacterMaster effectOriginMaster)
        {
            float remainder = luck % 1;
            if (remainder < 0)
                remainder += 1;
            if (remainder > Single.Epsilon && Util.CheckRoll(remainder * 100, 0))
            {
                luck = (float)Math.Ceiling(luck);
            }
            else
            {
                luck = (float)Math.Floor(luck);
            }
            return orig(percentChance, luck, effectOriginMaster);
        }

        #region shield recharge delay
        private static void UpdateDangerMoreStats(On.RoR2.CharacterBody.orig_UpdateOutOfCombatAndDanger orig, CharacterBody self)
        {
            orig(self);
            MoreStatCoefficients stats = GetMoreStatsFromBody(self);
            UpdateShieldRechargeReady(self, stats);
        }

        private static void UpdateShieldRechargeReady(CharacterBody body, MoreStatCoefficients stats)
        {
            bool shouldShieldRecharge = body.outOfDangerStopwatch >= stats.shieldRechargeDelay;
            if (stats.shieldRechargeReady != shouldShieldRecharge)
            {
                stats.shieldRechargeReady = shouldShieldRecharge;
                body.statsDirty = true;
            }
        }

        private static void ModifyShieldRechargeReady(ILCursor c)
        {
            c.Index = 0;

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield")
                ) &&
            c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_outOfDanger")
                );
            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ModifyShieldRechargeReady));
                return;
            }

            c.Remove();
            c.EmitDelegate<Func<CharacterBody, bool>>((body) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                return stats.shieldRechargeReady;
            });
        }
        #endregion

        #region shield conversion
        private static void ProcessShieldConversion(ILCursor c)
        {
            c.Index = 0;

            ILLabel gotoHere = c.DefineLabel();
            int localShieldTotalLoc = 74;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.levelMaxShield)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out localShieldTotalLoc)
                );
            if (!b1)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessShieldConversion), 1);
                return;
            }

            bool b2 = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdloc(out _),
                x => x.MatchCallOrCallvirt<CharacterBody>("set_maxShield")
                );
            if(!b2)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessShieldConversion), 2);
                return;
            }
            c.MarkLabel(gotoHere);
            c.Index++;
            c.EmitDelegate<Func<CharacterBody, float, float>>(ConvertHealthToShield);

            bool b3 = c.TryGotoPrev(MoveType.Before,
                x => x.MatchBrfalse(out _)
                );
            if (!b3)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessShieldConversion), 3);
                return;
            }
            c.EmitDelegate<Func<bool, bool>>((_) => { return false; });

            bool b4 = c.TryGotoPrev(MoveType.Before,
                x => x.MatchDup(),
                x => x.MatchBrfalse(out _)
                );
            if (!b4)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(ProcessShieldConversion), 4);
                return;
            }
            c.Remove();
            c.Emit(OpCodes.Ldc_I4_0);
            //c.Index++;
            //c.EmitDelegate<Func<object, bool>>((_) => { return false; });
        }

        public static float ConvertHealthToShield(CharacterBody self, float maxShield)
        {
            float shieldHealthConversionReversed = CustomStats.healthToShieldConversionReversed;

            int transCount = self.inventory.GetItemCountEffective(RoR2Content.Items.ShieldOnly);
            bool isTrans = transCount > 0;
            bool isPerfected = self.HasBuff(RoR2Content.Buffs.AffixLunar);
            bool isOverloading = self.HasBuff(RoR2Content.Buffs.AffixBlue);


            float GetTransConversion(float transBase, float transStack, float perfected)
            {
                if (isTrans == false && isPerfected == false)
                    return 0;
                if (isTrans != isPerfected)
                {
                    if (isPerfected)
                        return perfected;
                    return transBase + transStack * (transCount - 1);
                }
                return Mathf.Max(transBase, perfected)
                    + (transStack * (transCount - 1));
            }

            shieldHealthConversionReversed *= 1 - GetTransConversion(BaseStats.TranscendenceShieldConversionFractionBase, BaseStats.TranscendenceShieldConversionFractionStack, BaseStats.PerfectedShieldConversionFraction);
            shieldHealthConversionReversed *= 1 - (isOverloading ? BaseStats.OverloadingShieldConversionFraction : 0);

            //defaults to 1
            float transHealthBonus = GetTransConversion(BaseStats.TranscendenceHealthBonusBase, BaseStats.TranscendenceHealthBonusStack, BaseStats.PerfectedHealthBonus);

            self.maxHealth *= transHealthBonus;
            float shieldBonus = self.maxHealth * (1 - shieldHealthConversionReversed);
            self.maxHealth = (shieldHealthConversionReversed == 0) ? 1 : self.maxHealth - shieldBonus;

            return maxShield + shieldBonus;
        }
        #endregion

        #region execution
        private static void InterceptExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition))
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );
            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(InterceptExecutionThreshold));
                return;
            }

            c.Emit(OpCodes.Ldloc, thresholdPosition);
            c.Emit(OpCodes.Ldarg, 0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((currentThreshold, hc) =>
            {
                float newThreshold = currentThreshold;

                newThreshold = RecalculateExecutionThreshold(currentThreshold, hc);

                return newThreshold;
            });
            c.Emit(OpCodes.Stloc, thresholdPosition);
        }

        private static float RecalculateExecutionThreshold(float currentThreshold, HealthComponent healthComponent, float mult = 1)
        {
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                    float t = Mathf.Max(currentThreshold, stats.selfExecutionThresholdBase * mult);
                    return t + stats.selfExecutionThresholdAdd;
                }
            }

            return currentThreshold;
        }

        private static HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(RecalculateExecutionThreshold(values.cullFraction, self, Mathf.Clamp01(1f - (1f - 1f / self.body.cursePenalty))));

            return values;
        }
        #endregion

        #region scrap

        private static void ProcessScrapCounts(ILCursor c)
        {
            ProcessScrapCount("ScrapWhite", (scrapCount, self) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                stats.bodyScrapWhiteCount = scrapCount + StatMods.scrapWhiteCountAdd;
                if(BaseStats.IncludeStrangeScrapInScrapTotal)
                    stats.bodyScrapWhiteCount += self.inventory.GetItemCountEffective(DLC1Content.Items.ScrapWhiteSuppressed);

                return stats.bodyScrapWhiteCount;
            });
            ProcessScrapCount("ScrapGreen", (scrapCount, self) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                stats.bodyScrapGreenCount = scrapCount + StatMods.scrapGreenCountAdd;
                if (BaseStats.IncludeStrangeScrapInScrapTotal)
                    stats.bodyScrapGreenCount += self.inventory.GetItemCountEffective(DLC1Content.Items.ScrapGreenSuppressed);

                return stats.bodyScrapGreenCount;
            });
            ProcessScrapCount("ScrapRed", (scrapCount, self) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                stats.bodyScrapRedCount = scrapCount + StatMods.scrapRedCountAdd;
                if (BaseStats.IncludeStrangeScrapInScrapTotal)
                    stats.bodyScrapRedCount += self.inventory.GetItemCountEffective(DLC1Content.Items.ScrapRedSuppressed);

                return stats.bodyScrapRedCount;
            });
            ProcessScrapCount("ScrapYellow", (scrapCount, self) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                stats.bodyScrapYellowCount = scrapCount + StatMods.scrapYellowCountAdd;

                return stats.bodyScrapYellowCount;
            });
            void ProcessScrapCount(string scrapName, Func<int, CharacterBody, int> callback)
            {
                c.Index = 0;
                bool b = c.TryGotoNext(MoveType.After,
                    x => x.MatchLdsfld("RoR2.RoR2Content/Items", scrapName))
                    && c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                    );

                if (!b)
                {
                    MoreStatsPlugin.DebugBreakpoint($"{nameof(ProcessScrapCount)}/{scrapName}");
                    return;
                }

                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate(callback);
            }
        }

        private static void DrifterUpdateScrapCounts(On.RoR2.DrifterTrashToTreasureController.orig_OnInventoryChanged orig, DrifterTrashToTreasureController self)
        {
            MoreStatCoefficients stats = GetMoreStatsFromBody(self.body);

            self.body.SetBuffCount(DLC3Content.Buffs.TrashToTreasureWhite.buffIndex, stats.bodyScrapWhiteCount);
            self.body.SetBuffCount(DLC3Content.Buffs.TrashToTreasureGreen.buffIndex, stats.bodyScrapGreenCount);
            self.body.SetBuffCount(DLC3Content.Buffs.TrashToTreasureRed.buffIndex, stats.bodyScrapRedCount);
            self.body.SetBuffCount(DLC3Content.Buffs.TrashToTreasureYellow.buffIndex, stats.bodyScrapYellowCount);
        }
        #endregion

        #region health gating
        private static void InsertHealthGates(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int localDamageLoc = 10;
            ILLabel jumpTo = c.DefineLabel();
            ILLabel branchHere = c.DefineLabel();
            bool b1 =
                //get local damage variable location
                c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<DamageInfo>("damage"),
                x => x.MatchStloc(out localDamageLoc))
                //find reference to minHealthPercentage item
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<RoR2.HealthComponent.ItemCounts>(nameof(RoR2.HealthComponent.ItemCounts.minHealthPercentage)))
                //record next label to jump to
                && c.TryGotoNext(MoveType.Before,
                x => x.MatchBle(out jumpTo)
                );
            if (!b1)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(InsertHealthGates), 1);
                return;
            }

            //c.GotoLabel(jumpTo, MoveType.Before);
            c.GotoLabel(jumpTo, MoveType.After);
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, localDamageLoc);
            c.EmitDelegate<Func<HealthComponent, float, float>>((self, damageIn) =>
            {
                if (self.itemCounts.minHealthPercentage > 0 || self.body == null)
                    return damageIn;

                MoreStatCoefficients statCoefficients = GetMoreStatsFromBody(self.body);

                if (statCoefficients.maxHealthGateCount <= 0)
                    return damageIn;

                float nextThresholdHealthFraction = statCoefficients.GetNextThresholdHealthFraction();
                if (nextThresholdHealthFraction < statCoefficients.GetHealthFractionSize())
                    return damageIn;

                float healthAboveNextThreshold = (self.combinedHealth) - (self.fullCombinedHealth * nextThresholdHealthFraction);
                if (damageIn < healthAboveNextThreshold)
                    return damageIn;

                RecordBodyLowestHealth(statCoefficients, nextThresholdHealthFraction - 0.01f);
                if (OnBodyHealthGateTriggeredGlobal != null)
                {
                    OnBodyHealthGateTriggeredGlobal.Invoke(self.body);
                }
                return healthAboveNextThreshold;
            });
            c.Emit(OpCodes.Stloc, localDamageLoc);
            //c.Index -= 4;
            //c.MarkLabel(branchHere);
            //
            //c.GotoPrev(MoveType.Before,
            //    x => x.MatchBle(jumpTo)
            //    );
            //c.Remove();
            //c.Emit(OpCodes.Ble_S, branchHere);
        }

        private static void DisplayHealthGates(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b1 = c.TryGotoNext(MoveType.Before,
                x => x.MatchStfld<RoR2.HealthComponent.HealthBarValues>(nameof(RoR2.HealthComponent.HealthBarValues.ospFraction))
                );
            if (!b1)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(DisplayHealthGates));
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((ospFractionIn, self) =>
            {
                float f = GetNewFraction();
                float GetNewFraction()
                {
                    if (self.body == null)
                        return ospFractionIn;

                    MoreStatCoefficients statCoefficients = GetMoreStatsFromBody(self.body);

                    //haha im tispy idek what i cooked here
                    float next = statCoefficients.GetNextThresholdHealthFraction();
                    if (next < statCoefficients.GetHealthFractionSize())
                        return ospFractionIn;

                    float nextAfterCurse = next * (1f - 1f / self.body.cursePenalty);
                    if (nextAfterCurse < ospFractionIn)
                        return ospFractionIn;
                    return next;
                }
                Debug.Log(f);
                return f;
            });
        }

        private static void RecordHealthFractionBody(CharacterBody obj)
        {
            RecordBodyLowestHealth(obj);
        }
        private static void RecordHealthFractionClient(DamageDealtMessage damageDealtMessage)
        {
            if(damageDealtMessage.victim && damageDealtMessage.victim.TryGetComponent(out CharacterBody victimBody))
            {
                RecordBodyLowestHealth(victimBody);
            }
        }
        private static void RecordHealthFractionServer(DamageReport damageReport)
        {
            RecordBodyLowestHealth(damageReport.victimBody);
        }
        private static void RecordBodyLowestHealth(CharacterBody victimBody)
        {
            if (victimBody == null)
                return;
            RecordBodyLowestHealth(GetMoreStatsFromBody(victimBody), victimBody.healthComponent.combinedHealthFraction);
        }
        private static void RecordBodyLowestHealth(MoreStatCoefficients statCoefficients, float healthFraction)
        {
            if (statCoefficients == null)
                return;
            statCoefficients.lowestCombinedHealthFraction = Mathf.Min(statCoefficients.lowestCombinedHealthFraction, healthFraction);
        }
        #endregion
    }
}
