using BepInEx;
using BepInEx.Configuration;
using MonoMod.RuntimeDetour;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.CharacterAI;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable 
namespace Ror2AggroTools
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI), nameof(RecalculateStatsAPI))]
    public partial class AggroToolsPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "Ror2AggroTools";
        public const string version = "1.0.0";
        #endregion

        public static BuffDef priorityAggro;
        public static ModdedDamageType AggroOnHit;

        public void Awake()
        {
            PInfo = Info;

            AggroOnHit = ReserveDamageType();

            priorityAggro = ScriptableObject.CreateInstance<BuffDef>();
            priorityAggro.canStack = false;
            priorityAggro.isHidden = true;
            priorityAggro.isDebuff = false;

            On.RoR2.GlobalEventManager.OnHitEnemy += AggroOnHitHook;
        }

        private void AggroOnHitHook(On.RoR2.GlobalEventManager.orig_OnHitEnemy orig, GlobalEventManager self, DamageInfo damageInfo, GameObject victim)
        {
            CharacterMaster attackerMaster = null;
            if (damageInfo.attacker != null)
            {
                CharacterBody aBody = damageInfo.attacker.GetComponent<CharacterBody>();
                if (aBody != null)
                {
                    attackerMaster = aBody.master;
                }
            }

            if (victim != null)
            {
                CharacterBody vBody = victim?.GetComponent<CharacterBody>();
                if (vBody != null)
                {
                    float procCoefficient = damageInfo.procCoefficient;
                    if (procCoefficient != 0 && !damageInfo.rejected)
                    {
                        if (damageInfo.HasModdedDamageType(AggroOnHit))
                        {
                            damageInfo.RemoveModdedDamageType(AggroOnHit);
                            Aggro.AggroMinionsToEnemy(attackerMaster, vBody);
                        }
                    }
                }
            }

            orig(self, damageInfo, victim);
        }
    }
}
