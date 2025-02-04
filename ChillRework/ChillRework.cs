using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security.Permissions;
using UnityEngine;
using UnityEngine.AddressableAssets;
using static R2API.DamageAPI;

#pragma warning disable CS0618 // Type or member is obsolete
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618 // Type or member is obsolete
[module: System.Security.UnverifiableCode]
#pragma warning disable 
namespace ChillRework
{
    [BepInDependency(R2API.LanguageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.ContentManagement.R2APIContentManager.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]
    [BepInDependency(R2API.DamageAPI.PluginGUID, BepInDependency.DependencyFlags.HardDependency)]

    [BepInPlugin(guid, modName, version)]
    [R2APISubmoduleDependency(nameof(LanguageAPI), nameof(ContentAddition), nameof(DamageAPI), nameof(RecalculateStatsAPI))]
    public partial class ChillRework : BaseUnityPlugin
    {
        #region plugin info
        public static PluginInfo PInfo { get; private set; }
        public const string guid = "com." + teamName + "." + modName;
        public const string teamName = "RiskOfBrainrot";
        public const string modName = "ChillRework";
        public const string version = "1.3.4";
        #endregion

        public static BuffDef ChillBuff;
        public static BuffDef ChillLimitBuff;
        public static ModdedDamageType ChillOnHit;
        public static ModdedDamageType MultiChillOnHit;
        public const string chillKeywordToken = "2R4R_KEYWORD_CHILL";
        public const int chillStacksMax = 10;
        public const int chillStacksOnFreeze = 3;
        public const float chillProcDuration = 8f;
        public const int chillProcChance = 100;
        public void Awake()
        {
            Debug.Log("Chill Rework initializing!");
            CreateIceNovaAssets();
            LangFixes();
            FixSnapfreeze();
            ReworkChill();
        }

        public void ReworkChill()
        {
            ChillBuff = Addressables.LoadAssetAsync<BuffDef>("RoR2/Base/Common/bdSlow80.asset").WaitForCompletion();
            ChillBuff.canStack = true;

            ChillLimitBuff = ScriptableObject.CreateInstance<BuffDef>();
            ChillLimitBuff.canStack = true;
            ChillLimitBuff.isHidden = true;
            ChillLimitBuff.isCooldown = true;
            ChillLimitBuff.name = "bdMaxChillRestriction";

            ChillOnHit = ReserveDamageType();
            MultiChillOnHit = ReserveDamageType();

            ChillHooks();
        }
        public void LangFixes()
        {
            //keywords
            LanguageAPI.Add(chillKeywordToken, "<style=cKeywordName>Chilling</style>" +
                $"<style=cSub>Has a chance to temporarily <style=cIsUtility>slow enemy speed</style> by <style=cIsDamage>80%.</style></style>");
            LanguageAPI.Add("KEYWORD_FREEZING",
                "<style=cKeywordName>Freezing</style>" +
                "<style=cSub>Freeze enemies in place and <style=cIsUtility>Chill</style> them, slowing them by 80% after they thaw. " +
                "Frozen enemies are <style=cIsHealth>instantly killed</style> if below <style=cIsHealth>30%</style> health.");

            //items
            LanguageAPI.Add("ITEM_ICERING_DESC",
                $"Hits that deal <style=cIsDamage>more than 400% damage</style> also blasts enemies with a " +
                $"<style=cIsDamage>runic ice blast</style>, " +
                $"<style=cIsUtility>Chilling</style> them for <style=cIsUtility>3s</style> <style=cStack>(+3s per stack)</style> and " +
                $"dealing <style=cIsDamage>250%</style> <style=cStack>(+250% per stack)</style> TOTAL damage. " +
                $"Recharges every <style=cIsUtility>10</style> seconds.");
            LanguageAPI.Add("ITEM_ICICLE_DESC",
                   "Killing an enemy surrounds you with an <style=cIsDamage>ice storm</style> " +
                   "that deals <style=cIsDamage>600% damage per second</style> and " +
                   "<style=cIsUtility>Chills</style> enemies for <style=cIsUtility>1.5s</style>. " +
                   "The storm <style=cIsDamage>grows with every kill</style>, " +
                   "increasing its radius by <style=cIsDamage>1m</style>. " +
                   "Stacks up to <style=cIsDamage>6m</style> <style=cStack>(+6m per stack)</style>.");

            //skills
            LanguageAPI.Add("MAGE_UTILITY_ICE_DESCRIPTION",
                "<style=cIsUtility>Freezing</style>. Create a barrier that hurts enemies for " +
                "up to <style=cIsDamage>12x100% damage</style>.");
        }

        #region interface
        public static void ApplyChillStacks(CharacterMaster attackerMaster, CharacterBody vBody, float procChance, float chillCount = 1, float chillDuration = chillProcDuration)
        {
            ApplyChillStacks(vBody, procChance, chillCount, chillDuration, attackerMaster ? attackerMaster.luck : 1);
        }
        public static void ApplyChillStacks(CharacterBody vBody, float procChance, float totalChillToApply = 1, float chillDuration = chillProcDuration, float luck = 1)
        {
            //the current chill stack on the victim
            int chillCount = vBody.GetBuffCount(ChillBuff);

            int chillLimitCount = vBody.GetBuffCount(ChillLimitBuff);
            //the cap on chill stacks, 10 by default but reduced by 3 per chill limit on the victim
            int chillCap = 10 - 3 * chillLimitCount;
            //if the current chill stacks is more than the cap, dont worry about applying more
            if (chillCount > chillCap)
                return;

            //cap the chill stacks applied to the difference between chill cap and chill count
            totalChillToApply = Mathf.Min(totalChillToApply, chillCap - chillCount);
            for (int i = 0; i < totalChillToApply; i++)
            {
                if (Util.CheckRoll(procChance, luck))
                {
                    vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, chillDuration);
                }
            }

            //i made this super unreadable because its funny
            /*if (chillCount <= 0)
                return;
            if (Util.CheckRoll(procChance, attackerMaster))
            {
                vBody.AddTimedBuffAuthority(RoR2Content.Buffs.Slow80.buffIndex, chillDuration);
            }
            ApplyChillStacks(attackerMaster, vBody, procChance, chillCount--, chillDuration);*/
        }

        public static void ApplyChillSphere(Vector3 origin, float radius, TeamIndex teamIndex, float duration = chillProcDuration, float chillCount = 3)
        {
            SphereSearch chillSphere = new SphereSearch();
            chillSphere.origin = origin;
            chillSphere.mask = LayerIndex.entityPrecise.mask;
            chillSphere.radius = radius;
            chillSphere.RefreshCandidates();
            chillSphere.FilterCandidatesByHurtBoxTeam(TeamMask.GetUnprotectedTeams(teamIndex));
            chillSphere.FilterCandidatesByDistinctHurtBoxEntities();
            chillSphere.OrderCandidatesByDistance();
            List<HurtBox> hurtboxBuffer = new List<HurtBox>();
            chillSphere.GetHurtBoxes(hurtboxBuffer);
            chillSphere.ClearCandidates();

            for (int i = 0; i < hurtboxBuffer.Count; i++)
            {
                HurtBox hurtBox = hurtboxBuffer[i];
                CharacterBody vBody = hurtBox.healthComponent?.body;
                if (vBody)
                {
                    ApplyChillStacks(vBody, 100, chillCount, duration);
                }
            }
            hurtboxBuffer.Clear();
        }

        public static float CalculateChillSlowCoefficient(int chillStacks, float baseSlowCoefficient = 0.8f)
        {
            chillStacks = Mathf.Min(chillStacks, chillStacksMax);
            return Mathf.Lerp(0, baseSlowCoefficient * 2, chillStacks / chillStacksMax);
        }
        #endregion
    }
}
