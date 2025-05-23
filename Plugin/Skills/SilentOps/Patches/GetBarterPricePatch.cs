﻿using System.Linq;
using System.Reflection;
using EFT;
using EFT.InventoryLogic;
using HarmonyLib;
using SkillsExtended.Skills.Core;
using SPT.Reflection.Patching;
using SPT.Reflection.Utils;
using UnityEngine;

namespace SkillsExtended.Skills.SilentOps.Patches;

public class GetBarterPricePatch : ModulePatch
{
    public static Item Selecteditem;
    
    protected override MethodBase GetTargetMethod()
    {
        return AccessTools.Method(typeof(TraderAssortmentControllerClass),
            nameof(TraderAssortmentControllerClass.GetBarterPrice));
    }

    [PatchPostfix]
    private static void Postfix(TraderAssortmentControllerClass __instance, ref TraderClass.GStruct264? __result, Item[] items, TraderClass ___traderClass)
    {
        if (items.IsNullOrEmpty()) return;
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;
        
        var scheme = __instance.GetSchemeForItem(items[0]);

        if (scheme is null) return;
        
        float price = 0;
        foreach (var item in items)
        {
            var barterScheme = __instance.GetSchemeForItem(item);
            
            if (barterScheme is null) continue;
            
            var num2 = Mathf.Ceil((float)barterScheme.Sum(TraderAssortmentControllerClass.Class1930.class1930_0.method_0));

            var bonus = 1f - SkillManagerExt.Instance(EPlayerSide.Usec).SilentOpsSilencerCostRedBuff;

            // Silencer Type
            if (item is SilencerItemClass)
            {
                num2 *= bonus;
            }
            
            price += num2;
        }

        Selecteditem = __instance.SelectedItem;
        
        __result = new TraderClass.GStruct264(scheme[0][0]._tpl, (int)Mathf.Ceil(price));
    }
}

public class RequiredItemsCountPatch : ModulePatch
{
    protected override MethodBase GetTargetMethod()
    {
        var type = PatchConstants.EftTypes.SingleCustom(t => t.GetProperty("RequiredItemsCount") != null);
        
        return AccessTools.PropertyGetter(type, "RequiredItemsCount");
    }

    [PatchPostfix]
    private static void Postfix(GClass2064 __instance, ref int __result)
    {
        // Suppressor type
        if (GetBarterPricePatch.Selecteditem is not SilencerItemClass) return;
        if (!SkillsPlugin.SkillData.SilentOps.Enabled) return;
        
        var bonus = 1f - SkillManagerExt.Instance(EPlayerSide.Usec).SilentOpsSilencerCostRedBuff;

        __result = (int)Mathf.Ceil(__result * bonus);
    }
}
