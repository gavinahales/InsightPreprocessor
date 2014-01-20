using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace InsightPreprocessor
{
    class Program
    {

        static String projpath;

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

                foreach (FileInfo file in datasetfiles)
                {
                    switch (file.Name)
                    {
                        case "autopsy.db":
                            Console.ForegroundColor = ConsoleColor.DarkGreen;
                            Console.WriteLine("Autopsy file found! Processing...");
                            Console.ForegroundColor = ConsoleColor.White;

                            //Begin processing autopsy file.
                            bool autopsysuccess = ProcessAutopsy(file.FullName);

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
                            bool l2tsuccess = ProcessL2T(file.FullName);

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
                }
                
            }
            catch (DirectoryNotFoundException e)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("FAILED: Directory not found. Error: "+e.Message);
            }

            

        }

        public static bool ProcessAutopsy(String path)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Autopsy processing not yet implemented.");
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }

        public static bool ProcessL2T(String path)
        {
            Console.ForegroundColor = ConsoleColor.DarkRed;
            Console.WriteLine("Autopsy processing not yet implemented.");
            Console.ForegroundColor = ConsoleColor.White;
            return false;
        }
    }
}
