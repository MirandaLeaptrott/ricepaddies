using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Common.Entities;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RicePaddies;

public class ItemTakuwa : ItemHoe
{
	public override void OnHeldInteractStart(ItemSlot itemslot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handHandling)
	{
		if (blockSel == null) return;

		if (byEntity.Controls.ShiftKey && byEntity.Controls.CtrlKey)
		{
			base.OnHeldInteractStart(itemslot, byEntity, blockSel, entitySel, firstEvent, ref handHandling);
			return;
		}

		BlockPos pos = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos);

		if (byEntity.World.BlockAccessor.GetBlock(pos.UpCopy()).Id != 0)
		{
			(api as ICoreClientAPI)?.TriggerIngameError(this, "covered", Lang.Get("Requires no block above"));
			handHandling = EnumHandHandling.PreventDefault;
			return;
		}

		byEntity.Attributes.SetInt("didtill", 0);

		// Widen vanilla's soil-only check to also catch existing farmland.
		if (block.Code.PathStartsWith("soil") || block.Code.PathStartsWith("farmland"))
		{
			handHandling = EnumHandHandling.PreventDefault;
		}
	}

	public override void DoTill(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
	{
		if (blockSel == null) return;
		BlockPos pos = blockSel.Position;
		Block block = byEntity.World.BlockAccessor.GetBlock(pos);

		string targetCode = null;
		bool fromFarmland = false;
		TreeAttribute prevData = null;

		if (block.Code.PathStartsWith("soil"))
		{
			// soil-{fertility}-{grassCoverage}; LastCodePart(1) = fertility.
			string fertility = block.LastCodePart(1);
			targetCode = "paddy-dry-" + fertility;

			var besn = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntitySoilNutrition;
			if (besn != null)
			{
				prevData = new TreeAttribute();
				besn.ToTreeAttributes(prevData);
			}
		}
		else if (block.Code.PathStartsWith("farmland"))
		{
			string state = block.LastCodePart(1);
			string fertility = block.LastCodePart(0);
			targetCode = $"paddy-{state}-{fertility}";
			fromFarmland = true;

			var bef = byEntity.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityFarmland;
			if (bef != null)
			{
				prevData = new TreeAttribute();
				bef.ToTreeAttributes(prevData);
			}
		}
		else
		{
			return;
		}

		Block paddy = byEntity.World.GetBlock(new AssetLocation("ricepaddies", targetCode));
		IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
		if (paddy == null || byPlayer == null) return;
		if (block.Sounds != null) byEntity.World.PlaySoundAt(block.Sounds.Place, pos, 0.4, null);

		byEntity.World.BlockAccessor.SetBlock(paddy.BlockId, pos);

		slot.Itemstack?.Collectible.DamageItem(byEntity.World, byEntity, byPlayer.InventoryManager.ActiveHotbarSlot);

		if (slot.Empty)
		{
			byEntity.World.PlaySoundAt(new AssetLocation("sounds/effect/toolbreak"), byEntity.Pos.X, byEntity.Pos.InternalY, byEntity.Pos.Z);
		}

		BlockEntity newBe = byEntity.World.BlockAccessor.GetBlockEntity(pos);
		if (fromFarmland)
		{
			if (newBe != null && prevData != null)
			{
				newBe.FromTreeAttributes(prevData, byEntity.World);
				newBe.MarkDirty(true);
			}
		}
		else if (newBe is BlockEntityFarmland bef)
		{
			// Soil to paddy: same nutrient seed as vanilla ItemHoe.
			bef.OnCreatedFromSoil(block, prevData);
		}

		byEntity.World.BlockAccessor.MarkBlockDirty(pos);
	}
}
