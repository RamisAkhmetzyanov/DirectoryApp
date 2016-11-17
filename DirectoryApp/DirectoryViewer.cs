using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace DirectoryApplication
{
    public static class DirectoryViewer
    {
        private static char _separator = '|';
        private static string _dateFormat = "yyyy-MM-dd";

        public static bool ResolveInput(string input, out string message)
        {
            string[] splitedInput = input.Split(_separator);
            if (splitedInput.Length < 2)
            {
                message = string.Format("Entered command doesn`t contain separator '{0}'", _separator);
                return false;
            }
            if (splitedInput.Length > 2)
            {
                message = string.Format("Too many separators '{0}' in command", _separator); 
                return false;
            }
            string rootFolderPath = splitedInput[0];
            string command = splitedInput[1];
            rootFolderPath = Environment.ExpandEnvironmentVariables(rootFolderPath);

            DirectoryInfo rootDirectory;
            try
            {
                rootDirectory = new DirectoryInfo(rootFolderPath);
            }
            catch
            {
                message = "Entered path is not valid or not accessible for creating subdirectories";
                return false;
            }
            if (!rootDirectory.Exists)
            {
                try
                {
                    rootDirectory.Create();
                }
                catch
                {
                    message = "Entered path is not valid or not accessible for creating subdirectories";
                    return false;
                }
            }
            try
            {
                if (rootDirectory.GetDirectories().Length == 0)
                    CreateTestFolders(rootDirectory);
            }
            catch
            {
                message = "Entered path is not valid or not accessible for creating subdirectories";
                return false;
            }

            Version version;
            DateTime date;
            bool isVersion = Version.TryParse(command, out version);
            bool isDate = DateTime.TryParseExact(command, _dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None, out date);
            if (isVersion)
            {
                var FoundFoldersNames = FindFolderOfVersion(rootDirectory, version);
                if (FoundFoldersNames.Length == 0)
                {
                    message = string.Format("Folder of version {0} or previous not found", version.ToString());
                    return false;
                }
                else
                {
                    message = FoundFoldersNames.Aggregate(seed: string.Empty, func: (result, f) => 
                        string.Format("{0}\r\n{1}", Path.Combine(rootDirectory.FullName, f), result));
                    return true;
                }
            }
            if (isDate)
            {
                var FoundFoldersNames = FindFolderOfDate(rootDirectory, date);
                if (FoundFoldersNames.Length == 0)
                {
                    message = string.Format("Folder of date {0} or previous not found", date.ToString(_dateFormat));
                    return false;
                }
                else
                {
                    message = FoundFoldersNames.Aggregate(seed: string.Empty, func: (result, f) =>
                        string.Format("{0}\r\n{1}", Path.Combine(rootDirectory.FullName, f), result));
                    return true;
                }
            }
            if (command.Equals("latest", StringComparison.CurrentCultureIgnoreCase))
            {
                string FoundFolderName = string.Empty;
                FoundFolderName = FindFolderOfLatestDate(rootDirectory);
                if (string.IsNullOrEmpty(FoundFolderName))
                {
                    message = "Folder of latest date not found. There are no subdirectories named by date";
                    return false;
                }

                message = Path.Combine(rootDirectory.FullName, FoundFolderName);
                return true;
            }

            message = "Entered command is not valid. Enter version, date(format: YYYY-MM-DD) or 'Latest' command";
            return false;
        }

        private static void CreateTestFolders(DirectoryInfo rootDirectory)
        {
            Random random = new Random();
            int versionFoldersCount = random.Next(5, 10);
            int dateFoldersCount = random.Next(5, 10);
            for (int i = 0; i < versionFoldersCount; i++)
                rootDirectory.CreateSubdirectory((new Version(random.Next(1, 3), random.Next(0, 10), random.Next(0, 10), 
                    random.Next(0, 10))).ToString());
            for (int i = 0; i < dateFoldersCount; i++)
                rootDirectory.CreateSubdirectory((new DateTime(random.Next(2014, DateTime.Today.Year), 
                    random.Next(1, DateTime.Today.Month), random.Next(1, DateTime.Today.Day))).ToString(_dateFormat));
        }

        private static string[] FindFolderOfVersion(DirectoryInfo rootDirectory, Version version)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs.Count == 0)
                return new string[0];
            List<Version> folderVersions = new List<Version>();
            List<DirectoryInfo> folders = new List<DirectoryInfo>();
            Version currentFolderVersion;

            foreach (DirectoryInfo subDir in subDirs)
                if (Version.TryParse(subDir.Name, out currentFolderVersion))
                {
                    folderVersions.Add(currentFolderVersion);
                    folders.Add(subDir);
                }
            folderVersions.Sort();
            Version foundVersion = folderVersions.FindLast(f => f.CompareTo(version) <= 0);

            if (foundVersion == null)
                return new string[0];
            return folders.FindAll(f => Version.Parse(f.Name).Equals(foundVersion)).Select(s => s.Name).ToArray<string>();
        }

        private static string[] FindFolderOfDate(DirectoryInfo rootDirectory, DateTime date)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs.Count == 0)
                return new string[0];
            List<DateTime> folderDates = new List<DateTime>();
            List<DirectoryInfo> folders = new List<DirectoryInfo>();
            DateTime currentFolderDate;

            foreach (DirectoryInfo subDir in subDirs)
                if (DateTime.TryParseExact(subDir.Name, _dateFormat, CultureInfo.CurrentCulture, DateTimeStyles.None,
                    out currentFolderDate))
                {
                    folderDates.Add(currentFolderDate);
                    folders.Add(subDir);
                }
            folderDates.Sort();
            DateTime foundDate = folderDates.FindLast(f => f.CompareTo(date) <= 0);

            if (foundDate == null)
                return new string[0];
            return folders.FindAll(s => DateTime.Parse(s.Name).Equals(foundDate)).Select(s => s.Name).ToArray<string>();
        }

        private static string FindFolderOfLatestDate(DirectoryInfo rootDirectory)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs.Count == 0)
                return string.Empty;
            DateTime currentFolderDate;
            Version currentFolderVersion;
            foreach(DirectoryInfo subDir in subDirs)
                if (!DateTime.TryParse(subDir.Name, out currentFolderDate) && !Version.TryParse(subDir.Name, out currentFolderVersion))
                    subDirs.Remove(subDir);
            return subDirs.OrderByDescending(s => s.CreationTime).First().Name;
        }
        
    }
}