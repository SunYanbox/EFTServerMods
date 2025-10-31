using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.records;

/// <summary>
/// 抽象信息基类
/// </summary>
public abstract record AbstractInfo
{
    [JsonIgnore] public static LocalLog? LocalLog;
    /// <summary>
    /// 需要更新Buffs时重写这个属性
    /// > [JsonIgnore] public new static bool ShouldUpdateDatabaseService => true;
    /// </summary>
    [JsonIgnore] public static bool ShouldUpdateDatabaseService => false;
    
    /// <summary>
    /// 封装更新TemplateItemProperties的逻辑
    /// </summary>
    /// <param name="properties"></param>
    public abstract void UpdateProperties(TemplateItemProperties properties);

    /// <summary>
    /// 更新DatabaseService
    /// </summary>
    /// <param name="databaseService"></param>
    public virtual void UpdateDatabaseService(DatabaseService databaseService) {}
    
    public override string ToString()
    {
        return $"{GetType().Name} {LocalLog.ToStringExcludeNulls(this)}";
    }
}