using Mono.Cecil;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;

namespace MoreItems.Items
{
	public class Thorn : Item_V2<Thorn>
	{
		public override ItemTier itemTier => ItemTier.Tier1;

		public override string displayName => "Thorn";

		[AutoConfig("Percentage of damage increase for all subsequent stacks", AutoConfigFlags.None, 0.01f, float.MaxValue)]
		public float stackPercentageIncrease { get; private set; } = 0.1f;

		public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] 
		{ 
			ItemTag.Damage 
		});

		protected override string GetDescString(string langID = null)
		{
			return "Increase outgoing damage by 5% per stack. Stacks multiplicatively.";
		}

		protected override string GetLoreString(string langID = null)
		{
			return 
				"Death by a thousand cuts.\n" +
				"Wow, what a great item";
		}

		protected override string GetNameString(string langID = null)
		{
			return displayName;
		}

		protected override string GetPickupString(string langID = null)
		{
			return "Increased damage. Grows exponentially.";
		}

		public static GameObject ItemBodyModelPrefab = null;

		public Thorn() 
		{
			modelResourcePath = "@MoreItems:Assets/Prefabs/Thorn/Thorn.prefab";
			iconResourcePath = "@MoreItems:Assets/Prefabs/Thorn/Icon.png";
		}

		public override void SetupAttributes()
		{
			if (ItemBodyModelPrefab == null)
			{
				ItemBodyModelPrefab = GetPickupModel();
			}

			base.SetupAttributes();
		}

		public override void SetupBehavior()
		{
			base.SetupBehavior();

			if (Compat_ItemStats.enabled == true) 
			{
				Compat_ItemStats.CreateItemStatDef(itemDef, (
				(count, inv, master) =>
				{
					return (float)Mathf.Pow((1 + stackPercentageIncrease), count);
				}, 
				(value, inv, master) => 
				{
					return $"Increases Damage By: {value.ToString("N2")} Times";
				}));
			}
		}

		public override void Install()
		{
			base.Install();

			IL.RoR2.HealthComponent.TakeDamage += HealthComponent_TakeDamage;
		}

		private void HealthComponent_TakeDamage(ILContext il)
		{
			ILCursor c = new ILCursor(il);
			bool ILFound = false;

			int locDmg = -1;
			ILFound = c.TryGotoNext(
				x => x.MatchLdarg(1),
				x => x.MatchLdfld<DamageInfo>("damage"),
				x => x.MatchStloc(out locDmg));

			if (!ILFound)
			{
				MoreItemsPlugin.logger.LogError("Failed to apply Thorn IL patch (damage var read), item will not work; target instructions not found");
				return;
			}

			FieldReference locEnemy = null;
			int locThis = -1;
			ILFound = c.TryGotoNext(
				x => x.MatchLdloc(2),
				x => x.MatchLdarg(out locThis),
				x => x.MatchLdfld(out locEnemy),
				x => x.MatchCallOrCallvirt<CharacterBody>("get_teamComponent"),
				x => x.MatchCallOrCallvirt<TeamComponent>("get_teamIndex"));

			if (!ILFound)
			{
				MoreItemsPlugin.logger.LogError("Failed to apply Thorn IL patch (damage var read), item will not work; target instructions not found");
				return;
			}

			int locChrm = -1;
			ILFound = c.TryGotoNext(
				x => x.MatchLdloc(out locChrm),
				x => x.MatchCallOrCallvirt<CharacterMaster>("get_inventory"),
				x => x.MatchLdcI4((int)ItemIndex.Crowbar))
				&& c.TryGotoPrev(MoveType.After,
				x => x.OpCode == OpCodes.Brfalse);

			if (ILFound)
			{
				c.Emit(OpCodes.Ldloc, locChrm);
				c.Emit(OpCodes.Ldarg, locThis);
				c.Emit(OpCodes.Ldloc, locDmg);
				c.EmitDelegate<Func<CharacterMaster, HealthComponent, float, float>>((character, body, originalDamage) =>
				{
					var count = GetCount(character.inventory);

					return originalDamage * Mathf.Pow((1 + stackPercentageIncrease), count);
				});

				c.Emit(OpCodes.Stloc, locDmg);
			}
			else
			{
				MoreItemsPlugin.logger.LogError("Failed to apply Thorn IL patch (damage var write), item will not work; target instructions not found");
				return;
			}
		}
	}
}
