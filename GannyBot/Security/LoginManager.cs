using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GannyBot.Security
{
    internal static class LoginManager
    {
        public static dynamic Login(string mail, string password)
        {
            UI.UIManager.clientSocket.SendData("{'type':'login', 'email':'" + mail + "', 'password':'" + password + "'}");
            dynamic receiveData = UI.UIManager.clientSocket.ReceiveData();

            return receiveData;
        }
    }
}
