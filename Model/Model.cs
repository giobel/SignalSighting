using Autodesk.Navisworks.Api;
using ComApi = Autodesk.Navisworks.Api.Interop.ComApi;
using ComApiBridge = Autodesk.Navisworks.Api.ComApi;
using System;
using System.Collections.Generic;
using Rhino.Geometry;
using System.Windows;
using Autodesk.Navisworks.Api.Interop.ComApi;
using Autodesk.Navisworks.Api.DocumentParts;
using System.Linq;

namespace NavisCustomRibbon
{
        public class Model
        {
        private Document doc { get; set; }
        private Point3D signalBB3dcenter { get; set; }
        private List<Point3d> alignmentPoints = new List<Point3d>();
        private Point3d closestPt { get; set; }
        private Point3d signalCentroid { get; set; }
        private Curve[] plcurve { get; set; }
        private List<Point3d> ViewpointsPts { get; set; } //points on alignment
        private List<Point3d> DriverPts { get; set; } //points on alignmnet at driver height
        private List<string> SphereNames { get; set; }
        private string signalMark { get; set; }

        public string GetSignal()
        {
            doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            Selection storedSelection = new Selection();
            storedSelection = new Selection();
            storedSelection.CopyFrom(doc.CurrentSelection.ToSelection());

            signalBB3dcenter = storedSelection.ExplicitSelection[0].BoundingBox().Center;
            PropertyCategoryCollection signalProperties = storedSelection.ExplicitSelection[0].PropertyCategories;

            foreach (var property in signalProperties)
            {
                if (property.DisplayName == "Identity Data")
                {
                    try
                    {
                        DataProperty dp = property.Properties.Where(x => x.DisplayName == "Mark").First();
                        signalMark = dp.Value.ToDisplayString();
                    }
                    catch
                    {
                        signalMark = "N/A";
                    }
                }
            }

            return $"Signal {signalMark} selected\n" +
                $"Centroid XYZ: " +
                $"{Math.Round(signalBB3dcenter.X,3)}," +
                $"{Math.Round(signalBB3dcenter.Y,3)}," +
                $"{Math.Round(signalBB3dcenter.Z,3)}";
        }

        public string GetTrack()
        {
            ModelItemCollection oModelColl = Autodesk.Navisworks.Api.Application.ActiveDocument.CurrentSelection.SelectedItems;
            //convert to COM selection

            ComApi.InwOpState oState = ComApiBridge.ComApiBridge.State;

            ComApi.InwOpSelection oSel = ComApiBridge.ComApiBridge.ToInwOpSelection(oModelColl);

            CallbackGeomListener callbkListener = new CallbackGeomListener();

            foreach (ComApi.InwOaPath3 path in oSel.Paths())
            {
                foreach (ComApi.InwOaFragment3 frag in path.Fragments())
                {
                    // https://forums.autodesk.com/t5/navisworks-api/geometry-in-wrong-location-when-accessing-in-api/m-p/5896205/highlight/true
                    // https://github.com/jotpunktbee/SpeckleNavisworks/blob/08204aed7ee9f99d8b5ba50c5df102a4c95907a2/SpeckleNavisworks/Models/SearchComparisonPlugIn.cs 
                    ComApi.InwLTransform3f3 localToWorld = (ComApi.InwLTransform3f3)(object)frag.GetLocalToWorldMatrix();

                    Array localToWorldMatrix = (Array)(object)localToWorld.Matrix;
                    //Elements = ToArray<double>(localToWorldMatrix);
                    callbkListener.Elements = Utils.ToArray<double>(localToWorldMatrix);

                    callbkListener.LCS2WCS = localToWorld;


                    // generate the primitives
                    frag.GenerateSimplePrimitives(ComApi.nwEVertexProperty.eNORMAL, callbkListener);
                }
            }

            alignmentPoints = Utils.RemoveDuplicatePoints(callbkListener.rhinoPts, 0.001);

            alignmentPoints.Sort();


            return $"{alignmentPoints.Count} points";
        }

        public string Run(double speed, double interval, double driverHeight, bool isUpDirection, bool createRhinoModel)
        {
            try
            {
                //closest point method needs a polyline. degree is 1 so crv an pl match
                Polyline pl = new Polyline(alignmentPoints);

                //signal centroid in Rhino Coordinates
                signalCentroid = new Point3d(signalBB3dcenter.X, signalBB3dcenter.Y, signalBB3dcenter.Z);

                //point on alignment            
                closestPt = pl.ClosestPoint(signalCentroid);

                double t = pl.ClosestParameter(closestPt);

                plcurve = pl.ToPolylineCurve().Split(t);

                Polyline viewpointsPl = null;

                if (isUpDirection)
                {
                    plcurve[1].TryGetPolyline(out viewpointsPl);
                }
                else
                {
                    //DOWN direction
                    plcurve[0].TryGetPolyline(out viewpointsPl);
                    viewpointsPl.Reverse();
                }

                double totalLength = speed * 1000 * interval / 3600; //222m at 80km/h

                List<double> distances = new List<double>();

                double currentDistance = 0;

                for (int i = 0; i < interval; i++)
                {
                    currentDistance += totalLength / interval;
                    distances.Add(currentDistance);
                }

                Point3d previousViewpoint = closestPt + new Point3d(0, 0, driverHeight);


                List<Viewpoint> allViewpoints = new List<Viewpoint>();
                ViewpointsPts = new List<Point3d>();
                DriverPts = new List<Point3d>();
                SphereNames = new List<string>();

                for (int i = 0; i < distances.Count; i++)
                {
                    double p = Utils.PolylineEvaluateLength(viewpointsPl, distances[i]);

                    Point3d vp = viewpointsPl.PointAt(p);
                    Point3d driverPoint = vp + new Point3d(0, 0, 2.7);
                    Line l = new Line(vp, driverPoint);

                    ViewpointsPts.Add(vp);
                    DriverPts.Add(driverPoint);
                    SphereNames.Add(distances[i].ToString());

                    Viewpoint itemVp = new Viewpoint();

                    Vector3d trainDirection = previousViewpoint - driverPoint;

                    Vector3D vector3D = new Vector3D(trainDirection.X, trainDirection.Y, trainDirection.Z);

                    itemVp.Position = new Point3D(driverPoint.X, driverPoint.Y, driverPoint.Z);
                    itemVp.AlignDirection(vector3D);
                    itemVp.WorldUpVector = new UnitVector3D(0, 0, 1);
                    itemVp.AlignUp(new UnitVector3D(0, 0, 1));
                    itemVp.LinearSpeed = totalLength / interval; //22m/s


                    allViewpoints.Add(itemVp);

                    previousViewpoint = driverPoint;
                }

                if (signalMark == null)
                {
                    signalMark = "Missing Signal Mark";
                }

                FolderItem fi = new FolderItem();
                
                fi.DisplayName = signalMark;
                
                
                doc.SavedViewpoints.AddCopy(fi);


                DocumentSavedViewpoints oSavePts = doc.SavedViewpoints;

                GroupItem oFolder = null;

                foreach (SavedItem oEachItem in oSavePts.Value)
                {
                    if (oEachItem.DisplayName == signalMark)
                    {
                        oFolder = oEachItem as GroupItem;

                        break;
                    }
                }



                for (int i = allViewpoints.Count; i > 0; i--)
                {
                    double name = distances[i - 1];
                    
                    SavedViewpoint savedViewpoint = new SavedViewpoint(allViewpoints[i - 1]);
                    string simplifiedName = String.Format("{0:0.##}", name);
                    savedViewpoint.DisplayName = simplifiedName;
                    doc.SavedViewpoints.AddCopy(savedViewpoint);

                    oSavePts.Move(oSavePts.RootItem, oSavePts.Value.Count - 1, oFolder, oFolder.Children.Count);

                }


                

                if (createRhinoModel)
                {
                    CreateRhinoModel();
                }

            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message);
            }

            return "Completed";
        }

        public string CreateRhinoModel()
        {
            Rhino.Geometry.Line line = new Rhino.Geometry.Line(signalCentroid, closestPt);

            var model = new Rhino.FileIO.File3dm();

            model.Settings.ModelUnitSystem = Rhino.UnitSystem.Meters;

            Rhino.DocObjects.ObjectAttributes alignCurveAttr = new Rhino.DocObjects.ObjectAttributes() { Name = "Alignment Curve" };
            Rhino.DocObjects.ObjectAttributes firstCurveAttr = new Rhino.DocObjects.ObjectAttributes()
            {
                Name = "First Curve",
                ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject,
                ObjectColor = System.Drawing.Color.Purple
            };
            Rhino.DocObjects.ObjectAttributes secondCurveAttr = new Rhino.DocObjects.ObjectAttributes()
            {
                Name = "Second Curve",
                ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject,
                ObjectColor = System.Drawing.Color.Aqua
            };

            model.Objects.AddPoint(signalCentroid);
            model.Objects.AddPoint(closestPt);
            model.Objects.AddLine(line);
            model.Objects.AddCurve(plcurve[0], firstCurveAttr);
            model.Objects.AddCurve(plcurve[1], secondCurveAttr);

            for (int i = 0; i < ViewpointsPts.Count; i++)
            {
                model.Objects.AddLine(ViewpointsPts[i], DriverPts[i]);

                Rhino.DocObjects.ObjectAttributes sphereAttr = new Rhino.DocObjects.ObjectAttributes()
                {
                    Name = $"{SphereNames[i]}",
                    ColorSource = Rhino.DocObjects.ObjectColorSource.ColorFromObject,
                    ObjectColor = System.Drawing.Color.Coral
                };

                model.Objects.AddSphere(new Sphere(ViewpointsPts[i], 0.5), sphereAttr);
            }

            string rhinoPath = (@"C:\temp\navisRhino2.3dm");

            model.Write(rhinoPath, 6);

            doc.AppendFile(rhinoPath);

            return $"File saved {rhinoPath}";
        }
    }
    
}
