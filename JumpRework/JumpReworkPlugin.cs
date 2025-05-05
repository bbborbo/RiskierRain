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
using MoreStats;

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
        public const string version = "1.3.1";
        #endregion
        #region config
        internal static ConfigFile CustomConfigFile { get; private set; }
        public static ConfigEntry<bool> NerfDoubleJumps { get; set; }
        public static ConfigEntry<bool> ReworkFeather { get; set; }
        public static ConfigEntry<bool> ReworkHeadstomper { get; set; }
        public static ConfigEntry<bool> ReworkUrn { get; set; }

        public static ConfigEntry<float> DoubleJumpVBonus { get; set; }
        public static ConfigEntry<float> DoubleJumpHBonus { get; set; }
        public static ConfigEntry<int> FeatherJumpCount { get; set; }
        public static ConfigEntry<int> UrnJumpCount { get; set; }
        public static ConfigEntry<int> HeadstomperJumpCount { get; set; }

        public static ConfigEntry<bool> HeadstomperBoostLast { get; set; }
        public static ConfigEntry<float> HeadstomperBoostStrengthFirst { get; set; }
        public static ConfigEntry<float> HeadstomperBoostStrengthLast { get; set; }

        public static ConfigEntry<float> UrnBallChance { get; set; }
        public static ConfigEntry<float> UrnBallDamageCoefficient { get; set; }
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
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + "\\FruityJumps.cfg", true);

            #region config reworks
            NerfDoubleJumps = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Nerf Double Jump Strength",
                true,
                ""
                );
            ReworkFeather = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Hopoo Feather",
                true,
                ""
                );
            ReworkHeadstomper = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Hopoo Feather",
                true,
                ""
                );
            ReworkUrn = CustomConfigFile.Bind<bool>(
                "Reworks",
                "Rework Hopoo Feather",
                true,
                ""
                );
            #endregion
            #region config jump strength
            DoubleJumpVBonus = CustomConfigFile.Bind<float>(
                "Jump Strength",
                "Double Jump Vertical Strength",
                0.8f,
                "Vertical strength bonus of double jumps. Vanilla base jumps are 1, double jumps 1.5");
            DoubleJumpHBonus = CustomConfigFile.Bind<float>(
                "Jump Strength",
                "Double Jump Horizontal Strength",
                1.1f,
                "Horizontal strength bonus of double jumps. Vanilla base jumps are 1, double jumps 1.3");
            #endregion

            #region jump counts
            FeatherJumpCount = CustomConfigFile.Bind<int>(
                "Jump Counts",
                "Hopoo Feather Jump Count",
                1,
                "Only applies if its respective rework is enabled.");
            HeadstomperJumpCount = CustomConfigFile.Bind<int>(
                "Jump Counts",
                "Headstomper Jump Count",
                3,
                "Only applies if its respective rework is enabled.");
            UrnJumpCount = CustomConfigFile.Bind<int>(
                "Jump Counts",
                "Mired Urn Jump Count",
                2,
                "Only applies if its respective rework is enabled.");
            #endregion
            #region headstompers
            HeadstomperBoostLast = CustomConfigFile.Bind<bool>(
                "Headstompers",
                "Should Headstompers boost the last jump instead of the first?",
                true,
                ""
                );
            HeadstomperBoostStrengthFirst = CustomConfigFile.Bind<float>(
                "Headstompers",
                "Headstompers First Super Jump Strength",
                1.2f,
                "Only applies if the Super Jump is configured to be first. Vanilla is 2");
            HeadstomperBoostStrengthLast = CustomConfigFile.Bind<float>(
                "Headstompers",
                "Headstompers Final Super Jump Strength",
                2,
                "Only applies if the Super Jump is configured to be last");
            #endregion
            #region urn
            UrnBallChance = CustomConfigFile.Bind<float>(
                "Mired Urn",
                "Mired Urn Ball Chance",
                0.25f,
                "Stacks identically, approaches 100%");
            UrnBallDamageCoefficient = CustomConfigFile.Bind<float>(
                "Mired Urn",
                "Urn Ball Damage Coefficient",
                6.5f,
                "Multiply by 100 for %, ie 6.5 is 650%");
            #endregion

            CreateMiredUrnTarball();
            if(ReworkUrn.Value || ReworkHeadstomper.Value)
                GetMoreStatCoefficients += JumpCounts;

            if (NerfDoubleJumps.Value)
            {
                IL.EntityStates.GenericCharacterMain.ProcessJump_bool += DoubleJumpStrengthNerf;
            }
            if (ReworkFeather.Value)
            {
                BaseStats.FeatherJumpCountBase = FeatherJumpCount.Value;
                BaseStats.FeatherJumpCountStack = 0;
                FeatherRework();
            }
            if (ReworkHeadstomper.Value)
            {
                StompersRework();
            }
            if (ReworkUrn.Value)
            {
                MiredUrnRework();
            }
        }

        private void JumpCounts(CharacterBody sender, MoreStatHookEventArgs args)
        {
            Inventory inv = sender.inventory;
            if (inv)
            {
                if (inv.GetItemCount(RoR2Content.Items.SiphonOnLowHealth) > 0 && ReworkUrn.Value)
                {
                    args.jumpCountAdd += UrnJumpCount.Value;
                }
                if (inv.GetItemCount(RoR2Content.Items.FallBoots) > 0 && ReworkHeadstomper.Value)
                {
                    args.jumpCountAdd += HeadstomperJumpCount.Value;
                }
            }
        }
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
            c.Emit(OpCodes.Ldc_R4, DoubleJumpHBonus.Value);
            c.Index++;

            int verticalBoostLoc = 4;
            c.GotoNext(MoveType.Before,
                x => x.MatchLdcR4(out _),
                x => x.MatchStloc(out verticalBoostLoc)
                );
            c.Remove();
            c.Emit(OpCodes.Ldc_R4, DoubleJumpVBonus.Value);
        }
    }
}
