using System.Text.Json.Serialization;

namespace easygame.Configs;

// 模组配置
internal record StimulatorConfig
{
    [JsonPropertyName("使用次数")]
    public int UseTimes {get; set;}
    [JsonPropertyName("价格倍率")]
    public double PriceModify {get; set;}
    [JsonPropertyName("单针剂重量")]
    public double Weight {get; set;}
}