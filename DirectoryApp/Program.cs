using System;

namespace DirectoryApp
{
    class Program
    {
        static void Main(string[] args)
        {
            string input = "";
            string output;
            Console.WriteLine("Enter command (for example:'%SYSTEMDRIVE%\\Test|41.0.1.0').");
            Console.WriteLine("To Exit Application Enter 'Exit'. ");
            while (input != "Exit")
            {
                input = Console.ReadLine();
                DirectoryViewer.ResolveInput(input, out output);
                Console.WriteLine(output);
                Console.WriteLine();
            }
        }
    }
}