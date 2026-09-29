using R2API;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using RoR2.Orbs;
using UnityEngine.AddressableAssets;
using System.Linq;
using static R2API.RecalculateStatsAPI;
using static SwanSongExtended.Modules.Language.Styling;

using RoR2.Items;
using SwanSongExtended.Modules;
using RoR2.ExpansionManagement;
using UnityEngine.Networking;

[assembly: HG.Reflection.SearchableAttribute.OptIn]

namespace SwanSongExtended.Items
{
    class NearbyDefense : ItemBase<NearbyDefense>
	{
		public override bool isEnabled => true;
		#region
		[AutoConfig("Armor Increase Unconditional", 10)]
        public static int opalArmorBase = 10;
		[AutoConfig("Regen Increase Unconditional", "Scales with level", 1f)]
		public static float opalRegenBase = 1f;
		[AutoConfig("Armor Increase Per Buff", 5)]
		public static int opalArmorPerBuff = 5;
		[AutoConfig("Regen Increase Per Buff", "Scales with level", 0.5f)]
		public static float opalRegenPerBuff = 0.5f;
		[AutoConfig("Max Opal Buff", 5)]
		public static int opalMaxBuff = 5;
		[AutoConfig("Opal Area Radius", 20)]
		public static float opalAreaRadius = 20;
        #endregion
        public static float opalAreaRadiusSqr => opalAreaRadius * opalAreaRadius;
		public static BuffDef opalStatBuff;
		public static GameObject opalAreaIndicator = null;

		public static GameObject radiusIndicatorPrefab;
		public override AssetBundle assetBundle => SwanSongPlugin.retierAssetBundle;

		static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

		public override string ItemName => "Nearby Defense";

        public override string ItemLangTokenName => "NEARBYDEFENSE";

        public override string ItemPickupDesc => "Increases armor and regen while enemies are nearby.";

        public override string ItemFullDescription => $"Increases base health regeneration by " +
			$"{HealingColor($"+{opalRegenBase} hp/s")} {StackText($"+{opalRegenBase} hp/s")} " +
			$"and armor by {HealingColor($"+{opalArmorBase}")} {StackText("+" + opalArmorBase)}. " +
			$"For each enemy within {UtilityColor(opalAreaRadius.ToString() +"m")}, also gain " +
			$"{HealingColor($"+{opalRegenPerBuff} hp/s")} {StackText($"+{opalRegenPerBuff} hp/s")} " +
			$"base health regeneration and {HealingColor($"+{opalArmorPerBuff}")} {StackText("+" + opalArmorPerBuff)}" +
			$" armor, up to {UtilityColor(opalMaxBuff.ToString())} times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override GameObject ItemModel => LoadDropPrefab();

        public override Sprite ItemIcon => LoadItemIcon();
		public override ExpansionDef RequiredExpansion => SwanSongPlugin.expansionDefSS2;

		public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
			return IDR;
        }

        public override void Hooks()
		{
			GetStatCoefficients += OpalStatCoefficients;
		}
        public override void Init()
        {
            base.Init();
			SwanSongPlugin.LoadAsync<GameObject>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_Base_NearbyDamageBonus.NearbyDamageBonusIndicator_prefab, CreateRangeIndicator);
			opalStatBuff = Content.CreateAndAddBuff("bdNearbyDefense",
				Addressables.LoadAssetAsync<Texture2D>(RoR2BepInExPack.GameAssetPaths.Version_1_39_0.RoR2_DLC1_OutOfCombatArmor.texBuffUtilitySkillArmor_tif).WaitForCompletion().AsSprite(),
				Color.magenta,
				canStack: true,
				isDebuff: false,
				isHidden: false
				);
		}

        private void CreateRangeIndicator(GameObject obj)
		{
			radiusIndicatorPrefab = obj.InstantiateClone("NearbyDefenseRangeIndicator", true);

			Transform radiusSpherical = radiusIndicatorPrefab.transform.GetChild(1);
			if (radiusSpherical)
			{
				radiusSpherical.transform.localScale = Vector3.one * opalAreaRadius * 2;

				if (radiusSpherical.gameObject.TryGetComponent(out MeshRenderer meshRenderer))
				{
					Material mat = UnityEngine.Object.Instantiate(meshRenderer.material);
					mat.SetColor("_TintColor", new Color32(50, 82, 115, 1));

					meshRenderer.material = mat;
				}
			}

			Modules.Content.AddNetworkedObjectPrefab(radiusIndicatorPrefab);
		}

        private void OpalStatCoefficients(CharacterBody sender, StatHookEventArgs args)
		{
			int itemCount = GetCount(sender);
			int buffCount = sender.GetBuffCount(opalStatBuff);

			args.armorAdd += itemCount * ((buffCount * opalArmorPerBuff) + opalArmorBase);
			args.baseRegenAdd += itemCount * ((buffCount * opalRegenPerBuff) + opalRegenBase) * (1 + (0.2f * sender.level));
		}
	}
	public class NearbyDefenseBehavior : BaseItemBodyBehavior
	{
		[ItemDefAssociation(useOnServer = true, useOnClient = false)]
		private static ItemDef GetItemDef() => CobaltShield.instance.ItemsDef;

		private GameObject nearbyDamageBonusIndicator;

		public float frequency = 2;
		float interval => 1 / frequency;
		float timer;

		public void FixedUpdate()
		{
			timer += Time.fixedDeltaTime;
			if (timer >= interval)
			{
				timer -= interval;
				TeamIndex alliedTeamIndex = this.body.teamComponent.teamIndex;
				int nearbyEnemyCount = 0;
				for (TeamIndex team = TeamIndex.Neutral; team < TeamIndex.Count; team++)
				{
					if (team != alliedTeamIndex && team > TeamIndex.Neutral && nearbyEnemyCount < NearbyDefense.opalMaxBuff)
					{
						foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(team))
						{
							Vector3 distanceVector = teamComponent.transform.position - this.body.corePosition;
							if (distanceVector.sqrMagnitude <= NearbyDefense.opalAreaRadiusSqr)
							{
								nearbyEnemyCount++;
								if(nearbyEnemyCount >= NearbyDefense.opalMaxBuff)
                                {
									break;
                                }
							}
						}
					}
				}
				this.SetBuffCount(nearbyEnemyCount);
			}
		}

		public void SetBuffCount(int nearbyEnemies)
		{
			if (!NetworkServer.active)
				return;
			int buffCount = this.body.GetBuffCount(NearbyDefense.opalStatBuff);

			if (buffCount != nearbyEnemies)
			{
				bool flag2 = buffCount < nearbyEnemies;
				if (flag2)
				{
					for (int i = 0; i < nearbyEnemies - buffCount; i++)
					{
						this.body.AddBuff(NearbyDefense.opalStatBuff);
					}
				}
				else
				{
					bool flag3 = buffCount > nearbyEnemies;
					if (flag3)
					{
						for (int j = 0; j < buffCount - nearbyEnemies; j++)
						{
							this.body.RemoveBuff(NearbyDefense.opalStatBuff);
						}
					}
				}
			}
		}
		private void OnEnable()
		{
			this.indicatorEnabled = true;
		}

		private void OnDisable()
		{
			this.indicatorEnabled = false;
		}
		private bool indicatorEnabled
		{
			get
			{
				return this.nearbyDamageBonusIndicator;
			}
			set
			{
				if (this.indicatorEnabled == value)
				{
					return;
				}
				if (value == true)
				{
					this.nearbyDamageBonusIndicator = UnityEngine.Object.Instantiate<GameObject>(NearbyDefense.radiusIndicatorPrefab, base.body.corePosition, Quaternion.identity);
					this.nearbyDamageBonusIndicator.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
					return;
				}
				//false
				UnityEngine.Object.Destroy(this.nearbyDamageBonusIndicator);
				this.nearbyDamageBonusIndicator = null;
			}
		}
	}
}
