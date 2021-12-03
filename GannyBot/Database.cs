using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Windows.Forms;
using System.Data.SqlClient;

using Nethereum.Util;

namespace GannyBot
{
    public class Database
    {
        SqlConnection connection;

        public void Initialize()
        {
            string databaseFileName = Path.GetDirectoryName(Application.ExecutablePath) + "\\GannyBot.mdf";
            Console.WriteLine(databaseFileName);

            connection = new SqlConnection();
            connection.ConnectionString = @"Data Source=(LocalDB)\MSSQLLocalDB;" +
                          "AttachDbFilename=|DataDirectory|\\GannyBot.mdf;" +
                          "Integrated Security=True;" +
                          "MultipleActiveResultSets=True;" +
                          "Connect Timeout=30;";
            try
            {
                connection.Open();
                MessageBox.Show("Connection opened");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            
            

            //string queryString = "SELECT * FROM dbo.Token";

            //SqlDataAdapter adapter = new SqlDataAdapter();
            //SqlCommand command = new SqlCommand(queryString, connection);


            //using (SqlDataReader oReader = command.ExecuteReader())
            //{
            //    while (oReader.Read())
            //    {
            //        Console.WriteLine(oReader["Name"].ToString());
            //    }
            //}

            //command.ExecuteNonQuery();

            //connection.Close();
        }

        public void AddWalletToken(Token token)
        {
            string queryString = "INSERT INTO dbo.Token (name,symbol,address,decimals,approved)" +
                $"VALUES ('{token.Name}', '{token.Symbol}', '{token.Address}', '{token.Decimals}', '{token.Approved}')";

            SqlCommand command = new SqlCommand(queryString, connection);
            Console.WriteLine(command.ExecuteNonQuery());
        }

        public List<Token> GetWalletTokenList()
        {
            List<Token> tokens = new List<Token>();

            string queryString = "SELECT * FROM dbo.Token";
            SqlCommand command = new SqlCommand(queryString, connection);

            using (SqlDataReader oReader = command.ExecuteReader())
            {
                while (oReader.Read())
                {
                    Token token = new Token();
                    token.Name = oReader["name"].ToString();
                    token.Symbol = oReader["symbol"].ToString();
                    token.Address = oReader["address"].ToString();
                    token.Decimals = (int)oReader["decimals"];
                    token.Approved = (BigDecimal)oReader["approved"];

                    tokens.Add(token);
                }
            }

            return tokens;
        }
    }
}
