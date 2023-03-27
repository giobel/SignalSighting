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
using Autodesk.Navisworks.Api.Interop;
using Autodesk.Navisworks.Internal.ApiImplementation;
using System.Diagnostics;
using System.Xml.Linq;

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
        private double scaleFactor { get; set; }

        public string GetSignal()
        {
            Debug.WriteLine("********Get signal");

            scaleFactor = LcOaUnit.ScaleFactor(Units.Meters, Autodesk.Navisworks.Api.Application.ActiveDocument.Units);

            Debug.WriteLine($"scale factor {scaleFactor}");

            if (scaleFactor != 1)
            {
                //MessageBox.Show("Model units must be in metres.\nPlease import the alignment file first or change the .ifc file import settings");
                //return "Error: model units not in metres";
            }

            doc = Autodesk.Navisworks.Api.Application.ActiveDocument;
            Selection storedSelection = new Selection();
            storedSelection = new Selection();
            storedSelection.CopyFrom(doc.CurrentSelection.ToSelection());

            /*
            if (doc.Units.ToString() == "Feet")
            {
                MessageBox.Show($"Model units are {doc.Units.ToString()}. Please change them to metres");
            }
            else
            {
                if (storedSelection.ExplicitSelection[0].Transform.Linear.ToString().Contains("0.001"))
                {
                    //model has been scaled so it's ok to procede
                }
                else
                {
                    MessageBox.Show("Model is in Millimiters. Please scale by 0.001");
                }
            }*/



            signalBB3dcenter = storedSelection.ExplicitSelection[0].BoundingBox().Center;

            Debug.WriteLine("Signal Bbox center:");
            Debug.WriteLine(PrintPoint(signalBB3dcenter));


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
                $"Centre: " +
                String.Format("{0:0.##},", signalBB3dcenter.X) +
                String.Format("{0:0.##},", signalBB3dcenter.Y) +
                String.Format("{0:0.##}", signalBB3dcenter.Z);
        }

        private string PrintPoint(Point3d rhinoPt)
        {
            return String.Format("{0:0.##},", rhinoPt.X) + String.Format("{0:0.##},", rhinoPt.Y)+ String.Format("{0:0.##},", rhinoPt.Z);
        }

        private string PrintPoint(Point3D navisPt)
        {
            return String.Format("{0:0.##},", navisPt.X) + String.Format("{0:0.##},", navisPt.Y) + String.Format("{0:0.##},", navisPt.Z);
        }


        public string GetTrack()
        {
            Debug.WriteLine("*****Get Track");

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

            Debug.WriteLine("Alignment points");

            foreach (Point3d item in alignmentPoints)
            {
                Debug.WriteLine(PrintPoint(item));
            }

            return $"{alignmentPoints.Count} points";
        }



        public string Run(double speed, double interval, double driverHeight, bool isUpDirection, bool createRhinoModel)
        {
            Debug.WriteLine("*****RUN");
            //signal centroid should be in m. Disable "Revit IFC" from file options
            try
            {
                if (alignmentPoints.Count < 1)
                {
                    MessageBox.Show("Please select a track alignment");
                    return "Error track alignment not found";
                }

                //closest point method needs a polyline. degree is 1 so crv an pl match
                Polyline pl = new Polyline(alignmentPoints);
                
                Debug.WriteLine("Polyline Start and End:");
                Debug.WriteLine(PrintPoint(pl[0]));
                Debug.WriteLine(PrintPoint(pl[pl.Count-1]));
                Debug.WriteLine("Polyline length");
                Debug.WriteLine(pl.Length);

                //signal centroid in Rhino Coordinates
                signalCentroid = new Point3d(signalBB3dcenter.X, signalBB3dcenter.Y, signalBB3dcenter.Z);

                Debug.WriteLine("Signal Centroid in Rhino coordinates:");
                Debug.WriteLine(PrintPoint(signalCentroid));

                //point on alignment            
                closestPt = pl.ClosestPoint(signalCentroid);

                Debug.WriteLine("Closest Point:");
                Debug.WriteLine(PrintPoint(closestPt));

                double t = pl.ClosestParameter(closestPt);

                Debug.WriteLine("Split parameter on curve:");
                Debug.WriteLine(t.ToString());

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

                Debug.WriteLine("Split Viewpoints polyline start and end points:");
                Debug.WriteLine(PrintPoint(viewpointsPl[0]));
                Debug.WriteLine(PrintPoint(viewpointsPl[viewpointsPl.Count-1]));
                Debug.WriteLine("Polyline length");
                Debug.WriteLine(viewpointsPl.Length);

                double totalLength = speed * 1000 * interval * scaleFactor / 3600; //222m at 80km/h

                List<double> distances = new List<double>();

                double currentDistance = 0;

                for (int i = 0; i < interval; i++)
                {
                    currentDistance += totalLength / interval;
                    distances.Add(currentDistance);
                }

                Point3d previousViewpoint = closestPt + new Point3d(0, 0, driverHeight * scaleFactor);


                List<Viewpoint> allViewpoints = new List<Viewpoint>();
                ViewpointsPts = new List<Point3d>();
                DriverPts = new List<Point3d>();
                SphereNames = new List<string>();

                for (int i = 0; i < distances.Count; i++)
                {
                    Debug.WriteLine(i);
                    Debug.WriteLine(distances[i]);

                    double p = Utils.PolylineEvaluateLength(viewpointsPl, distances[i]);
                    Debug.WriteLine("Parameter on polyline");
                    Debug.WriteLine(p);


                    Point3d vp = viewpointsPl.PointAt(p);

                    Debug.WriteLine("ViewPoint on Polyline");
                    Debug.WriteLine(PrintPoint(vp));

                    Point3d driverPoint = vp + new Point3d(0, 0, driverHeight * scaleFactor);

                    Debug.WriteLine("Driverpoint on Polyline");
                    Debug.WriteLine(PrintPoint(driverPoint));

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
                    itemVp.LinearSpeed = totalLength * scaleFactor / interval; //22m/s

                    Debug.WriteLine("Viewpoints");
                    Debug.WriteLine(PrintPoint(itemVp.Position));

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

            string rhinoPath = (@"C:\temp\NavisRhinoTemp.3dm");

            model.Write(rhinoPath, 6);

            doc.AppendFile(rhinoPath);

            return $"File saved {rhinoPath}";
        }
    }
    
}
