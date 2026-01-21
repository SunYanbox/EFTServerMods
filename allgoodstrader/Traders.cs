using System.Reflection;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Enums;
using SPTarkov.Server.Core.Models.Spt.Config;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Routers;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Utils;
using Path = System.IO.Path;
// ReSharper disable MemberCanBePrivate.Global

namespace allgoodstrader;

[Injectable(InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 99999)]
internal class Traders(
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
    private List<TraderData> _traders = [];
    private ModConfig _modConfig = new();
    private Dictionary<MongoId, MongoId> _itemCache = new();
    private readonly Dictionary<MongoId, TraderBase> _traderBases = new();
    private string? _resPath;
    private string? _itemCachePath;

    private readonly TraderConfig _traderConfig = configServer.GetConfig<TraderConfig>();
    private readonly RagfairConfig _ragfairConfig = configServer.GetConfig<RagfairConfig>();
    private Dictionary<MongoId,TemplateItem>? _itemTpls;
    
    public static readonly Dictionary<MongoId, List<MongoId>> TraderClass = new()
    {
        { new MongoId("68dcbdecd6e04c263b42f6ba"), ItemCategories.WeaponsAndAccessories },
        { new MongoId("68e24cd2607f5c9ae44c27b0"), ItemCategories.FoodDrinkAndMedical },
        { new MongoId("68e24cdc607f5c9ae44c27b1"), ItemCategories.EquipmentAndAmmo },
        { new MongoId("68e24cdc607f5c9ae44c27b2"), ItemCategories.Miscellaneous }
    };

    public static readonly List<MongoId> SecureContainerIds =
    [
        ItemTpl.SECURE_CONTAINER_ALPHA,
        ItemTpl.SECURE_CONTAINER_BETA,
        ItemTpl.SECURE_CONTAINER_BOSS,
        ItemTpl.SECURE_CONTAINER_EPSILON,
        ItemTpl.SECURE_CONTAINER_GAMMA,
        ItemTpl.SECURE_CONTAINER_GAMMA_TUE,
        ItemTpl.SECURE_CONTAINER_KAPPA,
        ItemTpl.SECURE_CONTAINER_KAPPA_DESECRATED,
        ItemTpl.SECURE_CONTAINER_THETA
    ];

    public Task OnLoad()
    {
        // 加载数据
        try
        {
            _itemTpls ??= databaseServer.GetTables().Templates.Items;
            string pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());
            TryCatch("初始化资源路径", () =>
            {
                _resPath ??= Path.Combine(pathToMod, "res");
                _itemCachePath ??= Path.Combine(pathToMod, "data/itemCache.json");
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
            TraderBase traderBase = new();
            TryCatch("反序列化商人基础数据", () =>
            {
                traderBase = modHelper.GetJsonDataFromFile<TraderBase>(pathToMod, "data/base.json");
                return Task.CompletedTask;
            });

            foreach (TraderData trader in _traders)
            {
                MongoId traderId = new MongoId(trader.Id);

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
                    _traderBases[traderId].ItemsBuy = new ItemBuyData
                    {
                        IdList = [],
                        Category = new HashSet<MongoId>(TraderClass[traderId])
                    };
                    _traderBases[traderId].SellCategory = new List<string>(TraderClass[traderId].Select(x => x.ToString()));
                    logger.Debug($"data of items_buy: {jsonUtil.Serialize(_traderBases[traderId].ItemsBuy)}");
                    return Task.CompletedTask;
                });
                logger.Debug($"[AllGoodsTrader] 注册{trader.Name ?? "佚名"}的ID: {_traderBases[traderId].Id}");
                if (_traderBases[traderId].LoyaltyLevels != null && _traderBases[traderId].LoyaltyLevels?.Count > 0)
                {
                    foreach (TraderLoyaltyLevel l in _traderBases[traderId].LoyaltyLevels ?? [])
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
                    string[] routeRes = GetImgRouteRegisterPara(trader.ResName ?? "");
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
                    if (traderId.ToString() == "68e24cdc607f5c9ae44c27b2")
                    {
                        foreach (MongoId secureContainerId in SecureContainerIds)
                        {
                            CreateOrGetItemData(secureContainerId, assort);
                        }
                    }
                    return Task.CompletedTask;
                });
                // logger.Info($"[AllGoodsTrader] 商人{_traderBases[traderId].Name}({_traderBases[traderId].Id})获取到{assort.Items.Count}条商品数据");
                // if (trader.Id == "68e24cdc607f5c9ae44c27b1")
                // {
                //     File.WriteAllText("assort.json", jsonUtil.Serialize(assort));
                // }
                addCustomTraderHelper.OverwriteTraderAssort(_traderBases[traderId].Id, assort);
            }

            try
            {
                if (_itemCachePath != null)
                    File.WriteAllTextAsync(_itemCachePath, jsonUtil.Serialize(_itemCache) ?? "{}");
            }
            catch (Exception e)
            {
                Console.WriteLine("物品缓存保存失败: "+e.Message);
            }

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
            logger.Debug($"[AllGoodsTrader]<{name}> 任务完成");
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
        string path = Path.Combine(_resPath ?? "", $"{resName}.png");
        // (traderBase.avatar.replace(".png", ""), `${res_path}/${trader.res_name}.png`)
        logger.Debug($"Avatar: {route}, path: {path}, ResPath: {_resPath}");
        return [route, path];
    }

    public TraderAssort GetAssort(MongoId[] baseClasses)
    {
        TraderAssort assort = new()
        {
            Items = [],
            BarterScheme = new Dictionary<MongoId, List<List<BarterScheme>>>(),
            LoyalLevelItems = new Dictionary<MongoId, int>()
        };

        foreach (MongoId baseClass in baseClasses)
        {
            foreach (MongoId itemTpl in itemHelper.GetItemTplsOfBaseType(baseClass.ToString()))
            {
                if ((baseClass == BaseClasses.VEST 
                     || baseClass == BaseClasses.HEADWEAR
                     || baseClass == BaseClasses.ARMOR) && itemHelper.ItemHasSlots(itemTpl)) continue;
                if (itemHelper.IsValidItem(itemTpl)) {
                    Item newItem = CreateOrGetItemData(itemTpl, assort);
                    // 确保RPG有弹药
                    HandleRocket(assort, itemTpl, newItem.Id);
                    // 确保弹药盒有弹药
                    HandleAmmoBox(assort, itemTpl, newItem.Id);
                }
            }
        }
        
        return assort;
    }

    public MongoId GetDynamicId(MongoId itemTpl)
    {
        if (_itemCache.TryGetValue(itemTpl, out MongoId ammoId)) return ammoId;
        // 新ID
        _itemCache[itemTpl] = new MongoId();
        return _itemCache[itemTpl];
    }

    public void HandleRocket(TraderAssort assort, MongoId itemTpl, MongoId launcherId)
    {
        if (itemTpl == ItemTpl.ROCKETLAUNCHER_RSHG2_725MM_ROCKET_LAUNCHER)
        {
            MongoId dynamicIdRocket725Shg2 = GetDynamicId(ItemTpl.ROCKET_725_SHG2);
            double priceRocket725Shg2 = itemHelper.GetItemPrice(ItemTpl.ROCKET_725_SHG2) ?? 0;

            Item item = new Item
            {
                Id = dynamicIdRocket725Shg2,
                Template = ItemTpl.ROCKET_725_SHG2,
                ParentId = launcherId.ToString(),
                SlotId = "patron_in_weapon"
            };
            AddItemToAssort(assort, item, priceRocket725Shg2 * _modConfig.PriceModify ?? 0, 1);
        }
    }
    
    public void HandleAmmoBox(TraderAssort assort, MongoId itemTpl, MongoId boxId)
    {
        if (!itemHelper.IsOfBaseclass(itemTpl, BaseClasses.AMMO_BOX)) return;
        TemplateItem? box = _itemTpls?[itemTpl] ?? null;
        if (box == null || box.Properties == null || box.Properties.StackSlots == null) return;
        foreach (StackSlot stackSlot in box.Properties.StackSlots)
        {
            if (stackSlot.Properties == null || stackSlot.Properties.Filters == null || stackSlot.MaxCount == null) continue;
            foreach (SlotFilter filter in stackSlot.Properties.Filters)
            {
                if (filter.Filter == null) continue;
                foreach (MongoId ammoTpl in filter.Filter)
                {
                    // 要确保唯一, 不能用封装的默认方法
                    // MongoId ammoDynamicId = GetDynamicId(ammoTpl);
                    MongoId ammoDynamicId = new();
                    double price = itemHelper.GetItemPrice(ammoTpl) ?? 0;
                    Item ammoInner = new Item
                    {
                        Id = ammoDynamicId,
                        Template = ammoTpl,
                        ParentId = boxId.ToString(),
                        SlotId = "cartridges",
                        Location = 0,
                        Upd = new Upd
                        {
                            StackObjectsCount= stackSlot.MaxCount
                        },
                    };
                    // logger.Debug($"[AllGoodsTrader] 已添加弹药盒的弹药: {ammoInner}\n");
                    AddItemToAssort(assort, ammoInner, price * _modConfig.PriceModify ?? 0, 1);
                    // logger.Info($"[AllGoodsTrader] 弹药盒的弹药ID(Tpl: {ammoTpl}, dynamicId: {ammodynamicId})\n\t在assort中的数量: {assort.Items.Count(x => x.Id == ammoInner.Id)}\n");
                }
            }
        }
    }

    public void AddItemToAssort(TraderAssort assort, Item item, double price = 0, int loyalLevel = 1)
    {
        assort.Items.Add(item);
        assort.LoyalLevelItems[item.Id] = 1;
        assort.BarterScheme[item.Id] =
        [
            [
                new BarterScheme
                {
                    Count = price,
                    Template = RubId
                }
            ]
        ];
    }

    public Item CreateOrGetItemData(MongoId itemTpl, TraderAssort assort)
    {
        // TemplateItem tempItem = _itemTpls[itemTpl];
        double price = itemHelper.GetItemPrice(itemTpl) ?? 0;
        MongoId dynamicId = GetDynamicId(itemTpl);

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
        AddItemToAssort(assort, newItem, price * _modConfig.PriceModify ?? 0, 1);
        return newItem;
    }

    public static readonly MongoId RubId = Money.ROUBLES;
}