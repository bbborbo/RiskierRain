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
            // Luck Stat Fixes
            On.RoR2.CharacterMaster.OnInventoryChanged += UpdateMoreLuckStat;

            // Barrier Decay And Shield Recharge
            IL.RoR2.HealthComponent.ServerFixedUpdate += HookHealthComponentUpdate;

            // Execution
            IL.RoR2.HealthComponent.TakeDamageProcess += InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;

            ILHook luckHook = new ILHook(typeof(CharacterMaster).GetMethod("get_luck", (BindingFlags)(-1)), ModifyLuck);
        }

        private static void ModifyLuck(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchRet()
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<Single, CharacterMaster, Single>>((baseLuck, master) =>
            {
                if (master == null || !master.hasBody)
                    return baseLuck;

                CharacterBody body = master.GetBody();
                MoreStatCoefficients msc = GetMoreStatsFromBody(body);
                float newLuck = baseLuck + msc.luckAdd;
                float remainder = newLuck % 1;
                if (remainder > Single.Epsilon && Util.CheckRoll(remainder * 100, 0))
                {
                    newLuck = Mathf.CeilToInt(newLuck);
                }
                else
                {
                    newLuck = Mathf.FloorToInt(newLuck);
                }
                return newLuck;
            });
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

            //multipliers
            public float barrierDecayIncreaseMultiplier = 1;
            public float barrierDecayDecreaseDivisor = 1;
            public float barrierDecayMultiplier 
            {
                get
                {
                    if (barrierDecayDecreaseDivisor <= 0 || barrierDecayIncreaseMultiplier <= 0)
                        return 0;
                    return barrierDecayIncreaseMultiplier / barrierDecayDecreaseDivisor;
                }
            }

            //flats
            /// <summary>
            /// Added to barrier decay, i.e +25 means 25 barrier is generated per second
            /// Does not get multiplied by barrier decay multipliers
            /// </summary>
            public float barrierGenPerSecondFlat = 0;
            /// <summary>
            /// Subtracted from barrier decay, i.e +25 means 25 more barrier decays per second
            /// </summary>
            public float barrierDecayPerSecondFlat = 0;

            //base decay
            /// <summary>
            /// pls dont touch this unless ur a rework mod
            /// </summary>
            public float barrierBaseStaticDecayRateMaxHealthTime = 30;
            /// <summary>
            /// pls dont touch this unless ur a rework mod
            /// </summary>
            public float barrierBaseDynamicDecayRateHalfLife = 0;
            #endregion

            #region jumps
            public int jumpCountAdd = 0;
            //public float jumpVerticalIncreaseMultiplier = 1;
            //public float jumpVerticalDecreaseDivisor = 1;
            //public float jumpHorizontalIncreaseMultiplier = 1;
            //public float jumpHorizontalDecreaseMultiplier = 1;

            /// <summary>
            /// pls dont touch this unless ur a rework mod
            /// </summary>
            public int featherJumpCountBase = 1;
            /// <summary>
            /// pls dont touch this unless ur a rework mod
            /// </summary>
            public int featherJumpCountStack = 1;
            #endregion

            #region on hit
            /// <summary>
            /// Out of 100, ie +20 is 20% chance to ignite
            /// </summary>
            public float burnChanceOnHit = 0;
            #endregion

            #region shield delay
            /// <summary>
            /// Subtract to reduce delay, add to increase
            /// </summary>
            public float shieldDelayIncreaseInSeconds = 0f;
            /// <summary>
            /// Multiply by one minus your percentage reduction. Please don't let this stat reach zero.
            /// </summary>
            public float shieldDelayMultiplier = 1f;
            #endregion

            #region luck
            public int luckAdd = 0;
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
                CustomStats = characterCustomStats.GetOrCreateValue(body);
                CustomStats.ResetStats();

                CustomStats.luckAdd = StatMods.luckAdd;

                //process shield recharge delay
                CustomStats.shieldRechargeDelay = (MoreStatsPlugin.BaseShieldRechargeDelay + StatMods.shieldDelayIncreaseInSeconds) * StatMods.shieldDelayMultiplier;
                //Debug.Log(stats.shieldRechargeDelay);
                UpdateShieldRechargeReady(body, CustomStats);

                CustomStats.selfExecutionThresholdAdd = StatMods.selfExecutionThresholdAdd;
                CustomStats.selfExecutionThresholdBase = StatMods.selfExecutionThresholdBase;
            });

            ProcessBarrierDecayRate_RecalcStats(c);
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

        private static void HookHealthComponentUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);
            ModifyShieldRechargeReady(c);
            ModifyBarrierDecayRate_ServerFixedUpdate(c);
        }

        #region barrier
        private static void ProcessBarrierDecayRate_RecalcStats(ILCursor c)
        {
            c.Index = 0;

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("set_barrierDecayRate")
                );
            c.GotoPrev(MoveType.After,
                x => x.MatchLdcR4(out _)
                );
            c.Remove(); //remove div
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, float, CharacterBody, float>>((maxBarrier, decayTime, body) =>
            {
                //process barrier decay stats
                float decayRate = 0;
                bool decayFrozen = StatMods.barrierFreezeCount > 0;
                float decayMultiplier = StatMods.barrierDecayDecreaseDivisor > 0 ? StatMods.barrierDecayIncreaseMultiplier / StatMods.barrierDecayDecreaseDivisor : 0;

                CustomStats.barrierDecayFrozen = StatMods.barrierFreezeCount > 0;
                CustomStats.barrierDecayDynamicHalfLife = decayMultiplier > 0 ? StatMods.barrierBaseDynamicDecayRateHalfLife / decayMultiplier : 0;

                if (!decayFrozen && decayMultiplier > 0)
                {
                    decayRate = StatMods.barrierDecayPerSecondFlat;
                    if (StatMods.barrierBaseStaticDecayRateMaxHealthTime > 0)
                    {
                        decayRate += maxBarrier / StatMods.barrierBaseStaticDecayRateMaxHealthTime;
                    }
                    decayRate *= decayMultiplier;
                }

                decayRate -= StatMods.barrierGenPerSecondFlat;
                //if(StatMods.barrierGenPerSecondFlat > 0)
                //{
                //    if(StatMods.barrierGenPerSecondFlat > decayRate)
                //    {
                //        float excessBarrierGen = StatMods.barrierGenPerSecondFlat - decayRate;
                //        decayRate = 0;
                //        CustomStats.barrierGenRate = excessBarrierGen;
                //    }
                //    else
                //    {
                //        decayRate -= StatMods.barrierGenPerSecondFlat;
                //    }
                //}

                return decayRate;
            });
            //c.EmitDelegate<Func<float>>(() => StatMods.barrierBaseStaticDecayRateMaxHealthTime);
        }

        private static void ModifyBarrierDecayRate_ServerFixedUpdate(ILCursor c)
        {
            c.Index = 0;

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<HealthComponent>("barrier"),
                x => x.MatchLdcR4(out _)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, HealthComponent, float>>((minBarrier, healthComponent) =>
            {
                CharacterBody body = healthComponent.body;
                if (body)
                {
                    if (body.barrierDecayRate < 0)
                    {
                        //return -1;
                        minBarrier += body.barrierDecayRate;
                    }
                }
                return minBarrier;
            });

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_barrierDecayRate")
                );
            c.Remove();
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<CharacterBody, HealthComponent, float>>((body, healthComponent) =>
            {
                float barrierDecayRate = body.barrierDecayRate;
                MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                if(stats == null)
                    return barrierDecayRate;

                if (!stats.barrierDecayFrozen && stats.barrierDecayDynamicHalfLife > 0)
                {
                    barrierDecayRate += Mathf.Max(1 - stats.barrierGenRate, healthComponent.barrier * Mathf.Log(2) / stats.barrierDecayDynamicHalfLife);
                }

                //healthComponent.AddBarrier(stats.barrierGenRate * Time.fixedDeltaTime);

                return barrierDecayRate;
            });
        }
        #endregion

        #region jumps
        private static void ProcessMaxJumpCount(ILCursor c)
        {
            c.Index = 0;

            int featherCountLoc = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Feather")
                );
            c.GotoNext(MoveType.After,
                x => x.MatchStloc(out featherCountLoc)
                );

            bool jumpCountILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpCount)),
                x => x.MatchLdloc(featherCountLoc)
                );
            if (jumpCountILFound)
            {
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<int, CharacterBody, int>>((featherCount, self) =>
                {
                    int jumpCount = 0;
                    MoreStatCoefficients stats = GetMoreStatsFromBody(self);
                    if (featherCount > 0)
                    {
                        jumpCount += StatMods.featherJumpCountBase + StatMods.featherJumpCountStack * (featherCount - 1);
                    }
                    jumpCount += StatMods.jumpCountAdd;

                    return jumpCount;
                });
            }
            else
            {
                Debug.LogError("MORE STATS JUMP COUNT HOOK FAILED");
            }
        }
        #endregion

        #region luck
        private static void UpdateMoreLuckStat(On.RoR2.CharacterMaster.orig_OnInventoryChanged orig, CharacterMaster self)
        {
            CharacterBody body = self.GetBody();
            if (body)
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                stats.luckAdd = 0;
            }
            orig(self);
        }
        #endregion

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

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_maxShield")
                );
            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>("get_outOfDanger")
                );
            c.Remove();
            c.EmitDelegate<Func<CharacterBody, bool>>((body) =>
            {
                MoreStatCoefficients stats = GetMoreStatsFromBody(body);
                return stats.shieldRechargeReady;
            });
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

        public float burnChance = 0;

        public bool  shieldRechargeReady = true;
        public float shieldRechargeDelay = MoreStatsPlugin.BaseShieldRechargeDelay;

        public float selfExecutionThresholdAdd = 0;
        public float selfExecutionThresholdBase = Mathf.NegativeInfinity;

        public int luckAdd = 0;

        public void ResetStats()
        {
            barrierDecayFrozen = false;
            barrierDecayDynamicHalfLife = 0;
            barrierGenRate = 0;

            burnChance = 0;

            shieldRechargeReady = true;
            shieldRechargeDelay = MoreStatsPlugin.BaseShieldRechargeDelay;

            selfExecutionThresholdAdd = 0;
            selfExecutionThresholdBase = 0;

            luckAdd = 0;
        }
    }
}
