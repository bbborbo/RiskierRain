using BepInEx;
using BepInEx.Configuration;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using R2API;
using RoR2;
using System;
using UnityEngine;

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
        public const string version = "1.0.2";
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
        }

        public static void AddSoulCostToBody(CharacterBody body, float soulCost)
        {
            AddSoulCostToBody(body, DLC2Content.Buffs.SoulCost.buffIndex, soulCost);
        }

        public static void AddSoulCostToBody(CharacterBody body, BuffIndex buffIndex, float soulCost)
        {
            if (!DoCradleSoulCost.Value)
            {
                int num = Mathf.CeilToInt(soulCost * 10);
                for(int i = 0; i < num; i++)
                {
                    body.AddBuff((BuffIndex)buffIndex);
                }
                return;
            }
            float oneMinus = 1 - soulCost;
            int currentBuffCount = body.GetBuffCount((BuffIndex)buffIndex);
            float currentHealthFraction = 1 / (1 + 0.1f * currentBuffCount);
            float idealHealthFraction = currentHealthFraction * oneMinus;

            int nextBuffCount = currentBuffCount;
            float nextMaxHealthFraction = 1;
            while (nextMaxHealthFraction > idealHealthFraction)
            {
                body.AddBuff((BuffIndex)buffIndex);
                nextBuffCount++;
                nextMaxHealthFraction = 1 / (1 + 0.1f * nextBuffCount);
            }
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
