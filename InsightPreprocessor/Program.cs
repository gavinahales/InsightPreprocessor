using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data.SQLite;
using System.Xml;

namespace InsightPreprocessor
{
    class Program
    {

        static String projpath;
        static XmlWriter xmldoc;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter path of project directory:");
            projpath = Console.ReadLine();

            try
            {
                DirectoryInfo projectdirectory = new DirectoryInfo(projpath);
                DirectoryInfo[] projsubdirs = projectdirectory.GetDirectories("datasets");

                if (projsubdirs.Length != 1)
                {
                    throw new DirectoryNotFoundException("Dataset directory not found.");
                }

                DirectoryInfo datasetdirectory = projsubdirs[0];
                FileInfo[] datasetfiles = datasetdirectory.GetFiles();

                //Create the output file
                XmlWriterSettings xmlsettings = new XmlWriterSettings();
                xmlsettings.Indent = true;
                xmlsettings.IndentChars = "\t";
                xmlsettings.OmitXmlDeclaration = true;
                xmldoc = XmlWriter.Create("insight.xml",xmlsettings);
                xmldoc.WriteStartDocument();
                xmldoc.WriteStartElement("data");

                foreach (FileInfo file in datasetfiles)
                {

                    #region Dataset Search and Process logic
                    switch (file.Name)
                    {
                        case "autopsy.db":
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("Autopsy file found! Processing...");
                            Console.ForegroundColor = ConsoleColor.White;

                            //Begin processing autopsy file.
                            bool autopsysuccess = ProcessAutopsyFile(file.FullName);

                            //Test if successful.
                            if (!autopsysuccess)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine("WARNING!! Failed to process Autopsy file. Preprocess output may be incomplete or corrupt.");
                                Console.ForegroundColor = ConsoleColor.White;
                            }
                            
                            break;
                        case "log2timeline.xml":
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("Log2Timeline file found! Processing...");
                            Console.ForegroundColor = ConsoleColor.White;

                            //Begin processing log2timeline file.
                            bool l2tsuccess = ProcessL2TFile(file.FullName);

                            //Test if successful.
                            if (!l2tsuccess)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkRed;
                                Console.WriteLine("WARNING!! Failed to process L2T file. Preprocess output may be incomplete or corrupt.");
                                Console.ForegroundColor = ConsoleColor.White;
                            }

                            break;

                        default:
                            break;
                    }
                    #endregion
                }

                //Close 'data' element and close the document
                xmldoc.WriteEndElement();
                xmldoc.WriteEndDocument();
                xmldoc.Close();

                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine("SUCCESS! Press any key to close.");
                Console.ForegroundColor = ConsoleColor.White;
                
            }
            catch (DirectoryNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED: Directory not found. Error: "+e.Message);
            }


            Console.ReadLine();
        }

        public static bool ProcessAutopsyFile(String path)
        {

            try
            {
                SQLiteConnection sqldb = new SQLiteConnection("Data Source=" + path + "; Version=3;");
                sqldb.Open();

                #region Get Web History
                SQLiteCommand getweb = new SQLiteCommand(sqldb);
                getweb.CommandText = "SELECT DISTINCT artifact_id FROM blackboard_attributes WHERE artifact_id IN (SELECT artifact_id FROM blackboard_artifacts WHERE artifact_type_id = 4) AND attribute_type_id = 33";
                SQLiteDataReader webresults = getweb.ExecuteReader();

                //Loop through each web history artifact
                while (webresults.Read())
                {
                    SQLiteCommand getdate = new SQLiteCommand(sqldb);
                    getdate.CommandText = "SELECT value_int64 FROM blackboard_attributes WHERE (artifact_id = " + webresults.GetValue(0).ToString() + " AND attribute_type_id = 33)";
                    String date = getdate.ExecuteScalar().ToString();

                    SQLiteCommand geturl = new SQLiteCommand(sqldb);
                    geturl.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + webresults.GetValue(0).ToString() + " AND attribute_type_id = 1)";
                    String url = geturl.ExecuteScalar().ToString();

                    SQLiteCommand getbrowser = new SQLiteCommand(sqldb);
                    getbrowser.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + webresults.GetValue(0).ToString() + " AND attribute_type_id = 4)";
                    String browser = getbrowser.ExecuteScalar().ToString();

                    DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
                    DateTime convertedDate = epoch.AddSeconds(int.Parse(date));
                    String datestring = convertedDate.ToLongDateString() + " " + convertedDate.ToLongTimeString();

                    AddEvent("autwh" + webresults.GetValue(0).ToString(), datestring, null, url, "LightBlue", "Browser Name: " + browser, null, url);

                }

                #endregion


                
                

            }

            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Autopsy processing failed. Error: " + e.Message);
                Console.ForegroundColor = ConsoleColor.White;
            }

            return true;
        }

        public static bool ProcessL2TFile(String path)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Log2Timeline processing not yet implemented.");
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }

        public static void AddEvent(string id, string start, string end, string title, string colour, string description, string imagepath, string link)
        {

            xmldoc.WriteStartElement("event");
            xmldoc.WriteAttributeString("id", id);
            xmldoc.WriteAttributeString("start", start);
            if (end != null) xmldoc.WriteAttributeString("end", end);
            xmldoc.WriteAttributeString("title", title);
            xmldoc.WriteAttributeString("color", colour);
            if (imagepath != null) xmldoc.WriteAttributeString("teaserimage", imagepath);
            if (link != null) xmldoc.WriteAttributeString("link", link);
            xmldoc.WriteString(description);
            xmldoc.WriteEndElement();

        }
    }
}
