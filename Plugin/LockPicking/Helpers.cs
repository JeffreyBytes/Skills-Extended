﻿using System;
using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.InventoryLogic;
using SkillsExtended.Models;
using System.Collections.Generic;
using System.Linq;
using SkillsExtended.Helpers;
using SkillsExtended.Skills;

namespace SkillsExtended.LockPicking;

internal static class Helpers
{
    public static readonly Dictionary<string, int> DoorAttempts = [];
    public static readonly List<string> InspectedDoors = [];

    private static SkillManager _skills => Utils.GetActiveSkillManager();
    private static Player _player => Singleton<GameWorld>.Instance.MainPlayer;

    private static LockPickingData _lockPicking => Plugin.SkillData.LockPickingSkill;

    private static readonly Dictionary<string, Dictionary<string, int>> LocationDoorIdLevels = new()
    {
        {"factory4_day", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
        {"factory4_night", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Factory},
        {"Woods", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Woods},
        {"bigmap", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Customs},
        {"Interchange", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Interchange},
        {"RezervBase", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Reserve},
        {"Shoreline", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Shoreline},
        {"laboratory", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Labs},
        {"Lighthouse", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Lighthouse},
        {"TarkovStreets", Plugin.SkillData.LockPickingSkill.DoorPickLevels.Streets},
        {"Sandbox", Plugin.SkillData.LockPickingSkill.DoorPickLevels.GroundZero},
    };
    
    /// <summary>
    /// Get the door level given a location ID and door ID
    /// </summary>
    /// <param name="locationId"></param>
    /// <param name="doorId"></param>
    /// <returns>Door level if found, -1 if not found</returns>
    public static int GetLevelForDoor(string locationId, string doorId)
    {
        if (!LocationDoorIdLevels.TryGetValue(locationId, out var levels))
        {
            Plugin.Log.LogError($"Could not find location ID: {locationId}");
            return -1;
        }
        
        if (levels.TryGetValue(doorId, out var level))
        {
            return level;
        }

        return -1;
    }

    /// <summary>
    /// Get any lockpick in the players equipment inventory
    /// </summary>
    /// <returns>All lockpick items in the players inventory</returns>
    public static IEnumerable<Item> GetLockPicksInInventory()
    {
        return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Where(x => x.TemplateId == "6622c28aed7e3bc72e301e22");
    }

    /// <summary>
    /// Gets if a flipper zero exists in the inventory
    /// </summary>
    /// <returns>true if in inventory</returns>
    public static bool IsFlipperZeroInInventory()
    {
        return Plugin.Session.Profile.Inventory.GetPlayerItems(EPlayerItems.Equipment)
            .Any(x => x.TemplateId == "662400eb756ca8948fe64fe8");
    }

    private static float xpToApply = 0.0f;
    
    public static void ApplyLockPickActionXp(WorldInteractiveObject interactiveObject, GamePlayerOwner owner, bool isInspect = false, bool isFailure = false)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        var xpExists = Plugin.SkillData.LockPickingSkill.XpTable.TryGetValue(doorLevel.ToString(), out var xp);

        if (!xpExists) return;
        
        xpToApply = isInspect
            ? xp * Plugin.SkillData.LockPickingSkill.InspectLockXpRatio
            : xp;

        // Failures recieve 25% xp
        xpToApply = isFailure
            ? xpToApply * 0.25f
            : xpToApply;
            
        
        Singleton<GameWorld>.Instance.MainPlayer.ExecuteSkill(CompleteLockPickAction);
    }

    private static void CompleteLockPickAction()
    {
        if (xpToApply == 0.0f) return;
        
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        
        skillMgrExt.LockPickAction.Complete(xpToApply);
    }
    
    public static void DisplayInspectInformation(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        int doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        // Display inspection info
        NotificationManagerClass.DisplayMessageNotification($"Key for door is {Plugin.Keys.KeyLocale[interactiveObject.KeyId]}");
        NotificationManagerClass.DisplayMessageNotification($"Lock level {doorLevel} chance for success {CalculateChanceForSuccess(interactiveObject, owner)}%");
    }

    public static float CalculateChanceForSuccess(WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        var doorLevel = GetLevelForDoor(owner.Player.Location, interactiveObject.Id);

        var levelDifference = _skills.Lockpicking.Level - doorLevel;

        var baseSuccessChance = InspectedDoors.Contains(interactiveObject.Id)
            ? _lockPicking.PickBaseSuccessChance + 10
            : _lockPicking.PickBaseSuccessChance;

        var difficultyModifier = _lockPicking.PickBaseDifficultyMod;

        // Never below 0, never above 100
        var successChance = UnityEngine.Mathf.Clamp(baseSuccessChance + (levelDifference * difficultyModifier), 0f, 100f);

        return successChance;
    }
    
    /// <summary>
    /// Returns true if the pick attempt succeeded
    /// </summary>
    /// <returns></returns>
    public static bool IsAttemptSuccessful(int doorLevel, WorldInteractiveObject interactiveObject, GamePlayerOwner owner)
    {
        var levelDifference = _skills.Lockpicking.Level - doorLevel;
        
        // Player level is high enough to always pick this lock
        if (levelDifference > 10)
        {
            return true;
        }

        // Never below 0, never above 100
        var successChance = Helpers.CalculateChanceForSuccess(interactiveObject, owner);
        var roll = UnityEngine.Random.Range(0f, 100f);
        
        return successChance > roll;
    }

    public static float CalculateTimeForAction(float baseTime)
    {
        var skillMgrExt = Singleton<SkillManagerExt>.Instance;
        
        return (baseTime * (1 - skillMgrExt.LockPickingTimeBuff));
    }
}
