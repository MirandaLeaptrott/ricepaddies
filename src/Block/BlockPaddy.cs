using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.GameContent;

namespace RicePaddies;

// BlockFarmland that rejects every seed except rice. Paired with
// CropBehaviorPaddyOnly so rice grows fast here and crawls on regular farmland.
public class BlockPaddy : BlockFarmland
{
	public override bool OnBlockInteractStart(IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel)
	{
		var heldStack = byPlayer?.InventoryManager?.ActiveHotbarSlot?.Itemstack;
		var coll = heldStack?.Collectible;

		// Block non-rice seeds before vanilla Item.OnHeldInteractStart can plant them.
		if (coll is ItemPlantableSeed)
		{
			string croptype = coll.Variant?["type"];
			if (croptype != null && croptype != "rice")
			{
				if (world.Side == EnumAppSide.Server)
				{
					(byPlayer as IServerPlayer)?.SendIngameError(
						"paddy-rice-only",
						Lang.Get("ricepaddies:paddy-only-rice")
					);
				}
				return true;
			}
		}

		// Block non-bog berry cuttings. Cranberry and cloudberry both grow in real
		// peat bogs; everything else (strawberry, raspberry, currants, etc.) needs
		// well-drained soil and looks wrong half-submerged in a paddy.
		string collPath = coll?.Code?.Path;
		if (collPath != null && collPath.StartsWith("fruitingbushcutting-"))
		{
			if (!collPath.Contains("cranberry") && !collPath.Contains("cloudberry"))
			{
				if (world.Side == EnumAppSide.Server)
				{
					(byPlayer as IServerPlayer)?.SendIngameError(
						"paddy-bog-berries-only",
						Lang.Get("ricepaddies:paddy-only-bog-berries")
					);
				}
				return true;
			}
		}

		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	// Paddy sidesolid.up=false keeps the recessed top rendering correct, but
	// BehaviorUnstable on bog-berry bushes probes our UP face via this method.
	// Allow that one case explicitly; everything else falls through to vanilla.
	public override bool CanAttachBlockAt(IBlockAccessor blockAccessor, Block block, BlockPos pos, BlockFacing blockFace, Vintagestory.API.MathTools.Cuboidi attachmentArea = null)
	{
		if (blockFace == BlockFacing.UP)
		{
			string path = block?.Code?.Path;
			if (path != null
				&& (path.StartsWith("fruitingbush-") || path.StartsWith("fruitingbushcutting-"))
				&& (path.Contains("cranberry") || path.Contains("cloudberry")))
			{
				return true;
			}
		}
		return base.CanAttachBlockAt(blockAccessor, block, pos, blockFace, attachmentArea);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);

		// Forward to BE so it re-evaluates BFS reach to a natural water source.
		world.BlockAccessor.GetBlockEntity<BlockEntityPaddy>(pos)?.OnNeighborChanged();
	}
}
