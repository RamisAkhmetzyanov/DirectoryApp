using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace DirectoryApp
{
    public static class DirectoryViewer
    {
        private static char separator = '|';

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
            string command = input.Substring(separatorIndex + 1, input.Length - separatorIndex - 1);
            RootFolderPath = Environment.ExpandEnvironmentVariables(RootFolderPath);

            DirectoryInfo RootDirectory = new DirectoryInfo(RootFolderPath);
            if (!RootDirectory.Exists)
            {
                try
                {
                    RootDirectory.Create();
                }
                catch
                {
                    message = "Entered path is not valid or not accessible for creating subdirectories";
                    return false;
                }
            }
            try
            {
                if (RootDirectory.GetDirectories() == null || RootDirectory.GetDirectories().Count() == 0)
                    CreateTestFolders(RootDirectory);
            }
            catch
            {
                message = "Entered path is not valid or not accessible for creating subdirectories";
                return false;
            }
            Version version;
            DateTime date;
            bool isVersion = Version.TryParse(command, out version);
            bool isDate = DateTime.TryParse(command, out date) && !isVersion;
            string foundFolderName = "";
            if (isVersion)
            {
                foundFolderName = FindFolderOfVersion(RootDirectory, version);
                if (foundFolderName == "")
                {
                    message = "Folder of version " + version.ToString() + " or previous not found";
                    return false;
                }
                else
                {
                    foundFolderName = Path.Combine(RootDirectory.FullName, foundFolderName);
                    message = foundFolderName;
                    return true;
                }
            }
            if (isDate)
            {
                foundFolderName = FindFolderOfDate(RootDirectory, date);
                if (foundFolderName == "")
                {
                    message = "Folder of date " + command + " or early not found";
                    return false;
                }
            }
            if (command.ToLower().Equals("latest"))
            {
                foundFolderName = FindFolderOfLatestDate(RootDirectory);
                if (foundFolderName == "")
                {
                    message = "Folder of latest date not found. There are no subdirectories named by date";
                    return false;
                }
            }
            if (foundFolderName == "")
            {
                message = "Entered command is not valid. Enter version, date or 'Latest' command";
                return false;
            }

            foundFolderName = Path.Combine(RootDirectory.FullName, foundFolderName);
            message = foundFolderName;
            return true;
        }

        private static void CreateTestFolders(DirectoryInfo rootDirectory)
        {
            Random random = new Random();
            int versionFoldersCount = random.Next(5, 10);
            int dateFoldersCount = random.Next(5, 10);
            for (int i = 0; i < versionFoldersCount; i++)
                rootDirectory.CreateSubdirectory(String.Join(".", new string[] { random.Next(1, 3).ToString(), random.Next(0, 10).ToString(),
                    random.Next(0, 10).ToString(), random.Next(0, 10).ToString() }));
            for (int i = 0; i < dateFoldersCount; i++)
                rootDirectory.CreateSubdirectory(String.Join("-", new string[] { random.Next(2014, 2016).ToString(), random.Next(1, 12).ToString(),
                    random.Next(1, 31).ToString() }));
        }

        private static string FindFolderOfVersion(DirectoryInfo rootDirectory, Version version)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs == null || subDirs.Count() == 0)
                return "";
            List<Version> folderVersions = new List<Version>();
            Version v = new Version();
            int i = 0;
            while (i < subDirs.Count())
                if (Version.TryParse(subDirs[i].Name, out v))
                {
                    folderVersions.Add(v);
                    i++;
                }
                else
                    subDirs.RemoveAt(i);
            if (folderVersions.Count == 0)
                return "";
            folderVersions.Sort();
            int index = folderVersions.Where(f => f.CompareTo(version) <= 0).Count() - 1;
            if (index == -1)
                return "";

            return subDirs.Find(s => Version.Parse(s.Name).Equals(folderVersions[index])).Name;
        }

        private static string FindFolderOfDate(DirectoryInfo rootDirectory, DateTime folderDate)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs == null || subDirs.Count() == 0)
                return "";
            
            List<DateTime> folderDates = new List<DateTime>();
            DateTime date = new DateTime();
            int i = 0;
            while (i < subDirs.Count())
                if (DateTime.TryParse(subDirs[i].Name, out date))
                {
                    folderDates.Add(date);
                    i++;
                }
                else
                    subDirs.RemoveAt(i);
            folderDates.Sort();
            int index = folderDates.Where(f => f.CompareTo(folderDate) <= 0).Count() - 1;
            if (index == -1)
                return "";

            return subDirs.Find(s => DateTime.Parse(s.Name).Equals(folderDates[index])).Name;
        }

        private static string FindFolderOfLatestDate(DirectoryInfo rootDirectory)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs == null || subDirs.Count() == 0)
                return "";

            DateTime d;
            Version v;
            int i = 0;
            while (i < subDirs.Count())
                if (!DateTime.TryParse(subDirs[i].Name, out d) && !Version.TryParse(subDirs[i].Name, out v))
                    subDirs.RemoveAt(i);
                else
                    i++;

            return subDirs.OrderByDescending(s => s.CreationTime).First().Name;
        }
        
    }
}