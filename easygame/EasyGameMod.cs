using System.Diagnostics;
using System.Reflection;
using easygame.Configs;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Services;
using SuntionCore.Services.LogUtils;
using Locations = SPTarkov.Server.Core.Models.Spt.Server.Locations;
using Path = System.IO.Path;

namespace easygame;

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)] // 确保全物品商人能卖新的针剂
public class EasyGameMod(
    ModHelper modHelper,
    DatabaseService databaseService,
    ISptLogger<EasyGameMod> sptLogger,
    ItemHelper itemHelper) : IOnLoad
{
    private ModConfigData? _modConfigData;
    public static readonly ModLogger ModLogger = ModLogger.GetOrCreateLogger(nameof(EasyGameMod), logFileMaxSize: 120 * 1024);

    protected readonly Dictionary<string, ModTask> TasksDictionary = new();

    public void Info(string msg) => sptLogger.Info(ModLogger.Info(msg));

    public void Register(ModTask modTask) => TasksDictionary.Add(modTask.Name, modTask);

    public Task OnLoad()
    {
        LoadDataBase();

        InitModTasks();

        HandleModTasks();
        
        Info("模组加载完毕");
        return Task.CompletedTask;
    }

    public void LoadDataBase()
    {
        string pathToModData = Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "data");

        TryCatch("加载模组配置信息", () =>
        {
            _modConfigData = modHelper.GetJsonDataFromFile<ModConfigData>(pathToModData, "config.json");
            return Task.CompletedTask;
        });
    }

    public void InitModTasks()
    {
        Register(new ModTask
        {
            Callback = AdjustMaxInRaidAndLobby,
            Order = 10_0000,
            Condition = () => _modConfigData?.EnableFunction?.EnterGameItemLimit ?? false,
            Name = "根据自定义数据调整带入物品限制",
        });
        Register(new ModTask
        {
            Callback = ShowLabyrinthInChoiceMenu,
            Order = 1,
            Condition = () => _modConfigData?.EnableFunction?.ShowMapToChoiceScene ?? false,
            Name = "使得迷宫地图显示在地图选择界面",
        });
        Register(new ModTask
        {
            Callback = RemoveRestrictionOnSellingItemsInFlea,
            Order = 50_0000,
            Condition = () => _modConfigData?.EnableFunction?.IsUnlockAllItemsSellLimit ?? false,
            Name = "解除物品在跳蚤售卖限制",
        });
        Register(new ModTask
        {
            Callback = EnergyHydrationModify,
            Order = 1,
            Condition = () => _modConfigData?.EnableFunction?.EnergyHydrationModify ?? false,
            Name = "调整所有存档的基础血量与能量, 水分",
        });
        Register(new ModTask
        {
            Callback = RaidTimeAdjust,
            Order = 1,
            Condition = () => _modConfigData?.EnableFunction?.RaidTimeModify ?? false,
            Name = "战局时长修改",
        });
        Register(new ModTask
        {
            Callback = MagazineDataModification,
            Order = 5000,
            Condition = () => _modConfigData?.EnableFunction?.AmmoTimeModify ?? false,
            Name = "弹夹装单卸弹检查弹匣时间修改",
        });
        Register(new ModTask
        {
            Callback = FleaPendingOrderLimitModification,
            Order = 1,
            Condition = () => _modConfigData?.EnableFunction?.MaxActiveOfferCountModify ?? false,
            Name = "每级跳蚤市场挂单上限倍率",
        });
        Register(new ModTask
        {
            Callback = AdjustLabsAccess,
            Order = 10,
            Condition = () => _modConfigData?.EnableFunction?.AdjustLabsAccess ?? false,
            Name = "调整实验室访问卡次数",
        });
        Register(new ModTask
        {
            Callback = AdjustSimulator,
            Order = 500,
            Condition = () => _modConfigData?.EnableFunction?.AdjustSimulatorMaxHpResource ?? false,
            Name = "修改所有药剂耐久(吗啡除外)",
        });
        Register(new ModTask
        {
            Callback = AdjustLabysAccess,
            Order = 10,
            Condition = () => _modConfigData?.EnableFunction?.AdjustLabysAccess ?? false,
            Name = "调整迷宫访问卡次数",
        });
        Register(new ModTask
        {
            Callback = AdjustAmmoStackMaxSize,
            Order = 10,
            Condition = () => _modConfigData?.EnableFunction?.AdjustAmmoStack ?? false,
            Name = "修改所有弹药堆叠(Max规则)",
        });
        Register(new ModTask
        {
            Callback = AllExaminedByDefault,
            Order = 10000,
            Condition = () => _modConfigData?.EnableFunction?.AllExaminedByDefault ?? false,
            Name = "默认检视所有物品",
        });
    }
    
    public void HandleModTasks()
    {
        foreach (KeyValuePair<string, ModTask> modTask in TasksDictionary.OrderBy(x => x.Value.Order).ToList())
        {
            string name = modTask.Key;
            Task.Run(() =>
            {
                TryCatch($"{name}", () =>
                {
                    if (modTask.Value.Condition())
                    {
                        modTask.Value.Callback();
                        ModLogger.Info($"[{name}]: 已完成");
                    }
                    else
                    {
                        ModLogger.Info($"[{name}]: 已跳过");
                    }

                    return Task.CompletedTask;
                });
            }).Wait();
        }
    }

    /// <summary>
    /// 根据自定义数据调整带入物品限制
    /// </summary>
    public void AdjustMaxInRaidAndLobby()
    {
        foreach (RestrictionsInRaid dataItem in databaseService.GetTables().Globals.Configuration.RestrictionsInRaid)
        {
            dataItem.MaxInRaid = Math.Max(dataItem.MaxInRaid, _modConfigData?.EnterGameItemLimit ?? dataItem.MaxInRaid);
            dataItem.MaxInLobby =
                Math.Max(dataItem.MaxInLobby, _modConfigData?.EnterGameItemLimit ?? dataItem.MaxInLobby);
        }
        ModLogger.Info("已成功设置物品带入对局限制");
    }

    /// <summary>
    /// 使得迷宫地图显示在地图选择界面
    /// </summary>
    public void ShowLabyrinthInChoiceMenu()
    {
        Location mapLabyrinth = databaseService.GetTables().Locations.Labyrinth;
        mapLabyrinth.Base.Enabled = true;
        mapLabyrinth.Base.ForceOnlineRaidInPVE = false;
        ModLogger.Info("已成功设置迷宫地图显示在地图选择界面");
    }

    /// <summary>
    /// 解除物品在跳蚤售卖限制
    /// </summary>
    public void RemoveRestrictionOnSellingItemsInFlea()
    {
        List<MongoId> noProperties = [];
        KeyValuePair<MongoId, TemplateItem>[] existCanSellOnRagfair = databaseService.GetItems().Where(
            x => x.Value.Properties != null && x.Value.Properties.CanSellOnRagfair == false).ToArray();
        foreach ((MongoId tpl, TemplateItem item) in existCanSellOnRagfair)
        {
            if (item.Properties == null)
            {
                noProperties.Add(tpl);
                continue;
            }
            item.Properties.CanSellOnRagfair = true;
        }
        
        if (noProperties.Count != 0)
        {
            ModLogger.Error("修改物品在跳蚤市场售卖限制时这些物品模板没有Properties属性信息:\n\t - " + string.Join("\n\t - ", noProperties));
        }

        ModLogger.Info($"解除物品在跳蚤售卖限制成功率: {1.0d - (double)noProperties.Count / existCanSellOnRagfair.Length:P3}");
    }

    /// <summary>
    /// 所有存档的基础血量与能量, 水分
    /// </summary>
    public void EnergyHydrationModify()
    {
        List<string> successChange = [];
        foreach ((string profileName, ProfileSides profileSides) in databaseService.GetProfileTemplates())
        {
            // 血量修改
            foreach (TemplateSide? side in new[] { profileSides.Bear, profileSides.Usec })
            {
                if (side?.Character?.Health?.BodyParts == null) continue;
                foreach (var (_, bodyPartHealth) in side.Character.Health.BodyParts)
                {
                    if (bodyPartHealth?.Health?.Maximum != null)
                    {
                        bodyPartHealth.Health.Maximum *= _modConfigData.HealthModify;
                    }
                }
                if (side.Character.Health?.Hydration?.Maximum == null ||
                    side.Character.Health?.Energy?.Maximum == null)
                    continue;
                side.Character.Health.Energy.Maximum *= _modConfigData.EnergyHydrationModify;
                side.Character.Health.Hydration.Maximum *= _modConfigData.EnergyHydrationModify;
            }
            successChange.Add(profileName);
        }

        ModLogger.Debug("成功修改的存档信息: " + string.Join(", ", successChange));
    }

    /// <summary>
    /// 战局时长修改
    /// </summary>
    public void RaidTimeAdjust()
    {
        Locations locations = databaseService.GetLocations();
        List<string> mapChangeResults = [];
        foreach ((string mapName, Location location) in locations.GetDictionary())
        {
            if (location.Base?.EscapeTimeLimit == null) continue;
            double oldTimeLimit = location.Base.EscapeTimeLimit.Value;
            location.Base.EscapeTimeLimit *= _modConfigData.RaidTimeModify;
            location.Base.EscapeTimeLimit = Math.Max(1, location.Base.EscapeTimeLimit ?? 0);
            mapChangeResults.Add($"地图{mapName}的对局时间已修改: {oldTimeLimit}min -> {location.Base.EscapeTimeLimit ?? -1}min");
        }

        ModLogger.Info("地图持续时间修改结果:\n\t - " + string.Join("\n\t - ", mapChangeResults));
    }

    /// <summary>
    /// 弹夹数据修改
    /// </summary>
    public void MagazineDataModification()
    {
        Globals globals = databaseService.GetGlobals();
        globals.Configuration.BaseCheckTime *= _modConfigData.CheckAmmoTimeModify;
        globals.Configuration.BaseLoadTime *= _modConfigData.TakeInAmmoTimeModify;
        globals.Configuration.BaseUnloadTime *= _modConfigData.TakeOutAmmoTimeModify;
        ModLogger.Info($"弹夹相关修改结果: 装弹({globals.Configuration.BaseLoadTime}s) 卸弹({globals.Configuration.BaseUnloadTime}s) 检查({globals.Configuration.BaseCheckTime}s)");
    }

    /// <summary>
    /// 跳蚤挂单上限修改
    /// </summary>
    public void FleaPendingOrderLimitModification()
    {
        Globals globals = databaseService.GetGlobals();
        const string result = "跳蚤挂单上限修改结果(特刊计数->挂单数量): ";
        List<string> countChange = [];
        foreach (MaxActiveOfferCount offer in globals.Configuration.RagFair.MaxActiveOfferCount)
        {
            offer.Count = Math.Max(1, offer.Count);
            offer.Count *= _modConfigData.MaxActiveOfferCountModify;
            countChange.Add($"{offer.CountForSpecialEditions} -> {offer.Count}");
        }
        ModLogger.Debug(result + string.Join(", ", countChange));
    }
    
    /// <summary>
    /// 修改所有药剂耐久(吗啡除外)
    /// </summary>
    public void AdjustSimulator()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        Dictionary<MongoId,double> itemPrices = databaseService.GetTables().Templates.Prices;
        KeyValuePair<MongoId,TemplateItem>[] simulatorItems = itemTempaltes.Where(kvp => kvp.Value.Parent.ToString() == Constant.SimBase && kvp.Value.Id.ToString() != ItemTpl.DRUGS_MORPHINE_INJECTOR).ToArray();
        ModLogger.Debug($"获取到的针剂数量有: {simulatorItems.Length}个");
        List<MongoId> noProperties = [];
        List<MongoId> noWeightOrMaxHpResource = [];
        
        // 修改除了吗啡以外的药剂耐久
        foreach ((MongoId mongoId, TemplateItem templateItem) in simulatorItems)
        {
            if (templateItem.Properties == null)
            {
                noProperties.Add(mongoId);
                continue;
            }
            if (templateItem.Properties.MaxHpResource == null || templateItem.Properties.Weight == null)
            {
                noWeightOrMaxHpResource.Add(mongoId);
                continue;
            }
            templateItem.Properties.MaxHpResource = _modConfigData?.StimulatorConfig?.UseTimes ?? 1;
            templateItem.Properties.Weight = _modConfigData?.StimulatorConfig?.Weight ?? 0.05;
            itemPrices.TryAdd(mongoId, 0);
            itemPrices[mongoId] *= _modConfigData?.StimulatorConfig?.PriceModify ?? 1;
        }
        
        if (noProperties.Count > 0)
        {
            ModLogger.Error("修改针剂堆叠与质量时这些物品模板没有Properties属性信息:\n\t - " + string.Join("\n\t - ", noProperties));
        }
        
        if (noWeightOrMaxHpResource.Count > 0)
        {
            ModLogger.Error("修改针剂堆叠与质量时这些物品模板没有质量属性(一般大于0)与针剂使用次数属性(默认 1):\n\t - " + string.Join("\n\t - ", noWeightOrMaxHpResource));
        }

        ModLogger.Info($"修改针剂使用次数与质量成功率: {
            (double)(simulatorItems.Length - noProperties.Count - noWeightOrMaxHpResource.Count) 
            / simulatorItems.Length:P3}");
    }

    public void AllExaminedByDefault()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        KeyValuePair<MongoId,TemplateItem>[] items = itemTempaltes.Where(kvp => kvp.Value.Properties?.ExaminedByDefault == false).ToArray();
        ModLogger.Debug($"默认未检视物品有: {items.Length}个");
        List<MongoId> noProperties = [];
        foreach ((MongoId tpl, TemplateItem templateItem) in items)
        {
            if (templateItem.Properties == null)
            {
                noProperties.Add(tpl);
                continue;
            }
            templateItem.Properties.ExaminedByDefault = true;
        }

        if (noProperties.Count != 0)
        {
            ModLogger.Error("修改默认检视时这些物品模板没有Properties属性信息:\n\t - " + string.Join("\n\t - ", noProperties));
        }

        ModLogger.Info($"设置物品默认检视成功率: {1.0d - (double)noProperties.Count / items.Length:P3}");
    }
    
    /// <summary>
    /// 修改所有弹药堆叠
    /// </summary>
    public void AdjustAmmoStackMaxSize()
    {
        Dictionary<MongoId, TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        MongoId[] ammo = itemHelper.GetItemTplsOfBaseType(BaseClasses.AMMO.ToString()).ToArray();
        ModLogger.Debug($"准备修改弹药堆叠时获取到弹药类型: {ammo.Length}个");
        List<MongoId> cantFound = [];
        List<string> successChange = [];
        foreach (MongoId tpl in ammo)
        {
            if (!itemTempaltes.TryGetValue(tpl, out TemplateItem? templateItem))
            {
                cantFound.Add(tpl);
                continue;
            }
            if (templateItem.Properties != null && templateItem.Properties.StackMaxSize != null)
            {
                int beforeSize = templateItem.Properties.StackMaxSize.Value;
                templateItem.Properties.StackMaxSize = Math.Max(_modConfigData?.AmmoStack ?? 0, templateItem.Properties.StackMaxSize ?? 1);
                successChange.Add($"{templateItem.Name}({tpl}): {beforeSize} -> {templateItem.Properties.StackMaxSize}");
            }
        }

        if (cantFound.Count > 0)
        {
            ModLogger.Error("修改弹药堆叠时这些弹药未在数据库找到物品模板信息:\n\t - " + string.Join("\n\t - ", cantFound));
        }

        ModLogger.Debug("成功修改的弹药堆叠信息:\n\t - " + string.Join("\n\t - ", successChange) + $"\n\t * 成功率: {(double)successChange.Count / ammo.Length:P3}");
    }

    /// <summary>
    /// 调整实验室访问卡次数
    /// </summary>
    public void AdjustLabsAccess()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        TemplateItem templateItem = itemTempaltes[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS];
        ModLogger.Debug($"已获取到实验室访问卡: {templateItem.Id}, {templateItem.Name}, 可用次数: {templateItem.Properties?.MaximumNumberOfUsage}");
        if (templateItem.Properties != null)
        {
            templateItem.Properties.MaximumNumberOfUsage =
                Math.Max(templateItem.Properties.MaximumNumberOfUsage ?? 1,
                    _modConfigData?.LabsAccessMaximumNumberOfUsage ?? 1);
            ModLogger.Info($"已成功设置实验室访问卡次数为: {templateItem.Properties.MaximumNumberOfUsage}");
        }
    }
    
    /// <summary>
    /// 调整迷宫访问卡次数
    /// </summary>
    public void AdjustLabysAccess()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        TemplateItem templateItem = itemTempaltes[ItemTpl.KEYCARD_LABRYS_ACCESS];
        ModLogger.Debug($"已获取到迷宫访问卡: {templateItem.Id}, {templateItem.Name}, 可用次数: {templateItem.Properties?.MaximumNumberOfUsage}");
        if (templateItem.Properties != null)
        {
            templateItem.Properties.MaximumNumberOfUsage =
                Math.Max(templateItem.Properties.MaximumNumberOfUsage ?? 1,
                    _modConfigData?.LabysAccessMaximumNumberOfUsage ?? 1);
            ModLogger.Info($"已成功设置迷宫访问卡次数为: {templateItem.Properties.MaximumNumberOfUsage}");
        }
    }
    
    public void TryCatch(string name, Func<Task> func)
    {
        Stopwatch stopwatch = new();
        try
        {
            stopwatch.Start();
            func();
            stopwatch.Stop();
            ModLogger.Debug($"任务<{name}>执行成功, 耗时{stopwatch.Elapsed.TotalMilliseconds:F3} ms");
        }
        catch (Exception e)
        {
            sptLogger.Error(ModLogger.Error($"任务<{name}>执行失败: {e.Message}, {e.StackTrace}"), e);
            // throw;
        }
    }
}