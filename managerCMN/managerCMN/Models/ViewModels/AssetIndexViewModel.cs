using managerCMN.Models.Entities;

namespace managerCMN.Models.ViewModels;

public class AssetIndexViewModel
{
    public IEnumerable<Asset> Assets { get; set; } = new List<Asset>();
    public AssetFilterViewModel Filter { get; set; } = new AssetFilterViewModel();
}