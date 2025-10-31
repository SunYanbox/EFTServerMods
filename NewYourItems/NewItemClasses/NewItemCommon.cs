using System.Text.Json.Serialization;
using NewYourItems.@abstract;
using NewYourItems.infoClasses;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.NewItemClasses;

[Injectable]
public class NewItemCommon: AbstractNewItem
{
    [JsonPropertyName("attributeInfo")]
    public AttributeInfo? AttributeInfo { get; set; }
    [JsonPropertyName("buffsInfo")]
    public BuffsInfo? BuffsInfo { get; set; }
    
    protected override bool DoCustomValidation()
    {
        return true;
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        if (BuffsInfo != null)
        {
            if (BuffsInfo.StimulatorBuffs == null)
            {
                BuffsInfo.StimulatorBuffs = "";
                BuffsInfo.Buffs = null;
            }
        }
        
    }

    protected override void DoPropertyApplication(TemplateItemProperties props, DatabaseService? databaseService = null)
    {
        AttributeInfo?.Update(props, databaseService);
        BuffsInfo?.Update(props, databaseService);
    }
}