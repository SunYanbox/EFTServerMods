using System.Text.Json.Serialization;

namespace allgoodstrader;

record ModConfig
{
    [JsonPropertyName("priceModify")]
    public virtual double? PriceModify { get; set; }
}