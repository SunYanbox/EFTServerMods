using System.Text.Json.Serialization;

namespace easygame.Configs;

internal record ModConfigData {
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
    [JsonPropertyName("弹药堆叠")]
    public int AmmoStack {get; set;}
}