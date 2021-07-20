////////////////////////////////////////
// File: MultiLogon.cs
// Author: Brian Fehrman
// Date: 2021-07-19
// Description: Main class for monitoring for multiple logons from
//              a single user
////////////////////////////////////////
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Mail;
using System.Threading;

namespace MultiLogonDetect
{
    class MultiLogon
    {
        // Yea it's a bit ugly for a type...but can fix this later
        private static Dictionary<string, Dictionary<string, List<string>>> _logons;
        private static List<string> _ignoreList;
        private static string _emailAddress = "";
        private static string _password = "";

        // Constructor for class
        public MultiLogon(string emailAddress, string password, string ignoreListFileName )
        {
            _logons = new Dictionary<string, Dictionary<string, List<string>>>();
            _ignoreList = new List<string>();
            _emailAddress = emailAddress;
            _password = password;

            if (ignoreListFileName != "")
            {
                readIgnoreList(ignoreListFileName);
            }
        }

        private void readIgnoreList(string ignoreListFileName)
        {
            string account;

            // Iterate through accounts in file and add them to ignore list
            System.IO.StreamReader file = new System.IO.StreamReader(ignoreListFileName);
            while ((account = file.ReadLine()) != null)
            {
                _ignoreList.Add(account);
            }

            file.Close();
        }

        // Begin monitoring for multiple logons
        public void StartMonitoring()
        {
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
            // Handle logon and logoff events
            if (newEvent.Entry.EventID == 4624 || newEvent.Entry.EventID == 4634)
            {
                string accountDomain = "";
                string accountName = "";
                string accountKey = "";
                string computerIP = "";
                string logonID = "";
                bool genAlert = false;

                // Tokenize the log event entry based on lines in the message
                string[] lines = newEvent.Entry.Message.Split('\n');

                // Iterate through each line in the event entry message
                foreach (string line in lines)
                {
                    if (line.Contains("Account Name:") && !line.Contains("Network Account Name:"))
                    {
                        // Attempt to extract the account name that logged in
                        accountName = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("Account Domain:") && !line.Contains("Network Account Domain:"))
                    {
                        // Attempt to extract the account domain of the user that logged in
                        accountDomain = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("Logon ID:") && !line.Contains("Linked Logon ID:"))
                    {
                        // Attempt to extract the system on which the user logged in
                        logonID = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("Source Network Address:"))
                    {
                        // Attempt to extract the system on which the user logged in
                        computerIP = line.Split(':')[1].Trim();
                    }
                }

                // Filter out machine accounts
                if(accountName.Contains("$"))
                {
                    return;
                }

                // form the key for indexing the dictionary
                accountKey = accountName;

                Console.WriteLine(accountKey + " " + computerIP + " " + logonID);

                // don't alert if the account is in the ignore list
                if(_ignoreList.Contains(accountKey))
                {
                    return;
                }

                // Create new entry for account if it's not present in the dictionary
                if (!_logons.ContainsKey(accountKey))
                {
                    _logons[accountKey] = new Dictionary<string, List<string>>();
                }

                // Handle adding a system that was logged into
                if(newEvent.Entry.EventID == 4624)
                {
                    if (!_logons[accountKey].ContainsKey(computerIP))
                    {
                        _logons[accountKey][computerIP] = new List<string>();
                        // If we exceed the threshold, generate an alert
                        // Here, it's >1 for demonstrative purposes but
                        // in practice you'd probably want at least >2
                        // since somebody logging into a system and then
                        // RDP'ing to another system is common. Only do
                        // this if we added a new system to the list of systems
                        // and it put us over the threshold.
                        if (_logons[accountKey].Count > 1)
                        {
                            genAlert = true;
                        }
                    }

                    if (!_logons[accountKey][computerIP].Contains(logonID))
                    {
                        _logons[accountKey][computerIP].Add(logonID);
                    }
                } 
                else if (newEvent.Entry.EventID == 4634)
                {
                    // Handle removing a system from the list if a user logged off
                    if (_logons.ContainsKey(accountKey))
                    {
                        foreach (KeyValuePair<string, List<string>> system in _logons[accountKey])
                        {
                            if(system.Value.Contains(logonID))
                            {
                                _logons[accountKey][system.Key].Remove(logonID);
                                if (_logons[accountKey][system.Key].Count < 1)
                                {
                                    // If no logonIDs are left, the user has completely logged off this
                                    // system, so remove it from the list
                                    _logons[accountKey].Remove(system.Key);
                                }

                                break;
                            }
                        }
                    }
                }

                if (genAlert)
                {
                    // If the threshold for simultaneous logons was exceeded
                    // send an alert via email using the credentials that the user provided
                    Console.WriteLine("Sending alert for: " + accountDomain + "\\" + accountName);
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(_emailAddress, _password),
                        EnableSsl = true,
                    };

                    string body = "Suspicious logon activity:\n";
                    body += "Account: " + accountDomain + "\\" + accountName + "\n";
                    body += "Systems:\n";
                    foreach( KeyValuePair<string, List<string>> system in _logons[accountKey])
                    {
                        body += system.Key + "\n";
                    }

                    Console.WriteLine(body);
                    return;

                    smtpClient.Send(_emailAddress, _emailAddress, "SUSPICIOUS LOGON:" + accountName, body);
                }
            }
        }
    }
}
