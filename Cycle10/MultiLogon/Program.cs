////////////////////////////////////////
// File: Program.cs
// Author: Brian Fehrman
// Date: 2021-07-19
// Description: Main entry point for MultiLogon program
////////////////////////////////////////

using System;

namespace MultiLogonDetect
{
    class Program
    {
        static void Main(string[] args)
        {
            string ignoreListFilename = "";
            string emailAddress = "";
            string password = "";

            // Check for min number of arguments. Print usage message and exit if
            // insufficient arguments are provided
            if (args.Length < 2)
            {
                Console.WriteLine("Usage: MultiLogon.exe emailAddress password [ignoreListFilename]");
                return;
            }
            else if (2 < args.Length)
            {
                // If we got the optional ignoreListFilename parameter, assign it here
                ignoreListFilename = args[2];
            }

            emailAddress = args[0];
            password = args[1];

            MultiLogon mLogon = new MultiLogon(emailAddress, password, ignoreListFilename);

            mLogon.StartMonitoring();
        }
    }
}
