
using System.Reflection;
using System.Text.Json.Serialization;
using Microsoft.VisualBasic;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;
using SPTarkov.Server.Core.Utils.Cloners;
using Path = System.IO.Path;

namespace allgoodstrader;

public record ModMetadata : AbstractModMetadata
{
    public override string ModGuid { get; init; } = "com.suntion.allgoodstrader";
    public override string Name { get; init; } = "AllGoodsTrader";
    public override string Author { get; init; } = "Suntion";
    public override List<string>? Contributors { get; init; } = [];
    public override SemanticVersioning.Version Version { get; init; } = new("0.4.0");
    public override SemanticVersioning.Range SptVersion { get; init; } = new("~4.0.0");
    
    
    public override List<string>? Incompatibilities { get; init; }
    public override Dictionary<string, SemanticVersioning.Range>? ModDependencies { get; init; }
    public override string? Url { get; init; } = "https://github.com/sp-tarkov/server-mod-examples";
    public override bool? IsBundleMod { get; init; } = false;
    public override string? License { get; init; } = "MIT";
}

[Injectable(TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class AddCustomTraderHelper(
    ISptLogger<AddCustomTraderHelper> logger,
    ICloner cloner,
    DatabaseService databaseService,
    LocaleService localeService)
{

    /// <summary>
    /// Add the traders update time for when their offers refresh
    /// </summary>
    /// <param name="traderConfig">trader config to add our trader to</param>
    /// <param name="baseJson">json file for trader (db/base.json)</param>
    /// <param name="refreshTimeSecondsMin">How many seconds between trader stock refresh min time</param>
    /// <param name="refreshTimeSecondsMax">How many seconds between trader stock refresh max time</param>
    public void SetTraderUpdateTime(TraderConfig traderConfig, TraderBase baseJson, int refreshTimeSecondsMin, int refreshTimeSecondsMax)
    {
        // Add refresh time in seconds to config
        var traderRefreshRecord = new UpdateTime
        {
            TraderId = baseJson.Id,
            Seconds = new MinMax<int>(refreshTimeSecondsMin, refreshTimeSecondsMax)
        };

        traderConfig.UpdateTime.Add(traderRefreshRecord);
    }

    /// <summary>
    /// Add a traders base data to the server, no assort items
    /// </summary>
    /// <param name="traderDetailsToAdd">trader details</param>
    public void AddTraderWithEmptyAssortToDb(TraderBase traderDetailsToAdd)
    {
        // Create an empty assort ready for our items
        var emptyTraderItemAssortObject = new TraderAssort
        {
            Items = [],
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };

        // Create trader data ready to add to database
        var traderDataToAdd = new Trader
        {
            Assort = emptyTraderItemAssortObject,
            Base = cloner.Clone(traderDetailsToAdd),
            QuestAssort = new() // quest assort is empty as trader has no assorts unlocked by quests
            {
                // We create 3 empty arrays, one for each of the main statuses that are possible
                { "Started", new() },
                { "Success", new() },
                { "Fail", new() }
            },
            Dialogue = []
        };

        // Add the new trader id and data to the server
        if (!databaseService.GetTables().Traders.TryAdd(traderDetailsToAdd.Id, traderDataToAdd))
        {
            //Failed to add trader!
        }
    }

    /// <summary>
    /// Add traders name/location/description to all locales (e.g. German/French/English)
    /// </summary>
    /// <param name="baseJson">json file for trader (db/base.json)</param>
    /// <param name="firstName">First name of trader</param>
    /// <param name="description">Flavor text of whom the trader is</param>
    public void AddTraderToLocales(TraderBase baseJson, string firstName, string description)
    {
        // For each language, add locale for the new trader
        var locales = databaseService.GetTables().Locales.Global;
        var newTraderId = baseJson.Id;
        var fullName = baseJson.Name;
        var nickName = baseJson.Nickname;
        var location = baseJson.Location;

        foreach (var (localeKey, localeKvP) in locales)
        {
            // We have to add a transformer here, because locales are lazy loaded due to them taking up huge space in memory
            // The transformer will make sure that each time the locales are requested, the ones added below are included
            localeKvP.AddTransformer(lazyloadedLocaleData =>
            {
                lazyloadedLocaleData.Add($"{newTraderId} FullName", fullName);
                lazyloadedLocaleData.Add($"{newTraderId} FirstName", firstName);
                lazyloadedLocaleData.Add($"{newTraderId} Nickname", nickName);
                lazyloadedLocaleData.Add($"{newTraderId} Location", location);
                lazyloadedLocaleData.Add($"{newTraderId} Description", description);
                return lazyloadedLocaleData;
            });
        }
    }

    /// <summary>
    /// Overwrite the desired traders assorts with the ones provided
    /// </summary>
    /// <param name="traderId">Trader to override assorts of</param>
    /// <param name="newAssorts">new assorts we want to add</param>
    public void OverwriteTraderAssort(string traderId, TraderAssort newAssorts)
    {
        if (!databaseService.GetTables().Traders.TryGetValue(traderId, out var traderToEdit))
        {
            logger.Warning($"Unable to update assorts for trader: {traderId}, they couldn't be found on the server");

            return;
        }

        // Override the traders assorts with the ones we passed in
        traderToEdit.Assort = newAssorts;
    }
}

record TraderData
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

record ModConfig
{
    [JsonPropertyName("priceModify")]
    public virtual double? PriceModify { get; set; }
}

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 99999)]
class Traders(
    ModHelper modHelper,
    ISptLogger<Traders> logger,
    ImageRouter imageRouter,
    ConfigServer configServer,
    TimeUtil timeUtil,
    ItemHelper itemHelper,
    JsonUtil jsonUtil,
    DatabaseServer databaseServer,
    AddCustomTraderHelper addCustomTraderHelper
) : IOnLoad
{
    List<TraderData> _traders = new();
    ModConfig _modConfig = new();
    Dictionary<MongoId, MongoId> _itemCache = new();
    Dictionary<MongoId, TraderBase> _traderBases = new();
    public bool loadTag = false;
    private string? ResPath;
    private string? ItemCachePath;

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private Dictionary<MongoId,TemplateItem>? _itemTpls;
    
    public static readonly Dictionary<MongoId, List<MongoId>> TraderClass = new Dictionary<MongoId, List<MongoId>>
    {
        { new MongoId("68dcbdecd6e04c263b42f6ba"), ItemCategories.WeaponsAndAccessories },
        { new MongoId("68e24cd2607f5c9ae44c27b0"), ItemCategories.FoodDrinkAndMedical },
        { new MongoId("68e24cdc607f5c9ae44c27b1"), ItemCategories.EquipmentAndAmmo },
        { new MongoId("68e24cdc607f5c9ae44c27b2"), ItemCategories.Miscellaneous }
    };

    public Task OnLoad()
    {
        // 加载数据
        try
        {
            _itemTpls ??= databaseServer.GetTables().Templates.Items;
            string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            TryCatch("初始化资源路径", () =>
            {
                ResPath ??= Path.Combine(pathToMod, "res");
                ItemCachePath ??= Path.Combine(pathToMod, "data/itemCache.json");
                return Task.CompletedTask;
            });
            TryCatch("反序列化配置文件", () =>
            {
                _modConfig = modHelper.GetJsonDataFromFile<ModConfig>(pathToMod, "data/config.json");
                return Task.CompletedTask;
            });
            TryCatch("反序列化商人数据", () =>
            {
                _traders = modHelper.GetJsonDataFromFile<List<TraderData>>(pathToMod, "data/traders.json");
                return Task.CompletedTask;
            });
            TryCatch("反序列化物品缓存", () =>
            {
                _itemCache = modHelper.GetJsonDataFromFile<Dictionary<MongoId, MongoId>>(pathToMod, "data/itemCache.json");
                return Task.CompletedTask;
            });
            TraderBase traderBase = new TraderBase();
            TryCatch("反序列化商人基础数据", () =>
            {
                traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/base.json");
                return Task.CompletedTask;
            });

            foreach (var trader in _traders)
            {
                MongoId traderId = new MongoId(trader.Id);
                TraderBase _traderBase = new TraderBase();
                
                TryCatch($"初始化{traderId}的traderBase数据", () =>
                {
                    _traderBases[traderId] = traderBase with
                    {
                        Id = traderId,
                        Name = trader.Name ?? "佚名",
                        Location = trader.Location,
                        Nickname = trader.Name ?? "佚名",
                        Surname = trader.Name ?? "佚名",
                    };
                    return Task.CompletedTask;
                });
                logger.Debug($"[AllGoodsTrader] 注册{trader.Name ?? "佚名"}的ID: {_traderBases[traderId].Id}");
                if (_traderBases[traderId].LoyaltyLevels != null && _traderBases[traderId]?.LoyaltyLevels?.Count > 0)
                {
                    foreach (var l in _traderBases[traderId].LoyaltyLevels ?? [])
                    {
                        TryCatch($"设置商人{traderId}的LoyaltyLevels数据", () =>
                        {
                            l.BuyPriceCoefficient = trader.BuyPriceCoef ?? 100;
                            l.RepairPriceCoefficient = trader.RepairPriceCoef ?? 100;
                            l.InsurancePriceCoefficient = trader.InsurancePriceCoef ?? 100;
                            return Task.CompletedTask;
                        });
                    }
                }
                TryCatch($"注册商人{traderId}图片路由", () =>
                {
                    var routeRes = GetImgRouteRegisterPara(trader.ResName ?? "");
                    _traderBases[traderId].Avatar = routeRes[0];
                    imageRouter.AddRoute(routeRes[0], routeRes[1]);
                    addCustomTraderHelper.SetTraderUpdateTime(_traderConfig, _traderBases[traderId], timeUtil.GetHoursAsSeconds(1),
                        timeUtil.GetHoursAsSeconds(2));
                    return Task.CompletedTask;
                });
                // Add our trader to the config file, this lets it be seen by the flea market
                TryCatch($"添加商人{traderId}进入跳蚤市场", () =>
                {
                    _ragfairConfig.Traders.TryAdd(_traderBases[traderId].Id, true);
                    return Task.CompletedTask;
                });
                addCustomTraderHelper.AddTraderWithEmptyAssortToDb(_traderBases[traderId]);

                TryCatch($"添加商人{traderId}本地化文本", () =>
                {
                    // 在数据库中为我们的交易员添加本地化文本，以便显示给使用不同语言的玩家
                    addCustomTraderHelper.AddTraderToLocales(_traderBases[traderId], trader.Name ?? "佚名", "<保密>");
                    return Task.CompletedTask;
                });
                TraderAssort assort = new TraderAssort();
                TryCatch($"获取商人{traderId}的Assort数据", () =>
                {
                    List<MongoId> baseClasses = TraderClass[traderId];
                    assort = GetAssort(baseClasses.ToArray());
                    _traderBases[traderId].SellCategory ??= [];
                    foreach (var typeId in baseClasses)
                    {
                        _traderBases[traderId].SellCategory?.Add(typeId.ToString());
                    }

                    return Task.CompletedTask;
                });
                addCustomTraderHelper.OverwriteTraderAssort(_traderBases[traderId].Id, assort);
            }

            try
            {
                File.WriteAllTextAsync(ItemCachePath, jsonUtil.Serialize(_itemCache) ?? "{}");
            }
            catch (Exception e)
            {
                Console.WriteLine("物品缓存保存失败: "+e.Message);
            }
            loadTag = true;
            logger.Info("[AllGoodsTrader] 商人数据加载完成");
        }
        catch (Exception e)
        {
            logger.Info("[AllGoodsTrader] 商人数据加载失败: " + e.Message + e.StackTrace);
        }

        return Task.CompletedTask;
    }

    public void TryCatch(string name, Func<Task> func)
    {
        try
        {
            func();
        }
        catch (Exception e)
        {
            logger.Error($"[AllGoodsTrader]<{name}> {e.Message}, {e.StackTrace}");
            throw;
        }
    }

    public string[] GetImgRouteRegisterPara(string resName)
    {
        string route = $"/files/trader/avatar/{resName}";
        string path = Path.Combine(ResPath ?? "", $"{resName}.png");
        // (traderBase.avatar.replace(".png", ""), `${res_path}/${trader.res_name}.png`)
        logger.Debug($"Avatar: {route}, path: {path}, ResPath: {ResPath}");
        return [route, path];
    }

    public TraderAssort GetAssort(MongoId[] baseClasses)
    {
        TraderAssort assort = new TraderAssort();
        assort.Items = new List<Item>();
        assort.BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>();
        assort.LoyalLevelItems = new Dictionary<MongoId, int>();

        foreach (MongoId baseClass in baseClasses)
        {
            foreach (MongoId itemTpl in itemHelper.GetItemTplsOfBaseType(baseClass.ToString()))
            {
                if ((baseClass == BaseClasses.VEST 
                     || baseClass == BaseClasses.HEADWEAR
                     || baseClass == BaseClasses.ARMOR) && itemHelper.ItemHasSlots(itemTpl)) continue;
                if (itemHelper.IsValidItem(itemTpl)) {
                    CreateOrGetItemData(itemTpl, assort);
                }
            }
        }
        
        return assort;
    }

    public void CreateOrGetItemData(MongoId itemTpl, TraderAssort assort)
    {
        // TemplateItem tempItem = _itemTpls[itemTpl];
        double price = itemHelper.GetItemPrice(itemTpl) ?? 0;
        MongoId dynamicId = _itemCache.TryGetValue(itemTpl, out var id) ? id : new MongoId();
        _itemCache[itemTpl] = dynamicId;

        // logger.Debug($"[AllGoodsTrader] 物品{itemTpl}的动态ID: {dynamicId}");
        
        Item newItem = new Item
        {
            Id = dynamicId,
            Template = itemTpl,
            ParentId = "hideout",
            SlotId = "hideout",
            Upd = new Upd
            {
                UnlimitedCount = true,
                StackObjectsCount = 9999999
            }
        };
        assort.Items.Add(newItem);
        assort.LoyalLevelItems[dynamicId] = 1;
        assort.BarterScheme[dynamicId] = new List<List<BarterScheme>>
        {
            new List<BarterScheme>
            {
                new BarterScheme
                {
                    Count = price * _modConfig.PriceModify ?? 0,
                    Template = RebId
                }
            }
        };
    }

    public static readonly MongoId RebId = new MongoId("5449016a4bdc2d6f028b456f");
}


public static class ItemCategories
{
    // 武器与配件
    public static readonly List<MongoId> WeaponsAndAccessories = new List<MongoId>
    {
        BaseClasses.ASSAULT_CARBINE, // 突击卡宾枪
        BaseClasses.ASSAULT_RIFLE, // 突击步枪
        BaseClasses.PISTOL, // 手枪
        BaseClasses.SHOTGUN, // 霰弹枪
        BaseClasses.SMG, // 冲锋枪
        BaseClasses.SNIPER_RIFLE, // 狙击步枪
        BaseClasses.MARKSMAN_RIFLE, // 精确射手步枪
        BaseClasses.MACHINE_GUN, // 机枪
        BaseClasses.GRENADE_LAUNCHER, // 榴弹发射器
        BaseClasses.LAUNCHER, // 发射器
        BaseClasses.ROCKET_LAUNCHER, // 火箭发射器
        BaseClasses.REVOLVER, // 左轮手枪
        BaseClasses.SPECIAL_WEAPON, // 特殊武器
        BaseClasses.KNIFE, // 刀
        BaseClasses.THROW_WEAP, // 投掷武器
        BaseClasses.ROCKET, // 火箭弹
        
        BaseClasses.BARREL, // 枪管
        BaseClasses.COMPENSATOR, // 补偿器
        BaseClasses.FLASH_HIDER, // 消焰器
        BaseClasses.SILENCER, // 消音器
        BaseClasses.MUZZLE, // 枪口
        BaseClasses.MUZZLE_COMBO, // 枪口组合件
        BaseClasses.GASBLOCK, // 导气块
        BaseClasses.HANDGUARD, // 护木
        BaseClasses.RECEIVER, // 机匣
        BaseClasses.STOCK, // 枪托
        BaseClasses.PISTOL_GRIP, // 手枪式握把
        BaseClasses.SHAFT, // 枪机框
        
        BaseClasses.ASSAULT_SCOPE, // 突击瞄具
        BaseClasses.COLLIMATOR, // 准直瞄具
        BaseClasses.COMPACT_COLLIMATOR, // 紧凑型准直瞄具
        BaseClasses.IRON_SIGHT, // 机械瞄具
        BaseClasses.OPTIC_SCOPE, // 光学瞄准镜
        BaseClasses.SPECIAL_SCOPE, // 特殊瞄准镜
        BaseClasses.SIGHTS, // 瞄具
        
        BaseClasses.BIPOD, // 两脚架
        BaseClasses.FOREGRIP, // 前握把
        BaseClasses.FLASHLIGHT, // 手电筒
        BaseClasses.LIGHT_LASER, // 激光指示器
        BaseClasses.MOUNT, // 导轨座
        BaseClasses.RAIL_COVERS, // 导轨护盖
        BaseClasses.TACTICAL_COMBO, // 战术组合件
        
        BaseClasses.MAGAZINE, // 弹匣
        BaseClasses.CYLINDER_MAGAZINE, // 弹鼓
        BaseClasses.SPRING_DRIVEN_CYLINDER, // 弹簧驱动弹筒
        
        BaseClasses.AUXILIARY_MOD, // 辅助改装件
        BaseClasses.FUNCTIONAL_MOD, // 功能改装件
        BaseClasses.GEAR_MOD, // 装备改装件
        BaseClasses.MASTER_MOD, // 主改装件
        BaseClasses.MOD, // 改装件
        // BaseClasses.BUILT_IN_INSERTS // 内置插板
    };

    // 食物饮品与药品
    public static readonly List<MongoId> FoodDrinkAndMedical = new List<MongoId>
    {
        BaseClasses.DRINK, // 饮品
        BaseClasses.FOOD, // 食物
        BaseClasses.FOOD_DRINK, // 食品饮料
        
        BaseClasses.DRUGS, // 药品
        BaseClasses.MEDICAL, // 医疗物品
        BaseClasses.MEDICAL_SUPPLIES, // 医疗用品
        BaseClasses.MEDS, // 医疗物资
        BaseClasses.MED_KIT, // 医疗包
        BaseClasses.STIMULATOR // 兴奋剂
    };

    // 装备与弹药
    public static readonly List<MongoId> EquipmentAndAmmo = new List<MongoId>
    {
        BaseClasses.ARMOR, // 护甲
        BaseClasses.ARMOR_PLATE, // 装甲板
        BaseClasses.ARMORED_EQUIPMENT, // 装甲装备
        BaseClasses.VEST, // 胸挂/弹挂
        BaseClasses.HEADWEAR, // 头戴装备
        BaseClasses.HEADPHONES, // 耳机
        BaseClasses.FACE_COVER, // 面部防护
        BaseClasses.VISORS, // 面罩
        BaseClasses.BACKPACK, // 背包
        BaseClasses.EQUIPMENT, // 装备
        
        BaseClasses.AMMO, // 弹药
        BaseClasses.AMMO_BOX, // 弹药箱
        
        BaseClasses.ARM_BAND, // 臂章
        BaseClasses.NIGHT_VISION, // 夜视仪
        BaseClasses.THERMAL_VISION, // 热成像仪
        BaseClasses.COMPASS, // 指南针
        BaseClasses.PORTABLE_RANGE_FINDER // 便携式测距仪
    };

    // 杂物与其他
    public static readonly List<MongoId> Miscellaneous = new List<MongoId>
    {
        BaseClasses.BARTER_ITEM, // 交易物品
        BaseClasses.BATTERY, // 电池
        BaseClasses.BUILDING_MATERIAL, // 建筑材料
        BaseClasses.CHARGE, // 充电器
        BaseClasses.COMPOUND_ITEM, // 复合物品
        BaseClasses.CULTIST_AMULET, // 邪教护身符
        BaseClasses.ELECTRONICS, // 电子设备
        BaseClasses.FUEL, // 燃料
        BaseClasses.FLYER, // 传单
        BaseClasses.HOUSEHOLD_GOODS, // 家居用品
        BaseClasses.INFO, // 信息物品
        BaseClasses.INVENTORY, // 库存物品
        BaseClasses.ITEM, // 物品
        BaseClasses.JEWELRY, // 珠宝
        BaseClasses.KEY, // 钥匙
        BaseClasses.KEY_MECHANICAL, // 机械钥匙
        BaseClasses.KEYCARD, // 钥匙卡
        BaseClasses.LOCKABLE_CONTAINER, // 可上锁容器
        BaseClasses.LOOT_CONTAINER, // 战利品容器
        BaseClasses.LUBRICANT, // 润滑剂
        BaseClasses.MAP, // 地图
        BaseClasses.MARK_OF_UNKNOWN, // 未知标记
        BaseClasses.MOB_CONTAINER, // 动态容器
        BaseClasses.MONEY, // 货币
        BaseClasses.MULTITOOLS, // 多功能工具
        BaseClasses.OTHER, // 其他
        BaseClasses.PLANTING_KITS, // 种植工具包
        BaseClasses.PMS, // 个人医疗站
        BaseClasses.POCKETS, // 口袋
        BaseClasses.RADIO_TRANSMITTER, // 无线电发射器
        BaseClasses.RANDOM_LOOT_CONTAINER, // 随机战利品容器
        BaseClasses.REPAIR_KITS, // 维修工具包
        BaseClasses.SEARCHABLE_ITEM, // 可搜索物品
        BaseClasses.SIMPLE_CONTAINER, // 简单容器
        BaseClasses.SORTING_TABLE, // 整理台
        BaseClasses.SPEC_ITEM, // 特殊物品
        BaseClasses.STACKABLE_ITEM, // 可堆叠物品
        BaseClasses.STASH, // 藏匿处
        BaseClasses.STATIONARY_CONTAINER, // 固定容器
        BaseClasses.TOOL, // 工具
        BaseClasses.HIDEOUT_AREA_CONTAINER // 藏身处区域容器
    };
}



