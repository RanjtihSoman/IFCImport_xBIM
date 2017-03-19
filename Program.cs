using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using System.Text;
using Xbim.XbimExtensions;
using Xbim.Common.XbimExtensions;

namespace xBIM_IFC_Trial
{

    class Program
    {
        //const string file = "D:\\THESIS_ANI\\xBIM\\2011-09-14-Duplex-IFC\\Duplex_A_20110907_optimized.ifc";
        //const string file = "D:\\THESIS_ANI\\xBIM\\trial_openings_IFC_2x3coord.ifc";
        //const string file = "D:\\THESIS_ANI\\xBIM\\simplegeometry_trial.ifc";
        /***********************************************************************************************************************************/
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\trialbim_simple.ifc";
        const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\simplegeometry_openings_2x3.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\simplegeometry_openings_4.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\20160414office_model_CV2_fordesign.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\wall.ifc";

        public static void Main()
        {

            using (var model = IfcStore.Open(file))
            {

                Console.WriteLine("\n" + "---------------------------------------S T A R T---------------------------------------" + "\n");
                Dictionary<string, IfcSpace> spaceids;
                Dictionary<string, IfcBuildingStorey> storeyids;
                
                var project = model.Instances.FirstOrDefault<IIfcProject>();

                IEnumerable<IfcSpace> spaces = model.Instances.OfType<IfcSpace>();
                spaceids = getspaceelementids(spaces);

                IEnumerable<IfcBuildingStorey> storeys = model.Instances.OfType<IfcBuildingStorey>();
                storeyids = getstoreyelementids(storeys);

                var context = new Xbim3DModelContext(model);
                context.CreateContext();

                var productshape = context.ShapeInstances();

                var _productShape = context.ShapeInstances().Where(s => s.RepresentationType != XbimGeometryRepresentationType.OpeningsAndAdditionsExcluded).ToList();
                
                Console.WriteLine("OPENED MODEL : " + file.Split(new char[] { '\\' }).Last() + " | No of shape Instances in the model is : " + _productShape.Count() + "\n");


                PrintHierarchy(project, 0, spaceids, storeyids, _productShape, context);

                Console.WriteLine("\n" + "---------------------------------------E N D---------------------------------------" + "\n");
                /********************************************************************************/
            }

        }

        private static void PrintHierarchy(IIfcObjectDefinition o, int level, Dictionary<string, IfcSpace> spaceidset, Dictionary<string, IfcBuildingStorey> storeyidset, List<XbimShapeInstance> _shapes, Xbim3DModelContext mod_context)
        {
            Console.WriteLine($"{GetIndent(level)}{" >> " + o.Name} [{o.GetType().Name}{ " | #" + o.EntityLabel  }] {"\n"}");
            var item = o.IsDecomposedBy.SelectMany(r => r.RelatedObjects);

            foreach (var i in item)
            {

                var id = i.GlobalId.ToString();
                
                //Console.WriteLine("------------------Matches found :" + _si.ShapeGeometryLabel.ToString());

                PrintHierarchy(i, level + 2, spaceidset, storeyidset, _shapes, mod_context);

                //Console.WriteLine("Instance ID: " + eid);

                if (spaceidset.ContainsKey(id))
                {
                    IfcSpace spacenode;
                    spaceidset.TryGetValue(id, out spacenode);
                    var spacenodelelems = spacenode.GetContainedElements();

                    if (spacenodelelems.Count() > 0)
                    {
                        Console.WriteLine($"{GetIndent(level + 4)}" + "OBJECTS FOUND UNDER SPACE ARE: \n");
                        foreach (var sne in spacenodelelems)
                        {
                            var parent = sne.IsContainedIn;
                            var eid = sne.EntityLabel.ToString();
                            
                            Console.WriteLine($"{GetIndent(level + 5)}{" --> " + sne.Name} [{sne.GetType().Name}{ " | #" + sne.EntityLabel }{" | PARENT : #" + parent.EntityLabel}]");

                            var si = _shapes.Find(x => x.IfcProductLabel.ToString() == eid);
                            //Console.WriteLine("------------------Matches found :" + si.ShapeGeometryLabel.ToString());
                            getgeometry(si, mod_context);
                        }
                    }
                }

                else if (storeyidset.ContainsKey(id))
                {
                    IfcBuildingStorey bsnode;
                    storeyidset.TryGetValue(id, out bsnode);
                    var bsnodelelems = bsnode.GetContainedElements();

                    if (bsnodelelems.Count() > 0)
                    {
                        Console.WriteLine($"{GetIndent(level + 4)}" + "OTHER OBJECTS FOUND UNDER STOREY ARE: \n");
                        foreach (var bsne in bsnodelelems)
                        {
                            var parent = bsne.IsContainedIn;
                            var eid = bsne.EntityLabel.ToString();
                            
                            Console.WriteLine($"{GetIndent(level + 5)}{" --> " + bsne.Name} [{bsne.GetType().Name}{ " | #" + bsne.EntityLabel } {" | PARENT : #" + parent.EntityLabel }]");

                            var si = _shapes.Find(x => x.IfcProductLabel.ToString() == eid);
                           
                            getgeometry(si, mod_context);
                        }
                    }

                }


                /***************************************************************************/

            }

        }

        private static string GetIndent(int level)
        {
            var indent = "";
            for (int i = 0; i < level; i++)
                indent += "  ";
            return indent;
        }

        private static Dictionary<string, IfcSpace> getspaceelementids(IEnumerable<IfcSpace> spaces_ien)
        {
            Dictionary<string, IfcSpace> eids = new Dictionary<string, IfcSpace>();
            foreach (IfcSpace s in spaces_ien)
            {
                eids.Add(s.GlobalId.ToString(), s);
                //Console.WriteLine("Gid for " + s.Name + " is: " +s.GlobalId.ToString());
            }

            return eids;
        }

        private static Dictionary<string, IfcBuildingStorey> getstoreyelementids(IEnumerable<IfcBuildingStorey> storeys_ien)
        {
            Dictionary<string, IfcBuildingStorey> eids = new Dictionary<string, IfcBuildingStorey>();
            foreach (IfcBuildingStorey s in storeys_ien)
            {
                eids.Add(s.GlobalId.ToString(), s);
                //Console.WriteLine("Gid for " + s.Name + " is: " +s.GlobalId.ToString());
            }

            return eids;
        }

        private static void getgeometry(XbimShapeInstance shape, Xbim3DModelContext m_context)
        {

            XbimShapeTriangulation mesh = null;

            var geometry = m_context.ShapeGeometry(shape);

            //Console.WriteLine("====" + geometry.GetType());
            Console.WriteLine($"{"\n"}{GetIndent(11)}{"--Geometry Type: " + geometry.Format}");


            var ms = new MemoryStream(((IXbimShapeGeometryData)geometry).ShapeData);
            var br = new BinaryReader(ms);

            mesh = br.ReadShapeTriangulation();
            mesh = mesh.Transform(((XbimShapeInstance)shape).Transformation);

            var facesfound = mesh.Faces.ToList();

            Console.WriteLine($"{"\n"}{GetIndent(11)}{"  -----No. of faces on the shape #" + shape.IfcProductLabel + ": " + facesfound.Count()}");
            
            foreach (XbimFaceTriangulation f in facesfound)
            {
                
                Console.WriteLine($"{"\n"}{GetIndent(13)}{"  -----Triangle count on face: " + f.GetType() + " :mesh is  " + f.TriangleCount}");
                //foreach (var fi in f.Indices)
                //{
                //    Console.WriteLine($"{GetIndent(13)}{" -> " + fi}");
                    
                //}
                composetrianglesets(f,mesh);


            }

            //Console.WriteLine($"{"\n"}{GetIndent(13)}{" -----Vertices of the shape: #" + shape.IfcProductLabel}");
            //foreach (var v in mesh.Vertices.ToList())
            //{
            //    Console.WriteLine($"{GetIndent(13)}{" --vertex_" + mesh.Vertices.ToList().IndexOf(v) + " : " + Math.Round((double)v.X, 2) + " | " + Math.Round((double)v.Y, 2) + " | " + Math.Round((double)v.Z, 2)}");

            //}

            Console.WriteLine("\n");
        }

        
        private static void composetrianglesets(XbimFaceTriangulation face, XbimShapeTriangulation shapemesh)
        {
            Dictionary<string, List<int>> triangles = new Dictionary<string, List<int>>();
            Dictionary<string, XbimPoint3D> vertices = new Dictionary<string,XbimPoint3D>();

            List<XbimPoint3D> verts = shapemesh.Vertices.ToList();

            //for (int i = 0; i < verts.Count(); i++)
            //{
            //    string name = "vertex_" + (i).ToString();
            //    vertices.Add(name, verts[i]);

            //}
            //foreach (var v in vertices)
            //{
            //    Console.WriteLine($"{GetIndent(15)}{v.Key + ": "}{v.Value.X + ", "}{v.Value.Y + ", "}{v.Value.Z}");
            //}
            /*******************************************************************************************************/
            for (int i = 0; i < face.TriangleCount; i++)
            {
                string name = "triangle_" + (i+1).ToString();
                triangles.Add(name, face.Indices.ToList().GetRange(i*3,3));

            }
            foreach (var x in triangles)
            {
                var vert1 = x.Value[0];
                var vert2 = x.Value[1];
                var vert3 = x.Value[2];
                Console.WriteLine($"{"\n"}{GetIndent(15)}{x.Key + ": "}{vert1 + ","}{vert2 + ","}{vert3}");
                Console.WriteLine($"{GetIndent(15)}{"---------------------"}");
                for (int y =0; y < x.Value.Count(); y++)
                {
                    Console.WriteLine($"{GetIndent(15)}{"vertex_"+ x.Value[y] + ": "}{Math.Round((double)verts[x.Value[y]].X, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Y, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Z, 2)}");
                }
                

            }
            /*******************************************************************************************************/
            

        }

    }
}