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
            bool isDate = DateTime.TryParse(command, out date) && !isVersion; //подумать!!!!!!!!!!!!!!!!!!!!!!
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
            for (int i = 0; i < subDirs.Count(); i++)
                if (Version.TryParse(subDirs[i].Name, out v))
                    folderVersions.Add(v);
            if (folderVersions.Count == 0)
                return "";
            
            bool previousPartIsGreater = false;
            List<int[]> folderVersionsParts = folderVersions.Select(f => new int[4] { f.Major, f.Minor, f.Build, f.Revision }).ToList<int[]>();
            int[] versionParts = new int[4] { version.Major, version.Minor, version.Build, version.Revision };
            int[] foundVersionParts;
            if (!FindVersion(folderVersionsParts, versionParts, previousPartIsGreater, 0, out foundVersionParts))
                return "";
            else
            {
                Version foundVersion;
                Version.TryParse(String.Join(".", foundVersionParts), out foundVersion);
                return subDirs[folderVersions.IndexOf(foundVersion)].Name;
            }
        }

        private static bool FindVersion(List<int[]> folderVersionsParts, int[] versionParts, bool previousPartIsGreater, int j, out int[] foundVersion)
        {
            List<int[]> filteredFolderVersionsParts = new List<int[]>(folderVersionsParts);
            bool found = false;
            foundVersion = new int[] { -1, -1, -1, -1 };
            if (j <= 3)
                if (!previousPartIsGreater)
                {
                    int versionPart = versionParts[j];
                    if (versionPart == -1)
                        versionPart = 0;
                    filteredFolderVersionsParts.RemoveAll(f => versionPart < f[j]);
                    if (filteredFolderVersionsParts.Count == 0)
                        return false;
                    filteredFolderVersionsParts = filteredFolderVersionsParts.OrderBy(f => versionPart - f[j]).ToList<int[]>();
                    if (j == 3)
                    {
                        foundVersion = filteredFolderVersionsParts.First();
                        return true;
                    }
                    for (int i = 0; i < filteredFolderVersionsParts.Count; i++)
                    {
                        if (versionPart - filteredFolderVersionsParts[i][j] > 0)
                            previousPartIsGreater = true;
                        if (!found)
                            found = FindVersion(filteredFolderVersionsParts.Where(f => f[j] == filteredFolderVersionsParts[i][j]).ToList<int[]>(), versionParts,
                                previousPartIsGreater, j + 1, out foundVersion);
                        else
                            break;
                    }
                }
                else
                {
                    filteredFolderVersionsParts = filteredFolderVersionsParts.OrderByDescending(f => f[j]).ToList<int[]>();
                    if (j == 3)
                    {
                        foundVersion = filteredFolderVersionsParts.First();
                        return true;
                    }
                    for (int i = 0; i < filteredFolderVersionsParts.Count; i++)
                        if (!found)
                            found = FindVersion(filteredFolderVersionsParts.Where(f => f[j] == filteredFolderVersionsParts[i][j]).ToList<int[]>(), versionParts, 
                                previousPartIsGreater, j + 1, out foundVersion);
                        else
                            break;
                }

            return found;
        }

        private static string FindFolderOfDate(DirectoryInfo rootDirectory, DateTime folderDate)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs == null || subDirs.Count() == 0)
                return "";

            int index = -1;
            DateTime date = new DateTime();
            DateTime foundDate = new DateTime();
            TimeSpan dateDiff = TimeSpan.MaxValue;
            for (int i = 0; i < subDirs.Count(); i++)
            {
                if (DateTime.TryParse(subDirs[i].Name, out date) && folderDate >= date && folderDate - date < dateDiff)
                {
                    foundDate = date;
                    index = i;
                    dateDiff = folderDate - date;
                }
            }
            if (index == -1)
                return "";
            return subDirs[index].Name;
        }

        private static string FindFolderOfLatestDate(DirectoryInfo rootDirectory)
        {
            List<DirectoryInfo> subDirs = rootDirectory.GetDirectories().ToList<DirectoryInfo>();
            if (subDirs == null || subDirs.Count() == 0)
                return "";

            DateTime d = new DateTime();
            DateTime latestDate = DateTime.MinValue;
            int index = -1;
            for (int i = 0; i < subDirs.Count(); i++)
                if (DateTime.TryParse(subDirs[i].Name, out d) && d > latestDate)
                {
                    latestDate = d;
                    index = i;
                }
            if (index == -1)
                return "";
            return subDirs[index].Name;
        }
        
    }
}