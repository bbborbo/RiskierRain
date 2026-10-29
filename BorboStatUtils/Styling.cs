using System;
using System.Collections.Generic;
using System.Text;

namespace RainrotSharedUtils
{
    public static class Styling
    {
        public static string AsPercent(this float d)
        {
            return (d * 100f).ToString() + "%";
        }
        public static string ConvertDecimal(float value)
        {
            return (value * 100).ToString() + "%";
        }
        public static string DamageColor(string text)
        {
            return $"<style=cIsDamage>{text}</style>";
        }
        public static string HealingColor(string text)
        {
            return $"<style=cIsHealing>{text}</style>";
        }
        public static string DamageValueText(float value)
        {
            return DamageColor(value.AsPercent() + " damage");
        }
        public static string UtilityColor(string text)
        {
            return $"<style=cIsUtility>{text}</style>";
        }
        public static string RedText(string text) => HealthColor(text);
        public static string HealthColor(string text)
        {
            return $"<style=cIsHealth>{text}</style>";
        }
        public static string HexTierColor(string text)
        {
            return $"<style=cDeath>{text}</style>";
        }
        public static string KeywordText(string keyword, string sub)
        {
            return $"<style=cKeywordName>{keyword}</style><style=cSub>{sub}</style>";
        }
        public static string ScepterDescription(string desc)
        {
            return $"\n<color=#d299ff>SCEPTER: {desc}</color>";
        }
        public static string VoidColor(string text)
        {
            return $"<style=cIsVoid>{text}</style>";
        }
        public static string StackText(string text)
        {
            return StackColor($"({text} per stack)");
        }
        public static string StackColor(string text)
        {
            return $"<style=cStack>{text}</style>";
        }

        public static string GetAchievementNameToken(string identifier)
        {
            return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_NAME";
        }
        public static string GetAchievementDescriptionToken(string identifier)
        {
            return $"ACHIEVEMENT_{identifier.ToUpperInvariant()}_DESCRIPTION";
        }

        public static string NumToAdj(int num)
        {
            switch (num)
            {
                default:
                    return num + "th";
                case 1:
                    return num + "st";
                case 2:
                    return num + "nd";
                case 3:
                    return num + "rd";
            }
        }
    }
}
