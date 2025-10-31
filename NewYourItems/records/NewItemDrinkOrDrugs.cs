using System.Text.Json.Serialization;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;

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
    
    protected override void DoPropertyApplication(TemplateItemProperties props)
    {
        throw new NotImplementedException();
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        BuffsInfo ??= new BuffsInfo();
        if (BuffsInfo.StimulatorBuffs == null)
            BuffsInfo.StimulatorBuffs = "";
    }

    protected override bool DoCustomValidation()
    {
        return base.DoCustomValidation();
    }
}