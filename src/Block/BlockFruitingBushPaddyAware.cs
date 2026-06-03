using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RicePaddies;

// Drop-in BlockRequireFertileGround replacement that sinks cranberry bushes 3/16
// into the water when planted on a paddy. Leaves visible above water surface,
// stems partly submerged. Mimics how cranberries grow in real bogs.
public class BlockFruitingBushPaddyAware : BlockRequireFertileGround, IDrawYAdjustable
{
	// 3/16 below water surface, 1/16 above paddy solid top (y=0.75).
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
