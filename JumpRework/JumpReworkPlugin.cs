using BepInEx;
using BepInEx.Configuration;
using MissileRework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace JumpRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition))]
    public partial class JumpReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityJumps";
        public const string version = "1.0.5";
        #endregion
        public static bool IsMissileArtifactEnabled()
        {
            if (Tools.isLoaded(MissileReworkPlugin.guid))
            {
                return GetMissileArtifactEnabled();
            }
            return false;
        }

        [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
        private static bool GetMissileArtifactEnabled()
        {
            return RunArtifactManager.instance.IsArtifactEnabled(MissileReworkPlugin.MissileArtifact);
        }


        public void Awake()
        {
            CreateMiredUrnTarball();

            RoR2Application.onLoad += JumpReworks;
        }
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


        void JumpReworks()
        {
            IL.RoR2.CharacterBody.RecalculateStats += JumpReworkJumpCount;
            On.EntityStates.GenericCharacterMain.ApplyJumpVelocity += DoJumpEvent;
            IL.RoR2.CharacterMotor.PreMove += DynamicJump;
            IL.EntityStates.GenericCharacterMain.ProcessJump += FeatherNerf;

            FeatherRework();
            StompersRework();
            MiredUrnRework();
        }

        public static float doubleJumpVerticalBonus = 1.0f; //1.5f
        public static float doubleJumpHorizontalBonus = 1.1f; //1.3f; //1.5f
        private void FeatherNerf(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<EntityStates.EntityState>("get_characterBody"),
                x => x.MatchLdfld<CharacterBody>("baseJumpCount")
                );

            int horizontalBoostLoc = 3;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out horizontalBoostLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, doubleJumpHorizontalBonus);
            c.Index++;

            int verticalBoostLoc = 4;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out verticalBoostLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, doubleJumpVerticalBonus);
        }


        public static float dynamicJumpAscentHoldGravity = 0.8f; //1f
        public static float dynamicJumpAscentReleaseGravity = 1.3f; //1f
        public static float dynamicJumpDescentGravity = 1f; //1f
        private void DynamicJump(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<UnityEngine.Physics>("get_gravity"),
                x => x.MatchLdfld<Vector3>("y")
                );

            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<float, CharacterMotor, float>>((gravityIn, motor) =>
            {
                float gravityOut = gravityIn;

                if (!motor.disableAirControlUntilCollision)
                {
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
                }

                return gravityOut;
            });
        }

        private void JumpReworkJumpCount(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int featherCountLoc = 0;
            c.GotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", "Feather"),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCount)),
                x => x.MatchStloc(out featherCountLoc)
                );

            c.GotoNext(MoveType.After,
                x => x.MatchLdfld<CharacterBody>(nameof(CharacterBody.baseJumpCount)),
                x => x.MatchLdloc(featherCountLoc)
                );
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<int, CharacterBody, int>>((featherCount, self) =>
            {
                int jumpCount = 0;
                Inventory inv = self.inventory;
                if (inv != null)
                {
                    if (featherCount > 0)
                    {
                        jumpCount += featherJumpCount;
                    }
                    if (inv.GetItemCount(RoR2Content.Items.SiphonOnLowHealth) > 0)
                    {
                        jumpCount += urnJumpCount;
                    }
                    if (inv.GetItemCount(RoR2Content.Items.FallBoots) > 0)
                    {
                        jumpCount += fallBootsJumpCount;
                    }
                    jumpCount += JumpStatHook.InvokeStatHook(self);
                }

                return jumpCount;
            });
        }

        private void DoJumpEvent(On.EntityStates.GenericCharacterMain.orig_ApplyJumpVelocity orig,
            CharacterMotor characterMotor, CharacterBody characterBody, float horizontalBonus, float verticalBonus, bool vault)
        {
            //OnJumpEvent?.Invoke(characterMotor, ref verticalBonus);
            JumpStatHook.InvokeJumpHook(characterMotor, ref verticalBonus);
            orig(characterMotor, characterBody, horizontalBonus, verticalBonus, vault);
        }
    }

    public class JumpStatHook
    {
        public delegate void JumpStatHandler(CharacterBody sender, ref int jumpCount);
        public static event JumpStatHandler JumpStatCoefficient;

        public static int InvokeStatHook(CharacterBody self)
        {
            int jumpCount = 0;
            JumpStatCoefficient?.Invoke(self, ref jumpCount);
            return jumpCount;
        }

        public delegate void OnJumpHandler(CharacterMotor sender, ref float verticalBonus);
        public static event OnJumpHandler OnJumpEvent;
        public static float InvokeJumpHook(CharacterMotor self, ref float verticalBonus)
        {
            OnJumpEvent?.Invoke(self, ref verticalBonus);
            return verticalBonus;
        }
    }
}
