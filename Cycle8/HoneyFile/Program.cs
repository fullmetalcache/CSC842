//////////////////////////////////////////////
// File: Program.cs
// Author: Brian Fehrman
// Date: 2021-07-07
// Description: Main entry point for HoneyFile program
//////////////////////////////////////////////

using System;

namespace HoneyFile
{
    class Program
    {
        static void Main(string[] args)
        {
            string inFilename = "";
            string emailAddress = "";
            string outFilename = "";
            string password = "";

            // Check for min number of arguments. Print usage message and exit if
            // insufficient arguments are provided
            if (args.Length < 3)
            {
                Console.WriteLine("Usage: HoneyFile.exe outFile emailAddress password [inFile]");
                return;
            }
            else if (3 < args.Length)
            {
                // If we got the optional inFilename parameter, assign it here
                inFilename = args[3];
            }

            outFilename = args[0].Replace("\\", "\\\\");
            Console.WriteLine(outFilename);
            emailAddress = args[1];
            password = args[2];

            HoneyFiles hf = new HoneyFiles();

            // Enable file auditing for the system
            hf.EnableAuditing();

            // Create the honey file and set auditing attributes
            hf.DeployFile(outFilename, inFilename);

            // Begin monitoring and alerting on the honey file
            hf.StartMonitoring(outFilename, emailAddress, password);
        }
    }
}
