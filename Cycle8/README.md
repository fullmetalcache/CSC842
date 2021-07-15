# HoneyFiles

C# Program to catch suspicious file access within a Windows Active Directory environment.

## Enable File System Auditing
Before we can generate alerts on a particular file being accessed, we have to first enable file auditing on the system. There are ways to do this procedure manually through Group Policy (https://www.varonis.com/blog/windows-file-system-auditing/). An easy way to do this programmatically is to use Windows built-in auditpol program. The program will make a call to the auditpol program with the necessary arguments to enable file auditing for the system.

## Create File and Set Auditing Attributes
After file system auditing is enabled, the program will create a honey file. A user can specify a file to use as the content for the honey file. If no content file is specified, the program will generate content for the file.

Once the honey file is created, the program will set auditing attributes for the file such that any user of the _Domain Users_ group that attempts to access the file will create a windows security event with an ID of 4663.
  
## Monitoring and Alerting
After the users are created, the program goes into a monitoring phase. The program will:
  - Check for Windows Event ID 4663 (file accessed)
  - Check if file that was accessed is the file that is being monitored
  - Extract the username (domain\account) of the user who attempted to access the file
  - Send an alert via email that contains the file that was accessed and the user who attempted to access it
    - User provides email credentials at run time

## Requirements
- C# (written with .NET 5.0)
- Windows (Windows 10 used here)
- Run on Domain Controller (preferred for the current PoC)

## Usage
Load the solution in Visual Studio. Build the program using Visual Studio.

Run the program with the following command:

```
HoneyFiles.exe outFilename emailAddress password [inFilename]
```
Attempt to access the file that was created. Verify that an email alert was sent to the email address that was provided.

## Future Work
Allow for deploying throughout a Windows domain environment rather than on a single computer. Include automatically setting up Windows Event Forwarding (WEF) to consolidate the logs onto a single system.

Add logic for better variety in the files that are generated, even if a user provides a content file, so that it is less likely that an attacker can determine a file is a honey file before or after accessing it.

## Video Demo
https://screencast-o-matic.com/watch/crijeRV1SwE

