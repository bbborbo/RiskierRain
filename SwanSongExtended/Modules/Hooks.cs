using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using static MoreStats.OnHit;
using SwanSongExtended.Items;

namespace SwanSongExtended.Modules
{
    public static class Hooks
    {
        public static void Init()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += SquidOnDeath;

            On.RoR2.CharacterBody.OnSkillActivated += SnailyOnSkill;
        }

        private static void SnailyOnSkill(On.RoR2.CharacterBody.orig_OnSkillActivated orig, CharacterBody self, GenericSkill skill)
        {
            orig(self, skill);

            if (skill != self.skillLocator.primary)
                return;
            int count = 0;
            if(RainbowWave.instance.isEnabled && self.HasBuff(RainbowWave.rainbowBuff))
            {
                count = RainbowWave.instance.GetCount(self);
                RainbowWave.FireRainbowWave(self, count - 1);
            }
            else if(Boomerang.instance.isEnabled && self.HasBuff(Boomerang.boomerangBuff))
            {
                count = Boomerang.instance.GetCount(self);
                Boomerang.FireBoomerang(self, count - 1);
            }
            else if(Peashooter.instance.isEnabled && (count = Peashooter.instance.GetCount(self)) > 0)
            {
                Peashooter.FirePeashooter(self, count - 1);
            }
        }

        #region squid hooks

        private static void SquidOnDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            orig(self, damageReport);
            int squidCount = CountSquids(damageReport.victimBody);
            if (squidCount == 0) { return; }
            SquidDeathBlast(damageReport.victimBody, squidCount);
        }

        private static void SquidDeathBlast(CharacterBody body, int squidCount)
        {
            //temp location. put somewhere good later! when you do that you can also 
            float radiusBase = 28f;
            //float durationBase = 12f;
            //EffectManager.SpawnEffect(scugNovaEffectPrefab, new EffectData
            //{
            //    origin = body.transform.position,
            //    scale = radiusBase
            //}, true);
            //ChillRework.ChillRework.ApplyChillSphere(body.corePosition, radiusBase, body.teamComponent.teamIndex, durationBase);
            BlastAttack squidNova = new BlastAttack()
            {
                baseDamage = (4f + 1f * (squidCount -1)) * body.damage, //400% to trigger those effects? i think???
                radius = radiusBase,
                procCoefficient = 1f,
                position = body.transform.position,
                attacker = body.gameObject,
                baseForce = 900,
                crit = Util.CheckRoll(body.crit, body.master),
                falloffModel = BlastAttack.FalloffModel.None,
                damageType = DamageType.ClayGoo,
                teamIndex = TeamComponent.GetObjectTeam(body.gameObject)
            };
            squidNova.Fire();
        }

        private static int CountSquids(CharacterBody body)
        {
            if (!body) { return 0; }
            TeamIndex team = body.teamComponent.teamIndex;
            int num = 0;//number of squid items on at least one time. idk man helpp
            using (IEnumerator<CharacterMaster> enumerator = CharacterMaster.readOnlyInstancesList.GetEnumerator())//gets each character on the (a?) team and checks each ones inventory
            {
                while (enumerator.MoveNext())
                {
                    int itemCount = enumerator.Current.inventory.GetItemCount(RoR2Content.Items.Squid);
                    if (itemCount > 0 && enumerator.Current.teamIndex == team)
                    {
                        num += itemCount;
                    }
                }
            }
            return num;
        }
        #endregion
    }

}
