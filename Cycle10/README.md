# MultiLogon Monitor

C# Program to catch suspicious logons within a Windows Active Directory environment.
  
## Monitoring and Alerting
When a threat-actor compromises credentials on an internal network, they will sometimes use them to open multiple sessions in the environment. Most users will not have more than 1 or 2 sessions open. This program will:
- Track logon events for users (Security Event 4624)
- Track logoff events for users (Security Event 4634)
- Use logonID to correlate logons with logoffs
- Generate an alert if the account logs on to more the X number of unique systemns (currently set to 1 for PoC purposes)
  - Sends an alert via email using the credentials provided
  - Alert includes potentially-compromised user account and the systems that the user logged onto.
- The program accepts a list of users to ignore

## Requirements
- C# (written with .NET 5.0)
- Windows (Windows 10 used here)
- Run on Domain Controller (preferred for the current PoC)

## Usage
Load the solution in Visual Studio. Build the program using Visual Studio.

Run the program with the following command:

```
MultiLogonDetect.exe emailAddress password [ignoreFileName]
```
Use an account to log on to more than one system and observe that an alert is generated.

## Future Work
Fix some of the lingering issues with strange behavior related to logon/logoff events being generated. Allow for monitoring only specific systems in the environment. Further narrow down suspicious behavior based upon logon types (e.g., interactive) or possible logon hours.

## Video Demo
https://screencast-o-matic.com/watch/criqrxVit2l

