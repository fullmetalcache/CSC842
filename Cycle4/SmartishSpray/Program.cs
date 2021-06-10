namespace SmartishSpray
{
    public class ClsUsers
    {
        public string AccountName { get; set; }
        public string DisplayName { get; set; }
    }

    class Program
    {
        static void Main(string[] args)
        {
            SmartishSprayer sprayer = new SmartishSprayer();

            sprayer.GetLDAPPath();
            sprayer.GetADUsers();
            sprayer.BuildPasswordLists();
            sprayer.SprayUsers();
        }
    }
}
