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
        public override bool GetPrerequisites()
        {
            return StormsCore.stormsEnabled;
        }
        public override bool forcePrerequisites => true;
        static ItemDef brokenItemDef;
        public override bool CanBeTemporary => false;
        public override string ItemName => "Wishbone";

        public override string ItemLangTokenName => "WISHBONE";

        public override string ItemPickupDesc => "Breaks during storms. If taken to the Teleporter, a wish will be granted.";

        public override string ItemFullDescription => ItemPickupDesc;

        public override string ItemLore => "loooorrrrreeeeeeee";

        public override ItemTier Tier => ItemTier.Boss;

        public override ItemTag[] ItemTags => new ItemTag[] 
            { ItemTag.WorldUnique, ItemTag.CannotCopy, ItemTag.InteractableRelated, 
                ItemTag.HoldoutZoneRelated, ItemTag.ObjectiveRelated, 
                ItemTag.CannotSteal, ItemTag.DevotionBlacklist, ItemTag.RebirthBlacklist };

        public override GameObject ItemModel => LoadDropPrefab("mdlWishbone");

        public override Sprite ItemIcon => LoadItemIcon("texIconWishbone");// Addressables.LoadAssetAsync<Sprite>(RoR2BepInExPack.GameAssetPaths.RoR2_Base_Common_MiscIcons.texWIPIcon_png).WaitForCompletion();

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
            return new ItemDisplayRuleDict();
        }
        public override void Init()
        {
            brokenItemDef = CreateNewUntieredItem("BROKENWISH",
                LoadItemIcon("texIconWishboneBroken"));
            DoLangForItem(brokenItemDef, "Bone", "The mundane half of a broken wishbone. Better luck next time.");
            base.Init();
        }

        public override void Hooks()
        {
            TeleporterInteraction.onTeleporterBeginChargingGlobal += StealWishboneOnTeleCharge;
            CharacterBody.onBodyStartGlobal += DestroyWishboneOnStart;
            IL.RoR2.BossGroup.DropRewards += WishboneRewards;
            On.RoR2.BossGroup.DropRewards += WishboneRewardMessage;
            On.RoR2.HealthComponent.TakeDamageProcess += DestroyWishboneOnDamage;
        }

        private void BreakWishbones(CharacterBody body, int wishboneCount, bool badBreak = true)
        {
            if (wishboneCount <= 0)
                return;

            if (!badBreak)
            {
                body.inventory.RemoveItemPermanent(this.ItemsDef.itemIndex, wishboneCount);
                return;
            }

            //transform into broken wishbone
            Inventory.ItemTransformation.TryTransformResult tryTransformResult;
            new Inventory.ItemTransformation
            {
                originalItemIndex = ItemsDef.itemIndex,
                newItemIndex = brokenItemDef.itemIndex,
                maxToTransform = int.MaxValue,
                transformationType = (ItemTransformationTypeIndex)CharacterMasterNotificationQueue.TransformationType.Default
            }.TryTransform(body.inventory, out tryTransformResult);

            EffectData effectData2 = new EffectData
            {
                origin = body.corePosition
            };
            effectData2.SetNetworkedObjectReference(body.gameObject);
            EffectManager.SpawnEffect(HealthComponent.AssetReferences.fragileDamageBonusBreakEffectPrefab, effectData2, true);
        }

        public static int upgradeChance = 30;
        static bool upgradeAlt1 = false;
        static bool upgradeAlt2 = false;
        static int serverWishboneCount = 0;
        static PickupIndex wishPickupAlt1 = PickupIndex.none;
        static PickupIndex wishPickupAlt2 = PickupIndex.none;

        private static void CreateBossRewardDroplet(UniquePickup pickup, Vector3 position, Vector3 velocity, int indexOfCurrentReward, BossGroup bossGroup)
        {
            GenericPickupController.CreatePickupInfo pickupInfo = new GenericPickupController.CreatePickupInfo
            {
                rotation = Quaternion.identity,
                pickup = pickup,
                position = position
            };
            int rewardIndexPerPlayer = indexOfCurrentReward % (1 + bossGroup.bonusRewardCount);
            bool firstRewardPerPlayer = rewardIndexPerPlayer == 0;
            //bool idk = indexOfCurrentReward == rewardIndexPerPlayer;
            //if any wishbones have been added and the current reward is the first for each player
            if (firstRewardPerPlayer && serverWishboneCount > 0)
            {
                //subtract a wishbone from the total
                serverWishboneCount--;
                UniquePickup pickupAlt1 = GetWishPickup(ref wishPickupAlt1, upgradeAlt1);
                UniquePickup pickupAlt2 = GetWishPickup(ref wishPickupAlt2, upgradeAlt2);

                pickupInfo.pickerOptions = PickupPickerController.GenerateOptionsFromList(new List<UniquePickup>(3) { pickupAlt1, pickup, pickupAlt2 });
                pickupInfo.prefabOverride = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/OptionPickup/OptionPickup.prefab").WaitForCompletion();
                pickupInfo.pickupIndex = PickupCatalog.FindPickupIndex(ItemTier.Tier2);
            }
            PickupDropletController.CreatePickupDroplet(pickupInfo, position, velocity);
        }
        private static bool GetWishUpgraded()
        {
            if (serverWishboneCount <= Run.instance.participatingPlayerCount)
                return false;

            serverWishboneCount--;
            //if (Util.CheckRoll(upgradeChance))
                return true;
            //return false;
        }
        private static UniquePickup GetWishPickup(ref PickupIndex pickupIndex, bool isUpgraded)
        {
            List<PickupIndex> list = Run.instance.availableTier2DropList;
            if (isUpgraded)
            {
                if(Util.CheckRoll(upgradeChance))
                    list = Run.instance.availableTier3DropList;
            }
            pickupIndex = Run.instance.bossRewardRng.NextElementUniform<PickupIndex>(list);
            return new UniquePickup(pickupIndex);
        }

        #region hooks
        private void WishboneRewardMessage(On.RoR2.BossGroup.orig_DropRewards orig, BossGroup self)
        {
            if (serverWishboneCount > 0)
            {
                Chat.ServerAttemptBroadcastChat("<style=cIsDamage>A wish is granted...</style>");
            }
            orig(self);
            serverWishboneCount = 0;
            upgradeAlt1 = false;
            upgradeAlt2 = false;
        }
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
                        x => x.MatchLdsfld<UniquePickup>(nameof(UniquePickup.none)),
                        x => x.MatchStloc(out rewardLoc))
                    && c.TryGotoNext(MoveType.Before,
                        x => x.MatchCallOrCallvirt<PickupDropletController>(nameof(PickupDropletController.CreatePickupDroplet)));
                if (ILFound2)
                {
                    c.Remove();
                    c.Emit(OpCodes.Ldloc, rewardIndexLoc);
                    c.Emit(OpCodes.Ldarg_0);
                    c.EmitDelegate<Action<UniquePickup, Vector3, Vector3, int, BossGroup>>
                        ((pickup, position, velocity, rewardIndex, bossGroup) =>
                        CreateBossRewardDroplet(pickup, position, velocity, rewardIndex, bossGroup));
                }
            }
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

        private void DestroyWishboneOnStart(CharacterBody self)
        {
            if (!NetworkServer.active)
                return;
            int wishboneCount = GetCount(self, true);
            BreakWishbones(self, wishboneCount, false);
        }

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
                    int wishboneCount = GetCount(characterMaster.inventory, true);
                    if (body.healthComponent.alive)
                    {
                        serverWishboneCount += wishboneCount;

                        //item transfer effect
                        if (wishboneCount > 0)
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

            upgradeAlt1 = GetWishUpgraded();
            upgradeAlt2 = GetWishUpgraded();
        }
        #endregion
    }
}
