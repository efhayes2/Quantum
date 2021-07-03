using System;
using System.IO;
using Microsoft.Data.Sqlite;
using System.Reflection;
using Connection;

namespace SqliteTester
{
    internal class Program
    {
        private static readonly string ExecutableLocation = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
        private static readonly string ConnectionPath = Path.Combine(ExecutableLocation, @"\data\db\ghk.db");
        //public static string ConnectionString { get; } = @"Data Source = " + ConnectionPath;
        public static string ConnectionString { get; } = @"Data Source = file:C:\sqlite\db\ghk.db";
        


        private static void Main(string[] args)
        {
            var temp = new PostgreSqlConnection();
            
            var sqliteConn = new QSqliteConnection();
            const string query = "Select DISTINCT ticker from stock_prices";
            var dt = sqliteConn.ExecuteQueryCommand(query);

            const string query2 = "Select date, ticker, price from stock_prices order by date desc";
            dt = sqliteConn.ExecuteQueryCommand(query2);

        }


        private static SqliteConnection CreateConnection()
        {
            // Create a new database connection:
            // var sqliteConn = new SqliteConnection("Data Source=database.db");
            var sqliteConn = new SqliteConnection(ConnectionString);
            
            // Open the connection:
            try
            {
                sqliteConn.Open();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

            return sqliteConn;
        }

        private static void CreateTable(SqliteConnection conn)
        {
            const string createSql = "CREATE TABLE SampleTable(Col1 VARCHAR(20), Col2 INT)";
            const string createSql1 = "CREATE TABLE SampleTable1(Col1 VARCHAR(20), Col2 INT)";
            var sqliteCmd = conn.CreateCommand();
            sqliteCmd.CommandText = createSql;
            sqliteCmd.ExecuteNonQuery();
            sqliteCmd.CommandText = createSql1;
            sqliteCmd.ExecuteNonQuery();
        }

        private static void InsertData(SqliteConnection conn)
        {
            var sqliteCmd = conn.CreateCommand();
            sqliteCmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test Text ', 1); ";
            sqliteCmd.ExecuteNonQuery();
            sqliteCmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test1 Text1 ', 2); ";
            sqliteCmd.ExecuteNonQuery();
            sqliteCmd.CommandText = "INSERT INTO SampleTable(Col1, Col2) VALUES('Test2 Text2 ', 3); ";
            sqliteCmd.ExecuteNonQuery();
            sqliteCmd.CommandText = "INSERT INTO SampleTable1(Col1, Col2) VALUES('Test3 Text3 ', 3); ";
            sqliteCmd.ExecuteNonQuery();
        }

        private static void ReadData(SqliteConnection conn)
        {
            var sqliteCmd = conn.CreateCommand();
            sqliteCmd.CommandText = "SELECT date, ticker, price FROM stock_prices";

            // sqliteCmd.CommandText = @"SELECT name FROM sqlite_master  WHERE type = 'table' ORDER BY 1";
            var reader = sqliteCmd.ExecuteReader();
            while (reader.Read())
            {
                var date = (string) reader["date"];
                var ticker = (string) reader["ticker"];
                var price = (double) reader["price"];
                
                Console.WriteLine(date + "," + ticker + "," + price + ",");
            }
            conn.Close();
        }
    }
}