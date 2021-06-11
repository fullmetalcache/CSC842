//////////////////////////////////
// File: Program.cs
// Author: Brian Fehrman
// Date: 2020-06-10
// Description: Main entry point for the SmartishSpray program
//////////////////////////////////

namespace SmartishSpray
{
    class Program
    {
        static void Main(string[] args)
        {
            // Instantiate class
            SmartishSprayer sprayer = new SmartishSprayer();

            // Form the LDAP path based on user selection for domain and domain controller
            sprayer.GetLDAPPath();

            // Get a list of AD Users and applicable attributes
            sprayer.GetADUsers();

            // Build list of passwords for each user
            sprayer.BuildPasswordLists();

            // Spray passwords against each user
            sprayer.SprayUsers();
        }
    }
}
