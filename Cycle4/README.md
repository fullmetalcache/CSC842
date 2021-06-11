# SmartishSpray

C# Program to perform "smartish" password spraying against users in a target Active Directory (AD) domain.

Allows user to select a target domain based upon the current AD forest and then select a Domain Controller to target. The program determines which Domain Controller is the Primary Domain Controller (PDC) for the selected domain, as this is typically the one to use since it certainly has the AD role.

The program then gets a list of user accounts in the selected domain and applicable attributes. The attributes are:

- samAccountUsername
- pwdLastSet
- badPwdCount

For each user account, the program forms a list of targeted passwords based upon when the user last set their password (pwdLastSet). Currently, the program creates two passwords for each user and are of the form:

- \<SeasonSet\>\<YearSet\>
- \<MonthSet\>\<YearSet\>

In the case of \<MonthSet\>\<YearSet\>, if the password that is formed is less than 8 characters then the program will add an exclamation point. This was chosen because, in my experience, most organizations have at least an 8-character minimum.

The program then tries the targeted list for each user and saves any credentials that it found to be valid. The program ensures that the number of current, invalid attempts for each user (badPwdCount) does not exceed 2 since the lockout for most organizations is at least 3 or more.

## Requirements
- C# (written with .NET 5.0)
- System.DirectoryServices (NuGet Package)
- System.DirectoryServices.AccountManagement (NuGet Package)
- Windows (Windows 10 used here)

## Usage
Load the solution in Visual Studio, install the Nuget Packages and .NET 5.0 if needed. Build the program using Visual Studio.

Run the program with the following command:

```
SmartishSpray.exe
```

Select the target Domain from the provided list

Select the target Domain Controller from the provided list

Watch the spraying happen!

## Are there other tools that do this?
There are multiple spraying tools out there but I have not seen any that take an automated approach to forming targeted passwords while keeping lockout policies in mind.

## Future Work
Add more password formats.

Inspect the password policy for the domain and fine grained policies.

Loop over more passwords with a wait time in between and check the badPwdCount for each user to ensure users are not locked out.

## Video Demo

