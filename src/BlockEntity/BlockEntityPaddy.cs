using System.Collections.Generic;
using System.Reflection;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RicePaddies;

// Vanilla farmland BE plus auto-flood
public class BlockEntityPaddy : BlockEntityFarmland
{
	private bool floodedByMe;
	private Block waterSourceBlock;
	private long moistureEnforceListener;
	private long tallplantBoostListener;

	// 16x16 paddy field cap; protects tick budget from pathological networks.
	private const int MaxNetworkScan = 256;
	private const double RiceBonusHours = 6.0;
	private const double TallplantBonusHoursPerTick = 2.0;

	// Reflection handle for BlockEntityTransient.transitionHoursLeft (private double)
	private static readonly FieldInfo transitionHoursLeftField =
		typeof(BlockEntityTransient).GetField(
			"transitionHoursLeft",
			BindingFlags.NonPublic | BindingFlags.Instance);

	public override void Initialize(ICoreAPI api)
	{
		base.Initialize(api);
		waterSourceBlock = api.World.GetBlock(new AssetLocation("game:water-still-7"));

		if (api.Side == EnumAppSide.Server)
		{

			api.Event.RegisterCallback(_ => Evaluate(), 200);
			api.Event.RegisterCallback(_ => Evaluate(), 5000);

			// Vanilla updateMoistureLevel drifts to ~85% even with water at d=0
			moistureEnforceListener = RegisterGameTickListener(EnforceFloodedMoisture, 10000);

			// Boost regrowth of harvested cattails/papyrus/tule above the paddy.
			tallplantBoostListener = RegisterGameTickListener(BoostTallplantGrowth, 60000);
		}
	}

	private void EnforceFloodedMoisture(float dt)
	{
		if (!floodedByMe) return;
		moistureLevel = 1f;
		lastWaterDistance = 0f;
		UpdateFarmlandBlock();
	}

	public void OnNeighborChanged()
	{
		if (Api?.Side != EnumAppSide.Server) return;
		Evaluate();
	}

	public override void OnBlockRemoved()
	{
		if (Api?.Side == EnumAppSide.Server)
		{
			ClearOurWater();
			NotifyPaddyNeighbors();

			if (moistureEnforceListener != 0)
			{
				UnregisterGameTickListener(moistureEnforceListener);
				moistureEnforceListener = 0;
			}
			if (tallplantBoostListener != 0)
			{
				UnregisterGameTickListener(tallplantBoostListener);
				tallplantBoostListener = 0;
			}
		}
		base.OnBlockRemoved();
	}

	public override void OnBlockUnloaded()
	{
		if (Api?.Side == EnumAppSide.Server)
		{
			if (moistureEnforceListener != 0)
			{
				UnregisterGameTickListener(moistureEnforceListener);
				moistureEnforceListener = 0;
			}
			if (tallplantBoostListener != 0)
			{
				UnregisterGameTickListener(tallplantBoostListener);
				tallplantBoostListener = 0;
			}
		}
		base.OnBlockUnloaded();
	}

	public bool IsFlooded => floodedByMe;


	public void ApplyRicePaddyBonus()
	{
		if (Api?.Side != EnumAppSide.Server) return;
		totalHoursForNextStage -= RiceBonusHours;
		MarkDirty();
	}

	private void BoostTallplantGrowth(float dt)
	{
		if (transitionHoursLeftField == null) return;

		BlockPos above = Pos.UpCopy();
		Block aboveBlock = Api.World.BlockAccessor.GetBlock(above);
		string path = aboveBlock?.Code?.Path;
		if (path == null || !path.Contains("harvested")) return;
		if (!path.Contains("coopersreed") && !path.Contains("papyrus") && !path.Contains("tule")) return;

		var transient = Api.World.BlockAccessor.GetBlockEntity(above) as BlockEntityTransient;
		if (transient == null) return;

		double current = (double)transitionHoursLeftField.GetValue(transient);
		if (current > TallplantBonusHoursPerTick)
		{
			transitionHoursLeftField.SetValue(transient, current - TallplantBonusHoursPerTick);
			transient.MarkDirty();
		}
	}



	private void Evaluate()
	{
		if (waterSourceBlock == null) return;
		if (Block is not BlockPaddy) return;

		bool fed = NetworkHasNaturalSource();

		if (fed && !floodedByMe)
		{
			TryPlaceWater();
			if (floodedByMe)
			{
				moistureLevel = 1f;
				lastWaterDistance = 0f;
				UpdateFarmlandBlock();
			}
			NotifyPaddyNeighbors();
		}
		else if (!fed && floodedByMe)
		{
			ClearOurWater();
			NotifyPaddyNeighbors();
		}
	}

	private bool NetworkHasNaturalSource()
	{
		HashSet<BlockPos> visited = new HashSet<BlockPos>();
		visited.Add(Pos.Copy());
		Queue<BlockPos> frontier = new Queue<BlockPos>();
		frontier.Enqueue(Pos.Copy());

		int scanned = 0;
		while (frontier.Count > 0 && scanned++ < MaxNetworkScan)
		{
			BlockPos here = frontier.Dequeue();

			// 4 horizontals + up. No down: paddies don't drain upward.
			BlockPos[] neighbors = new BlockPos[]
			{
				here.NorthCopy(), here.EastCopy(), here.SouthCopy(), here.WestCopy(),
				here.UpCopy()
			};

			foreach (BlockPos nbr in neighbors)
			{
				Block nbrSolid = Api.World.BlockAccessor.GetBlock(nbr, BlockLayersAccess.Solid);
				Block nbrFluid = Api.World.BlockAccessor.GetBlock(nbr, BlockLayersAccess.Fluid);
				bool nbrIsPaddy = nbrSolid is BlockPaddy;

				// Other paddies' placed water never counts: prevents a flooded
				// ring from sustaining itself after source disconnects.
				if (!nbrIsPaddy && IsNaturalWaterSource(nbrFluid))
				{
					return true;
				}

				if (nbrIsPaddy && !visited.Contains(nbr))
				{
					BlockPos nbrCopy = nbr.Copy();
					visited.Add(nbrCopy);
					frontier.Enqueue(nbrCopy);
				}
			}
		}

		return false;
	}

	private bool IsNaturalWaterSource(Block fluid)
	{
		if (fluid == null || !fluid.IsLiquid()) return false;
		if (fluid.Code?.Path == null || !fluid.Code.Path.StartsWith("water")) return false;
		return fluid.LiquidLevel == 7;
	}


	private void TryPlaceWater()
	{
		Block existingFluid = Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Fluid);
		if (existingFluid != null && existingFluid.Id != 0)
		{
			// Paddy built inside an existing pond. Don't clobber; mark not-ours.
			floodedByMe = false;
			MarkDirty(true);
			return;
		}

		Api.World.BlockAccessor.SetBlock(waterSourceBlock.Id, Pos, BlockLayersAccess.Fluid);
		floodedByMe = true;
		MarkDirty(true);
	}

	private void ClearOurWater()
	{
		if (!floodedByMe) return;

		Block fluid = Api.World.BlockAccessor.GetBlock(Pos, BlockLayersAccess.Fluid);
		if (fluid != null && fluid.IsLiquid() && fluid.Code?.Path?.StartsWith("water") == true)
		{
			Api.World.BlockAccessor.SetBlock(0, Pos, BlockLayersAccess.Fluid);
		}
		floodedByMe = false;
		MarkDirty(true);
	}

	private void NotifyPaddyNeighbors()
	{
		BlockPos[] neighbors = new BlockPos[]
		{
			Pos.NorthCopy(), Pos.EastCopy(), Pos.SouthCopy(), Pos.WestCopy(),
			Pos.UpCopy(), Pos.DownCopy()
		};
		foreach (BlockPos nbr in neighbors)
		{
			Api.World.BlockAccessor.GetBlockEntity<BlockEntityPaddy>(nbr)?.Evaluate();
		}
	}


	public override void ToTreeAttributes(ITreeAttribute tree)
	{
		base.ToTreeAttributes(tree);
		tree.SetBool("floodedByMe", floodedByMe);
	}

	public override void FromTreeAttributes(ITreeAttribute tree, IWorldAccessor worldAccessForResolve)
	{
		base.FromTreeAttributes(tree, worldAccessForResolve);
		floodedByMe = tree.GetBool("floodedByMe", false);
	}
}
