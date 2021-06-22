///////////////////////////////////////////////////////
// File: EventMonitor.cs
// Author: Brian Fehrman
// Date: 2021-06-21
// Description: 
//		Class for monitoring and alerting on logins for
//		a given list of honey accounts
///////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace HoneyAccounts
{
    class EventMonitor
    {
        //Constructor
        public EventMonitor() 
        {

        }

        private static List<string> _users;

		// Perpetually monitor for new security events
		// with a one second wait between each update check
        public void StartMonitoring(List<string> users)
        {
            _users = users;

            var eventLog = new EventLog("Security")
            {
                EnableRaisingEvents = true
            };

            eventLog.EntryWritten += EventLog_EntryWritten;

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        //https://stackoverflow.com/questions/53430475/reading-windows-logs-efficiently-and-fast
        private static void EventLog_EntryWritten(object sender, EntryWrittenEventArgs newEvent)
        {
			// Check for failed logon event ID
            if (newEvent.Entry.EventID == 4625)
            {
				// Get the username from the event
                string accountName = newEvent.Entry.Message.Split("Logon Type")[1].Split("Account Name:")[1].Split("\r\n")[0].Trim();
                
				// Check if the username for the failed logon event
				// matches username in our monitoring list.
				// Generate an alert if so
				foreach( string user in _users)
                {
                    if( accountName == user)
                    {
                        string workstationSource = newEvent.Entry.Message.Split("Network Information")[1].Split("Workstation Name:")[1].Split("\r\n")[0].Trim();
                        Console.WriteLine(String.Format("Logon Detected for: {0}", user));
                        Console.WriteLine(String.Format("From: {0}", workstationSource));
                        Console.WriteLine("");
                        break;
                    }
                }
            }
        }
    }
}
