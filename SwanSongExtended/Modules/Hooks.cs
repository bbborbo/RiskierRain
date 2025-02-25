using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SwanSongExtended.Modules
{
    public static class Hooks
    {
        public static void Init()
        {

        }
    }
    public static class SquidHooks
    {
        public static void Init()
        {
            On.RoR2.GlobalEventManager.OnCharacterDeath += SquidOnDeath;
        }

        private static void SquidOnDeath(On.RoR2.GlobalEventManager.orig_OnCharacterDeath orig, GlobalEventManager self, DamageReport damageReport)
        {
            int squidCount = CountSquids(damageReport.victimBody);
            if (squidCount <= 0) { return; }
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
                baseDamage = (4f + 1f * squidCount --)* body.damage, //400% to trigger those effects? i think???
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
                    if (itemCount > 0)
                    {
                        num += itemCount;
                    }
                }
            }

            return num;
        }
    }
}
