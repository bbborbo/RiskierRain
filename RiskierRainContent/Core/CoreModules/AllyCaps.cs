using RoR2;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace RiskierRain.CoreModules
{
    //ill be real i basically stole this from chaotic skills 
    class AllyCaps : CoreModule
    {
        public override void Init()
        {
            On.RoR2.CharacterBody.Start += HopooWhy;
        }
        public struct AllyCap
        {
            public GameObject prefab;
            public int cap;
            public int lysateCap;
        }

        private static List<AllyCap> caps;

        public static void RegisterAllyCap(GameObject prefab, int max = 1, int maxWithLysate = 2)
        {
            if (caps == null)
            {
                caps = new List<AllyCap>();
            }

            caps.Add(new AllyCap
            {
                prefab = prefab,
                cap = max,
                lysateCap = maxWithLysate
            });
        }

        private static void HopooWhy(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (NetworkServer.active)
            {
                if (caps.Count > 0 && caps != null)
                {
                    foreach (AllyCap cap in caps)
                    {
                        if (self.master?.minionOwnership?.ownerMaster)
                        {
                            BodyIndex index = BodyCatalog.FindBodyIndex(cap.prefab);
                            if (self.bodyIndex == index)
                            {
                                MinionOwnership[] minions = GameObject.FindObjectsOfType<MinionOwnership>().Where(
                                    x => x.ownerMaster && x.ownerMaster == self.master.minionOwnership.ownerMaster && x.GetComponent<CharacterMaster>()
                                    && x.GetComponent<CharacterMaster>().GetBody() && x.GetComponent<CharacterMaster>().GetBody().bodyIndex == index
                                ).ToArray();

                                int total = 0;
                                bool hasLysate = self.master.minionOwnership.ownerMaster.inventory.GetItemCount(DLC1Content.Items.EquipmentMagazineVoid) > 1;
                                int max = hasLysate ? cap.lysateCap : cap.cap;

                                foreach (MinionOwnership minion in minions)
                                {
                                    total += 1;
                                    if (total > max) minion.GetComponent<CharacterMaster>().TrueKill();
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
}
