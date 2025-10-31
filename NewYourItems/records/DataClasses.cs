using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.records;


/// <summary>
/// 新物品基础信息记录类
/// 包含物品的基本属性和元数据
/// </summary>
/// <remarks>
/// 属性列表:
/// - Id: 物品ID (必需) 
/// - Type: 模板类型，适合NewYourItem模组的数据类型 (可选) 
/// - Name: 物品名称 (可选) 
/// - Description: 物品描述 (可选) 
/// - Author: 作者名称 (可选) 
/// - License: 创建物品的协议 (可选) 
/// - Order: 影响新物品的创建顺序，数值越大加载越慢 (可选) 
/// - ParentId: 物品创建的ParentId (必需) 
/// - CloneId: 复制物品创建的原型Id (可选) 
/// - HandbookParentId: 复制物品创建的HandbookParentId (可选) 
/// - TraderId: 默认售卖该物品的商人Id (可选) 
/// - Price: 价格 (默认值: 1)
/// - Prefab: 物品模型 (可选)
/// - UsePrefab: 使用时的物品模型 (可选)
/// - CanSellOnRagfair: 是否允许在跳蚤市场售卖 (默认值: true)
/// 
/// - IsHadInit: 是否已进行过初始化与参数验证 (内部使用)
/// </remarks>
public record BaseInfo: AbstractInfo
{
    [JsonIgnore] public new static bool ShouldUpdateDatabaseService => false;
    /// <summary>
    /// 物品ID
    /// </summary>
    [JsonPropertyName("id")]
    public string? Id { get; set; }
    
    /// <summary>
    /// 模板类型(此处为适合NewYourItem模组的数据类型)  [可缺省]
    /// </summary>
    [JsonPropertyName("type")]
    public string? Type { get; set; }

    /// <summary>
    /// 物品名称  [可缺省]
    /// </summary>
    [JsonPropertyName("name")]
    public string? Name { get; set; }

    /// <summary>
    /// 物品描述  [可缺省]
    /// </summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }
    
    /// <summary>
    /// 作者名称  [可缺省]
    /// </summary>
    [JsonPropertyName("author")]
    public string? Author { get; set; }
    
    /// <summary>
    /// 创建的这个物品的协议  [可缺省]
    /// </summary>
    [JsonPropertyName("license")]
    public string? License { get; set; }
    
    /// <summary>
    /// 影响新物品的创建顺序, 数值越大加载越慢  [可缺省]
    /// </summary>
    [JsonPropertyName("order")]
    public int? Order { get; set; }
    
    /// <summary>
    /// 物品创建的ParentId
    /// </summary>
    [JsonPropertyName("parentId")]
    public string? ParentId { get; set; }
    
    /// <summary>
    /// 复制物品创建的原型Id  [可缺省]
    /// </summary>
    [JsonPropertyName("cloneId")]
    public string? CloneId { get; set; }
    
    /// <summary>
    /// 复制物品创建的HandbookParentId  [可缺省]
    /// </summary>
    [JsonPropertyName("handbookParentId")]
    public string? HandbookParentId { get; set; }
    
    /// <summary>
    /// 默认售卖该物品的商人Id  [可缺省]
    /// </summary>
    [JsonPropertyName("traderId")]
    public string? TraderId { get; set; }

    /// <summary>
    /// 价格
    /// </summary>
    [JsonPropertyName("price")]
    public double Price { get; set; } = 0;
    
    /// <summary>
    /// 物品模型
    /// </summary>
    [JsonPropertyName("prefab")]
    public Prefab? Prefab { get; set; }
    
    /// <summary>
    /// 使用物品模型
    /// </summary>
    [JsonPropertyName("usePrefab")]
    public Prefab? UsePrefab { get; set; }
    
    /// <summary>
    /// 是否允许在跳蚤市场售卖 默认为true
    /// </summary>
    [JsonPropertyName("CanSellOnRagfair")]
    public bool CanSellOnRagfair { get; set; } = true;

    /// <summary>
    /// 是否已进行过初始化与参数验证
    /// </summary>
    [JsonIgnore] public bool IsHadInit { get; set; } = false;
    [JsonIgnore] public string? ItemPath { get; set; }

    public override void UpdateProperties(TemplateItemProperties properties)
    {
        if (!string.IsNullOrEmpty(Name)) properties.Name = Name;
        if (!string.IsNullOrEmpty(Name)) properties.ShortName = Name;
        if (!string.IsNullOrEmpty(Description)) properties.Description = Description;
        properties.CanSellOnRagfair = CanSellOnRagfair;
        try
        {
            if (Prefab != null)
            {
                if (string.IsNullOrEmpty(Prefab.Path))
                {
                    LocalLog?.LocalLogMsg(LocalLogType.Warn, $"物品{Name}的Prefab.Path为空, 请检查{ItemPath}");
                }
                properties.Prefab = Prefab;
            }
            if (UsePrefab != null)
            {
                if (string.IsNullOrEmpty(UsePrefab.Path))
                {
                    LocalLog?.LocalLogMsg(LocalLogType.Warn, $"物品{Name}的UsePrefab.Path为空, 请检查{ItemPath}");
                }
                properties.UsePrefab = UsePrefab;
            }
        }
        catch (Exception e)
        {
            LocalLog?.LocalLogMsg(LocalLogType.Error, $"物品{Name}的Prefab或UsePrefab语法错误, 无法解析 - {ItemPath} - {e.Message}");
        }
    }
}

public record AttributeInfo: AbstractInfo
{
    [JsonIgnore] public new static bool ShouldUpdateDatabaseService => false;
    [JsonPropertyName("Weight")]
    public double? Weight { get; set; }
    [JsonPropertyName("width")]
    public int? Width { get; set; }
    [JsonPropertyName("height")]
    public int? Height { get; set; }
    [JsonPropertyName("RarityPvE")]
    public string? RarityPvE { get; set; }
    [JsonPropertyName("DiscardLimit")]
    public double? DiscardLimit { get; set; }
    /// <summary>
    /// 默认为"generic"
    /// </summary>
    [JsonPropertyName("ItemSound")]
    public string? ItemSound { get; set; }
    // 检视与经验相关
    [JsonPropertyName("StackMaxSize")]
    public int? StackMaxSize { get; set; } = 1;
    
    [JsonPropertyName("ExaminedByDefault")]
    public bool? ExaminedByDefault { get; set; } = true;
    
    [JsonPropertyName("ExamineTime")]
    public double? ExamineTime { get; set; }
    
    [JsonPropertyName("LootExperience")]
    public int? LootExperience { get; set; }
    
    [JsonPropertyName("ExamineExperience")]
    public int? ExamineExperience { get; set; }

    public override void UpdateProperties(TemplateItemProperties properties)
    {
        if (Weight != null && Weight >= 0) properties.Weight = Weight;
        if (Width != null && Width >= 0) properties.Width = Width;
        if (Height != null && Height >= 0) properties.Height = Height;
        if (DiscardLimit != null) properties.DiscardLimit = DiscardLimit;
        if (ExamineTime != null) properties.ExamineTime = ExamineTime;
        if (LootExperience != null) properties.LootExperience = LootExperience;
        if (ExamineExperience != null) properties.ExamineExperience = ExamineExperience;
        if (StackMaxSize != null) properties.StackMaxSize = StackMaxSize;
        if (ExaminedByDefault != null) properties.ExaminedByDefault = ExaminedByDefault;
        if (RarityPvE != null)
        {
            string? rarity = ItemRarityData.GetRarityKey(RarityPvE);
            if (!string.IsNullOrEmpty(rarity)) properties.RarityPvE = rarity;
        }
        if (ItemSound != null)
        {
            string? itemSound = ItemSoundData.GetItemSoundKey(ItemSound);
            if (!string.IsNullOrEmpty(itemSound)) properties.ItemSound = itemSound;
        }
    }
}

public record BuffsInfo : AbstractInfo
{
    [JsonIgnore] public new static bool ShouldUpdateDatabaseService => true;
    
    [JsonPropertyName("stimulatorBuffs")]
    public string? StimulatorBuffs { get; set; }
    [JsonPropertyName("buffs")]
    public List<Buff>? Buffs {get; set;}
    
    public override void UpdateProperties(TemplateItemProperties properties)
    {
        if (!string.IsNullOrEmpty(StimulatorBuffs))
            properties.StimulatorBuffs = StimulatorBuffs;
    }
    
    public override void UpdateDatabaseService(DatabaseService databaseService)
    {
        var buffs = databaseService.GetTables().Globals.Configuration.Health.Effects.Stimulator.Buffs;
        if (StimulatorBuffs != null && Buffs != null)
        {
            buffs[StimulatorBuffs] = Buffs;
        }
    }
}

public record DrinkDrugInfo: AbstractInfo
{
    [JsonIgnore] public new static bool ShouldUpdateDatabaseService => false;
    [JsonPropertyName("foodUseTime")]
    public double? FoodUseTime { get; set; }
    [JsonPropertyName("hydration")]
    public double? Hydration { get; set; }
    [JsonPropertyName("energy")]
    public double? Energy { get; set; }
    [JsonPropertyName("maxResource")]
    public int? MaxResource { get; set; }

    public Dictionary<HealthFactor, EffectsHealthProperties> GetEffectsHealth()
    {
        return new Dictionary<HealthFactor, EffectsHealthProperties>
        {
            { HealthFactor.Energy, new EffectsHealthProperties { Value = Energy } },
            { HealthFactor.Hydration, new EffectsHealthProperties { Value = Hydration } },
        };
    }

    public override void UpdateProperties(TemplateItemProperties properties)
    {
        if (MaxResource != null && MaxResource >= 1) properties.MaxResource = MaxResource;
        if (FoodUseTime != null && FoodUseTime >= 0) properties.FoodUseTime = FoodUseTime;
        if (Hydration != null || Energy != null)
            properties.EffectsHealth = GetEffectsHealth();
    }

}
