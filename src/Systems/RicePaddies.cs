using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Server;

namespace RicePaddies;

public class RicePaddiesMod : ModSystem
{
    // Populated server-side from ricepaddies.json; read by BlockEntityPaddy for crop gating.
    public static HashSet<string> AllowedCrops = new HashSet<string> { "game:crop-rice" };

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

    public override void StartServerSide(ICoreServerAPI api)
    {
        RicePaddiesConfig config;
        try { config = api.LoadModConfig<RicePaddiesConfig>("ricepaddies.json") ?? new RicePaddiesConfig(); }
        catch { config = new RicePaddiesConfig(); }
        api.StoreModConfig(config, "ricepaddies.json");
        AllowedCrops = new HashSet<string>(config.AllowedCrops);
    }
}
