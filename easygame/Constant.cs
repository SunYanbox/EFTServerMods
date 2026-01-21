using SPTarkov.Server.Core.Models.Common;
using SPTarkov.Server.Core.Models.Enums;

namespace easygame;

public static class Constant
{
    public const string ModName = "EasyGameMod";

    
    // 针剂基类
    public const string SimBase = "5448f3a64bdc2d60728b456a"; // BaseClasses.STIMULATOR

    // 卢布
    public static readonly MongoId RubId = Money.ROUBLES;
    
    // 猛攻: 黄(Propital); 体力: 蓝(SJ6); 负重: MULE; 绝地: 红(SJ1); 永劫轮回: 紫(Zagustin)
}