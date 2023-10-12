using BepInEx;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using UnityEngine;

namespace BorboStatUtils
{
    [BepInPlugin(guid, teamName, modName)]
    [R2APISubmoduleDependency(nameof(LanguageAPI))]
    public class BorboStatUtilsPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "HouseOfFruits";
        public const string modName = "2R4RStatUtils";
        public const string version = "1.0.0";
        #endregion

        public const string executeKeywordToken = "2R4R_EXECUTION_KEYWORD";
        public const float survivorExecuteThreshold = 0.15f;

        public void Awake()
        {
            LanguageAPI.Add(executeKeywordToken,
                $"<style=cKeywordName>Finisher</style>" +
                $"<style=cSub>Enemies targeted by this skill can be " +
                $"<style=cIsHealth>instantly killed</style> if below " +
                $"<style=cIsHealth>{survivorExecuteThreshold  * 100}% health</style>.</style>");
        }

        #region luck
        internal static void SetLuckHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats += RecalculateLuckStat;
        }
        internal static void UnsetLuckHooks()
        {
            On.RoR2.CharacterBody.RecalculateStats -= RecalculateLuckStat;
        }

        public delegate void LuckHookEventHandler(CharacterBody sender, float luck);
        public static event LuckHookEventHandler ModifyLuckStat
        {
            add
            {
                SetLuckHooks();

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
            CalculateLuck(self.master);
        }
        public static void CalculateLuck(CharacterMaster master)
        {
            float luck = 0;
            CharacterBody body = master.GetBody();
            if (body)
            {
                //luck += body.GetBuffCount(luckBuffIndex);

                if (_modifyLuckStat != null)
                {
                    foreach (ExecuteHookEventHandler @event in _modifyLuckStat.GetInvocationList())
                    {
                        try
                        {
                            @event(body, luck);
                        }
                        catch (Exception e)
                        {
                            Debug.LogError(e);
                        }
                    }
                }
            }
            luck += (float)master.inventory.GetItemCount(RoR2Content.Items.Clover);
            luck -= (float)master.inventory.GetItemCount(RoR2Content.Items.LunarBadLuck);

            master.luck = luck;
        }
        #endregion

        #region execute
        internal static void SetExecutionHooks()
        {
            IL.RoR2.HealthComponent.TakeDamage += InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues += DisplayExecutionThreshold;
        }
        internal static void UnsetExecutionHooks()
        {
            IL.RoR2.HealthComponent.TakeDamage -= InterceptExecutionThreshold;
            On.RoR2.HealthComponent.GetHealthBarValues -= DisplayExecutionThreshold;
        }

        public delegate void ExecuteHookEventHandler(CharacterBody sender, float executeThreshold);
        public static event ExecuteHookEventHandler GetExecutionThreshold
        {
            add
            {
                SetExecutionHooks();

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
                    //GetExecutionThreshold?.Invoke(currentThreshold, body);
                    if (_getExecutionThreshold != null)
                    {
                        foreach (ExecuteHookEventHandler @event in _getExecutionThreshold.GetInvocationList())
                        {
                            try
                            {
                                @event(body, newThreshold);
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

        public static float ModifyExecutionThreshold(ref float currentThreshold, float newThreshold, bool condition)
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
