using RoR2;
using System;
using System.Collections.Generic;
using System.Text;

namespace MoreStats
{
    public static class OnJump
    {
        public static bool IsDoubleJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped > baseJumpCount)
                return true;
            return false;
        }
        public static bool IsBaseJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped <= baseJumpCount)
                return true;
            return false;
        }
        public static bool IsLastJump(CharacterMotor motor, CharacterBody body)
        {
            int maxJumpCount = body.maxJumpCount;
            int baseJumpCount = body.baseJumpCount;
            int timesJumped = motor.jumpCount + 1;

            if (timesJumped == maxJumpCount)
                return true;
            return false;
        }


        static bool initialized = false;
        internal static void Init()
        {
            if (initialized)
                return;
            initialized = true;

            On.EntityStates.GenericCharacterMain.ApplyJumpVelocity += CharacterMain_JumpVelocity;
        }


        public delegate void OnJumpHandler(CharacterMotor sender, CharacterBody body, ref float verticalBonus);
        public static event OnJumpHandler OnJumpEvent;
        private static void CharacterMain_JumpVelocity(On.EntityStates.GenericCharacterMain.orig_ApplyJumpVelocity orig,
            CharacterMotor characterMotor, CharacterBody characterBody, float horizontalBonus, float verticalBonus, bool vault)
        {
            //OnJumpEvent?.Invoke(characterMotor, ref verticalBonus);
            OnJumpEvent?.Invoke(characterMotor, characterBody, ref verticalBonus);
            orig(characterMotor, characterBody, horizontalBonus, verticalBonus, vault);
        }
    }
}
