using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Broker
{
    static class UserCred
    {
        public static List<User> users;
        static UserCred()
        {
            users = new List<User>
            {
                new User{Username = "User1",Password="Pass@1"},
                new User{Username = "User2",Password="Pass@2"},
                new User{Username = "AuthUser",Password="User@111"},
            };
        }
    }

    class User
    {
        public string Username { get; set; }
        public string Password { get; set; }
    }
}
