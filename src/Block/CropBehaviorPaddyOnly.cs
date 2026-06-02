using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.Server;

namespace RicePaddies;


public class CropBehaviorPaddyOnly : CropBehavior
{
	private float poorGrowthChance = 0.10f;

	public CropBehaviorPaddyOnly(Block block) : base(block)
	{
	}

	public override void Initialize(JsonObject properties)
	{
		base.Initialize(properties);

		float configured = properties["poorGrowthChance"].AsFloat(0.10f);
		if (configured > 0f && configured <= 1f)
		{
			poorGrowthChance = configured;
		}
	}

	public override bool TryGrowCrop(ICoreAPI api, IFarmlandBlockEntity farmland, double currentTotalHours, int newGrowthStage, ref EnumHandling handling)
	{
		Block soil = api.World.BlockAccessor.GetBlock(farmland.Pos);
		if (IsPaddy(soil))
		{
			// Vanilla advances the stage, then adds hoursForNextStage to set the
			// next deadline. Pre-decrementing makes that deadline land sooner.
			if (farmland is BlockEntityPaddy paddyBe)
			{
				paddyBe.ApplyRicePaddyBonus();
			}
			handling = EnumHandling.PassThrough;
			return false;
		}

		if (api.World.Rand.NextDouble() < poorGrowthChance)
		{
			handling = EnumHandling.PassThrough;
			return false;
		}

		handling = EnumHandling.PreventDefault;
		return false;
	}

	public override void OnPlanted(ICoreAPI api, ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel)
	{
		base.OnPlanted(api, itemslot, byEntity, blockSel);

		if (api.Side != EnumAppSide.Server) return;

		Block soil = api.World.BlockAccessor.GetBlock(blockSel.Position);
		if (IsPaddy(soil)) return;

		// One-shot heads-up: planted rice on farmland, expect slow growth.
		if (byEntity is EntityPlayer ep && ep.Player is IServerPlayer sp)
		{
			sp.SendIngameError(
				"rice-grows-poorly",
				Lang.Get("ricepaddies:rice-grows-poorly-on-farmland")
			);
		}
	}

	private static bool IsPaddy(Block block)
	{
		if (block?.Code == null) return false;
		return block.Code.Domain == "ricepaddies"
			&& block.Code.Path.StartsWith("paddy");
	}
}
