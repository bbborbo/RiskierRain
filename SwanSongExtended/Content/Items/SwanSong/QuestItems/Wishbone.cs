using Mono.Cecil.Cil;
using MonoMod.Cil;
using R2API;
using RoR2;
using RoR2.Orbs;
using SwanSongExtended.Components;
using SwanSongExtended.Storms;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static MoreStats.OnHit;

namespace SwanSongExtended.Items
{
    class Wishbone : ItemBase<Wishbone>
    {
        public override bool lockEnabled => true;
        static ItemDef brokenItemDef;
        public override string ItemName => "Wishbone";

        public override string ItemLangTokenName => "WISHBONE";

        public override string ItemPickupDesc => "Breaks during storms. If taken to the Teleporter, a wish will be granted.";

        public override string ItemFullDescription => ItemPickupDesc;

        public override string ItemLore => "loooorrrrreeeeeeee";

        public override ItemTier Tier => ItemTier.Boss;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.WorldUnique, ItemTag.CannotCopy, ItemTag.InteractableRelated, ItemTag.HoldoutZoneRelated };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texWIPIcon_png).WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
        public override void Init()
        {
            brokenItemDef = CreateNewUntieredItem("BROKENWISH",
                Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Core.texNullIcon_png).WaitForCompletion());
            DoLangForItem(brokenItemDef, "Bone", "The shorter half of a broken wishbone. It is useless.", "Useless.");
            base.Init();
        }

        public override void Hooks()
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal += StealWishboneOnTeleCharge;
            IL.RoR2.BossGroup.DropRewards += WishboneRewards;
            On.RoR2.BossGroup.DropRewards += WishboneRewardMessage;
            On.RoR2.CharacterBody.Start += DestroyWishboneOnStart;
            On.RoR2.HealthComponent.TakeDamageProcess += DestroyWishboneOnDamage;
        }

        private void WishboneRewardMessage(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            if (serverWishboneCount > 0)
            {
                Chat.AddMessage("<style=cIsDamage>A wish is granted...</style>");
            }
            orig(self);
        }

        private void DestroyWishboneOnDamage(On.RoR2.HealthComponent.orig_TakeDamageProcess orig, HealthComponent self, DamageInfo damageInfo)
        {
            orig(self, damageInfo);
            if (!NetworkServer.active)
                return;
            if (damageInfo.procCoefficient <= 0)
                return;
            if (damageInfo.attacker && damageInfo.attacker == self.gameObject)
                return;
            int count = GetCount(self.body);
            if (count > 0 && (StormRunBehavior.instance?.hasBegunStorm == true || !self.alive))
            {
                BreakWishbones(self.body, count);
            }
        }

        private void DestroyWishboneOnStart(On.RoR2.CharacterBody.orig_Start orig, CharacterBody self)
        {
            orig(self);
            if (!NetworkServer.active)
                return;
            int wishboneCount = GetCount(self);
            BreakWishbones(self, wishboneCount, false);
        }

        private void BreakWishbones(CharacterBody body, int wishboneCount, bool badBreak = true)
        {
            if (wishboneCount <= 0)
                return;
            body.inventory.RemoveItem(this.ItemsDef.itemIndex, wishboneCount);
            if (badBreak)
            {
                body.inventory.GiveItem(brokenItemDef.itemIndex, wishboneCount);

                CharacterMasterNotificationQueue.SendTransformNotification(body.master,
                    this.ItemsDef.itemIndex, brokenItemDef.itemIndex,
                    CharacterMasterNotificationQueue.TransformationType.Default);

                EffectData effectData2 = new EffectData
                {
                    origin = body.corePosition
                };
                effectData2.SetNetworkedObjectReference(body.gameObject);
                EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData2, true);
            }
        }

        public static int upgradeChance = 20;
        static int serverWishboneCount = 0;
        private void StealWishboneOnTeleCharge(TeleporterInteraction obj)
        {
            WishboneCarcassComponent.ClearAllCarcasses();
            if (!NetworkServer.active)
                return;
            serverWishboneCount = 0;

            foreach (CharacterMaster characterMaster in CharacterMaster.readOnlyInstancesList)
            {
                CharacterBody body = characterMaster.GetBody();
                if (body)
                {
                    int wishboneCount = GetCount(characterMaster.inventory);
                    if (body.healthComponent.alive)
                    {
                        serverWishboneCount += wishboneCount;

                        //item transfer effect
                        if(wishboneCount > 0)
                        {
                            EffectData effectData = new EffectData
                            {
                                origin = body.corePosition,
                                genericFloat = 1f, //duration
                                genericUInt = Util.IntToUintPlusOne((int)this.ItemsDef.itemIndex)
                            };
                            effectData.SetNetworkedObjectReference(obj.gameObject);
                            EffectManager.SpawnEffect(ItemTransferOrb.orbEffectPrefab, effectData, true);
                        }
                    }
                    BreakWishbones(body, wishboneCount, false);
                }
            }
        }

        PickupIndex wishPickupAlt1 = PickupIndex.none;
        PickupIndex wishPickupAlt2 = PickupIndex.none;
        private void WishboneRewards(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            int rewardLoc = 1;
            int rewardCountLoc = 2;
            int rewardIndexLoc = 8;
            bool ILFound1 = 
                c.TryGotoNext(MoveType.After,
                    x => x.MatchCallOrCallvirt<BossGroup>("get_bonusRewardCount"))
                && c.TryGotoNext(MoveType.After,
                    x => x.MatchStloc(out rewardCountLoc))
                && c.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(out rewardIndexLoc),
                x => x.MatchLdloc(rewardCountLoc),
                x => x.MatchBlt(out _));
            if (ILFound1)
            {
                c.Index = 0;
                bool ILFound2 = c.TryGotoNext(MoveType.After,
                        x => x.MatchLdsfld<PickupIndex>(nameof(PickupIndex.none)),
                        x => x.MatchStloc(out rewardLoc))
                    && c.TryGotoNext(MoveType.Before,
                        x => x.MatchCallOrCallvirt<PickupDropletController>(nameof(PickupDropletController.CreatePickupDroplet)));
                if (ILFound2)
                {
                    c.Remove();
                    c.Emit(OpCodes.Ldloc, rewardCountLoc);
                    c.Emit(OpCodes.Ldloc, rewardIndexLoc);
                    c.EmitDelegate<Action<PickupIndex, Vector3, Vector3, int, int>>((pickupIndex, position, velocity, rewardCount, rewardIndex) => CreatePickupDroplet(pickupIndex, position, velocity, rewardCount, rewardIndex));
                    void CreatePickupDroplet(PickupIndex pickupIndex, Vector3 position, Vector3 velocity, int rewardCount, int rewardIndex)
                    {
                        GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
                        {
                            rotation = Quaternion.identity,
                            pickupIndex = pickupIndex,
                            position = position
                        };
                        if(serverWishboneCount > 0 && rewardIndex == rewardCount % Run.instance.participatingPlayerCount)
                        {
                            PickupIndex pickupAlt1 = GetWishPickup(ref wishPickupAlt1);
                            PickupIndex pickupAlt2 = GetWishPickup(ref wishPickupAlt2);

                            PickupIndex[] options = new PickupIndex[] { wishPickupAlt1, pickupIndex, wishPickupAlt2 };
                            pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromArray(options);
                            pickupInfo.prefabOverride = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
                            pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2);

                            if (rewardIndex == rewardCount - 1)
                                serverWishboneCount = 0;
                        }
                        PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
                    }
                }
            }
        }

        private static PickupIndex GetWishPickup(ref PickupIndex pickupIndex)
        {
            if (pickupIndex != PickupIndex.none)
                return pickupIndex;
            List<PickupIndex> list = Run.instance.availableTier2DropList;
            bool shouldTryUpgrade = serverWishboneCount > Run.instance.participatingPlayerCount;
            if (shouldTryUpgrade)
            {
                serverWishboneCount--;
                if (Util.CheckRoll(upgradeChance))
                {
                    list = Run.instance.availableTier3DropList;
                }
            }
            pickupIndex = Run.instance.bossRewardRng.NextElementUniform<PickupIndex>(list);
            return pickupIndex;
        }
    }
}
