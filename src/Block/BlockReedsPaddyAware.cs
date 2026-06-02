using Vintagestory.API.Client;
using Vintagestory.API.Client.Tesselation;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RicePaddies;


public class BlockReedsPaddyAware : BlockReeds, IDrawYAdjustable
{
	// Per-quad flag in MeshData.RenderPassesAndExtraBits
	private const short DisableRandomDrawOffsetBit = 1 << 10;

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

	// Suppress the random X/Z jitter when sitting on a paddy so the mesh stays
	// centered and doesn't overhang the recessed top. Natural ground keeps jitter.
	public override void OnJsonTesselation(ref MeshData sourceMesh, ref int[] lightRgbsByCorner, BlockPos pos, Block[] chunkExtBlocks, int extIndex3d)
	{
		base.OnJsonTesselation(ref sourceMesh, ref lightRgbsByCorner, pos, chunkExtBlocks, extIndex3d);

		Block nblock = chunkExtBlocks[extIndex3d + TileSideEnum.MoveIndex[TileSideEnum.Down]];
		if (nblock is BlockPaddy && sourceMesh?.RenderPassesAndExtraBits != null)
		{
			short[] arr = sourceMesh.RenderPassesAndExtraBits;
			for (int i = 0; i < arr.Length; i++)
			{
				arr[i] |= DisableRandomDrawOffsetBit;
			}
		}
	}
}
