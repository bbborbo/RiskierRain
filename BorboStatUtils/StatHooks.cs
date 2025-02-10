using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace RainrotSharedUtils
{
    public static class StatHooks
    {
        #region luck
        // i basically copied how this delegate stuff works from RecalculateStatsAPI
        internal static void SetLuckHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += RecalculateLuckStat;
        }
        internal static void UnsetLuckHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats -= RecalculateLuckStat;
        }

        public delegate void LuckHookEventHandler(CharacterBody sender, ref float luck);
        public static event LuckHookEventHandler ModifyLuckStat
        {
            add
            {
                if (_modifyLuckStat == null ||
                    _modifyLuckStat.GetInvocationList()?.Length == 0)
                {
                    SetLuckHooks();
                }

                _modifyLuckStat += value;
            }

            remove
            {
                _modifyLuckStat -= value;

                if (_modifyLuckStat == null ||
                    _modifyLuckStat.GetInvocationList()?.Length == 0)
                {
                    UnsetLuckHooks();
                }
            }
        }
        private static event LuckHookEventHandler _modifyLuckStat;

        private static void RecalculateLuckStat(On.RoR2.CharacterBody.orig_RecalculateStats orig, CharacterBody self)
        {
            orig(self);
            CalculateLuck(self);
        }
        public static void CalculateLuck(CharacterBody self)
        {
            CharacterMaster master = self.master;
            if (master == null)
                return;

            float luck = 0;
            if (self)
            {
                //luck += body.GetBuffCount(luckBuffIndex);

                if (_modifyLuckStat != null)
                {
                    foreach (LuckHookEventHandler @event in _modifyLuckStat.GetInvocationList())
                    {
                        try
                        {
                            @event(self, ref luck);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }
            if (self.inventory)
            {
                luck += (float)self.inventory.GetItemCount(RoR2Content.Items.Clover);
                luck -= (float)self.inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);
            }

            master.luck = luck;
        }
        #endregion

        #region execute
        // i basically copied how this delegate stuff works from RecalculateStatsAPI
        internal static void SetExecutionHooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess += InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;
        }
        internal static void UnsetExecutionHooks()
        {
            IL.RoR2.HealthComponent.TakeDamageProcess -= InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues -= DisplayExecutionThreshold;
        }

        public delegate void ExecuteHookEventHandler(CharacterBody sender, ref float executeThreshold);
        public static event ExecuteHookEventHandler GetExecutionThreshold
        {
            add
            {
                if (_getExecutionThreshold == null ||
                    _getExecutionThreshold.GetInvocationList()?.Length == 0)
                {
                    SetExecutionHooks();
                }

                _getExecutionThreshold += value;
            }

            remove
            {
                _getExecutionThreshold -= value;

                if (_getExecutionThreshold == null ||
                    _getExecutionThreshold.GetInvocationList()?.Length == 0)
                {
                    UnsetExecutionHooks();
                }
            }
        }
        private static event ExecuteHookEventHandler _getExecutionThreshold;
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

        private static float RecalculateExecutionThreshold(float currentThreshold, HealthComponent healthComponent)
        {
            float newThreshold = currentThreshold;
            CharacterBody body = healthComponent.body;

            if (body != null)
            {
                if (!body.bodyFlags.HasFlag(CharacterBody.BodyFlags.ImmuneToExecutes))
                {
                    //GetExecutionThreshold?.Invoke(body, ref currentThreshold);
                    if (_getExecutionThreshold != null)
                    {
                        foreach (ExecuteHookEventHandler @event in _getExecutionThreshold.GetInvocationList())
                        {
                            try
                            {
                                @event(body, ref newThreshold);
                            }
                            catch (Exception e)
                            {
                                Debug.LogError(e);
                            }
                        }
                    }
                }
            }

            return newThreshold;
        }

        public static float ModifyExecutionThreshold(float currentThreshold, float newThreshold, bool condition)
        {
            if (condition)
            {
                currentThreshold = Mathf.Max(currentThreshold, newThreshold);
            }
            return currentThreshold;
        }

        private static HealthComponent.HealthBarValues DisplayExecutionThreshold(On.RoR2.HealthComponent.orig_GetHealthBarValues orig, HealthComponent self)
        {
            HealthComponent.HealthBarValues values = orig(self);

            values.cullFraction = Mathf.Clamp01(RecalculateExecutionThreshold(values.cullFraction, self));

            return values;
        }
        #endregion
    }
}
