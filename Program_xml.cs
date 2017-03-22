using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.IO;
using Xbim.Ifc;
using Xbim.Ifc2x3.ProductExtension;
using Xbim.Ifc2x3.Extensions;
using Xbim.Ifc4.Interfaces;
using Xbim.Common.Geometry;
using Xbim.ModelGeometry.Scene;
using Xbim.Presentation;
using Xbim.XbimExtensions;
using Xbim.Common.XbimExtensions;
using System.Xml;
using System.Xml.Linq;
namespace Test
{
    class Program
    {
        //const string file = "D:\\THESIS_ANI\\xBIM\\2011-09-14-Duplex-IFC\\Duplex_A_20110907_optimized.ifc";
        //const string file = "D:\\THESIS_ANI\\xBIM\\trial_openings_IFC_2x3coord.ifc";
        //const string file = "D:\\THESIS_ANI\\xBIM\\simplegeometry_trial.ifc";
        /***********************************************************************************************************************************/
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\trialbim_simple.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\simplegeometry_openings_2x3.ifc";
        //const string file = "‪C:\\Users\\aniru\\Desktop\\exam.ifc";
        const string file = "C:\\Users\\aniru\\Desktop\\XBIM\\simplegeometry_trial.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\simplegeometry_openings_4.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\20160414office_model_CV2_fordesign.ifc";
        //const string file = "F:\\TUM\\STUDIES\\THESIS\\BIMtoUnity\\xBIM\\IFC_samplefiles\\2011-09-14-Clinic-IFC\\wall.ifc";

        //static Dictionary<int, Dictionary<int,Dictionary<int,Dictionary<int,List<int>>>>> temp;

        static XmlTextWriter xmlWriter = new XmlTextWriter("Wall.xml", null);

        //Use automatic indentation for readability.
       
 

        public static void Main()
        {



            using (var model = IfcStore.Open(file))
        {
                xmlWriter.Formatting = Formatting.Indented;

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
                var Number_OF_Walls = _productShape.Count();
            Console.WriteLine("OPENED MODEL : " + file.Split(new char[] { '\\' }).Last() + " | No of shape Instances in the model is : " + Number_OF_Walls + "\n");



                xmlWriter.WriteStartDocument();
               
                xmlWriter.WriteStartElement("Wall");

              
                xmlWriter.WriteAttributeString("NumWall",Number_OF_Walls.ToString());
              

                PrintHierarchy(project, 0, spaceids, storeyids, _productShape, Number_OF_Walls,context);
                
                
                xmlWriter.WriteEndDocument();
                xmlWriter.Close();
            Console.WriteLine("\n" + "---------------------------------------E N D---------------------------------------" + "\n");
                /********************************************************************************/
                
            }
            Console.ReadLine();
        }
        
        private static void PrintHierarchy(IIfcObjectDefinition o, int level, Dictionary<string, IfcSpace> spaceidset, Dictionary<string, IfcBuildingStorey> storeyidset, List<XbimShapeInstance> _shapes, int number_OF_Walls,Xbim3DModelContext mod_context)
    {
        Console.WriteLine($"{GetIndent(level)}{" >> " + o.Name} [{o.GetType().Name}{ " | #" + o.EntityLabel  }] {"\n"}");
        var item = o.IsDecomposedBy.SelectMany(r => r.RelatedObjects);

        foreach (var i in item)
        {

            var id = i.GlobalId.ToString();

            //Console.WriteLine("------------------Matches found :" + _si.ShapeGeometryLabel.ToString());

            PrintHierarchy(i, level + 2, spaceidset, storeyidset, _shapes,number_OF_Walls, mod_context);

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
                        Console.WriteLine(sne.EntityLabel);
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
                        Console.WriteLine("]]]]]]]]]]"+bsne.EntityLabel);
                        
                        var si = _shapes.Find(x => x.IfcProductLabel.ToString() == eid);

                            xmlWriter.WriteStartElement("Wall");
                            xmlWriter.WriteAttributeString("ID", bsne.EntityLabel.ToString());
                            
                            //xmlWriter.WriteString(bsne.EntityLabel.ToString());

                            getgeometry(si, mod_context,bsne.EntityLabel,number_OF_Walls);
                            //this is fo 
                         //   xmlWriter.WriteEndElement();
                           // xmlWriter.WriteWhitespace("\n");
                        }
                }

            }


            /***************************************************************************/

        }

    }

        private static void getgeometry(XbimShapeInstance si, Xbim3DModelContext mod_context)
        {
            throw new NotImplementedException();
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

    private static void getgeometry(XbimShapeInstance shape, Xbim3DModelContext m_context, int entityLabel, int number_OF_Walls)
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
            int FaceNum = 0;
            int Triangle_Count = 0;
            //xmlWriter.WriteElementString("Faces", facesfound.Count().ToString());
            xmlWriter.WriteStartElement("Faces");
            xmlWriter.WriteAttributeString("NumFaces", facesfound.Count().ToString());
          
            foreach (XbimFaceTriangulation f in facesfound)
        {
               
                Triangle_Count = f.TriangleCount;
            Console.WriteLine($"{"\n"}{GetIndent(13)}{"  -----Triangle count on face: " + f.GetType() + " :mesh is  " +Triangle_Count}");
                
                
                //foreach (var fi in f.Indices)
                //{
                //    Console.WriteLine($"{GetIndent(13)}{" -> " + fi}");

                //}
                FaceNum++;

                composetrianglesets(f, mesh, entityLabel, facesfound.Count(), FaceNum,Triangle_Count,number_OF_Walls);


        }
            //this is for the faces NUmber
            xmlWriter.WriteEndElement();
           // xmlWriter.WriteWhitespace("\n");
            //Console.WriteLine($"{"\n"}{GetIndent(13)}{" -----Vertices of the shape: #" + shape.IfcProductLabel}");
            //foreach (var v in mesh.Vertices.ToList())
            //{
            //    Console.WriteLine($"{GetIndent(13)}{" --vertex_" + mesh.Vertices.ToList().IndexOf(v) + " : " + Math.Round((double)v.X, 2) + " | " + Math.Round((double)v.Y, 2) + " | " + Math.Round((double)v.Z, 2)}");

            //}

            Console.WriteLine("\n");
    }

       

        private static void composetrianglesets(XbimFaceTriangulation face, XbimShapeTriangulation shapemesh, int entityLabel, int Number_Faces, int faceNum, int triangle_Count, int number_OF_Walls)
    {


           
            Dictionary<string, List<int>> triangles = new Dictionary<string, List<int>>();
        Dictionary<string, XbimPoint3D> vertices = new Dictionary<string, XbimPoint3D>();

        List<XbimPoint3D> verts = shapemesh.Vertices.ToList();
            xmlWriter.WriteStartElement("Face");
            xmlWriter.WriteAttributeString("ID",faceNum.ToString());


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
            xmlWriter.WriteStartElement("Triangles");
            xmlWriter.WriteAttributeString("Triangle_Number",triangle_Count.ToString());


        for (int i = 0; i < face.TriangleCount; i++)
        {
            string name = "triangle_" + (i + 1).ToString();
               
                triangles.Add(name, face.Indices.ToList().GetRange(i * 3, 3));
         }
           

            int IDtriangle = 0;
        foreach (var x in triangles)
            {
                xmlWriter.WriteStartElement("Triangle");
                xmlWriter.WriteAttributeString("ID",IDtriangle.ToString());


                var vert1 = x.Value[0];
            var vert2 = x.Value[1];
            var vert3 = x.Value[2];
            Console.WriteLine($"{"\n"}{GetIndent(15)}{x.Key + ": "}{vert1 + ","}{vert2 + ","}{vert3}");
            Console.WriteLine($"{GetIndent(15)}{"---------------------"}");
                xmlWriter.WriteStartElement("Vertices");
                xmlWriter.WriteAttributeString("VerticeNum", "3");

                for (int y = 0; y < x.Value.Count(); y++)
                {
                    var VerticeID = "vertex_" + x.Value[y];
                    Console.WriteLine($"{GetIndent(15)}{VerticeID+": "}{Math.Round((double)verts[x.Value[y]].X, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Y, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Z, 2)}");
                    xmlWriter.WriteStartElement("Vertice");

                    xmlWriter.WriteAttributeString("ID",VerticeID);
                    xmlWriter.WriteElementString("X", (Math.Round((double)verts[x.Value[y]].X, 2).ToString)());
                    //xmlWriter.WriteString((Math.Round((double)verts[x.Value[y]].X, 2).ToString)());
                 //   xmlWriter.WriteEndElement();
                   // xmlWriter.WriteWhitespace("\n");

                    xmlWriter.WriteElementString("Y", (Math.Round((double)verts[x.Value[y]].Y, 2).ToString)());
                 
                   // xmlWriter.WriteEndElement();
                  // xmlWriter.WriteWhitespace("\n");

                    xmlWriter.WriteElementString("Z", (Math.Round((double)verts[x.Value[y]].Z, 2).ToString)());
                    //    xmlWriter.WriteString((
                    //   xmlWriter.WriteEndElement();
                    //  xmlWriter.WriteWhitespace("\n");
                    //     Console.WriteLine("Wall_ID: " + entityLabel + " Face_Number :" + faceNum + " triangle_Count :" + triangle_Count + " triangle_index :" + x.Key + " Number_Vertices :" + x.Value.Count + $"{GetIndent(15)}{"vertex_" + x.Value[y] + ": "}{Math.Round((double)verts[x.Value[y]].X, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Y, 2)}{"|"}{Math.Round((double)verts[x.Value[y]].Z, 2)}");

                    //XmlWriter writer = XmlWriter.Create("C:\\Users\\aniru\\Desktop\\wall.xml");


                    //this is for the verticeID
                    //    xmlWriter.WriteEndElement();
                    //    xmlWriter.WriteWhitespace("\n");

                    xmlWriter.WriteEndElement();
                }
                //this is for the  vertices
                xmlWriter.WriteEndElement();
              //  xmlWriter.WriteWhitespace("\n");
          
                //this is for the triangle id

                xmlWriter.WriteEndElement();
//                xmlWriter.WriteWhitespace("\n");

                IDtriangle++;
             
            }
            //this is for the triangle Number
            xmlWriter.WriteEndElement();
  //          xmlWriter.WriteWhitespace("\n");

          
            //this is for the face id
            xmlWriter.WriteEndElement();
    //        xmlWriter.WriteWhitespace("\n");





            //        XElement Walls =
            //new XElement("Walls", new XAttribute("NumWalls", number_OF_Walls),
            //   new XElement("WallId", entityLabel.ToString(),
            //    new XElement("Faces", new XAttribute("NumFaces", Number_Faces),
            //        new XElement("FaceIndex", faceNum,
            //            new XElement("Triangles", new XAttribute("NumTriangles", triangle_Count),
            //                new XElement("TriangleIndex", x.Key,
            //                    new XElement("Vertices", new XAttribute("NumVertices", x.Value.Count),
            //                        new XElement("VerticeIndex", "vertex_" + x.Value[y],
            //                        new XElement("X", 2,
            //                        new XElement("Y", 3,
            //                        new XElement("Z", 4)))))))))));





            ////Console.WriteLine("_____________00000000______"+Walls.Element("WallId").ToString());
            //Walls.Descendants("Faces").FirstOrDefault(el => el.Attribute("NumFaces") != null).Add("xii", 3);
            //Console.WriteLine("------------------------------------"+Walls.Element("WallId").ToString());
            //Console.WriteLine("------------------------------------!" + Walls.Element("VerticeIndex"));
            //Console.WriteLine("------------------------------------!" + Walls.Descendants("Vertices"));



            //    foreach (var ad in Walls.Descendants("VerticeIndex"))
            //{
            //   ad.Add(new XElement("xii", 2));
            //    Console.WriteLine("sdasdasdasd");
            //}
            ////   Walls.Element("VerticeIndex").Add("ass", 2);


            //  Console.WriteLine(result.ToString());

            //                    Walls.Element("Faces")..Add(new XElement("xaaa", 2));



            //for (int j=0; j< x.Value.Count();j++)
            //{
            //    Walls.Element("VerticeIndex").Add(new XElement("x", Math.Round((double)verts[x.Value[y]].X, 2)),
            //            new XElement("Y", Math.Round((double)verts[x.Value[y]].Y, 2)),
            //            new XElement("Z", Math.Round((double)verts[x.Value[y]].Z, 2)));

            //}

            //String filename = "Walls.xml";
            //var doc = new XDocument();


            //if (File.Exists(filename))
            //{
            ////    Console.WriteLine("======================================================the file existsssssss ");
            //    //doc = XDocument.Load(filename);
            //    //doc.Element("Walls").Add(Walls);
            //    XDocument xdoc = XDocument.Load("Walls.xml");
            //    xdoc.Element("Walls").Add(Walls);  //append after the last backup element
            //    xdoc.Save("Walls.xml");
            //}
            //else
            //{
            // //   Console.WriteLine("___________________________________________________the file doesntttttttttttttt  000000  existsssssss ");
            //    Walls.Save("Walls.xml");
            //}







            //Console.WriteLine(str);

            //                 XElement root = new XElement("Walls",new XAttribute("NumWalls",number_OF_Walls),
            //new XElement("WallID", "child content"),
            //new XElement("DASD","sdasda",
            // new XElement("asdweq","asdwqwe"))

            //               root.Save("root.xml");
            ////Console.WriteLine(str11);

            /*******************************************************************************************************/



        }

        public static void Append(string filename, string firstName)
        {
            var contact = new XElement("contact", new XElement("firstName", firstName));
            var doc = new XDocument();

            if (File.Exists(filename))
            {
                doc = XDocument.Load(filename);
                doc.Element("contacts").Add(contact);
            }
            else
            {
                doc = new XDocument(new XElement("contacts", contact));
            }
            doc.Save(filename);
        }
    }
}
