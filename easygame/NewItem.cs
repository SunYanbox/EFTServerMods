using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace easygame;

// 模组新物品
public class NewItem
{
    [JsonPropertyName("_id")]
    public string? Id { get; set; }
    [JsonPropertyName("_name")]
    public string? Name { get; set; }
    [JsonPropertyName("_parent")]
    public string? Parent { get; set; }
    [JsonPropertyName("_proto")]
    public string? Proto { get; set; }
    [JsonPropertyName("_type")]
    public string? Type { get; set; }
    [JsonPropertyName("_props")]
    public TemplateItemProperties? Props { get; set; }
    [JsonPropertyName("defaultTrader")]
    public MongoId? DefaultTrader { get; set; }
    
    [JsonPropertyName("itemTplToClone")]
    public MongoId? ItemTplToClone { get; set; }
    
    [JsonPropertyName("price")]
    public double? Price { get; set; }
    [JsonPropertyName("parentContainer")]
    public bool? ParentContainer { get; set; } // 是否使用父类的可放置容器筛选
}