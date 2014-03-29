﻿using Trinity.Config.Combat;
using Trinity.DbProvider;
using Trinity.Technicals;
using Zeta.Common.Plugins;
using Zeta.Game;
using Zeta.Game.Internals.Actors;

namespace Trinity
{
    public partial class Trinity : IPlugin
    {
        private static bool RefreshGizmo(bool AddToCache)
        {
            // start as true, then set as false as we go. If nothing matches below, it will return true.
            AddToCache = true;

            bool openResplendentChest = c_InternalName.ToLower().Contains("chest_rare");

            // Ignore it if it's not in range yet, except health wells and resplendent chests if we're opening chests
            if ((c_RadiusDistance > CurrentBotLootRange || c_RadiusDistance > 50) && c_ObjectType != GObjectType.HealthWell && c_ObjectType != GObjectType.Shrine && c_RActorGuid != LastTargetRactorGUID)
            {
                AddToCache = false;
                c_IgnoreSubStep = "NotInRange";
            }

            // re-add resplendent chests
            if (openResplendentChest)
            {
                AddToCache = true;
                c_IgnoreSubStep = "";
            }

            // Retrieve collision sphere radius, cached if possible
            if (!CacheData.CollisionSphere.TryGetValue(c_ActorSNO, out c_Radius))
            {
                try
                {
                    c_Radius = c_diaObject.CollisionSphere.Radius;
                    // Minimum range clamp
                    if (c_Radius <= 1f)
                        c_Radius = 1f;

                    c_RadiusDistance = c_Radius + c_CentreDistance;
                }
                catch
                {
                    Logger.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "Safely handled exception getting collisionsphere radius for object {0} [{1}]", c_InternalName, c_ActorSNO);
                    c_IgnoreSubStep = "CollisionSphereException";
                    AddToCache = false;
                }
                CacheData.CollisionSphere.Add(c_ActorSNO, c_Radius);
            }

            // A "fake distance" to account for the large-object size of monsters
            c_RadiusDistance -= (float)c_Radius;
            if (c_RadiusDistance <= 1f)
                c_RadiusDistance = 1f;

            // Anything that's been disabled by a script
            bool isGizmoDisabledByScript = false;
            try
            {
                switch (c_ObjectType)
                {
                    case GObjectType.Shrine:
                    case GObjectType.Door:
                    case GObjectType.Container:
                    case GObjectType.Interactable:
                        isGizmoDisabledByScript = ((DiaGizmo)c_diaObject).IsGizmoDisabledByScript;
                        break;
                }
            }
            catch
            {
                Logger.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement,
                    "Safely handled exception getting Gizmo-Disabled-By-Script attribute for object {0} [{1}]", c_InternalName, c_ActorSNO);
                c_IgnoreSubStep = "isGizmoDisabledByScriptException";
                AddToCache = false;
            }
            if (isGizmoDisabledByScript)
            {
                AddToCache = false;
                c_IgnoreSubStep = "GizmoDisabledByScript";
                return AddToCache;
            }

            double minDistance;
            bool gizmoUsed = false;
            switch (c_ObjectType)
            {
                case GObjectType.Door:
                    {
                        AddToCache = true;

                        if (c_diaObject is DiaGizmo && ((DiaGizmo)c_diaObject).HasBeenOperated)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "Door has been operated";
                            return AddToCache;
                        }

                        try
                        {
                            string currentAnimation = c_CommonData.CurrentAnimation.ToString().ToLower();
                            gizmoUsed = currentAnimation.EndsWith("open") || currentAnimation.EndsWith("opening");

                            // special hax for A3 Iron Gates
                            if (currentAnimation.Contains("irongate") && currentAnimation.Contains("open"))
                                gizmoUsed = false;
                            if (currentAnimation.Contains("irongate") && currentAnimation.Contains("idle"))
                                gizmoUsed = true;
                        }
                        catch { }
                        if (gizmoUsed)
                        {
                            hashRGUIDBlacklist3.Add(c_RActorGuid);
                            AddToCache = false;
                            c_IgnoreSubStep = "Door is Open or Opening";
                            return AddToCache;
                        }

                        try
                        {
                            int gizmoState = c_CommonData.GetAttribute<int>(ActorAttributeType.GizmoState);
                            if (gizmoState == 1)
                            {
                                AddToCache = false;
                                c_IgnoreSubStep = "GizmoState=1";
                                return AddToCache;
                            }
                        }
                        catch
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "GizmoStateException";
                            return AddToCache;
                        }

                        if (AddToCache)
                        {
                            try
                            {
                                DiaGizmo door = null;
                                if (c_diaObject is DiaGizmo)
                                {
                                    door = (DiaGizmo)c_diaObject;

                                    if (door != null && door.IsGizmoDisabledByScript)
                                    {
                                        hashRGUIDBlacklist3.Add(c_RActorGuid);
                                        AddToCache = false;
                                        c_IgnoreSubStep = "DoorDisabledbyScript";
                                        return AddToCache;
                                    }
                                }
                                else
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "InvalidCastToDoor";
                                }
                            }

                            catch { }
                        }
                    }
                    break;
                case GObjectType.Interactable:
                    AddToCache = true;
                    // Special interactables
                    if (c_CentreDistance > 30f)
                    {
                        AddToCache = false;
                        c_IgnoreSubStep = "interactableDistance";
                        return AddToCache;
                    }
                    c_Radius = 4f;
                    break;
                case GObjectType.HealthWell:
                    {
                        AddToCache = true;
                        try
                        {
                            gizmoUsed = (c_CommonData.GetAttribute<int>(ActorAttributeType.GizmoCharges) <= 0 && c_CommonData.GetAttribute<int>(ActorAttributeType.GizmoCharges) > 0);
                        }
                        catch
                        {
                            Logger.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "Safely handled exception getting shrine-been-operated attribute for object {0} [{1}]", c_InternalName, c_ActorSNO);
                            AddToCache = true;
                            //return bWantThis;
                        }
                        if (gizmoUsed)
                        {
                            c_IgnoreSubStep = "GizmoCharges";
                            AddToCache = false;
                            return AddToCache;
                        }
                    }
                    break;
                case GObjectType.Shrine:
                    {
                        AddToCache = true;
                        // Shrines
                        // Check if either we want to ignore all shrines
                        if (!Settings.WorldObject.UseShrine)
                        {
                            // We're ignoring all shrines, so blacklist this one
                            c_IgnoreSubStep = "IgnoreAllShrinesSet";
                            AddToCache = false;
                            return AddToCache;
                        }

                        try
                        {
                            int gizmoState = c_CommonData.GetAttribute<int>(ActorAttributeType.GizmoState);
                            if (gizmoState == 1)
                            {
                                AddToCache = false;
                                c_IgnoreSubStep = "GizmoState=1";
                                return AddToCache;
                            }
                        }
                        catch
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "GizmoStateException";
                            return AddToCache;
                        }

                        // Determine what shrine type it is, and blacklist if the user has disabled it
                        switch (c_ActorSNO)
                        {
                            case 176077:  //Frenzy Shrine
                                if (!Settings.WorldObject.UseFrenzyShrine)
                                {
                                    hashRGUIDBlacklist60.Add(c_RActorGuid);
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    AddToCache = false;
                                }
                                if (Player.ActorClass == ActorClass.Monk && Settings.Combat.Monk.TROption.HasFlag(TempestRushOption.MovementOnly) && Hotbar.Contains(SNOPower.Monk_TempestRush))
                                {
                                    // Frenzy shrines are a huge time sink for monks using Tempest Rush to move, we should ignore them.
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            case 176076:  //Fortune Shrine
                                if (!Settings.WorldObject.UseFortuneShrine)
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            case 176074:  //Protection Shrine
                                if (!Settings.WorldObject.UseProtectionShrine)
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            case 260330:  //Empowered Shrine
                                if (!Settings.WorldObject.UseEmpoweredShrine)
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            case 176075:  //Enlightened Shrine
                                if (!Settings.WorldObject.UseEnlightenedShrine)
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            case 260331:  //Fleeting Shrine
                                if (!Settings.WorldObject.UseFleetingShrine)
                                {
                                    AddToCache = false;
                                    c_IgnoreSubStep = "IgnoreShrineOption";
                                    return AddToCache;
                                }
                                break;

                            default:
                                break;
                        }  //end switch

                        // Bag it!
                        c_Radius = 4f;
                        break;
                    }
                case GObjectType.Barricade:
                    {
                        AddToCache = true;

                        float maxRadiusDistance = -1f;

                        if (DataDictionary.DestructableObjectRadius.TryGetValue(c_ActorSNO, out maxRadiusDistance))
                        {
                            if (c_RadiusDistance < maxRadiusDistance)
                            {
                                AddToCache = true;
                                c_IgnoreSubStep = "";
                            }
                        }

                        // Set min distance to user-defined setting
                        minDistance = Settings.WorldObject.DestructibleRange + c_Radius;
                        if (ForceCloseRangeTarget)
                            minDistance += 6f;

                        // This object isn't yet in our destructible desire range
                        if (minDistance <= 0 || c_RadiusDistance > minDistance)
                        {
                            c_IgnoreSubStep = "NotInBarricadeRange";
                            AddToCache = false;
                            return AddToCache;
                        }

                        break;
                    }
                case GObjectType.Destructible:
                    {
                        AddToCache = false;

                        if (Player.ActorClass == ActorClass.Monk && Hotbar.Contains(SNOPower.Monk_TempestRush) && TimeSinceUse(SNOPower.Monk_TempestRush) <= 150)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "MonkTR";
                            break;
                        }

                        if (Player.ActorClass == ActorClass.Monk && Hotbar.Contains(SNOPower.Monk_SweepingWind) && GetHasBuff(SNOPower.Monk_SweepingWind))
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "MonkSW";
                            break;
                        }

                        if (!DataDictionary.ForceDestructibles.Contains(c_ActorSNO) && Settings.WorldObject.DestructibleOption == DestructibleIgnoreOption.ForceIgnore)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "ForceIgnoreDestructibles";
                            break;
                        }

                        // Set min distance to user-defined setting
                        minDistance = Settings.WorldObject.DestructibleRange;
                        if (ForceCloseRangeTarget)
                            minDistance += 6f;

                        // Only break destructables if we're stuck and using IgnoreNonBlocking
                        if (Settings.WorldObject.DestructibleOption == DestructibleIgnoreOption.DestroyAll)
                        {
                            minDistance += 12f;
                            AddToCache = true;
                            c_IgnoreSubStep = "";
                        }

                        float maxRadiusDistance = -1f;

                        if (DataDictionary.DestructableObjectRadius.TryGetValue(c_ActorSNO, out maxRadiusDistance))
                        {
                            if (c_RadiusDistance < maxRadiusDistance)
                            {
                                AddToCache = true;
                                c_IgnoreSubStep = "";
                            }
                        }
                        // Always add large destructibles within ultra close range
                        if (!AddToCache && c_Radius >= 10f && c_RadiusDistance <= 5.9f)
                        {
                            AddToCache = true;
                            c_IgnoreSubStep = "";
                            break;
                        }

                        // This object isn't yet in our destructible desire range
                        if (AddToCache && (minDistance <= 1 || c_RadiusDistance > minDistance) && PlayerMover.GetMovementSpeed() >= 1)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "NotInDestructableRange";
                        }
                        if (AddToCache && c_RadiusDistance <= 2f && PlayerMover.GetMovementSpeed() < 1)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "NotStuck2";
                        }

                        // Add if we're standing still and bumping into it
                        if (c_RadiusDistance <= 2f && PlayerMover.GetMovementSpeed() <= 0)
                        {
                            AddToCache = true;
                            c_IgnoreSubStep = "";
                        }

                        if (c_RActorGuid == LastTargetRactorGUID)
                        {
                            AddToCache = true;
                            c_IgnoreSubStep = "";
                        }

                        break;
                    }
                case GObjectType.Container:
                    {
                        AddToCache = false;

                        bool isRareChest = c_InternalName.ToLower().Contains("chest_rare") || DataDictionary.ResplendentChestIds.Contains(c_ActorSNO);
                        bool isChest = (!isRareChest && c_InternalName.ToLower().Contains("chest")) ||
                            c_InternalName.ToLower().Contains("rack") ||
                            DataDictionary.ContainerWhiteListIds.Contains(c_ActorSNO); // We know it's a container but this is not a known rare chest
                        bool isCorpse = c_InternalName.ToLower().Contains("corpse");

                        // We want to do some vendoring, so don't open anything new yet
                        if (ForceVendorRunASAP)
                        {
                            AddToCache = false;
                            c_IgnoreSubStep = "ForceVendorRunASAP";
                        }
                        // Already open, blacklist it and don't look at it again
                        bool chestOpen = false;
                        try
                        {
                            chestOpen = (c_CommonData.GetAttribute<int>(ActorAttributeType.ChestOpen) > 0);
                        }
                        catch
                        {
                            Logger.Log(TrinityLogLevel.Debug, LogCategory.CacheManagement, "Safely handled exception getting container-been-opened attribute for object {0} [{1}]", c_InternalName, c_ActorSNO);
                            c_IgnoreSubStep = "ChestOpenException";
                            AddToCache = false;
                            return AddToCache;
                        }

                        // Check if chest is open
                        if (chestOpen)
                        {
                            // It's already open!
                            AddToCache = false;
                            c_IgnoreSubStep = "AlreadyOpen";
                            return AddToCache;
                        }

                        if (isChest && Settings.WorldObject.OpenContainers && c_RadiusDistance <= Settings.WorldObject.ContainerOpenRange)
                        {
                            AddToCache = true;
                            return AddToCache;
                        }

                        if (isCorpse && Settings.WorldObject.InspectCorpses && c_RadiusDistance <= Settings.WorldObject.ContainerOpenRange)
                        {
                            AddToCache = true;
                            return AddToCache;
                        }

                        if (isRareChest && Settings.WorldObject.OpenRareChests)
                        {
                            AddToCache = true;
                            return AddToCache;
                        }

                        if (!isChest && !isCorpse && !isRareChest)
                        {
                            Logger.LogDebug("Possible Chest SNO: {0} ({1})", c_ActorSNO, c_InternalName);
                            c_IgnoreSubStep = "InvalidContainer";
                        }
                        else
                        {
                            c_IgnoreSubStep = "IgnoreContainer";
                        }
                        break;
                    }
            }
            return AddToCache;
        }
    }
}
