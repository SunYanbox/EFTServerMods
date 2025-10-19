using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Services.Mod;
using Path = System.IO.Path;

namespace easygame;


public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.easygame";
    public override string Name { get; init; } = "EasyGame";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; } = [];
    public override SemanticVersioning.Version Version { get; init; } = new("0.2.5");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "CC-BY-SA";
}


public static class Constant
{
    public const string ModName = "EasyGameMod";

    // 针剂基类
    public const string SimBase = "5448f3a64bdc2d60728b456a";

    // 新添针剂
    public const string SimMengGong = "5f9d9b8e6f8b4a1e3c7d5a2b";
    public const string SimTiLi = "5f9d9b8e6f8b4a1e3c7d5a2c";
    public const string SimFuZhong = "5f9d9b8e6f8b4a1e3c7d5a2d";
    public const string SimJueDi = "5f9d9b8e6f8b4a1e3c7d5a2e";
    public const string SimYongJie = "5f9d9b8e6f8b4a1e3c7d5a30";

    // 商人ID
    public static readonly MongoId TherapistId = new MongoId("54cb57776803fa99248b456e");
    public static readonly MongoId RebId = new MongoId("5449016a4bdc2d6f028b456f");
    
    // 猛攻: 黄(Propital); 体力: 蓝(SJ6); 负重: MULE; 绝地: 红(SJ1); 永劫轮回: 紫(Zagustin)

    public static readonly Dictionary<string, MongoId> ItemTplToClones = new Dictionary<string, MongoId>
    {
        { SimMengGong, ItemTpl.STIM_PROPITAL_REGENERATIVE_STIMULANT_INJECTOR },
        { SimTiLi, ItemTpl.STIM_SJ6_TGLABS_COMBAT_STIMULANT_INJECTOR },
        { SimFuZhong, ItemTpl.STIM_MULE_STIMULANT_INJECTOR },
        { SimJueDi, ItemTpl.STIM_SJ1_TGLABS_COMBAT_STIMULANT_INJECTOR },
        { SimYongJie, ItemTpl.STIM_ZAGUSTIN_HEMOSTATIC_DRUG_INJECTOR }
    };

    public static readonly Dictionary<string, double> ItemPrices = new Dictionary<string, double>
    {
        { SimMengGong, 26e4 },
        { SimTiLi, 17e4 },
        { SimFuZhong, 25e4 },
        { SimJueDi, 35e4 },
        { SimYongJie, 335503.36 }
    };
}

public struct ModTask
{
    public string Name { get; set; }
    public int Order { get; set; }
    public Func<bool> Condition { get; set; }
    public Action Callback { get; set; }
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)] // 确保全物品商人能卖新的针剂
public class EasyGameMod(
    ModHelper modHelper,
    ISptLogger<EasyGameMod> logger,
    DatabaseService databaseService,
    ConfigServer configServer,
    CustomItemService customItemService) : IOnLoad
{
    private ModConfigData? _modConfigData;
    private Dictionary<MongoId, NewItem> _newItems = new();
    private Dictionary<string, List<Buff>> _newEffects = new();

    protected Dictionary<string, ModTask> TasksDictionary = new Dictionary<string, ModTask>();

    public void Info(string msg) => logger.Info($"[{Constant.ModName}] {msg}");
    public void Error(string msg) => logger.Error($"[{Constant.ModName}] {msg}");

    public void Register(ModTask modTask) => TasksDictionary.Add(modTask.Name, modTask);

    // 我们通过调用 GetConfig<>()获取配置，并在菱形 <> 括弧内传递配置的 "类型"。
    // 这些字段以 _ 开头，当你在代码中创建私有字段时，这是一个很好的约定。
    // 它们也是只读的，因为你不应该覆盖配置，而只是编辑其中的值
    private readonly BotConfig _botConfig = configServer.GetConfig<BotConfig>();
    private readonly HideoutConfig _hideoutConfig = configServer.GetConfig<HideoutConfig>();
    private readonly WeatherConfig _weatherConfig = configServer.GetConfig<WeatherConfig>();
    private readonly AirdropConfig _airdropConfig = configServer.GetConfig<AirdropConfig>();
    private readonly PmcChatResponse _pmcChatResponseConfig = configServer.GetConfig<PmcChatResponse>();
    private readonly QuestConfig _questConfig = configServer.GetConfig<QuestConfig>();

    private readonly PmcConfig _pmcConfig = configServer.GetConfig<PmcConfig>();

    // private readonly BackupConfig _backupConfig = configServer.GetConfig<BackupConfig>();
    private readonly InRaidConfig _raidConfig = configServer.GetConfig<InRaidConfig>();


    public Task OnLoad()
    {
        LoadDataBase();

        InitModTasks();

        HandleModTasks();
        
        logger.Info($"[{Constant.ModName}] 模组加载完毕");
        return Task.CompletedTask;
    }

    public void LoadDataBase()
    {
        var pathToMod = Path.Combine(modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly()), "data");

        TryCatch("加载模组配置信息", () =>
        {
            _modConfigData = modHelper.GetJsonDataFromFile<ModConfigData>(pathToMod, "config.json");
            return Task.CompletedTask;
        });
        TryCatch("加载新物品信息", () =>
        {
            _newItems = modHelper.GetJsonDataFromFile<Dictionary<MongoId, NewItem>>(pathToMod, "items.json");
            return Task.CompletedTask;
        });
        TryCatch("加载新针剂效果信息", () =>
        {
            _newEffects = modHelper.GetJsonDataFromFile<Dictionary<string, List<Buff>>>(pathToMod, "effects.json");
            return Task.CompletedTask;
        });
    }

    public void InitModTasks()
    {
        Register(new ModTask
        {
            Callback = AddNewSimulatorItem,
            Order = 5000,
            Condition = () => _modConfigData?.EnableFunction?.NewStimulator ?? false,
            Name = "添加新的强力针剂",
        });
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
    }
    
    public void HandleModTasks()
    {
        foreach (var modTask in TasksDictionary.OrderBy(x => x.Value.Order).ToList())
        {
            string name = modTask.Key;
            Task.Run(() =>
            {
                TryCatch($"{name}", () =>
                {
                    if (modTask.Value.Condition())
                    {
                        modTask.Value.Callback();
                        Info($" {DateTime.Now:MM-dd HH:mm:ss:fff} [{name}]: 已完成");
                    }
                    else
                    {
                        Info($" [{name}]: 已跳过");
                    }

                    return Task.CompletedTask;
                });
            }).Wait();
        }
    }
    
    /// <summary>
    /// 添加新的强力针剂
    /// </summary>
    public void AddNewSimulatorItem()
    {
        if (_modConfigData?.EnableFunction == null) return;
        if (!_modConfigData.EnableFunction.NewStimulator) return;

        TryCatch("注册针剂效果", () =>
        {
            AddNewEffectForSimulator();
            return Task.CompletedTask;
        });

        if (!databaseService.GetTables().Traders.TryGetValue(Constant.TherapistId, out var therapistTrader))
        {
            logger.Warning(
                $"[EasyGame] 无法找到商人Therapist的实例, 请检查你的Therapist商人ID是否是{Constant.TherapistId}, 如果不是, 可能被意外修改");
        }

        foreach (var (_, newItem) in _newItems)
        {
            if (string.IsNullOrEmpty(newItem.Id) || newItem.Props == null) continue;
            NewItemFromCloneDetails details = new NewItemFromCloneDetails
            {
                ItemTplToClone = Constant.ItemTplToClones[newItem.Id],
                ParentId = newItem.Parent,
                NewId = newItem.Id,
                FleaPriceRoubles = Constant.ItemPrices[newItem.Id],
                HandbookPriceRoubles = Constant.ItemPrices[newItem.Id],
                Locales = new Dictionary<string, LocaleDetails>
                {
                    {
                        "ch", new LocaleDetails
                        {
                            Name = newItem.Props.Name,
                            ShortName = newItem.Props.ShortName,
                            Description = newItem.Props.Description
                        }
                    }
                },
                OverrideProperties = newItem.Props
            };
            customItemService.CreateItemFromClone(details);
            if (therapistTrader != null)
            {
                TraderAssort assort = therapistTrader.Assort;
                Item item = new Item
                {
                    Id = new MongoId(),
                    Template = newItem.Id,
                    ParentId = "hideout",
                    SlotId = "hideout",
                    Upd = new Upd
                    {
                        UnlimitedCount = true,
                        StackObjectsCount = 9999999
                    }
                };
                AddItemToAssort(assort, item, Constant.ItemPrices[newItem.Id], 1);
            }
        }
    }

    public void AddNewEffectForSimulator()
    {
        Dictionary<string, IEnumerable<Buff>> simulator =
            databaseService.GetTables().Globals.Configuration.Health.Effects.Stimulator.Buffs;

        foreach (var (buffName, newEffect) in _newEffects)
        {
            simulator[buffName] = newEffect;
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
    }

    /// <summary>
    /// 使得迷宫地图显示在地图选择界面
    /// </summary>
    public void ShowLabyrinthInChoiceMenu()
    {
        var mapLabyrinth = databaseService.GetTables().Locations.Labyrinth;
        mapLabyrinth.Base.Enabled = true;
        mapLabyrinth.Base.ForceOnlineRaidInPVE = false;
    }

    /// <summary>
    /// 解除物品在跳蚤售卖限制
    /// </summary>
    public void RemoveRestrictionOnSellingItemsInFlea()
    {
        foreach (var (_, item) in databaseService.GetItems().Where(
                     x => x.Value.Properties != null && x.Value.Properties.CanSellOnRagfair == false))
        {
            if (item.Properties == null) continue;
            item.Properties.CanSellOnRagfair = true;
        }
    }

    /// <summary>
    /// 所有存档的基础血量与能量, 水分
    /// </summary>
    public void EnergyHydrationModify()
    {
        foreach (var (profileName, profileSides) in databaseService.GetProfileTemplates())
        {
            // 血量修改
            foreach (var side in new TemplateSide?[] { profileSides.Bear, profileSides.Usec })
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
            if (_modConfigData.OutputResultLogOfAdjust) Info($"存档类型{profileName}修改完毕");
        }
    }

    /// <summary>
    /// 战局时长修改
    /// </summary>
    public void RaidTimeAdjust()
    {
        var locations = databaseService.GetLocations();
        foreach (var (mapName, location) in locations.GetDictionary())
        {
            if (location.Base?.EscapeTimeLimit == null) continue;
            location.Base.EscapeTimeLimit *= _modConfigData.RaidTimeModify;
            location.Base.EscapeTimeLimit = Math.Max(1, location.Base?.EscapeTimeLimit ?? 0);
            if (_modConfigData.OutputResultLogOfAdjust) Info($"地图{mapName}的对局时间已被修改至: {location.Base?.EscapeTimeLimit ?? -1}");
        }
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
        if (_modConfigData.OutputResultLogOfAdjust) Info($"弹夹相关修改结果: 装弹({globals.Configuration.BaseLoadTime}s) 卸弹({globals.Configuration.BaseUnloadTime}s) 检查({globals.Configuration.BaseCheckTime}s)");
    }

    /// <summary>
    /// 跳蚤挂单上限修改
    /// </summary>
    public void FleaPendingOrderLimitModification()
    {
        Globals globals = databaseService.GetGlobals();
        string result = "跳蚤挂单上限修改结果(特刊计数->挂单数量): \n";
        foreach (var offer in globals.Configuration.RagFair.MaxActiveOfferCount)
        {
            offer.Count *= _modConfigData.MaxActiveOfferCountModify;
            result += $"\t{offer.CountForSpecialEditions} -> {offer.Count}";
        }
        if (_modConfigData.OutputResultLogOfAdjust) Info(result);
    }
    
    /// <summary>
    /// 修改所有药剂耐久(吗啡除外)
    /// </summary>
    public void AdjustSimulator()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        Dictionary<MongoId,double> itemPrices = databaseService.GetTables().Templates.Prices;
        // 修改除了吗啡以外的药剂耐久
        if (_modConfigData?.EnableFunction == null) return;
        if (_modConfigData.EnableFunction.StimulatorChange)
        {
            foreach (var (mongoId, templateItem) in itemTempaltes
                         .Where(kvp => kvp.Value.Parent.ToString() == Constant.SimBase && kvp.Value.Id.ToString() != ItemTpl.DRUGS_MORPHINE_INJECTOR))
            {
                if (templateItem.Properties != null)
                {
                    if (templateItem.Properties.MaxHpResource == null || templateItem.Properties.Weight == null) continue;
                    templateItem.Properties.MaxHpResource = _modConfigData?.StimulatorConfig?.UseTimes ?? 1;
                    templateItem.Properties.Weight = _modConfigData?.StimulatorConfig?.Weight ?? 0.05;
                    if (!itemPrices.ContainsKey(mongoId)) itemPrices[mongoId] = 0;
                    itemPrices[mongoId] *= _modConfigData?.StimulatorConfig?.PriceModify ?? 1;
                }
            }
        }
    }

    /// <summary>
    /// 调整实验室访问卡次数
    /// </summary>
    public void AdjustLabsAccess()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        TemplateItem templateItem = itemTempaltes[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS];
        if (_modConfigData?.EnableFunction == null) return;
        if (_modConfigData.EnableFunction.LabAccessChange)
        {
            if (templateItem.Properties != null)
                templateItem.Properties.MaximumNumberOfUsage = 
                    Math.Max(templateItem.Properties.MaximumNumberOfUsage ?? 1, _modConfigData?.LabsAccessMaximumNumberOfUsage ?? 1);
        }
    }
    
    /// <summary>
    /// 调整迷宫访问卡次数
    /// </summary>
    public void AdjustLabysAccess()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        TemplateItem templateItem = itemTempaltes[ItemTpl.KEYCARD_LABRYS_ACCESS];
        if (_modConfigData?.EnableFunction == null) return;
        if (_modConfigData.EnableFunction.LabAccessChange)
        {
            if (templateItem.Properties != null)
                templateItem.Properties.MaximumNumberOfUsage = 
                    Math.Max(templateItem.Properties.MaximumNumberOfUsage ?? 1, _modConfigData?.LabysAccessMaximumNumberOfUsage ?? 1);
        }
    }
    
    public void TryCatch(string name, Func<Task> func)
    {
        try
        {
            func();
        }
        catch (Exception e)
        {
            logger.Error($"[{Constant.ModName}]<{name}> {e.Message}, {e.StackTrace}");
            // throw;
        }
    }
    
    public void AddItemToAssort(TraderAssort assort, Item item, double price = 0, int loyalLevel = 1)
    {
        assort.Items.Add(item);
        assort.LoyalLevelItems[item.Id] = 1;
        assort.BarterScheme[item.Id] = new List<List<BarterScheme>>
        {
            new List<BarterScheme>
            {
                new BarterScheme
                {
                    Count = price,
                    Template = Constant.RebId
                }
            }
        };
    }
    
    
    
    
    
    
    
    
}











