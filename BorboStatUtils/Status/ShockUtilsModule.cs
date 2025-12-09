using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using R2API;

namespace RainrotSharedUtils.Status
{
    public static class ShockUtilsModule
    {
        public static float shockForceExitFraction = 0.10f;
        static bool _UseShockSparks = false;
        public static bool UseShockSparks
        {
            get
            {
                return _UseShockSparks;
            }
            set
            {
                if(value == true)
                {
                    LanguageAPI.Add("KEYWORD_SHOCKING",
                        $"<style=cKeywordName>Shocking</style>" +
                        $"<style=cSub>Interrupts enemies and stuns them. " +
                        $"The stun is broken if the target takes more than " +
                        $"<style=cIsHealth>{shockForceExitFraction * 100}%</style> " +
                        $"of their maximum health in damage. " +
                        $"Breaking shock creates <style=cIsUtility>Energizing Sparks</style>.</style>");
                }
                _UseShockSparks = true;
            }
        }

        public static void Init()
        {
            On.RoR2.Skills.SkillCatalog.Init += OnSkillCatalogInit;
        }

        private static void OnSkillCatalogInit(On.RoR2.Skills.SkillCatalog.orig_Init orig)
        {
            orig();
            if (UseShockSparks)
            {
                foreach (SkillDef skill in SkillCatalog.allSkillDefs)
                {
                    if (skill.keywordTokens.Contains("KEYWORD_SHOCKING"))
                    {
                        string s = SharedUtilsPlugin.sparkPickupKeywordToken;
                        HGArrayUtilities.ArrayAppend(ref skill.keywordTokens, ref s);
                    }
                }
            }
        }
    }
}
