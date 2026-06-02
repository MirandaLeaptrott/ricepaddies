using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RicePaddies;

public class BlockRicePaddyAware : BlockCrop, IDrawYAdjustable
{
	public new float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];

		if (nblock is BlockPaddy paddy)
		{
			// Auto-tracks paddy shape height changes via collision box Y2.
			float topY = 0.75f;
			if (paddy.CollisionBoxes != null && paddy.CollisionBoxes.Length > 0)
			{
				topY = paddy.CollisionBoxes[0].Y2;
			}
			return -(1.0f - topY);
		}

		return base.AdjustYPosition(pos, chunkExtBlocks, extIndex3d);
	}
}
