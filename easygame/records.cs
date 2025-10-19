using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

namespace easygame;

// 模组配置
record StimulatorConfig
{
    [JsonPropertyName("使用次数")]
    public int UseTimes {get; set;}
    [JsonPropertyName("价格倍率")]
    public double PriceModify {get; set;}
    [JsonPropertyName("单针剂重量")]
    public double Weight {get; set;}
}

record WhetherEnableFunction
{
    [JsonPropertyName("是否解除所有物品在跳蚤市场售卖限制")]
    public bool IsUnlockAllItemsSellLimit {get; set;}
    [JsonPropertyName("是否将迷宫地图显示于地图选择页面")]
    public bool ShowMapToChoiceScene {get; set;}
    [JsonPropertyName("是否启用新针剂")]
    public bool NewStimulator {get; set;}
    [JsonPropertyName("是否启用对针剂耐久的修改")]
    public bool StimulatorChange {get; set;}
    [JsonPropertyName("是否启用对实验室钥匙卡的修改")]
    public bool LabAccessChange {get; set;}
    [JsonPropertyName("是否自定义带入物品限制")]
    public bool EnterGameItemLimit {get; set;}
    [JsonPropertyName("是否修改血量与水分")]
    public bool EnergyHydrationModify {get; set;}
    [JsonPropertyName("是否修改战局时长")]
    public bool RaidTimeModify {get; set;}
    [JsonPropertyName("是否修改弹夹装单卸弹检查弹匣时间")]
    public bool AmmoTimeModify {get; set;}
    [JsonPropertyName("是否修改每级跳蚤市场挂单上限倍率")]
    public bool MaxActiveOfferCountModify {get; set;}
    [JsonPropertyName("是否调整实验室访问卡次数")]
    public bool AdjustLabsAccess {get; set;}
    [JsonPropertyName("是否修改所有药剂耐久(吗啡除外)")]
    public bool AdjustSimulatorMaxHpResource {get; set;}
    [JsonPropertyName("是否调整迷宫访问卡次数")]
    public bool AdjustLabysAccess {get; set;}
}

record ModConfigData {
    [JsonPropertyName("是否启用功能")]
    public WhetherEnableFunction? EnableFunction { get; set; }
    [JsonPropertyName("针剂")]
    public StimulatorConfig? StimulatorConfig { get; init; }
    [JsonPropertyName("带入对局物品限制")]
    public double EnterGameItemLimit {get; set;}
    [JsonPropertyName("所有存档血量倍率")]
    public double HealthModify {get; set;}
    [JsonPropertyName("所有存档能量与水分倍率")]
    public double EnergyHydrationModify {get; set;}
    [JsonPropertyName("战局时长倍率")]
    public double RaidTimeModify {get; set;}
    [JsonPropertyName("装弹时间倍率")]
    public double TakeInAmmoTimeModify {get; set;}
    [JsonPropertyName("卸弹时间倍率")]
    public double TakeOutAmmoTimeModify {get; set;}
    [JsonPropertyName("检查弹匣时间倍率")]
    public double CheckAmmoTimeModify {get; set;}
    [JsonPropertyName("每级跳蚤市场挂单上限倍率")]
    public double MaxActiveOfferCountModify {get; set;}
    [JsonPropertyName("输出修改结果日志")]
    public bool OutputResultLogOfAdjust {get; set;}
    [JsonPropertyName("实验室访问卡耐久")]
    public int LabsAccessMaximumNumberOfUsage {get; set;}
    [JsonPropertyName("迷宫访问卡耐久")]
    public int LabysAccessMaximumNumberOfUsage {get; set;}
}
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
}

// public class ItemProps
// {
//     [JsonPropertyName("Description")]
//     public string? Description { get; set; }
//     
//     [JsonPropertyName("LootExperience")]
//     public int LootExperience { get; set; }
//     
//     [JsonPropertyName("MaxHpResource")]
//     public int MaxHpResource { get; set; }
//     
//     [JsonPropertyName("Name")]
//     public string? Name { get; set; }
//     
//     [JsonPropertyName("ShortName")]
//     public string? ShortName { get; set; }
//     
//     [JsonPropertyName("StimulatorBuffs")]
//     public string? StimulatorBuffs { get; set; }
//     
//     [JsonPropertyName("Weight")]
//     public double Weight { get; set; }
//     
//     [JsonPropertyName("effects_damage")]
//     public EffectsDamage? EffectsDamage { get; set; }
//     
//     [JsonPropertyName("effects_health")]
//     public List<object>? EffectsHealth { get; set; }
//     
//     [JsonPropertyName("hpResourceRate")]
//     public int HpResourceRate { get; set; }
//     
//     [JsonPropertyName("medEffectType")]
//     public string? MedEffectType { get; set; }
//     
//     [JsonPropertyName("medUseTime")]
//     public int MedUseTime { get; set; }
// }
//
// public class EffectsDamage
// {
//     [JsonPropertyName("Pain")]
//     public EffectDamage? Pain { get; set; }
//     
//     [JsonPropertyName("Contusion")]
//     public EffectDamage? Contusion { get; set; }
//     
//     [JsonPropertyName("Fracture")]
//     public EffectDamage? Fracture { get; set; }
//     
//     [JsonPropertyName("HeavyBleeding")]
//     public EffectDamage? HeavyBleeding { get; set; }
//     
//     [JsonPropertyName("Intoxication")]
//     public EffectDamage? Intoxication { get; set; }
//     
//     [JsonPropertyName("LightBleeding")]
//     public EffectDamage? LightBleeding { get; set; }
//     
//     [JsonPropertyName("RadExposure")]
//     public EffectDamage? RadExposure { get; set; }
// }
//
// public class EffectDamage
// {
//     [JsonPropertyName("delay")]
//     public int Delay { get; set; }
//     
//     [JsonPropertyName("duration")]
//     public int Duration { get; set; }
//     
//     [JsonPropertyName("fadeOut")]
//     public int FadeOut { get; set; }
// }
