using Vintagestory.API.Common;

namespace RicePaddies;

public class RicePaddiesMod : ModSystem
{
    public override void Start(ICoreAPI api)
    {
        api.RegisterItemClass("ItemTakuwa",           typeof(ItemTakuwa));
        api.RegisterBlockEntityClass("Paddy",         typeof(BlockEntityPaddy));
        api.RegisterBlockClass("BlockPaddy",          typeof(BlockPaddy));
        api.RegisterBlockClass("BlockRicePaddyAware", typeof(BlockRicePaddyAware));
        api.RegisterBlockClass("BlockReedsPaddyAware",typeof(BlockReedsPaddyAware));
        api.RegisterBlockClass("BlockFruitingBushPaddyAware",        typeof(BlockFruitingBushPaddyAware));
        api.RegisterBlockClass("BlockFruitingBushCuttingPaddyAware", typeof(BlockFruitingBushCuttingPaddyAware));
        api.RegisterCropBehavior("PaddyOnly",         typeof(CropBehaviorPaddyOnly));
    }
}
