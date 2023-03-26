using System;
using System.Collections.Generic;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;
using Rhino.Geometry;

namespace NavisCustomRibbon
{
    class CallbackGeomListener : ComApi.InwSimplePrimitivesCB
    {
        public ComApi.InwLTransform3f3 LCS2WCS;
        public List<string> coords = new List<string>();
        public List<Point3d> rhinoPts = new List<Point3d>();
        public double[] Elements;

        public void Line(ComApi.InwSimpleVertex v1, ComApi.InwSimpleVertex v2)

        {

            Array array_v1 = (Array)(object)v1.coord;
            Array array_v2 = (Array)(object)v2.coord;


            double w = (Elements[3] * Convert.ToDouble(array_v1.GetValue(1))) +
                        (Elements[7] * Convert.ToDouble(array_v1.GetValue(2))) +
                        (Elements[11] * Convert.ToDouble(array_v1.GetValue(3))) +
                        Elements[15];

            double newPointX = ((Elements[0] * Convert.ToDouble(array_v1.GetValue(1))) + (Elements[4] * Convert.ToDouble(array_v1.GetValue(2))) + (Elements[8] * Convert.ToDouble(array_v1.GetValue(3))) + Elements[12]) / w;
            double newPointY = ((Elements[1] * Convert.ToDouble(array_v1.GetValue(1))) + (Elements[5] * Convert.ToDouble(array_v1.GetValue(2))) + (Elements[9] * Convert.ToDouble(array_v1.GetValue(3))) + Elements[13]) / w;
            double newPointZ = ((Elements[2] * Convert.ToDouble(array_v1.GetValue(1))) + (Elements[6] * Convert.ToDouble(array_v1.GetValue(2))) + (Elements[10] * Convert.ToDouble(array_v1.GetValue(3))) + Elements[14]) / w;

            rhinoPts.Add(new Point3d(newPointX, newPointY, newPointZ));


            double w2 = (Elements[3] * Convert.ToDouble(array_v2.GetValue(1))) +
            (Elements[7] * Convert.ToDouble(array_v2.GetValue(2))) +
            (Elements[11] * Convert.ToDouble(array_v2.GetValue(3))) +
            Elements[15];

            double newPoint2X = ((Elements[0] * Convert.ToDouble(array_v2.GetValue(1))) + (Elements[4] * Convert.ToDouble(array_v2.GetValue(2))) + (Elements[8] * Convert.ToDouble(array_v2.GetValue(3))) + Elements[12]) / w2;
            double newPoint2Y = ((Elements[1] * Convert.ToDouble(array_v2.GetValue(1))) + (Elements[5] * Convert.ToDouble(array_v2.GetValue(2))) + (Elements[9] * Convert.ToDouble(array_v2.GetValue(3))) + Elements[13]) / w2;
            double newPoint2Z = ((Elements[2] * Convert.ToDouble(array_v2.GetValue(1))) + (Elements[6] * Convert.ToDouble(array_v2.GetValue(2))) + (Elements[10] * Convert.ToDouble(array_v2.GetValue(3))) + Elements[14]) / w2;

            rhinoPts.Add(new Point3d(newPoint2X, newPoint2Y, newPoint2Z));

            coords.Add($"{newPointX},{newPointY},{newPointZ}");
            coords.Add($"{newPoint2X},{newPoint2Y},{newPoint2Z}");

        }



        public void Point(ComApi.InwSimpleVertex v1)
        {

            // do your work

        }



        public void SnapPoint(ComApi.InwSimpleVertex v1)
        {

            // do your work

        }



        public void Triangle(ComApi.InwSimpleVertex v1, ComApi.InwSimpleVertex v2, ComApi.InwSimpleVertex v3)
        {

            // do your work

        }




    }
}
