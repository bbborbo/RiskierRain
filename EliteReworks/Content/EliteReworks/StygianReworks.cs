using RoR2;
using RoR2BepInExPack.Utilities;
using System;
using System.Collections.Generic;
using System.Text;

namespace FruityElites.EliteReworks
{
    public class StygianReworks : EliteReworkBase<StygianReworks>
    {
        public static FixedConditionalWeakTable<MinionOwnership, HealthComponent> stygianMinionHealthComponents = new FixedConditionalWeakTable<MinionOwnership, HealthComponent>();
        public static HealthComponent GetStygianMinionHealthComponent(MinionOwnership body)
        {
            if (body == null)
                return null;
            return stygianMinionHealthComponents.GetOrCreateValue(body);
        }
        public override string eliteName => "Stygian";

        public override void Init()
        {
            base.Init();
            On.RoR2.CharacterBody.OnTakeDamageServer += StygianOnTakeDamage;
        }

        private void StygianOnTakeDamage(On.RoR2.CharacterBody.orig_OnTakeDamageServer orig, RoR2.CharacterBody self, RoR2.DamageReport damageReport)
        {
            orig(self, damageReport);
            if (self.HasBuff(DLC4Content.Buffs.EliteDeath) && self.master != null)
            {
                if(MinionOwnership.MinionGroup.TryFindGroup(self.master.netId, out MinionOwnership.MinionGroup group) && group.memberCount > 0)
                {
                    foreach(MinionOwnership member in group.members)
                    {
                        HealthComponent healthComponent = GetStygianMinionHealthComponent(member);
                        if(healthComponent == null && member.TryGetComponent(out CharacterMaster master))
                        {
                            CharacterBody minionBody = master.GetBody();
                            if(minionBody && minionBody.healthComponent.alive)
                            {
                                healthComponent = minionBody.healthComponent;
                                if (stygianMinionHealthComponents.ContainsKey(member) == false)
                                    stygianMinionHealthComponents.Add(member, healthComponent);
                                else
                                    stygianMinionHealthComponents[member] = healthComponent;
                            }
                        }
                        if (healthComponent == null)
                            continue;
                        IOnTakeDamageServerReceiver[] array3 = healthComponent.onTakeDamageReceivers;
                        for (int i = 0; i < array3.Length; i++)
                        {
                            array3[i].OnTakeDamageServer(damageReport);
                        }
                    }
                }
            }
        }
    }
}
