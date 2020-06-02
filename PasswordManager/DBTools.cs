using System;
using System.Data.SQLite;
using System.IO;

namespace PasswordManager
{
    class DBTools
    {

        private static DBTools _instance;

        protected DBTools() { }

        public static DBTools Instance()
        {
            if (_instance == null)
            {
                _instance = new DBTools();
            }

            return _instance;
        }


        public string get_path()
        {
            string path;
            path = "%AppData%\\PasswordManager\\passwords.db";
            path = Environment.ExpandEnvironmentVariables(path);
            return path;
        }

        public  string get_folder_path()
        {
            string folder_path;
            folder_path = "%AppData%\\PasswordManager";
            folder_path = Environment.ExpandEnvironmentVariables(folder_path);
            return folder_path;
        }

        public  void insert(String adr, String login, String pass)
        {
            if (!File.Exists(get_path()))
            {
                Directory.CreateDirectory(get_folder_path());
                SQLiteConnection.CreateFile(get_path());
            }

            string connString = string.Format("Data Source={0}", get_path());
            SQLiteConnection m_dbConnection = new SQLiteConnection(connString);
            m_dbConnection.Open();

            if (!TableExists("PASSWORDS_TABLE", m_dbConnection))
            {
                string t_query = "create table PASSWORDS_TABLE (site varchar(64), login varchar(64),pass varchar(64))";

                SQLiteCommand command = new SQLiteCommand(t_query, m_dbConnection);
                command.ExecuteNonQuery();
            }

            string query = string.Format("insert into PASSWORDS_TABLE (site, login,pass) values ('{0}', '{1}','{2}')", adr, login, pass);

            SQLiteCommand cmd = new SQLiteCommand(query, m_dbConnection);
            cmd.ExecuteNonQuery();

            m_dbConnection.Close();
            GC.Collect();
        }

        public  bool TableExists(String tableName, SQLiteConnection connection)
        {
            using (SQLiteCommand cmd = new SQLiteCommand())
            {
                cmd.CommandType = System.Data.CommandType.Text;
                cmd.Connection = connection;
                cmd.CommandText = "SELECT * FROM sqlite_master WHERE type = 'table' AND name = @name";
                cmd.Parameters.AddWithValue("@name", tableName);

                using (SQLiteDataReader sqlDataReader = cmd.ExecuteReader())
                {
                    if (sqlDataReader.Read())
                        return true;
                    else
                        return false;
                }
            }
        }
    }
}
