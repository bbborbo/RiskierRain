using BepInEx;
using BepInEx.Configuration;
using R2API;
using RiskierRain.CoreModules;
using RoR2;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.Networking;
using static R2API.RecalculateStatsAPI;

namespace RiskierRain.Items
{
    class WeirdlyShapedOpal : ItemBase<WeirdlyShapedOpal>
	{
		public static int opalArmorBase = 12;
		public static float opalRegenBase = 1f;
		public static int opalArmorPerBuff = 12;
		public static float opalRegenPerBuff = 0.5f;
		public static int opalMaxBuff = 5;
		public static float opalAreaRadius = 20;
		public static float opalAreaRadiusSqr => opalAreaRadius * opalAreaRadius;
		public static BuffDef opalStatBuff;
		public static GameObject opalAreaIndicator;

		static ItemDisplayRuleDict IDR = new ItemDisplayRuleDict();

		public override string ItemName => "Weirdly-shaped Opal";

        public override string ItemLangTokenName => "BORBOOPAL";

        public override string ItemPickupDesc => "Increases armor and regen while enemies are nearby.";

        public override string ItemFullDescription => $"Increases base health regeneration by +{opalRegenBase} (+{opalRegenBase} per stack) " +
			$"and armor by {opalArmorBase} (+{opalArmorBase} per stack). For each enemy within {opalAreaRadius}m, also gain " +
			$"+{opalRegenPerBuff} (+{opalRegenPerBuff} per stack) base health regeneration and {opalArmorPerBuff} (+{opalArmorPerBuff} per stack) armor, " +
			$"up to {opalMaxBuff} times.";

        public override string ItemLore => "";

        public override ItemTier Tier => ItemTier.Tier2;

        public override ItemTag[] ItemTags => new ItemTag[] { ItemTag.Healing };

        public override BalanceCategory Category => BalanceCategory.None;

        public override GameObject ItemModel => Resources.Load<GameObject>("prefabs/pickupmodels/PickupOddlyShapedOpal");

        public override Sprite ItemIcon => Resources.Load<Sprite>("textures/itemicons/texOddlyShapedOpalIcon");

        public override ItemDisplayRuleDict CreateItemDisplayRules()
        {
			return IDR;
        }

        public override void Hooks()
		{
			CloneVanillaDisplayRules(instance.ItemsDef, DLC1Content.Items.OutOfCombatArmor);
			On.RoR2.CharacterBody.OnInventoryChanged += AddOpalItemBehavior;
			GetStatCoefficients += OpalStatCoefficients;
		}

        public override void Init(ConfigFile config)
		{
			Debug.LogError("Opal rework needs to depend on SOTV!");
			CreateAssets();
			CreateItem();
			CreateLang();
			Hooks();
		}

        private void CreateAssets()
		{
			RiskierRainPlugin.RetierItem(DLC1Content.Items.OutOfCombatArmor);

			opalStatBuff = Addressables.LoadAssetAsync<BuffDef>("RoR2/DLC1/OutOfCombatArmor/bdOutOfCombatArmorBuff.asset").WaitForCompletion();
			opalStatBuff.canStack = true;

			opalAreaIndicator = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/NearbyDamageBonusIndicator").InstantiateClone("OpalAreaIndicator", true);
			Transform transform = opalAreaIndicator.transform.Find("Radius, Spherical");
			transform.localScale = Vector3.one * opalAreaRadius * 2f;
			/*MaterialControllerComponents.HGIntersectionController hgintersectionController = transform.gameObject.AddComponent<MaterialControllerComponents.HGIntersectionController>();
			MeshRenderer component = transform.GetComponent<MeshRenderer>();
			hgintersectionController.renderer = component;
			Material original = SpikestripContentBase.AddressablesLoad<Material>("d0eb35f70367cdc4882f3bb794b65f2b");
			Material material = Object.Instantiate<Material>(original);
			material.SetTexture("_RemapTex", SpikestripContentBase.AddressablesLoad<Texture>("385005992afbfce4089807386adc07b0"));
			material.SetColor("_TintColor", SpikestripContentBase.ColorRGB(50f, 82f, 115f, 1f));
			component.material = material;
			hgintersectionController.material = material;
			material.SetFloat("_BrightnessBoost", 0.1f);*/
			Assets.networkedObjectPrefabs.Add(opalAreaIndicator);
		}

        private void OpalStatCoefficients(CharacterBody sender, StatHookEventArgs args)
		{
			int itemCount = GetCount(sender);
			int buffCount = sender.GetBuffCount(opalStatBuff);

			args.armorAdd += itemCount * ((buffCount * opalArmorPerBuff) + opalArmorBase);
			args.baseRegenAdd += itemCount * ((buffCount * opalRegenPerBuff) + opalRegenBase) * (1 + (0.2f * sender.level));
		}

		private void AddOpalItemBehavior(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, RoR2.CharacterBody self)
		{
			orig(self);
            if (NetworkServer.active)
			{
				self.AddItemBehavior<WeirdOpalBehavior>(GetCount(self));
			}
		}
	}
	public class WeirdOpalBehavior : CharacterBody.ItemBehavior
	{
		GameObject areaIndicatorInstance;
		public float frequency = 6;
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
					if (team != alliedTeamIndex && team > TeamIndex.Neutral && nearbyEnemyCount < WeirdlyShapedOpal.opalMaxBuff)
					{
						foreach (TeamComponent teamComponent in TeamComponent.GetTeamMembers(team))
						{
							Vector3 distanceVector = teamComponent.transform.position - this.body.corePosition;
							if (distanceVector.sqrMagnitude <= WeirdlyShapedOpal.opalAreaRadiusSqr)
							{
								nearbyEnemyCount++;
								if(nearbyEnemyCount >= WeirdlyShapedOpal.opalMaxBuff)
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
			int buffCount = this.body.GetBuffCount(WeirdlyShapedOpal.opalStatBuff);

			if (buffCount != nearbyEnemies)
			{
				bool flag2 = buffCount < nearbyEnemies;
				if (flag2)
				{
					for (int i = 0; i < nearbyEnemies - buffCount; i++)
					{
						this.body.AddBuff(WeirdlyShapedOpal.opalStatBuff);
					}
				}
				else
				{
					bool flag3 = buffCount > nearbyEnemies;
					if (flag3)
					{
						for (int j = 0; j < buffCount - nearbyEnemies; j++)
						{
							this.body.RemoveBuff(WeirdlyShapedOpal.opalStatBuff);
						}
					}
				}
			}
		}
		private void Start()
		{
			this.indicatorEnabled = true;
		}
		private void OnDestroy()
		{
			this.indicatorEnabled = false;
		}
		private bool indicatorEnabled
		{
			get
			{
				return this.areaIndicatorInstance;
			}
			set
			{
				if (this.indicatorEnabled == value)
				{
					if (value)
					{
						this.areaIndicatorInstance = Instantiate<GameObject>(WeirdlyShapedOpal.opalAreaIndicator, this.body.corePosition, Quaternion.identity);
						this.areaIndicatorInstance.GetComponent<NetworkedBodyAttachment>().AttachToGameObjectAndSpawn(base.gameObject, null);
						//this.areaIndicatorInstance.transform.localScale = Vector3.one * WeirdlyShapedOpal.opalAreaRadius;
					}
					else
					{
						Destroy(this.areaIndicatorInstance);
						this.areaIndicatorInstance = null;
					}
				}
			}
		}
	}
}
