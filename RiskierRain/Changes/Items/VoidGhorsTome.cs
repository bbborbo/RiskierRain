using BepInEx.Configuration;
using HarmonyLib;
using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace RiskierRain.Items
{
    class VoidGhorsTome : ItemBase<VoidGhorsTome>
    {
		public static float codexTriggerChanceBase = 0.24f;
		public static float codexTriggerChanceStack = 0.12f;

		public static BasicPickupDropTable voidT2DropTable;
		public static BasicPickupDropTable voidT3DropTable;
		public static BasicPickupDropTable voidBossDropTable;
        public override string ItemName => "Quantum Codex";

        public override string ItemLangTokenName => "VOIDGHORS";

        public override string ItemPickupDesc => "Chance to <style=cIsVoid>evolve</style> your boss rewards into a rare void item. " +
            "<style=cIsVoid>Corrupts all Ghor\'s Tomes.</style>";

        public override string ItemFullDescription => $"<style=cIsUtility>{Tools.ConvertDecimal(codexTriggerChanceBase)}</style> " +
			$"<style=cStack>(+{Tools.ConvertDecimal(codexTriggerChanceStack)} per stack)</style> chance to " +
			$"<style=cIsVoid>evolve</style> your boss reward, turning it into a random <style=cIsDamage>rare void item</style>. " +
			$"<style=cIsVoid>Corrupts all Ghor\'s Tomes.</style>";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.VoidTier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Utility, ItemTag.AIBlacklist, ItemTag.BrotherBlacklist, ItemTag.HoldoutZoneRelated };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => LegacyResourcesAPI.Load<GameObject>("prefabs/NullModel");

        public override Sprite ItemIcon => LegacyResourcesAPI.Load<Sprite>("textures/miscicons/texWIPIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return null;
        }

        public override void Hooks()
        {
			On.RoR2.BossGroup.DropRewards += OverrideDropRewards;
            On.RoR2.Items.ContagiousItemManager.Init += CreateTransformation;
        }


        private void CreateTransformation(On.RoR2.Items.ContagiousItemManager.orig_Init orig)
        {
            ItemDef.Pair transformation = new ItemDef.Pair()
            {
                itemDef1 = RoR2Content.Items.TreasureCache, //consumes lepton daisy
                itemDef2 = VoidGhorsTome.instance.ItemsDef
            };
            ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem] = ItemCatalog.itemRelationships[DLC1Content.ItemRelationshipTypes.ContagiousItem].AddToArray(transformation);
            orig();
        }
        private void OverrideDropRewards(On.RoR2.BossGroup.orig_DropRewards orig, RoR2.BossGroup self)
		{
			if (!Run.instance)
			{
				Debug.LogError("No valid run instance!");
				return;
			}
			if (self.rng == null)
			{
				Debug.LogError("RNG is null!");
				return;
			}
			int participatingPlayerCount = Run.instance.participatingPlayerCount;
			if (participatingPlayerCount != 0)
			{
				if (self.dropPosition)
				{
					int[] codexCounts = new int[participatingPlayerCount];
					for(int n = 0; n < participatingPlayerCount; n++)
                    {
						PlayerCharacterMasterController pcmc = PlayerCharacterMasterController.instances[n];
						CharacterBody body = pcmc?.body;
                        if (body)
                        {
							codexCounts[n] = GetCount(body);
                        }
                    }

					PickupIndex pickupIndex = PickupIndex.none;
					if (self.dropTable)
					{
						pickupIndex = self.dropTable.GenerateDrop(self.rng);
					}
					else
					{
						List<PickupIndex> list = Run.instance.availableTier2DropList;
						if (self.forceTier3Reward)
						{
							list = Run.instance.availableTier3DropList;
						}
						pickupIndex = self.rng.NextElementUniform<PickupIndex>(list);
					}
					int num = 1 + self.bonusRewardCount;
					if (self.scaleRewardsByPlayerCount)
					{
						num *= participatingPlayerCount;
					}
					float angle = 360f / (float)num;
					Vector3 vector = Quaternion.AngleAxis((float)UnityEngine.Random.Range(0, 360), Vector3.up) * (Vector3.up * 40f + Vector3.forward * 5f);
					Quaternion rotation = Quaternion.AngleAxis(angle, Vector3.up);
					bool flag = self.bossDrops != null && self.bossDrops.Count > 0;
					bool flag2 = self.bossDropTables != null && self.bossDropTables.Count > 0;
					int i = 0;
					while (i < num)
                    {
                        PickupIndex pickupIndex2 = pickupIndex;

                        int currentCodexCount = codexCounts[i % participatingPlayerCount];
                        float currentVoidChance = GetCurrentVoidChance(currentCodexCount);
                        Debug.LogError("VoidChance: " + currentVoidChance);

                        bool shouldVoid = self.rng.nextNormalizedFloat <= currentVoidChance;
                        bool shouldBoss = self.rng.nextNormalizedFloat <= self.bossDropChance;

                        if (self.bossDrops != null && ((flag || flag2) && shouldBoss))
                        {
                            if (shouldVoid)
                            {
                                pickupIndex2 = voidBossDropTable.GenerateDrop(self.rng);
                            }
                            else
                            {
                                if (flag2)
                                {
                                    PickupDropTable pickupDropTable = self.rng.NextElementUniform<PickupDropTable>(self.bossDropTables);
                                    if (pickupDropTable != null)
                                    {
                                        pickupIndex2 = pickupDropTable.GenerateDrop(self.rng);
                                    }
                                }
                                else
                                {
                                    pickupIndex2 = self.rng.NextElementUniform<PickupIndex>(self.bossDrops);
                                }
                            }
                        }
                        else if(shouldVoid)
                        {
                            if (self.forceTier3Reward)
                            {
                                if(voidT3DropTable.selector.Count > 0)
                                    pickupIndex2 = voidT3DropTable.GenerateDrop(self.rng);
                            }
                            else
                            {
                                pickupIndex2 = voidT2DropTable.GenerateDrop(self.rng);
                            }
                        }

                        PickupDropletController.CreatePickupDroplet(pickupIndex2, self.dropPosition.position, vector);
                        i++;
                        vector = rotation * vector;
                    }
                    return;
				}
				Debug.LogWarning("dropPosition not set for BossGroup! No item will be spawned.");
			}
		}

        internal static float GetCurrentVoidChance(int currentCodexCount)
        {
            return 1 - ((1 - codexTriggerChanceBase) * Mathf.Pow(1 - codexTriggerChanceStack, currentCodexCount - 1));
        }

        public override void Init(ConfigFile config)
		{
			voidT2DropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            voidT2DropTable.tier1Weight = 0;
            voidT2DropTable.tier2Weight = 0;
            voidT2DropTable.voidTier1Weight = 0;
            voidT2DropTable.voidTier2Weight = 4;
            voidT2DropTable.voidTier3Weight = 1;
            voidT2DropTable.voidBossWeight = 0;
			voidT3DropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
            voidT3DropTable.tier1Weight = 0;
            voidT3DropTable.tier2Weight = 0;
            voidT3DropTable.voidTier1Weight = 0;
            voidT3DropTable.voidTier2Weight = 0;
            voidT3DropTable.voidTier3Weight = 1;
            voidT3DropTable.voidBossWeight = 0;
			voidBossDropTable = ScriptableObject.CreateInstance<BasicPickupDropTable>();
			voidBossDropTable.tier1Weight = 0;
			voidBossDropTable.tier2Weight = 0;
			voidBossDropTable.voidTier1Weight = 0;
			voidBossDropTable.voidTier2Weight = 0;
			voidBossDropTable.voidTier3Weight = 0;
			voidBossDropTable.voidBossWeight = 1;

			CreateItem();
            CreateLang();
            //CreateBuff();
            Hooks();
		}
    }
}
