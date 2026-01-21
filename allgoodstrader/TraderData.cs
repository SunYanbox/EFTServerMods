using System.Text.Json.Serialization;
// ReSharper disable ClassNeverInstantiated.Global

namespace allgoodstrader;

public record TraderData
{
    [JsonPropertyName("id")]
    public virtual string? Id { get; set; }
    [JsonPropertyName("name")]
    public virtual string? Name { get; set; }
    [JsonPropertyName("location")]
    public virtual string? Location { get; set; }
    [JsonPropertyName("res_name")]
    public virtual string? ResName { get; set; }
    [JsonPropertyName("buyPriceCoef")]
    public virtual int? BuyPriceCoef { get; set; }
    [JsonPropertyName("repairPriceCoef")]
    public virtual int? RepairPriceCoef { get; set; }
    [JsonPropertyName("insurancePriceCoef")]
    public virtual int? InsurancePriceCoef { get; set; }
}