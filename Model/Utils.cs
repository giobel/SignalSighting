using System;
using System.Collections.Generic;
using Rhino.Geometry;

namespace NavisCustomRibbon
{
    public static class Utils
    {
        public static List<Point3d> RemoveDuplicatePoints(List<Point3d> points, double tolerance)
        {
            List<Point3d> cleanedPoints = points;
            // Compare against the square of the tolerance
            double t2 = tolerance * tolerance;

            // Go over each point on the list
            for (int i = 0; i < cleanedPoints.Count; i++)
            {
                // Compare with the rest of the points
                // Loop backwards to make sure we don't skip any item
                for (int j = cleanedPoints.Count - 1; j > i; j--)
                {
                    // Use the squared distance to compare points faster
                    double d2 = DistanceSquared(cleanedPoints[i], cleanedPoints[j]);
                    if (d2 < t2)
                    {
                        //Print(i + " is duplicate of " + j);
                        cleanedPoints.RemoveAt(j);
                    }
                }
            }

            return cleanedPoints;
        }
        public static double DistanceSquared(Point3d a, Point3d b)
        {
            // Calculate sides of the triangle
            double dx = b.X - a.X;
            double dy = b.Y - a.Y;
            double dz = b.Z - a.Z;

            // Calculate squared length of the larger side of the triangle
            double d2 = dx * dx + dy * dy + dz * dz;

            return d2;
        }
        public static T[] ToArray<T>(Array arr)
        {
            T[] result = new T[arr.Length];
            Array.Copy(arr, result, result.Length);
            return result;
        }

        public static double PolylineEvaluateLength(Polyline pl, double distance)
        {
            Line[] segments = pl.GetSegments();
            List<double> curveLengthBehindPoint = new List<double>();
            double curveLengthAfterPoint = 0;
            double parameter = -1;

            for (int i = 0; i < segments.Length; i++)
            {
                curveLengthAfterPoint += segments[i].Length;
                curveLengthBehindPoint.Add(curveLengthAfterPoint);
                if (distance < curveLengthAfterPoint)
                {
                    try
                    {
                        parameter = (distance - curveLengthBehindPoint[i - 1]) / (segments[i].Length) + i;
                    }
                    //if there is only one segment
                    catch
                    {
                        parameter = (distance - curveLengthBehindPoint[i]) / (segments[i].Length) + i;
                    }
                    break;
                }
            }

            return parameter;
        }

    }
}
