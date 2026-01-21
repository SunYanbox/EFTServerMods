using System.Text.Json.Serialization;

namespace easygame.Configs;

internal record WhetherEnableFunction
{
    [JsonPropertyName("是否解除所有物品在跳蚤市场售卖限制")]
    public bool IsUnlockAllItemsSellLimit {get; set;}
    [JsonPropertyName("是否将迷宫地图显示于地图选择页面")]
    public bool ShowMapToChoiceScene {get; set;}
    [JsonPropertyName("是否启用新针剂")]
    public bool NewStimulator {get; set;}
    [JsonPropertyName("是否启用新物品")]
    public bool NewItems {get; set;}
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
    [JsonPropertyName("是否调整迷宫访问卡次数")]
    public bool AdjustLabysAccess {get; set;}
    [JsonPropertyName("是否修改所有药剂耐久(吗啡除外)")]
    public bool AdjustSimulatorMaxHpResource {get; set;}
    [JsonPropertyName("是否调整弹药堆叠")]
    public bool AdjustAmmoStack {get; set;}
    [JsonPropertyName("是否令所有物品默认已检视")]
    public bool AllExaminedByDefault {get; set;}
}