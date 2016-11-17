using System;

namespace DirectoryApplication
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = "";
            string output;
            bool success = false;
            Console.WriteLine("Enter command (for example:'%SYSTEMDRIVE%\\Test|41.0.1.0').");
            Console.WriteLine("Date Format: 2016-01-01");
            Console.WriteLine("To Exit Application Enter 'Exit'. ");

            while (input != "Exit")
            {
                input = Console.ReadLine();
                success = DirectoryViewer.ResolveInput(input, out output);
                if (success)
                    Console.WriteLine("Found: {0}", output);
                else
                {
                    Console.WriteLine("Error: {0}", output);
                    Console.WriteLine();
                }
            }
        }
    }
}