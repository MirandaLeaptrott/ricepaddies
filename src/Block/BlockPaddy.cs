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

		return base.OnBlockInteractStart(world, byPlayer, blockSel);
	}

	public override void OnNeighbourBlockChange(IWorldAccessor world, BlockPos pos, BlockPos neibpos)
	{
		base.OnNeighbourBlockChange(world, pos, neibpos);

		// Forward to BE so it re-evaluates BFS reach to a natural water source.
		world.BlockAccessor.GetBlockEntity<BlockEntityPaddy>(pos)?.OnNeighborChanged();
	}
}
