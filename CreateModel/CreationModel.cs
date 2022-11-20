using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Autodesk.Revit.ApplicationServices;

namespace CreateModel
{
    [TransactionAttribute(TransactionMode.Manual)]
    public class CreationModel : IExternalCommand
    {
        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            Document doc = commandData.Application.ActiveUIDocument.Document;
            
            Level level1 = ListLevel(doc)
                .Where(x => x.Name.Equals("Уровень 1"))
                .FirstOrDefault();  
            
            Level level2 = ListLevel(doc)
                .Where(x => x.Name.Equals("Уровень 2"))
                .FirstOrDefault();

             CreateWall(doc, level1, level2);

            //ElementId id = doc.GetDefaultElementTypeId(ElementTypeGroup.RoofType);
            //RoofType type = doc.GetElement(id) as RoofType;
            //if (type == null)
            //{
            //    TaskDialog.Show("Error", "Not RoofType");
            //    return Result.Failed;
            //}
            //// Crear esquema
            //CurveArray curveArray = new CurveArray();
            //curveArray.Append(Line.CreateBound(new XYZ(0, 0, 0), new XYZ(0, 20, 20)));
            //curveArray.Append(Line.CreateBound(new XYZ(0, 20, 20), new XYZ(0, 40, 0)));
            //Level level = doc.ActiveView.GenLevel;
            //if (level == null)
            //{
            //    TaskDialog.Show("Error", "No es PlainView");
            //    return Result.Failed;
            //}
            //// Crear techo
            //using (Transaction tr = new Transaction(doc))
            //{
            //    tr.Start("Create ExtrusionRoof");
            //    ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(0, 0, 0), new XYZ(0, 0, 20), new XYZ(0, 20, 0), doc.ActiveView);
            //    doc.Create.NewExtrusionRoof(curveArray, plane, level, type, 0, 1);
            //    tr.Commit();
            //}



            #region 
            //List<FamilyInstance> window = new FilteredElementCollector(doc)
            //    .OfCategory(BuiltInCategory.OST_Windows)
            //    .WhereElementIsNotElementType()
            //    .Cast<FamilyInstance>()
            //    .ToList();

            //Transaction ts1 = new Transaction(doc, "уровень нижнего бруса");
            //ts1.Start();
            //foreach (var win in window)
            //{
            //    Parameter parameter = win.get_Parameter(BuiltInParameter.INSTANCE_SILL_HEIGHT_PARAM);
            //    parameter.Set(UnitUtils.ConvertToInternalUnits(500, UnitTypeId.Millimeters));
            //}
            //ts1.Commit();
            #endregion


            return Result.Succeeded;
        }

        static List<Level> ListLevel(Document doc)
        {
            List<Level> listLevel = new FilteredElementCollector(doc)
                .OfClass(typeof(Level))
                .OfType<Level>()
                .ToList();

            return listLevel;
        }

        static void CreateWall(Document doc, Level level1, Level level2)
        {
            double width = UnitUtils.ConvertToInternalUnits(10000, UnitTypeId.Millimeters);
            double depth = UnitUtils.ConvertToInternalUnits(5000, UnitTypeId.Millimeters);
            double dx = width / 2;
            double dy = depth / 2;

            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dx, -dy, 0));
            points.Add(new XYZ(dx, -dy, 0));
            points.Add(new XYZ(dx, dy, 0));
            points.Add(new XYZ(-dx, dy, 0));
            points.Add(new XYZ(-dx, -dy, 0));

            List<Wall> walls = new List<Wall>();

            Transaction ts = new Transaction(doc, "Построение стен");
            ts.Start();
            for (int i = 0; i < 4; i++)
            {
                Line line = Line.CreateBound(points[i], points[i + 1]);
                Wall wall = Wall.Create(doc, line, level1.Id, false);
                walls.Add(wall);
                wall.get_Parameter(BuiltInParameter.WALL_HEIGHT_TYPE).Set(level2.Id);
            }
            AddDoor(doc, level1, walls[0]);
            AddWindow(doc, level1, walls);
            //AddRoof(doc,level2, walls);
            AddRoof2(doc, level2, walls);
            ts.Commit();

        }

        private static void AddRoof2(Document doc, Level level, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
               .OfClass(typeof(RoofType))
               .OfType<RoofType>()
               .Where(x => x.Name.Equals("Типовая крыша - 500мм"))
               .Where(x => x.FamilyName.Equals("Базовая крыша"))
               .FirstOrDefault();
            double width = walls[0].get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            double depth = walls[1].get_Parameter(BuiltInParameter.CURVE_ELEM_LENGTH).AsDouble();
            double dx = (width / 2)+2;
            double dy = (depth / 2)+2;
            double dz = level.Elevation;

            CurveArray curveArray = new CurveArray();
            curveArray.Append(Line.CreateBound(new XYZ(-dx, -dy, dz), new XYZ(-dx, 0, (dz + dz / 2))));
            curveArray.Append(Line.CreateBound(new XYZ(-dx, 0, (dz + dz / 2)), new XYZ(-dx, dy, dz)));
            ReferencePlane plane = doc.Create.NewReferencePlane(new XYZ(-dx, -dy,dz ), new XYZ(-dx,-dy,(dz+dz/2)), new XYZ(0,-dy,0), doc.ActiveView);
            doc.Create.NewExtrusionRoof(curveArray, plane, level, roofType, 0, -dx*2);       
            
        }

        private static void AddRoof(Document doc, Level level2, List<Wall> walls)
        {
            RoofType roofType = new FilteredElementCollector(doc)
                .OfClass(typeof(RoofType))
                .OfType<RoofType>()
                .Where(x=>x.Name.Equals("Типовая крыша - 500мм"))
                .Where(x=>x.FamilyName.Equals("Базовая крыша"))
                .FirstOrDefault();

            double wallWidth = walls[0].Width;
            double dt = wallWidth / 2;
            List<XYZ> points = new List<XYZ>();
            points.Add(new XYZ(-dt, -dt, 0));
            points.Add(new XYZ(dt, -dt, 0));
            points.Add(new XYZ(dt, dt, 0));
            points.Add(new XYZ(-dt, dt, 0));
            points.Add(new XYZ(-dt, -dt, 0));

            Application application = doc.Application;
            CurveArray footprint = application.Create.NewCurveArray();
            for (int i=0; i<4; i++)
            {
                LocationCurve curve = walls[i].Location as LocationCurve;
                XYZ p1 = curve.Curve.GetEndPoint(0);
                XYZ p2 = curve.Curve.GetEndPoint(1);
                Line line = Line.CreateBound(p1 + points[i], p2+points[i+1]);
                footprint.Append(line);
            }
            ModelCurveArray footPrintToModelCurveMapping = new ModelCurveArray();
            FootPrintRoof footprintRoof = doc.Create.NewFootPrintRoof(footprint, level2, roofType, out footPrintToModelCurveMapping);
            ModelCurveArrayIterator iterator = footPrintToModelCurveMapping.ForwardIterator();
            iterator.Reset();
            while (iterator.MoveNext())
            {
                ModelCurve modelCurve = iterator.Current as ModelCurve;
                footprintRoof.set_DefinesSlope(modelCurve, true);
                footprintRoof.set_SlopeAngle(modelCurve, 0.5);
            }    
        }

        private static void AddWindow(Document doc, Level level1, List<Wall> walls)
        {
            FamilySymbol windowType = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Windows)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0406 x 0610 мм"))
                .Where(x => x.FamilyName.Equals("M_Неподвижный"))
                .FirstOrDefault();


            for (int i= 1; i< walls.Count; i++)
            {
                LocationCurve hostCurve = walls[i].Location as LocationCurve;
                XYZ point1 = hostCurve.Curve.GetEndPoint(0);
                XYZ point2 = hostCurve.Curve.GetEndPoint(1);
                XYZ point = (point1 + point2) / 2;
                if (!windowType.IsActive)
                    windowType.Activate();
                doc.Create.NewFamilyInstance(point, windowType, walls[i], level1, StructuralType.NonStructural);
            }
        }

        private static void AddDoor(Document doc, Level level1, Wall wall)
        {
          FamilySymbol doorType =  new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Doors)
                .OfType<FamilySymbol>()
                .Where(x => x.Name.Equals("0915 x 2134 мм"))
                .Where(x => x.FamilyName.Equals("M_Однопольные-Щитовые"))
                .FirstOrDefault();

            LocationCurve hostCurve = wall.Location as LocationCurve;
            XYZ point1 = hostCurve.Curve.GetEndPoint(0);
            XYZ point2 = hostCurve.Curve.GetEndPoint(1);
            XYZ point = (point1 + point2) / 2;

            if (!doorType.IsActive)
                doorType.Activate();

            doc.Create.NewFamilyInstance(point, doorType, wall, level1, StructuralType.NonStructural);
        }
    }
}
