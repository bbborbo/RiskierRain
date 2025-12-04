using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using System;
using UnityEngine;
using UnityEngine.Networking;

namespace BetterSoulCost
{

    [BepInPlugin(guid, modName, version)]
    //[R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI))]
    public class SoulCostPlugin : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }

        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "BetterSoulCost";
        public const string version = "1.0.6";
        #endregion
        #region config
        internal static ConfigFile CustomConfigFile { get; set; }
        public static ConfigEntry<bool> DoCradleSoulCost { get; set; }
        #endregion

        void Awake()
        {
            CustomConfigFile = new ConfigFile(Paths.ConfigPath + $"\\{modName}.cfg", true);

            DoCradleSoulCost = CustomConfigFile.Bind<bool>(modName + ": Reworks", "Change Soul Cost Stacking", true,
                "If true, soul penalties will increase exponentially to approximate consistent health loss, rather than hyperbolically.");
            RoR2Application.onLoad += FixSoulPayCost;
            IL.RoR2.ShrineColossusAccessBehavior.OnInteraction += ShapingShrineSoulSpread;
        }

        private void ShapingShrineSoulSpread(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            c.GotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.SetBuffCount))
                );
            c.Remove();
            c.EmitDelegate<Action<CharacterBody, int, int>>((body, buffIndex, buffCount) => AddSoulCostToBody(body, (BuffIndex)buffIndex, (int)buffCount * 0.1f));
        }

        public static void AddSoulCostToBody(CharacterBody body, float soulCost)
        {
            AddSoulCostToBody(body, DLC2Content.Buffs.SoulCost.buffIndex, soulCost);
        }

        public static void AddSoulCostToBody(CharacterBody body, BuffIndex buffIndex, float soulCost)
        {
            if (!NetworkServer.active)
                return;
            soulCost = Mathf.Min(soulCost, 0.99f);
            int currentBuffCount = body.GetBuffCount((BuffIndex)buffIndex);
            float buffsToAdd = soulCost * 10;
            if (DoCradleSoulCost.Value)
            {
                float currentHealthFraction = 1;
                if(currentBuffCount > 0)
                    currentHealthFraction = 1 / (1 + 0.1f * currentBuffCount); //10 stacks = 0.5
                //float oneMinus = 1 - soulCost;
                //float idealHealthFraction = currentHealthFraction * oneMinus;
                float conversion = (buffsToAdd * buffsToAdd) / (currentHealthFraction * (10 - buffsToAdd));
                buffsToAdd += conversion;
            }
            body.SetBuffCount((BuffIndex)buffIndex, currentBuffCount + Mathf.CeilToInt(buffsToAdd));
        }

        #region fixes
        [SystemInitializer(typeof(CostTypeCatalog))]
        private void FixSoulPayCost()
        {
            CostTypeDef ctd = CostTypeCatalog.GetCostTypeDef(CostTypeIndex.SoulCost);
            var method = ctd.payCost.Method;
            ILHook hook = new ILHook(method, FixSoulCost);
        }

        private void FixSoulCost(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.Before,
                x => x.MatchCallOrCallvirt<CharacterBody>(nameof(CharacterBody.SetBuffCount))
                );
            if (b)
            {
                c.Remove();
                c.EmitDelegate<Action<CharacterBody, int, int>>((body, buffIndex, buffCount) =>
                {
                    if (buffCount > 0)
                    {
                        //for (int i = 0; i < buffCount; i++)
                        //{
                        //    body.AddBuff((BuffIndex)buffIndex);
                        //}
                        int buffsToAdd = buffCount;

                        float curseAmt = buffCount * 0.1f;
                        AddSoulCostToBody(body, (BuffIndex)buffIndex, curseAmt);
                    }
                });
            }
            else
            {
                Debug.LogError("Could not hook void cradle paycost");
            }
        }
        #endregion
    }
}
