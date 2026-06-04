using System.Collections.Generic;

namespace RicePaddies;

// Each entry is the crop block code minus the stage suffix (e.g. "game:crop-rice" matches game:crop-rice-1 through -10).
public class RicePaddiesConfig
{
    public List<string> AllowedCrops { get; set; } = new List<string> { "game:crop-rice" };
}
