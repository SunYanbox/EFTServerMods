using System.Text.Json.Serialization;
using NewYourItems.infoClasses;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.NewItemClasses;

/// <summary>
/// 食物/饮品
/// </summary>
public class NewItemDrinkOrDrugs: NewItemCommon
{
    
    [JsonPropertyName("drinkDrugInfo")]
    public DrinkDrugInfo? DrinkDrugInfo { get; set; }
    
    protected override void DoPropertyApplication(TemplateItemProperties props, DatabaseService? databaseService = null)
    {
        base.DoPropertyApplication(props, databaseService);
        DrinkDrugInfo?.Update(props, databaseService);
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        base.DoCustomParameterValidation(oldResults);
        if (DrinkDrugInfo == null) oldResults["DrinkDrugInfo"] = "DrinkDrugInfo属性不存在, 无法正确生成食物与饮品数据";
    }

    protected override bool DoCustomValidation()
    {
        return base.DoCustomValidation() && DrinkDrugInfo != null;
    }
}