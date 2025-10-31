using System.Text.Json;
using NewYourItems.@abstract;
using NewYourItems.NewItemClasses;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Servers;
using SPTarkov.Server.Core.Services;
using SPTarkov.Server.Core.Utils;

namespace NewYourItems;

[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PostDBModLoader + 1)]
public class DataLoader(
        LocalLog localLog,
        ISptLogger<DataLoader> logger,
        JsonUtil jsonUtil,
        DatabaseServer dbServer,
        DatabaseService databaseService
    ): IOnLoad
{
    private static LocalLog? _localLog;
    /// <summary>
    /// 通用/默认创建物品接口
    /// </summary>
    public Dictionary<string, NewItemCommon> NewItemCommon = new Dictionary<string, NewItemCommon>();
    /// <summary>
    /// 食物饮品创建物品接口
    /// </summary>
    public Dictionary<string, NewItemDrinkOrDrugs> NewItemDrinkOrDrugs = new Dictionary<string, NewItemDrinkOrDrugs>();
    /// <summary>
    /// 药品创建
    /// </summary>
    public Dictionary<string, NewItemMedical> NewItemMedical = new Dictionary<string, NewItemMedical>();
    /// <summary>
    /// 弹药
    /// </summary>
    public Dictionary<string, NewItemAmmo> NewItemAmmo = new Dictionary<string, NewItemAmmo>();

    public Task OnLoad()
    {
        AbstractInfo.LocalLog ??= localLog;
        AbstractNewItem.LocalLog ??= localLog;
        AbstractNewItem.DatabaseService ??= databaseService;
        
        _localLog = localLog;
        if (localLog.DataFolderPath == null)
        {
            localLog.LocalLogMsg(LocalLogType.Error, $"数据文件路径为空\n\t堆栈: {LocalLog.GetCurrentStackTrace()}");
            return Task.CompletedTask;
        }
        
        
        List<string> foundFiles = new List<string>();
        
        TraverseForNyiFiles(localLog.DataFolderPath, foundFiles);
        
        foreach (string file in foundFiles)
        {
            try
            {
                NewItemCommon? newItemBase = DeserializeBasedOnType(File.ReadAllText(file));
                if (newItemBase == null) throw new Exception("反序列化的结果为null");
                if (newItemBase.BaseInfo == null) throw new Exception("反序列化后获取不到baseInfo字段");
                newItemBase.BaseInfo.ItemPath = file;
                newItemBase.ItemPath = file;
                newItemBase.Verify();
                localLog.LocalLogMsg(LocalLogType.Debug, $"已加载新物品 Id{newItemBase.BaseInfo.Id}({newItemBase.BaseInfo.Name}, @{newItemBase.BaseInfo.Author}) \t {newItemBase.BaseInfo.License} \n\t > Path = {file}");
                // 类型转换
                switch (newItemBase.BaseInfo.Type)
                {
                    case NyiType.Common: NewItemCommon.Add(file, newItemBase as NewItemCommon); break;
                    case NyiType.DrinkOrDrugs: NewItemDrinkOrDrugs.Add(file, newItemBase as NewItemDrinkOrDrugs); break;
                    case NyiType.Medical: NewItemMedical.Add(file, newItemBase as NewItemMedical); break;
                    case NyiType.Ammo: NewItemAmmo.Add(file, newItemBase as NewItemAmmo); break;
                    default: 
                        localLog.LocalLogMsg(LocalLogType.Error, $"在分类新物品数据\"{file}\"类型时出现问题: `baseInfo.type` (当前为: {newItemBase.BaseInfo.Type}) 不存在或不合法 \n\t > Path = {file}");
                        break;
                }
            }
            catch (Exception e)
            {
                localLog.LocalLogMsg(LocalLogType.Error, $"在反序列化\"{file}\"时出现问题: {e.Message}");
            }
        }
        
        localLog.LocalLogMsg(LocalLogType.Info, $"已处理{foundFiles.Count}条nyi文件");
        
        return Task.CompletedTask;
    }
    
    // 根据 TypeIdentifier 在反序列化时直接创建正确的类型
    private static NewItemCommon? DeserializeBasedOnType(string json)
    {
        using JsonDocument doc = JsonDocument.Parse(json);
        string typeIdentifier = doc.RootElement.GetProperty("$type").GetString();
    
        return typeIdentifier switch
        {
            NyiType.Common => JsonSerializer.Deserialize<NewItemCommon>(json),
            NyiType.DrinkOrDrugs => JsonSerializer.Deserialize<NewItemDrinkOrDrugs>(json),
            NyiType.Medical => JsonSerializer.Deserialize<NewItemMedical>(json),
            NyiType.Ammo => JsonSerializer.Deserialize<NewItemAmmo>(json),
            _ => JsonSerializer.Deserialize<NewItemCommon>(json)
        };
    }
    
    /// <summary>
    /// 递归遍历, 获取新物品数据
    /// </summary>
    /// <param name="path">目录路径</param>
    /// <param name="results">结果列表</param>
    /// <param name="currentDepth">当前递归深度（内部使用）</param>
    public static void TraverseForNyiFiles(string path, List<string> results, int currentDepth = 10)
    {
        try
        {
            // 检查递归深度
            if (currentDepth < 0)
            {
                if (_localLog != null) _localLog.LocalLogMsg(LocalLogType.Warn, $"达到最大递归深度，停止遍历: {path}");
                else Console.WriteLine($"[{LocalLog.GetModName()}] 达到最大递归深度，停止遍历: {path}");
                return;
            }

            // 遍历当前目录的所有文件
            foreach (string file in Directory.GetFiles(path))
            {
                if (file.EndsWith(".nyi") || file.EndsWith(".nyi.json"))
                {
                    results.Add(file);
                }
            }
        
            // 递归遍历所有子目录
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                TraverseForNyiFiles(subDirectory, results, currentDepth - 1);
            }
        }
        catch (UnauthorizedAccessException)
        {
            if (_localLog != null) _localLog.LocalLogMsg(LocalLogType.Error, $"无权访问目录: {path}");
            else Console.WriteLine($"[{LocalLog.GetModName()}] 无权访问目录: {path}");
        }
        catch (DirectoryNotFoundException)
        {
            if (_localLog != null) _localLog.LocalLogMsg(LocalLogType.Error, $"目录不存在: {path}");
            else Console.WriteLine($"[{LocalLog.GetModName()}] 目录不存在: {path}");
        }
        catch (Exception ex)
        {
            if (_localLog != null) _localLog.LocalLogMsg(LocalLogType.Error, $"处理目录 {path} 时出错: {ex.Message}");
            else Console.WriteLine($"[{LocalLog.GetModName()}] 处理目录 {path} 时出错: {ex.Message}");
        }
    }
}