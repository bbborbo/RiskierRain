using BepInEx;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace DynamicJump
{
    [BepInPlugin(guid, modName, version)]
    public class DynamicJumpPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "DynamicJump";
        public const string version = "1.0.1";
        #endregion

        void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);
            DynamicJumpAscentHoldGravity = CustomConfigFile.Bind<float>("Dynamic Jump", "Ascent Hold Gravity", 0.8f, 
                "The gravity multiplier applied to to the character while the jump input is HELD, during their ASCENT. Lower numbers make the character go higher.");
            DynamicJumpAscentReleaseGravity = CustomConfigFile.Bind<float>("Dynamic Jump", "Ascent Release Gravity", 1.5f, 
                "The gravity multiplier applied to to the character while the jump input is RELEASED, during their ASCENT. Higher numbers make the character go lower.");
            DynamicJumpDescentGravity = CustomConfigFile.Bind<float>("Dynamic Jump", "Descent Gravity", 1.0f, 
                "The gravity multiplier applied to to the character during their DESCENT. There's probably no reason to modify this value.");

            IL.RoR2.CharacterMotor.PreMove += DynamicJumpHook;
        }

        public static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<float> DynamicJumpAscentHoldGravity;
        public static ConfigEntry<float> DynamicJumpAscentReleaseGravity;
        public static ConfigEntry<float> DynamicJumpDescentGravity;
        public static float dynamicJumpAscentHoldGravity => DynamicJumpAscentHoldGravity.Value;// 0.8f; //1f
        public static float dynamicJumpAscentReleaseGravity => DynamicJumpAscentReleaseGravity.Value;// 1.3f; //1f
        public static float dynamicJumpDescentGravity => DynamicJumpDescentGravity.Value;// 1f; //1f
        private void DynamicJumpHook(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<UnityEngine.Physics>("get_gravity"),
                x => x.MatchLdfld<Vector3>("y")
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, CharacterMotor, float>>((gravityIn, motor) =>
            {
                if(motor?.body?.isPlayerControlled == false || motor.disableAirControlUntilCollision)
                {
                    return gravityIn;
                }

                float gravityOut = gravityIn;

                if (motor.velocity.y >= 0)
                {
                    if (motor.body.inputBank.jump.down)
                    {
                        gravityOut *= dynamicJumpAscentHoldGravity;
                    }
                    else
                    {
                        gravityOut *= dynamicJumpAscentReleaseGravity;
                    }
                }
                else
                {
                    gravityOut *= dynamicJumpDescentGravity;
                }

                return gravityOut;
            });
        }
    }
}
