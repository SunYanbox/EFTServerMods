using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.records;

/// <summary>
/// 食物/饮品
/// </summary>
public class NewItemDrinkOrDrugs: NewItemCommon
{
    [JsonPropertyName("attributeInfo")]
    public AttributeInfo? AttributeInfo { get; set; }
    [JsonPropertyName("buffsInfo")]
    public BuffsInfo? BuffsInfo { get; set; }
    [JsonPropertyName("drinkDrugInfo")]
    public DrinkDrugInfo? DrinkDrugInfo { get; set; }
    
    protected override void DoPropertyApplication(TemplateItemProperties props, DatabaseService? databaseService = null)
    {
        AttributeInfo?.Update(props, databaseService);
        BuffsInfo?.Update(props, databaseService);
        DrinkDrugInfo?.Update(props, databaseService);
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        BuffsInfo ??= new BuffsInfo();
        if (BuffsInfo.StimulatorBuffs == null)
            BuffsInfo.StimulatorBuffs = "";
        if (DrinkDrugInfo == null) oldResults["DrinkDrugInfo"] = "DrinkDrugInfo属性不存在, 无法正确生成食物与饮品数据";
    }

    protected override bool DoCustomValidation()
    {
        return base.DoCustomValidation() && DrinkDrugInfo != null;
    }
}