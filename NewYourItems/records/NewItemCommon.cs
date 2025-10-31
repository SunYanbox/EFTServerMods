using System.Text.Json.Serialization;
using SPTarkov.DI.Annotations;
using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Eft.Common.Tables;
using SPTarkov.Server.Core.Models.Spt.Mod;
using SPTarkov.Server.Core.Services;

namespace NewYourItems.records;


[Injectable]
public class NewItemCommon: AbstractNewItem
{
    protected override bool DoCustomValidation()
    {
        return true;
    }

    protected override void DoCustomParameterValidation(Dictionary<string, string> oldResults)
    {
        
    }

    protected override void DoPropertyApplication(TemplateItemProperties props, DatabaseService? databaseService = null) {}
}