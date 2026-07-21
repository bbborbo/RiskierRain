using EntityStates;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace MoreStats
{
    public static class JumpAPI
    {
        public class JumpSynergyInfo
        {
            public bool isConditionalJump = false;
            public bool didJumpIgnoreRequirements = false;
            public bool isAirJump = false;

            public JumpSynergyInfo(bool isConditionalJump, bool didJumpIgnoreRequirements)
            {
                this.isConditionalJump = isConditionalJump;
                this.didJumpIgnoreRequirements = didJumpIgnoreRequirements;
            }
        }
        public static bool IsDoubleJump(CharacterMotor motor)
        {
            CharacterBody body = motor.body;
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount;

            if (timesJumped > baseJumpCount)
                return true;
            return false;
        }
        public static bool IsBaseJump(CharacterMotor motor, bool mustBeGrounded = false)
        {
            CharacterBody body = motor.body;
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount;

            if (mustBeGrounded)
                return timesJumped == 0;

            if (timesJumped <= baseJumpCount)
                return true;
            return false;
        }
        public static bool IsLastJump(CharacterMotor motor)
        {
            CharacterBody body = motor.body;
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount;

            if (timesJumped >= maxJumpCount)
                return true;
            return false;
        }

        /// <summary>
        /// Only to be used inside of Jump API events. Uses the highest proposed value. 
        /// Calling this method outside of Jump API events will do nothing
        /// </summary>
        public static void SetJumpPowerForCurrentJump(float vBonus = 0, float hBonus = 0)
        {
            if (verticalBonus <= vBonus)
                verticalBonus = vBonus;

            if (horizontalBonus <= hBonus)
                horizontalBonus = hBonus;
        }


        static bool initialized = false;
        internal static void Init()
        {
            if (initialized)
                return;
            initialized = true;

            IL.EntityStates.GenericCharacterMain.ProcessJump_bool += InsertConditionalJumps;
        }

        #region events
        public delegate bool ConditionalJumpHandler(CharacterMotor sender, bool jumpIgnoredRequirements);
        /// <summary>
        /// Conditional jumps applied to this tier will ALL be triggered simulaneously provided their individual conditions are met to trigger. 
        /// Urgent effects are triggered before other jumps are consumed.
        /// </summary>
        public static event ConditionalJumpHandler OnConditionalJumpUrgent;
        /// <summary>
        /// Conditional jumps applied to this tier will be triggered one at a time based on listener order, before Hopoo Feathers are consumed.
        /// </summary>
        public static event ConditionalJumpHandler OnConditionalJumpPriority;
        /// <summary>
        /// Conditional jumps applied to this tier will be triggered one at a time based on listener order, after Hopoo Feathers are consumed.
        /// </summary>
        public static event ConditionalJumpHandler OnConditionalJumpLast;

        public delegate void OnJumpSynergyHandler(CharacterMotor sender, JumpSynergyInfo jumpSynergyInfo);
        public static event OnJumpSynergyHandler OnJumpEvent;
        #endregion


        private static void InsertConditionalJump(On.EntityStates.GenericCharacterMain.orig_ProcessJump_bool orig, EntityStates.GenericCharacterMain self, bool ignoreRequirements)
        {

            orig(self, ignoreRequirements);

            bool InvokeJumpSequentially(ConditionalJumpHandler jumpEvent)
            {
                foreach (Delegate del in jumpEvent.GetInvocationList())
                {
                    if ((bool)del.DynamicInvoke(self, self.characterBody, verticalBonus))
                        return true;
                }
                return false;
            }
        }
        private static float verticalBonus = 0;
        private static float horizontalBonus = 0;
        private static bool didConditionalJump = false;
        private static void InsertConditionalJumps(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.Index = 0;
            InsertConditionalJumpProc(c);
            c.Index = 0;
            InsertJumpBonus(c);
        }

        private static void InsertJumpBonus(ILCursor c)
        {
            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<GenericCharacterMain>(nameof(GenericCharacterMain.ApplyJumpVelocity)))
                && c.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(out _),
                x => x.MatchLdloc(out _)
                );
            if (!b)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(InsertJumpBonus), 0);
                return;
            }
            c.Index++;
            c.EmitDelegate<Func<float, float>>((hBonusIn) => AddJumpBonus(hBonusIn, horizontalBonus));
            c.Index++;
            c.EmitDelegate<Func<float, float>>((vBonusIn) => AddJumpBonus(vBonusIn, verticalBonus));

            float AddJumpBonus(float bonusIn, float bonusBonus)
            {
                return bonusIn + bonusBonus;
            }
        }

        private static void InsertConditionalJumpProc(ILCursor c)
        {
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<GenericCharacterMain, bool>>((self, ignoreRequirements) =>
            {
                verticalBonus = 0;
                horizontalBonus = 0;
                didConditionalJump = false;
                if (self.jumpInputReceived == false || self.hasCharacterMotor == false || (self.characterMotor.jumpCount <= 0) || !NetworkServer.active)
                    return;
                if (self.characterBody.isPlayerControlled)
                    Debug.Log(self.characterMotor.jumpCount);

                if (GetConditionalJump(self, ignoreRequirements))
                {
                    didConditionalJump = true;
                    //self.characterMotor.jumpCount--;
                }
                if (self.characterBody.isPlayerControlled)
                    Debug.Log(didConditionalJump);
            });

            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<GenericCharacterMain>(nameof(GenericCharacterMain.jumpInputReceived)))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out _)
                );
            if (!b1)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(InsertConditionalJumpProc), 1);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Func<bool, GenericCharacterMain, bool, bool>>((canJump, characterMain, ignoreRequirements) =>
            {
                if (canJump || didConditionalJump)
                {
                    if(characterMain.characterBody.isPlayerControlled)
                        Debug.Log("did jump");
                    OnJumpEvent?.Invoke(characterMain.characterMotor,
                        new JumpSynergyInfo(
                            isConditionalJump: didConditionalJump,
                            didJumpIgnoreRequirements: ignoreRequirements
                            )
                        );
                    return true;
                }
                return false;
            });

            bool b2 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterMotor>(nameof(CharacterMotor.jumpCount)),
                x => x.MatchLdcI4(1)
                );
            if (!b2)
            {
                MoreStatsPlugin.DebugBreakpoint(nameof(InsertConditionalJumpProc), 2);
                return;
            }
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int,GenericCharacterMain, int>>((jumpCountToAdd, self) =>
            {
                if (didConditionalJump)
                {
                    if (self.characterBody.isPlayerControlled)
                        Debug.Log("aaaa");
                    return 0;
                }
                if (self.characterBody.isPlayerControlled)
                    Debug.Log("uuuu");
                return jumpCountToAdd; //which would be 1 but idk dont wanna mess with it
            });
        }

        private static bool GetConditionalJump(GenericCharacterMain self, bool jumpIgnoredRequirements)
        {
            bool InvokeJumpSequentially(ConditionalJumpHandler jumpEvent, bool onlyOneJump = true)
            {
                if (jumpEvent == null)
                    return false;

                Delegate[] delegates = jumpEvent.GetInvocationList();
                if (delegates == null || delegates.Length == 0)
                    return false;

                bool hasTriggeredConditionalJump = false;
                foreach (ConditionalJumpHandler del in delegates)
                {
                    if (del.Invoke(self.characterMotor, jumpIgnoredRequirements))
                    {
                        hasTriggeredConditionalJump = true;

                        if (onlyOneJump)
                            break;
                    }
                }
                return hasTriggeredConditionalJump;
            }

            //urgent jumps after grounded jump, before base air jumps
            if (InvokeJumpSequentially(OnConditionalJumpUrgent, onlyOneJump: false))
                return true;

            //base air jumps; if the survivor has no additional base jumps, skip to priority
            if (self.characterMotor.jumpCount < self.characterBody.baseJumpCount)
                return false;

            //priority jumps between base air jumps and air jumps added by items like hopoo
            if (InvokeJumpSequentially(OnConditionalJumpPriority, onlyOneJump: true))
                return true;

            //hopoos; if the character has no jumps from items, skip to last
            if (self.characterMotor.jumpCount < self.characterBody.maxJumpCount)
                return false;

            //last jumps after all other jumps
            if (InvokeJumpSequentially(OnConditionalJumpLast, onlyOneJump: true))
                return true;

            return false;
        }
    }
}
