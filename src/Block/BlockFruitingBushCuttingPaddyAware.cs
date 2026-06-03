using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;

namespace RicePaddies;

// Drop-in Block replacement for fruitingbushcutting that sinks cranberry cuttings
// the same 3/16 amount as the mature bush, so there's no visual pop when the
// cutting matures into the bush over its 2-4 month transition.
public class BlockFruitingBushCuttingPaddyAware : Block, IDrawYAdjustable
{
	private const float CranberrySinkOnPaddy = -0.1875f;

	public float AdjustYPosition(BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
		if (nblock is BlockPaddy && IsBogBerry(Code?.Path))
		{
			return CranberrySinkOnPaddy;
		}
		return 0f;
	}

	private static bool IsBogBerry(string path)
	{
		if (path == null) return false;
		return path.Contains("cranberry") || path.Contains("cloudberry");
	}
}
