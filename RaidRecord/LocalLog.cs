using System.Reflection;
using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.DI;
using SPTarkov.Server.Core.Helpers;
using SPTarkov.Server.Core.Models.Utils;
using SPTarkov.Server.Core.Utils;

namespace RaidRecord;

public enum LocalLogType
{
    Info,
    Warn,
    Debug,
    Error
}

public record ModConfigData
{
    // 本地语言
    [JsonPropertyName("local")]
    public required string Local { get; set; }
    [JsonIgnore]
    public static readonly string LogFolder = "logs";
    
}

/// <summary>
/// 封装本地化日志, 获取模组配置信息
/// </summary>
[Injectable(InjectionType = InjectionType.Singleton, TypePriority = OnLoadOrder.PreSptModLoader + 1)]
public class LocalConfigLog(ModHelper modHelper, JsonUtil jsonUtil, ISptLogger<LocalConfigLog> logger): IOnLoad
{
    public ModConfigData? ModConfigData { get; set; }
    protected string? LogFolderPath { get; set; }
    protected Dictionary<LocalLogType, StreamWriter> LogWriters = new();
    protected readonly Lock LogLock = new();

    public bool TryCatch(string task, Func<bool> func)
    {
        try
        {
            bool result = func();
            logger.Debug($"[RaidRecord]<{task}> 任务完成");
            return result;
        }
        catch (Exception e)
        {
            logger.Error($"[RaidRecord]<{task}>: {e.Message}\n\t{e.StackTrace}");
            return false;
        }
    }
    
    public Task OnLoad()
    {
        var pathToMod = modHelper.GetAbsolutePathToModFolder(Assembly.GetExecutingAssembly());

        TryCatch("初始化本地日志核心", () => InitLogCore(pathToMod) );
        
        LocalLogHook("加载数据库", () =>
        {
            ModConfigData = modHelper.GetJsonDataFromFile<ModConfigData>(Path.Combine(pathToMod, "data"),"config.json");
            return Task.CompletedTask;
        });
        
        return Task.CompletedTask;
    }

    /// <summary>
    /// 初始化日志记录核心
    /// </summary>
    /// <param name="pathToMod">模组文件夹</param>
    /// <returns></returns>
    protected bool InitLogCore(string pathToMod)
    {
        const int maxLogFileSize = 10 * 1024 * 1024; // 10 MB
        LogFolderPath = Path.Combine(pathToMod, ModConfigData.LogFolder);

        TryCatch("创建日志文件夹", () =>
        {
            Directory.CreateDirectory(LogFolderPath);
            return true;
        });
        
        string infoPath = Path.Combine(LogFolderPath, "info.log");
        string warnPath = Path.Combine(LogFolderPath, "warn.log");
        string debugPath = Path.Combine(LogFolderPath, "debug.log");
        string errorPath = Path.Combine(LogFolderPath, "error.log");

        TryCatch("日志过大检测", () =>
        {
            foreach (var filePath in new string[] { infoPath, warnPath, debugPath, errorPath })
            {
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length > maxLogFileSize)
                    {
                        TryCatch($"日志文件过大，删除并重建: {filePath}", () =>
                        {
                            File.Delete(filePath);
                            return true;
                        });
                    }
                }
            }
            return true;
        });
        
        TryCatch("注册Info日志写入流", () =>
        {
            LogWriters[LocalLogType.Info] =
                new StreamWriter(new FileStream(infoPath, FileMode.Append, FileAccess.Write));
            return true;
        });
        TryCatch("注册Warn日志写入流", () =>
        {
            LogWriters[LocalLogType.Warn] =
                new StreamWriter(new FileStream(warnPath, FileMode.Append, FileAccess.Write));
            return true;
        });
        TryCatch("注册Debug日志写入流", () =>
        {
            LogWriters[LocalLogType.Debug] =
                new StreamWriter(new FileStream(debugPath, FileMode.Append, FileAccess.Write));
            return true;
        });
        TryCatch("注册Error日志写入流", () =>
        {
            LogWriters[LocalLogType.Error] =
                new StreamWriter(new FileStream(errorPath, FileMode.Append, FileAccess.Write));
            return true;
        });
        return true;
    }

    /// <summary>
    /// 记录日志到文件中
    /// </summary>
    /// <param name="type">日志类型</param>
    /// <param name="message">日志消息</param>
    public void LocalLog(LocalLogType type, string message)
    {
        if (LogWriters.TryGetValue(type, out var writer))
        {
            TryCatch("记录本地日志", () =>
            {
                lock (LogLock)
                {
                    using (var sw = new StreamWriter(writer.BaseStream, writer.Encoding, 1024, true) { AutoFlush = true })
                    {
                        sw.WriteLine($"{DateTime.Now:yyyy-MM-dd HH:mm:ss} - {type.ToString()} - {message}");
                    }
                }

                return true;
            });
        }
    }

    /// <summary>
    /// Hook一个任务函数, 跟踪函数的运行结果
    /// </summary>
    /// <param name="task">任务描述</param>
    /// <param name="func">任务闭包函数</param>
    /// <returns>任务执行情况</returns>
    public void LocalLogHook(string task, Func<Task> func)
    {
        Task? taskResult = null;
        LocalLog(LocalLogType.Debug, $"<任务: {task}> 开始");
        try
        {
            Task result = taskResult = func();
            LocalLog(LocalLogType.Debug, $"<任务: {task}> 执行结果: {result}({result.Id}, {result.CreationOptions}, 状态: {result.Status})");
            // return result;
        }
        catch (Exception e)
        {
            LocalLog(LocalLogType.Error, $"<任务: {task}> {e.Message}\n\t{e.StackTrace}\n\t{taskResult}({taskResult?.Id}, {taskResult?.CreationOptions}, 状态: {taskResult?.Status})");
            // return Task.FromException<Exception>(e);
        }
    }
}