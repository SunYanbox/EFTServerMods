using SPTarkov.Server.Core.Models.Spt.Mod;

namespace allgoodstrader;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.allgoodstrader";
    public override string Name { get; init; } = "All Goods Trader";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; } = [];
    public override SemanticVersioning.Version Version { get; init; } = new("0.4.3");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.4");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/SunYanbox/EFTServerMods/tree/master/allgoodstrader";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "CC-BY-SA";
}