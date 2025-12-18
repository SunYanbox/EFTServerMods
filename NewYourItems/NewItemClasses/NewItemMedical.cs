using System.Text.Json.Serialization;
using NewYourItems.infoClasses;
using NewYourItems.@abstract;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.NewItemClasses;

public class NewItemMedical: NewItemCommon
{
    [JsonPropertyName("medicalInfo")]
    public MedicalInfo? MedicalInfo { get; set; }
    
    protected override bool DoCustomValidation()
    {
        return base.DoCustomValidation();
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        base.DoCustomParameterValidation(oldResults);
        MedicalInfo ??= new MedicalInfo();
    }

    protected override void DoPropertyApplication(TemplateItemProperties props, DatabaseService? databaseService = null)
    {
        base.DoPropertyApplication(props, databaseService);
        MedicalInfo?.Update(props, databaseService);
    }
}