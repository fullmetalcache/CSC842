///////////////////////////////////////////////////////
// File: AccountManagement.cs
// Author: Brian Fehrman
// Date: 2021-06-21
// Description: 
//		Class for creating domain users based on a provided
//		list of usernames, creating random passwords for the
//		users, and then logging in as each of the users.
///////////////////////////////////////////////////////

using System;
using System.Collections.Generic;
using System.DirectoryServices.AccountManagement;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Security.Cryptography;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;
using System.Threading;

namespace HoneyAccounts
{
    class AccountManagement
    {
        // Constructor
        public AccountManagement()
        {

        }
		
		// Needed so we can use the LogonUser Windows API call
        [DllImport("advapi32.dll")]
        private static extern bool LogonUser(string name, string domain, string pass, int logType, int logpv, out IntPtr pht);

        public void AddUsers(List<String> users, int minPasswordLength)
        {
            Random rnd = new Random();

            //https://stackoverflow.com/questions/2305857/creating-active-directory-user-with-password-in-c-sharp
            // Get the context for the current domain
            using (var context = new PrincipalContext(ContextType.Domain))
            {
                // Loop through each user in the provided list and add them to the domain
                foreach (string username in users)
                {
					// Search for any existing users that match the 
					// was provided username that
                    UserPrincipal userTemp = UserPrincipal.FindByIdentity(context, username);

					// Delete the user if it already exists. This is
					// so we don't need to know the previous password
					// of the user
                    if (userTemp != null)
                    {
                        userTemp.Delete();
                    }

                    try
                    {
                        using (var userObj = new UserPrincipal(context))
                        {
							// Create the basic user attributes
                            userObj.SamAccountName = username;
                            userObj.EmailAddress = String.Format("{0}@{1}", username, Environment.UserDomainName);
                            Console.WriteLine(userObj.EmailAddress);
							
                            // No need to save the password, it will literally never be used
                            // after this function. When we update the password, we will just overwrite
                            // it with the new one without needing to know the old one.
                            string password = genPassword(minPasswordLength);
                            userObj.SetPassword( password );
                            userObj.Enabled = true;
                            userObj.Save();
                            context.ValidateCredentials(username, password);


                            // Log the user on so it doesn't look suspicious
                            IntPtr ptr;
                            bool retVal = LogonUser(username, "cooldom.bro", password, 3, 0, out ptr);

                            if(!retVal)
                            {
                                Console.WriteLine("Something happened..");
                            }
                        }
                    }
                    catch
                    {
                        Console.WriteLine(String.Format("Could not add user: {0}", username));
                    }

                    // Wait a random amount of time before creating next account
                    // to avoid some suspicion. In practice, this would be longer
                    int waitTime = rnd.Next(1000, 5000);
                    Thread.Sleep(waitTime);
                }
            }
        }

        //https://iq.direct/blog/329-source-code-csharp-true-random-password-generator.html
        private static string genPassword(int minLength)
        {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();

            // Character sets from which we want to choose
            string capitalLetters = "ABCDEFGHIJKLMNOPQRSTUVWXYZ";
            string lowerLetters = "abcdefghijklmnopqrstuvwxyz";
            string digits = "0123456789";
            string specialCharacters = "!@#$%^&*()-_=+<,>.";

            // Form the full set for use by the RNG
            string fullSet = capitalLetters + lowerLetters + digits + specialCharacters;

            // Track which types of characters we have chosen to make sure we get one
            // from each set
            bool capLetterChosen = false;
            bool lowLetterChosen = false;
            bool digChosen = false;
            bool specCharChosen = false;

            string newPassword = "";

            // Loop through until we reach the minimum length AND we haven't chosen
            // at least one character from each set
            while (newPassword.Length < minLength || !capLetterChosen || !lowLetterChosen || !digChosen || !specCharChosen)
            {
                // This is a little weird but it works...
                // basically, grab a random character and
                // see if it is in our full character set
                byte[] tempByte = new byte[1];
                char currChar;
                do
                {
                    provider.GetBytes(tempByte);
                    currChar = (char)tempByte[0];

                } while (!fullSet.Any(x => x == currChar));
                
                // Check to which set this character belongs
                // and mark it off as chosen
                if( capitalLetters.Contains(currChar) )
                {
                    capLetterChosen = true;
                }
                else if ( lowerLetters.Contains(currChar))
                {
                    lowLetterChosen = true;
                }
                else if (digits.Contains(currChar))
                {
                    digChosen = true;
                }
                else
                {
                    specCharChosen = true;
                }

                // Windows has a max length of 256. If we are getting close to that length
                // and haven't yet chosen at least one character from each set, don't append
                // the current character onto the password and try again to get a character
                // from the unchosen set(s). I ran this multiple times with a min password length
                // of 40 and found that, in each case, it selected one character from each set
                // by the time it got to 40 characters. The passwords appeared to be very
                // random each time.
                if(newPassword.Length > 250 && (!capLetterChosen || !lowLetterChosen || !digChosen || !specCharChosen))
                {
                    continue;
                }
                else
                {
                    // Otherwise, append the current character onto our password
                    newPassword += currChar;
                }
            }

            return newPassword;
        }
    }
}
