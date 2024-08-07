﻿using Mono.Cecil;
using System.Collections.Generic;
using System;
using System.Linq;
using BepInEx.Logging;
using System.Diagnostics;
using JetBrains.Annotations;
using FieldAttributes = Mono.Cecil.FieldAttributes;

public static class Patcher
{
    public static IEnumerable<string> TargetDLLs { get; } = new string[] { "Assembly-CSharp.dll" };
    public static TypeDefinition SkillManager;
    
    public static void Patch(ref AssemblyDefinition assembly)
    {
        try
        {
            SkillManager = assembly.MainModule.GetType("EFT.SkillManager");
            
            PatchNewBuffs(ref assembly);
            PatchNewAnim(ref assembly);
            
            Logger.CreateLogSource("Skills Extended PrePatch").LogInfo("Patching Complete!");
        } catch (Exception ex)
        {
            // Get stack trace for the exception with source file information
            var st = new StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();

            Logger.CreateLogSource("Skills Extended PrePatch").LogInfo("Error When Patching: " + ex.Message + " - Line " + line);
        }
    }
    
    private static FieldDefinition CreateNewEnum(ref AssemblyDefinition assembly, [CanBeNull] string AttributeName, string EnumName, TypeDefinition EnumClass, int CustomConstant)
    {
        var enumAttributeClass = assembly.MainModule.GetType("GAttribute21");
        
        var attributeConstructor = enumAttributeClass.Methods.First(m => m.IsConstructor);
        
        var attribute = new CustomAttribute(attributeConstructor);

        if (AttributeName is not null)
        {
            var valueArgument = new CustomAttributeArgument(assembly.MainModule.TypeSystem.String, AttributeName);
            attribute.ConstructorArguments.Add(valueArgument);
        }
        
        var newEnum = new FieldDefinition(
            EnumName, 
            FieldAttributes.Public | FieldAttributes.Static | FieldAttributes.Literal | FieldAttributes.HasDefault, EnumClass) 
            { Constant = CustomConstant };
        
        newEnum.CustomAttributes.Add(attribute);

        return newEnum;
    }

    private static void PatchNewBuffs(ref AssemblyDefinition assembly)
    {
        // New Buffs Enums
        var buffEnums = assembly.MainModule.GetType("EFT.EBuffId");

        var firstAidHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidHealingSpeed", 
            "FirstAidHealingSpeed", 
            buffEnums, 
            1000);
        
        var firstAidMaxHpEnum = CreateNewEnum(
            ref assembly, 
            "FirstAidMaxHp", 
            "FirstAidMaxHp",
            buffEnums, 
            1001);
        
        buffEnums.Fields.Add(firstAidHealingSpeedEnum);
        buffEnums.Fields.Add(firstAidMaxHpEnum);
        
        var fieldMedicineHealingSpeedEnum = CreateNewEnum(
            ref assembly, 
            "FieldMedicineSpeed", 
            "FieldMedicineSpeed", 
            buffEnums, 
            1002);
        
        buffEnums.Fields.Add(fieldMedicineHealingSpeedEnum);

        var usecArSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsRecoil", 
            "UsecArSystemsRecoil", 
            buffEnums, 
            1003);
        
        var usecArSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "UsecArSystemsErgo", 
            "UsecArSystemsErgo", 
            buffEnums, 
            1004);
        
        
        buffEnums.Fields.Add(usecArSystemsRecoilEnum);
        buffEnums.Fields.Add(usecArSystemsErgoEnum);
        
        var bearAkSystemsRecoilEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsRecoil", 
            "BearAkSystemsRecoil", 
            buffEnums, 
            1005);
        
        var bearAkSystemsErgoEnum = CreateNewEnum(
            ref assembly, 
            "BearAkSystemsErgo", 
            "BearAkSystemsErgo", 
            buffEnums, 
            1006);
        
        buffEnums.Fields.Add(bearAkSystemsRecoilEnum);
        buffEnums.Fields.Add(bearAkSystemsErgoEnum);
        
        var lockpickingTimeReduction = CreateNewEnum(
            ref assembly, 
            "LockpickingTimeReduction", 
            "LockpickingTimeReduction", 
            buffEnums, 
            1007);
        
        var lockpickingUseElite = CreateNewEnum(
            ref assembly, 
            "LockpickingUseElite", 
            "LockpickingUseElite", 
            buffEnums, 
            1008);
        
        buffEnums.Fields.Add(lockpickingTimeReduction);
        buffEnums.Fields.Add(lockpickingUseElite);
    }

    private static void PatchNewAnim(ref AssemblyDefinition assembly)
    {
        var animEnum = assembly.MainModule.GetType("EFT.InputSystem.ECommand");
        
        var lockPickingAnimStart = CreateNewEnum(
            ref assembly, 
            null, 
            "LockPickingStart", 
            animEnum, 
            1000);
        
        var lockPickingAnimEnd = CreateNewEnum(
            ref assembly, 
            null, 
            "LockPickingEnd", 
            animEnum, 
            1001);
        
        animEnum.Fields.Add(lockPickingAnimStart);
        animEnum.Fields.Add(lockPickingAnimEnd);
    }
}