using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.AddressableAssets;
using R2API;
using static MoreStats.StatHooks;
using RoR2.Items;
using SwanSongExtended.Modules;
using RoR2.ExpansionManagement;
using HG;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Changes
{
    public class GhorsTome : ReworkBase<GhorsTome>
    {
        ModdedProcType GildedDamageBonusMask;
        public static float gildedDamageMultiplierBase = 1f;
        public static float gildedDamageMultiplierStack = 0.5f;
        public override string ItemPath => RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_BonusGoldPackOnKill.BonusGoldPackOnKill_asset;

        public override string ItemName => "Ghors Tome";

        public override string ItemPickupDesc => "Every elite you touch turns into gold.";

        public override string ItemFullDesc => $"Deal +{} damage to Gilded elites. Contacting any Elite enemy instantly transmutates it into a Gilded elite, triggering all on-kill effects. Does not affect Boss enemies.";

        public override void Init()
        {
            GildedDamageBonusMask = ProcTypeAPI.ReserveProcType();
            base.Init();
        }
        public override void OnItemLoaded(ItemDef item)
        {
            base.OnItemLoaded(item);

            item.requiredExpansion = Addressables.LoadAssetAsync<ExpansionDef>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC2_Common.DLC2_asset).WaitForCompletion();

            item.tier = ItemTier.Tier3;
            item.deprecatedTier = ItemTier.Tier3;
            if (assetBundle.Contains("Assets/Icons/Ghors_Tome.png"))
            {
                Sprite sprite = assetBundle.LoadAsset<Sprite>("Assets/Icons/Ghors_Tome.png");
                if (sprite)
                    itemDef.pickupIconSprite = sprite;
            }
        }
        public override void Hooks()
        {
            //IL.RoR2.HealthComponent.TakeDamageProcess += BonusDamageAgainstGilded;
            On.RoR2.HealthComponent.TakeDamageProcess += GildedDamageBonus;
            IL.RoR2.GlobalEventManager.OnCharacterDeath += RemoveGhor;
        }

        public static void RemoveGhor(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool b = c.TryGotoNext(MoveType.After,
                x => x.MatchLdsfld("RoR2.RoR2Content/Items", nameof(RoR2Content.Items.BonusGoldPackOnKill)),
                x => x.MatchCallOrCallvirt<Inventory>(nameof(Inventory.GetItemCountEffective))
                );
            if (!b)
            {
                SwanSongPlugin.DebugBreakpoint(nameof(RemoveGhor));
                return;
            }
            c.Emit(OpCodes.Pop);
            c.Emit(OpCodes.Ldc_I4_0);
        }

        private void BonusDamageAgainstGilded(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int localDamageLoc = 0;
            bool b1 = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<DamageInfo>(nameof(DamageInfo.damage)),
                x => x.MatchStloc(out localDamageLoc));
        }

        private void GildedDamageBonus(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            if (self.body.HasBuff(DLC2Content.Buffs.EliteAurelionite) && damageInfo.procChainMask.HasModdedProc(GildedDamageBonusMask) == false)
            {
                if(damageInfo.attacker && damageInfo.attacker.TryGetComponent(out CharacterBody attackerBody))
                {
                    int count = GetCount(attackerBody);
                    if (count > 0)
                    {
                        damageInfo.damage *= gildedDamageMultiplierBase + gildedDamageMultiplierStack * (count - 1);
                        damageInfo.procChainMask.AddModdedProc(GildedDamageBonusMask);
                    }
                }
            }
            orig(self, damageInfo);
        }
    }
    public class WeakenOnContactBehavior : BaseItemBodyBehavior
    {
        [ItemDefAssociation(useOnServer = true, useOnClient = false)]
        public static ItemDef GetItemDef() => RoR2Content.Items.BonusGoldPackOnKill;

        public SphereSearch sphereSearch = new SphereSearch();

        public float age = 0;
        protected float timer = 1 / 8;

        //[Min(1E-45f)]
        public float tickRate = 1f;

        public float sizeCorrectionMultiplier = 4f;

        readonly float maxTickDuration = 0.1f;
        readonly float minTickDuration = 0.1f;
        float lerp_denominator = 2;

        public void OnEnable()
        {
            sphereSearch.mask = LayerIndex.entityPrecise.mask;
            sphereSearch.radius = body.radius * sizeCorrectionMultiplier;
            sphereSearch.queryTriggerInteraction = QueryTriggerInteraction.UseGlobal;

            lerp_denominator = body.baseMoveSpeed * body.sprintingSpeedMultiplier * 2f + body.baseMoveSpeed;
        }

        public void FixedUpdate()
        {
            sphereSearch.radius = Mathf.Max(6f, body.radius * sizeCorrectionMultiplier);
            AdjustFrequencyBasedOnSpeed();

            age -= Time.fixedDeltaTime;
            if (age <= 0f)
            {
                age = timer;
                List<HurtBox> candidates = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
                SearchForTargets(candidates);
                if (candidates.Count == 0)
                    goto ReturnCollection;
                foreach (HurtBox hurtBox in candidates)
                {
                    if (hurtBox == null || hurtBox.healthComponent == null)
                        continue;
                    CharacterBody body = hurtBox.healthComponent.body;
                    if (body.isElite == false || body.isBoss == true)
                        continue;
                    Inventory inv = body.inventory;
                    if (inv.GetEquipmentIndex() == DLC2Content.Equipment.EliteAurelioniteEquipment.equipmentIndex)
                        continue;
                    float damageDealt = 0;
                    DamageInfo damageInfo = new DamageInfo();
                    damageInfo.damage = damageDealt;
                    damageInfo.procCoefficient = 1;
                    damageInfo.position = body.corePosition;
                    damageInfo.crit = body.RollCrit();
                    damageInfo.attacker = body.gameObject;
                    DamageReport damageReport = new DamageReport(damageInfo, hurtBox.healthComponent, damageDealt, hurtBox.healthComponent.combinedHealth);

                    GlobalEventManager.instance.OnCharacterDeath(damageReport);
                    inv.SetEquipmentIndex(DLC2Content.Equipment.EliteAurelioniteEquipment.equipmentIndex, false);
                }
            ReturnCollection:
                CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(candidates);
            }
        }

        public void SearchForTargets(List<HurtBox> dest)
        {
            sphereSearch.origin = body.corePosition;
            sphereSearch.RefreshCandidates();
            sphereSearch.FilterCandidatesByHurtBoxTeam(TeamMask.GetEnemyTeams(body.teamComponent.teamIndex));
            sphereSearch.OrderCandidatesByDistance();
            sphereSearch.FilterCandidatesByDistinctHurtBoxEntities();
            sphereSearch.GetHurtBoxes(dest);
            sphereSearch.ClearCandidates();
        }

        public void AdjustFrequencyBasedOnSpeed()
        {
            timer = Mathf.Lerp(minTickDuration, maxTickDuration, body.moveSpeed / lerp_denominator);
        }
    }
}

