using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SQLite;
using System.IO;

using Newtonsoft.Json;
using System.Windows.Forms;

namespace GannyBot.Database
{
    internal class DatabaseManager
    {
        SQLiteConnection con;
        SQLiteDataAdapter dataAdapter;
        SQLiteCommand cmd;
        
        
        public DatabaseManager()
        {
            if (Exist())
            {
                try
                {
                    con = new SQLiteConnection("Data Source=" + AppDomain.CurrentDomain.BaseDirectory + "/GannyBot.db;Version=3;");
                }
                catch(Exception ex)
                {
                    MessageBox.Show("Database Error: " + ex.Message);
                }
                
            }
        }

        public bool Exist()
        {
            if (!File.Exists(AppDomain.CurrentDomain.BaseDirectory + "/GannyBot.db"))
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        public dynamic DataTableToJSON(DataSet dataSet)
        {
            string JSONString = string.Empty;
            JSONString = JsonConvert.SerializeObject(dataSet);
            dynamic JSONData = JsonConvert.DeserializeObject(JSONString);

            return JSONData;
        }

        public dynamic GetAccount()
        {
            dataAdapter = new SQLiteDataAdapter("SELECT * FROM account", con);
            DataSet dataSet = new DataSet();

            con.Open();
            dataAdapter.Fill(dataSet, "account");
            con.Close();

            dynamic JSONData = DataTableToJSON(dataSet);

            return JSONData.account;
        }

        public void AddAccount(string address, string key)
        {
            cmd = new SQLiteCommand();
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "INSERT INTO account (address, key) VALUES (@address, @key)";
            cmd.Parameters.AddWithValue("address", address);
            cmd.Parameters.AddWithValue("key", key);
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void DeleteAccount()
        {
            cmd = new SQLiteCommand();
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "DELETE FROM account";
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void AddWalletToken(Chain.Token token)
        {
            cmd = new SQLiteCommand();
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "INSERT INTO tokens (name, symbol, address, decimals, abi) VALUES (@name, @symbol, @address, @decimals, @abi)";
            cmd.Parameters.AddWithValue("name", token.Name);
            cmd.Parameters.AddWithValue("symbol", token.Symbol);
            cmd.Parameters.AddWithValue("address", token.Address);
            cmd.Parameters.AddWithValue("decimals", token.Decimals);
            cmd.Parameters.AddWithValue("abi", token.Abi);
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public dynamic GetWalletTokens()
        {
            dataAdapter = new SQLiteDataAdapter("SELECT * FROM tokens", con);
            DataSet dataSet = new DataSet();

            con.Open();
            dataAdapter.Fill(dataSet, "tokens");
            con.Close();

            dynamic JSONData = DataTableToJSON(dataSet);

            return JSONData.tokens;
        }

        public void UpdateWalletTokens(Chain.Token token)
        {
            cmd = new SQLiteCommand();
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "UPDATE tokens SET name=@name, symbol=@symbol, decimals=@decimals, abi=@abi WHERE address=@address";
            cmd.Parameters.AddWithValue("name", token.Name);
            cmd.Parameters.AddWithValue("symbol", token.Symbol);
            cmd.Parameters.AddWithValue("address", token.Address);
            cmd.Parameters.AddWithValue("decimals", token.Decimals);
            cmd.Parameters.AddWithValue("abi", token.Abi);
            cmd.ExecuteNonQuery();
            con.Close();
        }

        public void RemoveWalletTokens(string address)
        {
            cmd = new SQLiteCommand();
            con.Open();
            cmd.Connection = con;
            cmd.CommandText = "DELETE FROM tokens WHERE address=@address";
            cmd.Parameters.AddWithValue("address", address);
            cmd.ExecuteNonQuery();
            con.Close();
        }
    }
}
