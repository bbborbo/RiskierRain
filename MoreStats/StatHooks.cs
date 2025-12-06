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

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchStloc(out _)
                );
            if (b)
            {
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
            else
            {
                Debug.LogError("MoreStats Healing Hook Failed!!!!");
                Debug.LogError("MoreStats Healing Hook Failed!!!!");
                Debug.LogError("MoreStats Healing Hook Failed!!!!");
                Debug.LogError("MoreStats Healing Hook Failed!!!!");
                Debug.LogError("MoreStats Healing Hook Failed!!!!");
            }
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
            /// ADD to increase shield to health conversion
            /// Expressed as a decimal, i.e. 0.5 is 50% and 1 is 100%
            /// Max of 1 (100%), in which case the health stat will always be 1.
            /// </summary>
            public float shieldToHealthConversionFractionAdd = 0;
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
        }
        public delegate void MoreStatHookEventHandler(CharacterBody sender, MoreStatHookEventArgs args);
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

                CustomStats.selfExecutionThresholdAdd = StatMods.selfExecutionThresholdAdd;
                CustomStats.selfExecutionThresholdBase = StatMods.selfExecutionThresholdBase;
            });

            ProcessLuck(c);
            ProcessMaxJumpCount(c);
        }

        private static void GetStatMods(CharacterBody body)
        {
            StatMods = new MoreStatHookEventArgs();
            if(GetMoreStatCoefficients != null)
            {
                GetMoreStatCoefficients.Invoke(body, StatMods);
            }
        }

        #region barrier

        private static void ProcessLuck(ILCursor c)
        {
            c.Index = 0;

            bool ILFound = c.TryGotoNext(MoveType.Before, x => x.MatchCallOrCallvirt<CharacterMaster>("set_luck"));

            if (ILFound)
            {
                c.EmitDelegate<Func<float>>(() => StatMods.luckAdd);
                c.Emit(OpCodes.Add);
            }
            else
            {
                Debug.LogError("MORE STATS LUCK STAT HOOK FAILED!!!!");
            }
        }

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
            if(c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Feather")
                ) &&
            c.TryGotoNext(MoveType.After,
                x => x.MatchStloc(out featherCountLoc)
                ) &&
            c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpCount)),
                x => x.MatchLdloc(featherCountLoc)
                ))
            {
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
            else
            {
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED!!!!");
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED!!!!");
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED!!!!");
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED!!!!");
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED!!!!");
            }
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

            if(c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield")
                ) &&
            c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_outOfDanger")
                ))
            {

                c.Remove();
                c.EmitDelegate<Func<CharacterBody, bool>>((body) =>
                {
                    MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                    return stats.shieldRechargeReady;
                });
            }
            else
            {
                Debug.LogError("MORE STATS SHIELD DECAY HOOK FAILED!!!!");
                Debug.LogError("MORE STATS SHIELD DECAY HOOK FAILED!!!!");
                Debug.LogError("MORE STATS SHIELD DECAY HOOK FAILED!!!!");
                Debug.LogError("MORE STATS SHIELD DECAY HOOK FAILED!!!!");
                Debug.LogError("MORE STATS SHIELD DECAY HOOK FAILED!!!!");
            }
        }
        #endregion

        #region execution
        private static void InterceptExecutionThreshold(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int thresholdPosition = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdcR4(float.NegativeInfinity),
                x => x.MatchStloc(out thresholdPosition)
                );

            c.GotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<HealthComponent>("get_isInFrozenState")
                );

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
    }
    public class MoreStatCoefficients
    {
        public bool  barrierDecayFrozen = false;
        public float barrierDecayDynamicHalfLife = 0;
        public float barrierGenRate = 0;
        public float barrierDrainRate = 0;
        public float barrierDecayMult = 1;

        public float luckFromBody = 0;
        public float luckFromMaster = 0;
        public float burnChance = 0;
        //public float chillChance = 0;

        public bool  shieldRechargeReady = true;
        public float shieldRechargeDelay = BaseStats.BaseShieldDelaySeconds;
        float shieldToHealthConversion = 0;

        public float selfExecutionThresholdAdd = 0;
        public float selfExecutionThresholdBase = Mathf.NegativeInfinity;

        public float healingMult = 1;

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
        }
    }
}
