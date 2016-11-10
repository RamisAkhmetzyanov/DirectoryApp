using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;

namespace DirectoryApp
{
    public static class DirectoryViewer
    {
        private static char separator = '|';
        private static string dateFormat = "yyyy-MM-dd";

        public static bool ResolveInput(string input, out string message)
        {
            message = "";
            int separatorIndex = input.IndexOf(separator);
            if (separatorIndex <= 0)
            {
                message = "Entered command doesn`t contain separator '|'";
                return false;
            }
            string RootFolderPath = input.Substring(0, separatorIndex);
            if (RootFolderPath[RootFolderPath.Length - 1] != '\\')
                RootFolderPath = RootFolderPath + "\\";
            string command = input.Substring(separatorIndex + 1, input.Length - separatorIndex - 1);
            RootFolderPath = Environment.ExpandEnvironmentVariables(RootFolderPath);

            if (!Directory.Exists(RootFolderPath))
            {
                try
                {
                    Directory.CreateDirectory(RootFolderPath);
                    CreateTestFolders(RootFolderPath);
                }
                catch
                {
                    message = "Entered path is not valid or not accessible for creating subdirectories";
                    return false;
                }
            }
            Directory.SetCurrentDirectory(RootFolderPath);
            try
            {
                if (Directory.GetDirectories(RootFolderPath) == null || Directory.GetDirectories(RootFolderPath).Count() == 0)
                    CreateTestFolders(RootFolderPath);

            }
            catch
            {
                message = "Entered path is not valid or not accessible for creating subdirectories";
                return false;
            }
            Version version;
            bool isVersion = Version.TryParse(command, out version),
                isDate = IsDate(command);
            string foundFolderName = "";
            if (isVersion)
            {
                foundFolderName = FindFolderOfVersion(RootFolderPath, version);
                if (foundFolderName == "")
                {
                    message = "Folder of version " + version.ToString() + " or previous not found";
                    return false;
                }
                else
                {
                    foundFolderName = Path.GetFullPath(foundFolderName);
                    message = foundFolderName;
                    return true;
                }
            }
            if (isDate)
            {
                DateTime date;
                DateTime.TryParse(command, out date);
                foundFolderName = FindFolderOfDate(RootFolderPath, date);
                if (foundFolderName == "")
                {
                    message = "Folder of date " + command + " or early not found";
                    return false;
                }
            }
            if (command.ToLower() == "latest")
                foundFolderName = FindFolderOfLatestDate(RootFolderPath);
            if (foundFolderName == "")
            {
                message = "Entered command is not valid. Enter version, date or 'Latest' command";
                return false;
            }

            foundFolderName = Path.GetFullPath(foundFolderName);
            message = foundFolderName;
            return true;
        }

        private static void CreateTestFolders(string rootFolderPath)
        {
            Random random = new Random();
            int versionFoldersCount = random.Next(5, 10);
            int dateFoldersCount = random.Next(5, 10);
            for (int i = 0; i < versionFoldersCount; i++)
                Directory.CreateDirectory(rootFolderPath + random.Next(1, 3).ToString() + "." +
                    random.Next(0, 10).ToString() + "." + random.Next(0, 10).ToString() + "." +
                    random.Next(0, 10).ToString());
            for (int i = 0; i < dateFoldersCount; i++)
                Directory.CreateDirectory(rootFolderPath + random.Next(2014, 2016).ToString() + "-" +
                    random.Next(1, 12).ToString() + "-" + random.Next(1, 31).ToString());
        }

        private static string FindFolderOfVersion(string rootFolderPath, Version version)
        {
            string[] folders = Directory.GetDirectories(rootFolderPath);
            if (folders == null || folders.Count() == 0)
                return "";
            List<Version> folderVersions = new List<Version>();
            List<BigInteger> intVersions = new List<BigInteger>();
            BigInteger multiplier = BigInteger.Pow(10, 10);
            Version v = new Version();
            for (int i = 0; i < folders.Count(); i++)
            {
                folders[i] = Path.GetFileName(folders[i]);
                if (Version.TryParse(folders[i], out v))
                {
                    folderVersions.Add(v);
                    intVersions.Add(ConvertToBigInteger(v));
                }
            }
            if (folderVersions.Count == 0)
                return "";
            BigInteger intVersion = ConvertToBigInteger(version);
            BigInteger minDiff = -1;
            int index = -1;
            for (int i = 0; i < intVersions.Count; i++)
            {
                if (minDiff == -1 && intVersion >= intVersions[i] ||
                    minDiff != -1 && intVersion >= intVersions[i] && intVersion - intVersions[i] < minDiff)
                {
                    minDiff = intVersion - intVersions[i];
                    index = i;
                }
            }

            if (index == -1)
                return "";

            return folderVersions[index].ToString();
        }

        private static string FindFolderOfDate(string rootFolderPath, DateTime folderDate)
        {
            string[] folders = Directory.GetDirectories(rootFolderPath);
            if (folders == null || folders.Count() == 0)
                return "";

            int index = -1;
            List<DateTime> folderDates = new List<DateTime>();
            DateTime date = new DateTime(),
                foundDate = new DateTime();
            TimeSpan dateDiff = TimeSpan.MaxValue;
            for (int i = 0; i < folders.Count(); i++)
            {
                folders[i] = Path.GetFileName(folders[i]);
                if (DateTime.TryParse(folders[i], out date) && folderDate >= date && folderDate - date < dateDiff)
                {
                    foundDate = date;
                    index = i;
                    dateDiff = folderDate - date;
                }
            }
            if (index == -1)
                return "";
            return foundDate.ToString(dateFormat);
        }

        private static string FindFolderOfLatestDate(string rootFolderPath)
        {
            string[] folders = Directory.GetDirectories(rootFolderPath);
            if (folders == null || folders.Count() == 0)
                return "";

            DateTime d = new DateTime();
            DateTime latestDate = DateTime.MinValue;
            int index = 0;
            for (int i = 0; i < folders.Count(); i++)
            {
                folders[i] = Path.GetFileName(folders[i]);
                if (DateTime.TryParse(folders[i], out d) && d > latestDate)
                {
                    latestDate = d;
                    index = i;
                }
            }

            return folders[index];
        }

        private static BigInteger ConvertToBigInteger(Version v)
        {
            BigInteger multiplier = BigInteger.Pow(10, 10);
            return BigInteger.Pow(multiplier, 4) * v.Major + BigInteger.Pow(multiplier, 3) * v.Minor +
                         BigInteger.Pow(multiplier, 2) * v.Build + BigInteger.Pow(multiplier, 1) * v.Revision +
                         v.MajorRevision * multiplier + v.MinorRevision;
        }

        public static bool IsDate(string input)
        {
            DateTime date;
            return DateTime.TryParse(input, out date) && input.Length <= 11;
        }
    }
}