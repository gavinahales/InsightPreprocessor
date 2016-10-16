using System;
using System.IO;
using System.Data.SQLite;
using System.Xml;

namespace InsightPreprocessor
{
    class Program
    {

        static String projpath;
        static XmlWriter xmldoc;
        static bool CensorMode;
        static bool FilterPre2kMode;
        //static string DatasetDrive;

        static void Main(string[] args)
        {
            Console.WriteLine("Enter path of project directory (Enter a . if you want to use the current working directory):");
            projpath = Console.ReadLine();

            //Console.WriteLine("Enter dataset drive letter:");
            //DatasetDrive = Console.ReadLine();
            //DatasetDrive = DatasetDrive + ":";

            Console.WriteLine("Enable censor mode? (Y/N)");

            String censor = Console.ReadLine();

            Console.WriteLine("Enable Filter Pre-2k Mode? (Y/N)");
            String pre2k = Console.ReadLine();

            bool processSuccess = true;

            if (censor == "Y" || censor == "y")
            {
                CensorMode = true;
                Console.WriteLine("Censor Mode Enabled: Output will have personal information removed.");
            }
            else
            {
                CensorMode = false;
            }

            if (pre2k == "Y" || pre2k == "y")
            {
                FilterPre2kMode = true;
                Console.WriteLine("Filter Pre-2k Mode Enabled: Events occuring before 1 Jan 2000 will be removed.");
            }
            else
            {
                FilterPre2kMode = false;
            }

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
                                processSuccess = false;
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
                                processSuccess = false;
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

                if (processSuccess)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    Console.WriteLine("SUCCESS! Press any key to close.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                else
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine("FAILED! Process did not complete successfully.");
                    Console.ForegroundColor = ConsoleColor.White;
                }
                
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
                SQLiteDataReader queryresults;

                #region Get Web History
                SQLiteCommand getweb = new SQLiteCommand(sqldb);
                getweb.CommandText = "SELECT DISTINCT artifact_id FROM blackboard_attributes WHERE artifact_id IN (SELECT artifact_id FROM blackboard_artifacts WHERE artifact_type_id = 4) AND attribute_type_id = 33";
                queryresults = getweb.ExecuteReader();

                //Loop through each web history artifact
                while (queryresults.Read())
                {
                    String artifactID = queryresults.GetValue(0).ToString();

                    SQLiteCommand getdate = new SQLiteCommand(sqldb);
                    getdate.CommandText = "SELECT value_int64 FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 33)";
                    String date = getdate.ExecuteScalar().ToString();

                    SQLiteCommand geturl = new SQLiteCommand(sqldb);
                    geturl.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 1)";
                    String url = geturl.ExecuteScalar().ToString();

                    SQLiteCommand getbrowser = new SQLiteCommand(sqldb);
                    getbrowser.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 4)";
                    String browser = getbrowser.ExecuteScalar().ToString();

                    date = ConvertTimestamp(date);

                    //Censoring
                    if (CensorMode == true) url = "Information Removed";

                    AddEvent("autwh" + artifactID, date, null, url, "LightBlue", "Browser Name: " + browser, null, url);

                }

                #endregion

                #region Get Installed Programs

                SQLiteCommand getprograms = new SQLiteCommand(sqldb);
                getprograms.CommandText = "SELECT DISTINCT artifact_id FROM blackboard_attributes WHERE artifact_id IN (SELECT artifact_id FROM blackboard_artifacts WHERE artifact_type_id = 8) AND attribute_type_id = 2";
                queryresults = getprograms.ExecuteReader();

                //Loop through each installed program artifact
                while (queryresults.Read())
                {
                    String artifactID = queryresults.GetValue(0).ToString();

                    SQLiteCommand getdate = new SQLiteCommand(sqldb);
                    getdate.CommandText = "SELECT value_int64 FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 2)";
                    String date = getdate.ExecuteScalar().ToString();

                    //Check if date is invalid, if so, print error and go to next iteration
                    if (int.Parse(date) < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Warning: Artifact ID: " + artifactID + " was found to have invalid date. Artifact ommitted.");
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    SQLiteCommand getprogname = new SQLiteCommand(sqldb);
                    getprogname.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 4)";
                    //This string may need to be sanitized if problems with escape characters occur
                    String programname = getprogname.ExecuteScalar().ToString();

                    date = ConvertTimestamp(date);

                    AddEvent("autip" + artifactID, date, null, programname, "Green", "The application: " + programname + " was installed on " + date + ".", null, null);

                }

                #endregion

                #region Get Attached Devices

                SQLiteCommand getdevices = new SQLiteCommand(sqldb);
                getdevices.CommandText = "SELECT DISTINCT artifact_id FROM blackboard_attributes WHERE artifact_id IN (SELECT artifact_id FROM blackboard_artifacts WHERE artifact_type_id = 11) AND attribute_type_id = 2";
                queryresults = getdevices.ExecuteReader();

                //Loop through each installed program artifact
                while (queryresults.Read())
                {
                    String artifactID = queryresults.GetValue(0).ToString();

                    SQLiteCommand getdate = new SQLiteCommand(sqldb);
                    getdate.CommandText = "SELECT value_int64 FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 2)";
                    String date = getdate.ExecuteScalar().ToString();

                    //Check if date is invalid, if so, print error and go to next iteration
                    if (int.Parse(date) < 0)
                    {
                        Console.ForegroundColor = ConsoleColor.DarkYellow;
                        Console.WriteLine("Warning: Artifact ID: " + artifactID + " was found to have invalid date. Artifact ommitted.");
                        Console.ForegroundColor = ConsoleColor.White;
                        continue;
                    }

                    SQLiteCommand getdevname = new SQLiteCommand(sqldb);
                    getdevname.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 18)";
                    String devicename = getdevname.ExecuteScalar().ToString();

                    SQLiteCommand getdevID = new SQLiteCommand(sqldb);
                    getdevID.CommandText = "SELECT value_text FROM blackboard_attributes WHERE (artifact_id = " + artifactID + " AND attribute_type_id = 20)";
                    String deviceID = getdevID.ExecuteScalar().ToString();

                    date = ConvertTimestamp(date);

                    AddEvent("autad" + artifactID, date, null, devicename, "Yellow", "The device: " + devicename + " with an ID of " + deviceID + " was attached on " + date + ".", null, null);

                }

                #endregion

                #region Get EXIF Data

                SQLiteCommand getEXIFArtifact = new SQLiteCommand(sqldb);
                getEXIFArtifact.CommandText = "SELECT artifact_id, obj_id FROM blackboard_artifacts WHERE artifact_type_id = 16";
                queryresults = getEXIFArtifact.ExecuteReader();

                while (queryresults.Read())
                {
                    String artifactID = queryresults.GetValue(0).ToString();
                    String objID = queryresults.GetValue(1).ToString();

                    SQLiteDataReader fileQueryResults;

                    SQLiteCommand getFileInfo = new SQLiteCommand(sqldb);
                    getFileInfo.CommandText = "SELECT name, parent_path, mtime, atime FROM tsk_files WHERE obj_id = " + objID;

                    fileQueryResults = getFileInfo.ExecuteReader();
                    fileQueryResults.Read();
                    String filename = fileQueryResults.GetValue(0).ToString();
                    String parentpath = fileQueryResults.GetValue(1).ToString();
                    String modtime = ConvertTimestamp(fileQueryResults.GetValue(2).ToString());
                    String accesstime = ConvertTimestamp(fileQueryResults.GetValue(3).ToString());
                    fileQueryResults.Close();
                    
                    //Get EXIF Timestamp
                    //NOTE: The EXIF timestamp format seems to have changed between versions of Autopsy. In older versions, a string representation of the date is stored in the database,
                    //whereas in newer versions of Autopsy, the EXIF timestamp is stored as an integer timestamp.
                    getFileInfo.CommandText = "SELECT value_int64 FROM blackboard_attributes WHERE artifact_id = " + artifactID +" AND attribute_type_id = 2";
                    String EXIFtimestamp;
                    object EXIFtimestampresult = getFileInfo.ExecuteScalar();
                    int foo; //Required to stop the TryParse method from bitching.
                    if (EXIFtimestampresult != null && int.TryParse(EXIFtimestampresult.ToString(), out foo))
                    {
                        EXIFtimestamp = ConvertTimestamp(EXIFtimestampresult.ToString());
                    }
                    else if(EXIFtimestampresult != null && !int.TryParse(EXIFtimestampresult.ToString(), out foo))
                    {
                        EXIFtimestamp = EXIFtimestampresult.ToString();
                    }
                    else
                    {
                        EXIFtimestamp = "No EXIF timestamp found.";
                    }


                    //Get Camera Make
                    getFileInfo.CommandText = "SELECT value_text FROM blackboard_attributes WHERE artifact_id = " + artifactID + " AND attribute_type_id = 19";
                    object EXIFmakeresult = getFileInfo.ExecuteScalar();
                    String EXIFmake;
                    if (EXIFmakeresult != null)
                    {
                        EXIFmake = EXIFmakeresult.ToString();
                    }
                    else
                    {
                        EXIFmake = "No EXIF camera manufacturer found.";
                    }

                    //Get Camera Model
                    getFileInfo.CommandText = "SELECT value_text FROM blackboard_attributes WHERE artifact_id = " + artifactID + " AND attribute_type_id = 18";
                    object EXIFmodelresult = getFileInfo.ExecuteScalar();
                    String EXIFmodel;
                    if (EXIFmodelresult != null)
                    {
                        EXIFmodel = EXIFmodelresult.ToString();
                    }
                    else
                    {
                        EXIFmodel = "No EXIF camera model found.";
                    }

                    AddEvent("autex" + artifactID, modtime, null, "EXIF Tagged File Modified", "Orange", "The EXIF tagged file " + filename + " was modified.\nIt was last accessed on " + accesstime + ".\nParent Path: " + parentpath + "\nEXIF Timestamp: " + EXIFtimestamp + "\nEXIF Camera Manufacturer: " + EXIFmake + "\nEXIF Camera Model " + EXIFmodel, null, parentpath + filename);
                }

                #endregion

                #region Get File Type Mismatches

                SQLiteCommand getMismatchArtifact = new SQLiteCommand(sqldb);
                getMismatchArtifact.CommandText = "SELECT artifact_id, obj_id FROM blackboard_artifacts WHERE artifact_type_id = 34";
                queryresults = getMismatchArtifact.ExecuteReader();

                while(queryresults.Read())
                {
                    String artifactID = queryresults.GetValue(0).ToString();
                    String objID = queryresults.GetValue(1).ToString();

                    SQLiteDataReader fileQueryResults;

                    SQLiteCommand getFileInfo = new SQLiteCommand(sqldb);
                    getFileInfo.CommandText = "SELECT name, parent_path, mtime, atime FROM tsk_files WHERE obj_id = " + objID;

                    fileQueryResults = getFileInfo.ExecuteReader();
                    fileQueryResults.Read();
                    String filename = fileQueryResults.GetValue(0).ToString();
                    String parentpath = fileQueryResults.GetValue(1).ToString();
                    String modtime = ConvertTimestamp(fileQueryResults.GetValue(2).ToString());
                    String accesstime = ConvertTimestamp(fileQueryResults.GetValue(3).ToString());
                    fileQueryResults.Close();

                    //Get detected file type, this is not attached to the same artifact for some reason.
                    SQLiteCommand getDetectedTypeSubArtifact = new SQLiteCommand(sqldb);
                    getDetectedTypeSubArtifact.CommandText = "SELECT artifact_id FROM blackboard_artifacts WHERE obj_id = " + objID + " AND artifact_type_id = 1";
                    SQLiteDataReader subArtifactQuery = getDetectedTypeSubArtifact.ExecuteReader();
                    subArtifactQuery.Read();
                    String subArtifact = subArtifactQuery.GetValue(0).ToString();

                    SQLiteCommand getDetectedType = new SQLiteCommand(sqldb);
                    getDetectedType.CommandText = "SELECT value_text FROM blackboard_attributes WHERE artifact_id = " + subArtifact + " AND attribute_type_id = 62";
                    SQLiteDataReader detectedTypeQuery = getDetectedType.ExecuteReader();
                    detectedTypeQuery.Read();
                    String detectedType = detectedTypeQuery.GetValue(0).ToString();

                    AddEvent("auttm" + artifactID, modtime, null, "Potential File Type Mismatch", "Red", "The file " + filename + " has an extension which does not match it's actual file type signature. \nThe detected type is: " + detectedType + ".\nIt was last accessed on " + accesstime + ".\nParent Path: " + parentpath, null, parentpath + filename);
                }

                #endregion


            }

            catch (Exception e)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Autopsy processing failed. Error: " + e.Message);
                Console.ForegroundColor = ConsoleColor.White;
                return false;
            }

            return true;
        }

        public static bool ProcessL2TFile(String path)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("Log2Timeline processing not yet implemented.");
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }

        public static void AddEvent(string id, string start, string end, string title, string colour, string description, string imagepath, string link)
        {

            if (FilterPre2kMode && DateTime.Parse(start).Year < 2000)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine("Event prior to year 2000 found. ID = "+id+". Omitting.");
                Console.ForegroundColor = ConsoleColor.White;
            }
            else
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

        public static String ConvertTimestamp(String timestamp)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0);
            DateTime convertedDate = epoch.AddSeconds(int.Parse(timestamp));
            String datestring = convertedDate.ToLongDateString() + " " + convertedDate.ToLongTimeString();

            return datestring;
        }
    }
}
