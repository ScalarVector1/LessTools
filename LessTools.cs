using Microsoft.Xna.Framework;
using System.Collections.Generic;
using System.Linq;
using Terraria;
using Terraria.ModLoader;

namespace LessTools
{
	public class LessTools : Mod
	{
	}

	/// <summary>
	/// This system handles tracking all axes, hammers, and drills that should be removed from the game
	/// </summary>
	public class KillSystem : ModSystem
	{
		/// <summary>
		/// A collection of all item types that are naughty
		/// </summary>
		public static List<int> typesToKill = new();

		// This detects and adds all items that will be removed from the game to the typesToKill list
		public override void PostSetupContent()
		{
			for (int k = 0; k < ItemLoader.ItemCount; k++)
			{
				var entity = new Item();
				entity.SetDefaults(k);

				bool axeOrHammer = (entity.axe > 0 || entity.hammer > 0) && entity.pick == 0;
				bool drill = entity.pick > 0 && entity.useStyle == Terraria.ID.ItemUseStyleID.Shoot;

				bool special = entity.ModItem?.AltFunctionUse(Main.LocalPlayer) ?? false;

				bool kill = (axeOrHammer || drill) && !special;

				if (kill)
					typesToKill.Add(k);
			}
		}

		// This removes all recipes that make the offending items, and removes them as ingredients from recipes that would use them.
		public override void PostAddRecipes()
		{
			for (int i = 0; i < Recipe.numRecipes; i++)
			{
				Recipe recipe = Main.recipe[i];

				for (int k = 0; k < typesToKill.Count; k++)
				{
					int bad = typesToKill[k];

					if (recipe.TryGetIngredient(bad, out Item ingredient))
					{
						recipe.RemoveIngredient(bad);
					}

					if (recipe.HasResult(bad))
					{
						recipe.DisableRecipe();
						break;
					}
				}
			}
		}

		// This sets the removed items as deprecated similar to unused items in vanilla
		public override void PostSetupRecipes()
		{
			for (int k = 0; k < typesToKill.Count; k++)
			{
				Terraria.ID.ItemID.Sets.Deprecated[typesToKill[k]] = true;
			}
		}
	}

	/// <summary>
	/// This class handles the changes to pickaxes to make up for the removal of other tool types!
	/// </summary>
	public class PickEnhancer : GlobalItem
	{
		// Track the initial tool power, based on pickaxe power of the parent item
		int storedPower = 0;

		// Each relevant item gets it's own stored power
		public override bool InstancePerEntity => true;

		// Only applies to picks!
		public override bool AppliesToEntity(Item entity, bool lateInstantiation)
		{
			return entity.pick > 0;
		}

		// Sets axe power internally and lets this item be reusable on a right click
		public override void SetDefaults(Item entity)
		{
			entity.axe = entity.pick / 5;
			storedPower = entity.pick;

			Terraria.ID.ItemID.Sets.ItemsThatAllowRepeatedRightClick[entity.type] = true;
		}

		// Allows right click with picks to use as a hammer
		public override bool AltFunctionUse(Item item, Player player)
		{
			return true;
		}

		// Toggles between pick/axe and hammer mode depending on lmb/rmb
		public override bool CanUseItem(Item item, Player player)
		{
			if (player.altFunctionUse == 2)
			{
				item.pick = 0;
				item.axe = 0;
				item.hammer = storedPower;
				item.autoReuse = true;
			}
			else
			{
				item.hammer = 0;
				item.pick = storedPower;
				item.axe = storedPower / 5;
			}

			return true;
		}

		// Allows picks to be auto-reused if they aren't, just incase
		public override bool? CanAutoReuseItem(Item item, Player player)
		{
			return true;
		}

		// Replaces the specific tool power tooltips with a generic one, and adds hint text about how to use as a hammer
		public override void ModifyTooltips(Item item, List<TooltipLine> tooltips)
		{
			TooltipLine toolLine = tooltips.FirstOrDefault(n => n.Name == "PickPower");
			int index = tooltips.IndexOf(toolLine);

			tooltips.RemoveAll(n => n.Name == "PickPower" || n.Name == "AxePower" || n.Name == "HammerPower");

			tooltips.Insert(index, new(Mod, "ToolPower", $"{storedPower}% Tool Power"));

			var helpLine = new TooltipLine(Mod, "ToolHelp", "Acts as both an axe and pickaxe. Right click to use as a hammer.")
			{
				OverrideColor = Color.LightGray
			};
			tooltips.Add(helpLine);
		}
	}
}