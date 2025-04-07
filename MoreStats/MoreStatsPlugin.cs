using BepInEx;
using System;
using System.Security;
using System.Security.Permissions;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: UnverifiableCode]
#pragma warning disable 
namespace MoreStats
{
    [BepInPlugin(guid, modName, version)]
    public class MoreStatsPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "MoreStats";
        public const string version = "1.2.4";
        #endregion

        public const float BaseShieldDelaySeconds = 7f;
        public const float MinShieldDelaySeconds = 1f;
        public const float MinBarrierDecayWithDynamicRate = 1f;

        void Awake()
        {
            StatHooks.Init();
            OnHit.Init();
            OnJump.Init();
        }
    }
}
