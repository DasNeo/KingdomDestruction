using System;
using System.Collections.Generic;
using System.IO;

namespace KingdomDestruction
{
    public static class Config
    {
        private static string configFilePath = AppDomain.CurrentDomain.BaseDirectory.Substring(0, AppDomain.CurrentDomain.BaseDirectory.Length - 26) + "Modules\\KingdomDestruction\\config.txt";

        private static Dictionary<string, double> configValues = new();
        private static string configFileString =
            "Set the timer in days for a kingdom to recover from no fiefs and no vassals, they are destroyed otherwise. Default is 14.\n" +
            "destroyKingdomTimerInDays=14.0\n\n" +

            "Set the relation the displaced ruler needs to automatically join when asked. Also used for amtAwayFromJoinFree in the formula below. Default is 35.\n" +
            "relationNeededForDisplacedClanToJoinFree=35.0\n\n" +

            "Set the relation the displaced ruler needs to never join you when asked. Default is -50.\n" +
            "relationNeededForDisplacedClanToNeverJoin=-50.0\n\n" +

            "Set the gold needed with a multiplier. The formula is; amtAwayFromJoinFree * thisMultiplier. The result IS rounded. Default is 1000.\n" +
            "goldNeededMultiplierForDisplacedClanToJoin=1000.0\n\n" +

            "Set if vassals need to be gone for destruction timer to start. Set to 0 to only check fief count for kingdom destruction. Default is 1.\n" +
            "vassalsNeedToBeGone=1.0\n\n";

        private static void CreateConfigFile()
        {
            StreamWriter sw = new(configFilePath);
            sw.WriteLine(configFileString);
            sw.Close();
        }

        public static void LoadConfig()
        {
            StreamReader sr = new(configFilePath);
            string line;
            // Read and display lines from the file until the end of
            // the file is reached.
            while ((line = sr.ReadLine()) != null)
            {
                int indexOfEqualSign = line.IndexOf('=');
                if (indexOfEqualSign != -1)
                {
                    string key = line.Substring(0, indexOfEqualSign);
                    string value = line.Substring(indexOfEqualSign + 1);
                    configValues[key] = Convert.ToDouble(value);
                }
            }
            sr.Close();
        }

        static Config()
        {
            if (!File.Exists(configFilePath))
                CreateConfigFile();
            LoadConfig();
        }

        public static double GetKeyValue(string key)
        {
            try
            {
                return configValues[key];
            }
            catch (KeyNotFoundException e)
            {
                File.Delete(configFilePath);
                CreateConfigFile();
                LoadConfig();
                return configValues[key];
            }
        }
    }
}
