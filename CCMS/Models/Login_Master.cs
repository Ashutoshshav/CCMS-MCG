using System.ComponentModel.DataAnnotations;

namespace CCMS.Models
{
    public class Login_Master
    {
        [Key]
        public string User_Name { get; set; }
        public string Password { get; set; }

        public override string ToString()
        {
            return $"User_Name: {User_Name}, Password: {Password}";
        }
    }
}
