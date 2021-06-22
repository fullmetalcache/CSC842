///////////////////////////////////////////////////////
// File: Program.cs
// Author: Brian Fehrman
// Date: 2021-06-21
// Description: 
//		Main entry point for the HoneyAccounts program
///////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;

namespace HoneyAccounts
{
    class Program
    {
        static void Main(string[] args)
        {
			// Check that we have the correct amount of arguments
            if (args.Length < 1)
            {
                Console.WriteLine("Usage: HoneyAcounts.exe users.txt");
                Environment.Exit(-1);
            }

            string filename = args[0];

            List<string> userList = new List<string>();
			
            // Read in and store list of users
            string[] fin = System.IO.File.ReadAllLines(filename);
            foreach (string user in fin)
            {
                userList.Add(user);
            }

			// Create the accounts
            AccountManagement am = new AccountManagement();
            am.AddUsers(userList, 40);

			// Monitor the accounts
            EventMonitor em = new EventMonitor();
            em.StartMonitoring(userList);

        }
    }
}
