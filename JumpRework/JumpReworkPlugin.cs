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
using static MoreStats.StatHooks;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable
namespace JumpRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MoreStats.MoreStatsPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(MissileRework.MissileReworkPlugin.guid, BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency(DynamicJump.DynamicJumpPlugin.guid, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition))]
    public partial class JumpReworkPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "FruityJumps";
        public const string version = "1.2.0";
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
        void JumpReworks()
        {
            GetMoreStatCoefficients += JumpCounts;
            IL.EntityStates.GenericCharacterMain.ProcessJump += DoubleJumpStrengthNerf;

            FeatherRework();
            StompersRework();
            MiredUrnRework();
        }

        private void JumpCounts(CharacterBody sender, MoreStatHookEventArgs args)
        {
            args.featherJumpCountBase = featherJumpCount;
            args.featherJumpCountStack = 0;
            Inventory inv = sender.inventory;
            if (inv)
            {
                if (inv.GetItemCount(RoR2Content.Items.SiphonOnLowHealth) > 0)
                {
                    args.jumpCountAdd += urnJumpCount;
                }
                if (inv.GetItemCount(RoR2Content.Items.FallBoots) > 0)
                {
                    args.jumpCountAdd += fallBootsJumpCount;
                }
            }
        }

        public static float doubleJumpVerticalBonus = 1.0f; //1.5f
        public static float doubleJumpHorizontalBonus = 1.1f; //1.3f; //1.5f
        private void DoubleJumpStrengthNerf(ILContext il)
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
    }
}
