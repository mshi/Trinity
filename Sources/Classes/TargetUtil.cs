﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Zeta.Common;

namespace GilesTrinity
{
    public class TargetUtil
    {
        public static Vector3 GetBestClusterPoint(float radius = 15f, float maxRange = 15f)
        {
            using (new Technicals.PerformanceLogger("TargetUtil.GetBestClusterPoint"))
            {
                Vector3 bestClusterPoint = Vector3.Zero;
                var clusterUnits =
                    (from u in GilesTrinity.GilesObjectCache
                     where u.Type == GObjectType.Unit
                     orderby u.IsElite descending
                     orderby u.NearbyUnitsWithinDistance(radius) descending
                     orderby u.CentreDistance
                     orderby u.HitPointsPct descending
                     select u.Position).ToList();

                if (clusterUnits.Any())
                    bestClusterPoint = clusterUnits.FirstOrDefault();
                else if (GilesTrinity.CurrentTarget != null)
                    bestClusterPoint = GilesTrinity.CurrentTarget.Position;
                else
                    bestClusterPoint = GilesTrinity.PlayerStatus.CurrentPosition;

                return bestClusterPoint;
            }
        }

        public static bool AnyMobsInRange(float range = 10f)
        {
            return (from o in GilesTrinity.GilesObjectCache
                    where o.Type == GObjectType.Unit &&
                    o.RadiusDistance <= range
                    select o).Any();
        }

        public static bool AnyMobsInRange(float range = 10f, int minCount = 1)
        {
            return (from o in GilesTrinity.GilesObjectCache
                    where o.Type == GObjectType.Unit &&
                    o.RadiusDistance <= range
                    select o).Count() >= minCount; 
        }

        public static bool AnyElitesInRange(float range = 10f)
        {
            return (from o in GilesTrinity.GilesObjectCache
                    where o.Type == GObjectType.Unit &&
                    o.IsBossOrEliteRareUnique &&
                    o.RadiusDistance <= range
                    select o).Any();
        }

        public static bool AnyElitesInRange(float range = 10f, int minCount = 1)
        {
            return (from o in GilesTrinity.GilesObjectCache
                    where o.Type == GObjectType.Unit &&
                    o.IsBossOrEliteRareUnique &&
                    o.RadiusDistance <= range
                    select o).Count() > minCount;
        }

        public static bool IsEliteTargetInRange(float range = 10f)
        {
            return GilesTrinity.CurrentTarget != null && GilesTrinity.CurrentTarget.IsBossOrEliteRareUnique && GilesTrinity.CurrentTarget.RadiusDistance <= range;
        }

    }
}