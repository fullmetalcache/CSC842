//////////////////////////////////////////////
// File: HoneyFiles.cs
// Author: Brian Fehrman
// Date: 2021-07-07
// Description: Main class for deploying, monitoring
//              and alerting on a HoneyFile
//////////////////////////////////////////////

using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Mail;
using System.Security.AccessControl;
using System.Threading;

namespace HoneyFile
{
    class HoneyFiles
    {
        private static string _emailAddress = "";
        private static string _monitorFilename = "";
        private static string _password = "";

        // Uses auditpol to enable file system auditing
        public void EnableAuditing()
        {
            Process proc = new Process();
            proc.StartInfo.FileName = "auditpol";
            proc.StartInfo.Arguments = "/set /subcategory:\"File System\" /success:enable /failure:enable";
            proc.StartInfo.CreateNoWindow = true;
            proc.Start();
        }

        // User-facing class for creating a HoneyFile
        public void DeployFile(string outFilename, string inFilename = "")
        {
            createFile(outFilename, inFilename);
            setFileAuditing(outFilename);
        }

        // Begin monitoring on a file
        public void StartMonitoring(string filename, string emailAddress, string password)
        {
            _monitorFilename = filename;
            _emailAddress = emailAddress;
            _password = password;

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

        // Create a new Honey File
        private void createFile(string outFilename, string inFilename = "")
        {
            // If user provided a template, copy that to the new file location for monitoring
            if (inFilename != "")
            {
                File.Copy(inFilename, outFilename);
            }
            else
            {
                // Write out some content to make the file seem more legit
                using (StreamWriter outputFile = new StreamWriter(outFilename))
                {
                    outputFile.WriteLine("psexec \\\\$args[0] -s net users admin Greatpassword1 /add");
                    outputFile.WriteLine("psexec \\\\$args[0] -s net localgroup Administrators admin /add");
                }
            }
        }

        //https://stackoverflow.com/questions/53430475/reading-windows-logs-efficiently-and-fast
        private static void EventLog_EntryWritten(object sender, EntryWrittenEventArgs newEvent)
        {
            if (newEvent.Entry.EventID == 4663)
            {
                string accountDomain = "";
                string accountName = "";
                string fileName = "";
                bool fileAccessed = false;

                // Tokenize the log event entry based on lines in the message
                string[] lines = newEvent.Entry.Message.Split('\n');

                // Iterate through each line in the event entry message
                foreach (string line in lines)
                {
                    if (line.Contains("Account Name:"))
                    {
                        // Attempt to extract the account name that accessed the file
                        accountName = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("Account Domain:"))
                    {
                        // Attempt to extract the account domain of the user that accessed the file
                        accountDomain = line.Split(':')[1].Trim();
                    }
                    else if (line.Contains("Object Name:"))
                    {
                        // Grab just the filename and not the whole path as this can cause
                        // some issues depending on how the file was accessed
                        string[] checkTokens = _monitorFilename.Split('\\');
                        string checkName = checkTokens[checkTokens.Length - 1];

                        // Check if the filename that is monitored is contained in the
                        // path of the file that generated an alert
                        if (line.Contains(checkName))
                        {
                            // Extract the full path of the file from the message
                            fileName = line.Split(':')[1].Trim() + ":" + line.Split(':')[2].Trim(); ;
                            fileAccessed = true;
                        }
                    }
                }

                if (fileAccessed)
                {
                    // If the Honey File was accessed, send an alert via email using the
                    // credentials that the user provided
                    Console.WriteLine("Sending alert for: " + accountName);
                    var smtpClient = new SmtpClient("smtp.gmail.com")
                    {
                        Port = 587,
                        Credentials = new NetworkCredential(_emailAddress, _password),
                        EnableSsl = true,
                    };

                    smtpClient.Send(_emailAddress, _emailAddress, "HONEYFILE ALERT:" + accountName, fileName + 
                        " accessed by " + accountDomain + "\\" + accountName);
                }
            }
        }

        // Sets the file auditing attributes for a given file
        private void setFileAuditing(string filename)
        {
            // Get file handle to the target file's security settings
            FileSecurity fileSecSettings = File.GetAccessControl(filename);
            // Form the string to set the rule for Domain Users
            string userGroup = Environment.UserDomainName + "\\" + "Domain Users";

            fileSecSettings.AddAuditRule(new FileSystemAuditRule(userGroup, FileSystemRights.ReadData, AuditFlags.Success | AuditFlags.Failure));

            // Update target file with the new auditing settings
            File.SetAccessControl(filename, fileSecSettings);
        }
    }
}
