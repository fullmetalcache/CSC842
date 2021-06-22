# HoneyAccounts

C# Program to catch logon attacks, such as password spraying, within a Windows Active Directory environment.

## User Creation
Allow user to provide a list of usernames. The program then performs the following operations for each username:

- Create a domain account
- Create an email address of the form <username>@<current_domain>
- Create a long, random password
  - Default is a minimum of 40 characters and at least one numeric, one uppercase, one lowercase, and one special character
  - Does not store password on disk
- Logs in as newly-created user
  - Needed so that the lastLogon attribute does not show "Never", which is suspicious
  
## Monitoring and Alerting
After the users are created, the program goes into a monitoring phase. The program will:
  - Check for Windows Event ID 4625 (failed logon attempt)
  - Check if the failed logon attempt was for a user in the list of users that were created in the previous step
  - Generate an alert that shows which honey account was being attacked and the system from which it was attacked
    - Currently outputs to console for PoC but can be extended to other alerting (emailing, custom event ID, etc.)

## Requirements
- C# (written with .NET 5.0)
- System.Diagnostics.EventLog (NuGet Package)
- System.DirectoryServices (NuGet Package)
- System.DirectoryServices.AccountManagement (NuGet Package)
- Windows (Windows 10 used here)
- Domain Administrator rights

## Usage
Load the solution in Visual Studio, install the Nuget Packages and .NET 5.0 if needed. Build the program using Visual Studio.

Run the program with the following command:

```
HoneyAccounts.exe userlist.txt
```
Watch for events after the list of users is created.

## Future Work
Add better alerting, such as email alerting or custom event IDs.

Add logic for spacing out the creation of the accounts and when the accounts login, as that could look suspicious if they are too close together.

Add additional information to the alert that is generated, such as the time that it was generated and other useful data points.

## Video Demo
https://screencast-o-matic.com/watch/cr13V4V1YAI

