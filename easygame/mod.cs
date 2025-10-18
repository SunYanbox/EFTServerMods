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
    public override SemanticVersioning.Version Version { get; init; } = new("0.2.3");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; }
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "CC-BY-SA";
}



[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 100)] // 确保全物品商人能卖新的针剂
public class EasyGameMod(
        ModHelper modHelper,
        ISptLogger<EasyGameMod> logger, 
        DatabaseService databaseService,
        ConfigServer configServer,
        CustomItemService customItemService): IOnLoad
{
    public const string ModName = "EasyGameMod";
    private ModConfigData? _modConfigData;
    private Dictionary<MongoId, NewItem> newItems = new();
    private Dictionary<string, List<Buff>> newEffects = new();
    
    public void Info(string msg) => logger.Info($"[{ModName}] {msg}");
    public void Error(string msg) => logger.Error($"[{ModName}] {msg}");
    // 针剂基类
    public const string SimBase = "5448f3a64bdc2d60728b456a";
    // 新添针剂
    public const string SimMengGong = "5f9d9b8e6f8b4a1e3c7d5a2b";
    public const string SimTiLi = "5f9d9b8e6f8b4a1e3c7d5a2c";
    public const string SimFuZhong = "5f9d9b8e6f8b4a1e3c7d5a2d";
    public const string SimJueDi = "5f9d9b8e6f8b4a1e3c7d5a2e";
    public const string SimYongJie = "5f9d9b8e6f8b4a1e3c7d5a30";
    
    // 商人ID
    public MongoId TherapistId = new MongoId("54cb57776803fa99248b456e");
    public static readonly MongoId RebId = new MongoId("5449016a4bdc2d6f028b456f");
    
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

    public Task OnLoad()
    {
        LoadDataBase();

        TryCatch("调整带入物品数量限制", () =>
        {
            AdjustConfig();
            return Task.CompletedTask;
        });
        
        TryCatch("调整所有针剂", () =>
        {
            AdjustSimulator();
            return Task.CompletedTask;
        });

        TryCatch("注册新物品", () =>
        {
            AddNewItem();
            return Task.CompletedTask;
        });
        
        logger.Info($"[{ModName}] 模组加载完毕");
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
            newItems = modHelper.GetJsonDataFromFile<Dictionary<MongoId, NewItem>>(pathToMod, "items.json");
            return Task.CompletedTask;
        });
        TryCatch("调整实验室钥匙卡使用次数", () =>
        {
            AdjustCard();
            return Task.CompletedTask;
        });
        TryCatch("加载新针剂效果信息", () =>
        {
            newEffects = modHelper.GetJsonDataFromFile<Dictionary<string, List<Buff>>>(pathToMod, "effects.json");
            return Task.CompletedTask;
        });
    }
    
    public void AddNewItem()
    {
        TryCatch("注册针剂效果", () =>
        {
            AddNewEffect();
            return Task.CompletedTask;
        });

        if (!databaseService.GetTables().Traders.TryGetValue(TherapistId, out var therapistTrader))
        {
            logger.Warning($"[EasyGame] 无法找到商人Therapist的实例, 请检查你的Therapist商人ID是否是{TherapistId}, 如果不是, 可能被意外修改");
        }
        
        foreach (var (_, newItem) in newItems)
        {
            if (string.IsNullOrEmpty(newItem.Id) || newItem.Props == null) continue;
            NewItemFromCloneDetails details = new NewItemFromCloneDetails
            {
                ItemTplToClone = ItemTplToClones[newItem.Id],
                ParentId = newItem.Parent,
                NewId = newItem.Id,
                FleaPriceRoubles = ItemPrices[newItem.Id],
                HandbookPriceRoubles = ItemPrices[newItem.Id],
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
                AddItemToAssort(assort, item, ItemPrices[newItem.Id], 1);
            }
        }
    }

    public void AddNewEffect()
    {
        Dictionary<string,IEnumerable<Buff>> simulator = databaseService.GetTables().Globals.Configuration.Health.Effects.Stimulator.Buffs;
        
        foreach (var (buffName, newEffect) in newEffects)
        {
            simulator[buffName] = newEffect;
        }
    }

    public void AdjustConfig()
    {
        RestrictionsInRaid[] data = databaseService.GetTables().Globals.Configuration.RestrictionsInRaid;
        // Credits for @HiddenCirno
        foreach (RestrictionsInRaid dataItem in data)
        {
            dataItem.MaxInRaid = Math.Max(dataItem.MaxInRaid, _modConfigData?.EnterGameItemLimit ?? dataItem.MaxInRaid);
            dataItem.MaxInLobby = Math.Max(dataItem.MaxInLobby, _modConfigData?.EnterGameItemLimit ?? dataItem.MaxInLobby);
        }
        // 迷宫地图入口
        var mapLabyrinth = databaseService.GetTables().Locations.Labyrinth;
        mapLabyrinth.Base.Enabled = true;
        mapLabyrinth.Base.ForceOnlineRaidInPVE = false;
        if (_modConfigData == null) return;
        // 解除物品在跳蚤售卖限制
        if (_modConfigData.IsUnlockAllItemsSellLimit)
        {
            foreach (var (_, item) in databaseService.GetItems().Where(
                         x => x.Value.Properties != null && x.Value.Properties.CanSellOnRagfair == false))
            {
                if (item.Properties == null) continue;
                item.Properties.CanSellOnRagfair = true;
            }
        }
        // 所有存档的基础血量与能量, 水分
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
        // 战局时长修改
        var locations = databaseService.GetLocations();
        foreach (var (mapName, location) in locations.GetDictionary())
        {
            if (location.Base?.EscapeTimeLimit == null) continue;
            location.Base.EscapeTimeLimit *= _modConfigData.RaidTimeModify;
            location.Base.EscapeTimeLimit = Math.Max(1, location.Base?.EscapeTimeLimit ?? 0);
            if (_modConfigData.OutputResultLogOfAdjust) Info($"地图{mapName}的对局时间已被修改至: {location.Base?.EscapeTimeLimit ?? -1}");
        }
        // 弹夹熟悉修改
        Globals globals = databaseService.GetGlobals();
        TryCatch("修改弹夹装弹/检查/卸弹速度", () =>
        {
            globals.Configuration.BaseCheckTime *= _modConfigData.CheckAmmoTimeModify;
            globals.Configuration.BaseLoadTime *= _modConfigData.TakeInAmmoTimeModify;
            globals.Configuration.BaseUnloadTime *= _modConfigData.TakeOutAmmoTimeModify;
            if (_modConfigData.OutputResultLogOfAdjust) Info($"弹夹相关修改结果: 装弹({globals.Configuration.BaseLoadTime}s) 卸弹({globals.Configuration.BaseUnloadTime}s) 检查({globals.Configuration.BaseCheckTime}s)");
            return Task.CompletedTask;
        });
        // 跳蚤挂单上限修改
        TryCatch("跳蚤挂单上限修改", () =>
        {
            string result = "跳蚤挂单上限修改结果(特刊计数->挂单数量): \n";
            foreach (var offer in globals.Configuration.RagFair.MaxActiveOfferCount)
            {
                offer.Count *= _modConfigData.MaxActiveOfferCountModify;
                result += $"\t{offer.CountForSpecialEditions} -> {offer.Count}";
            }
            if (_modConfigData.OutputResultLogOfAdjust) Info(result);
            return Task.CompletedTask;
        });
    }
    
    public void AdjustSimulator()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        Dictionary<MongoId,double> itemPrices = databaseService.GetTables().Templates.Prices;
        // 修改除了吗啡以外的药剂耐久
        foreach (var (mongoId, templateItem) in itemTempaltes
                     .Where(kvp => kvp.Value.Parent.ToString() == SimBase && kvp.Value.Id.ToString() != ItemTpl.DRUGS_MORPHINE_INJECTOR))
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

    public void AdjustCard()
    {
        Dictionary<MongoId,TemplateItem> itemTempaltes = databaseService.GetTables().Templates.Items;
        TemplateItem templateItem = itemTempaltes[ItemTpl.KEYCARD_TERRAGROUP_LABS_ACCESS];
        if (templateItem.Properties != null)
            templateItem.Properties.MaximumNumberOfUsage = 10;
    }
    
    public void TryCatch(string name, Func<Task> func)
    {
        try
        {
            func();
        }
        catch (Exception e)
        {
            logger.Error($"[{ModName}]<{name}> {e.Message}, {e.StackTrace}");
            throw;
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
                    Template = RebId
                }
            }
        };
    }
    
    
    
    
    
    
    
    
}











