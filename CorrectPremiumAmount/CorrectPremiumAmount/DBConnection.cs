using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CorrectPremiumAmount
{
    public class DbConnection
    {
        private SqlConnection _conn = null;
        private string p;

        public DbConnection(string connString)
        {
            // TODO: Complete member initialization
            _conn = new SqlConnection();
            _conn.ConnectionString = connString;
        }

        public SqlConnection Connector {
            get { return _conn; }
        }

        public void CreateConnection(string connString)
        {
            using (_conn = new SqlConnection())
            {
                _conn.ConnectionString = connString;
            }
            
        }
        public SqlDataReader UpdateInsuredPremium(string SPName, string polNo, string apeNo)
        {
            SqlDataReader reader = null;
            try
            {
                using (SqlCommand cmd = new SqlCommand(SPName, _conn))
                {
                    cmd.CommandType = CommandType.StoredProcedure;

                    cmd.Parameters.Add("@PolicyNo", SqlDbType.VarChar).Value = polNo;
                    cmd.Parameters.Add("@AppEndosNo", SqlDbType.VarChar).Value = apeNo;
                    OpenConnection();
                    reader = cmd.ExecuteReader();
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                CloseConnection();
            }

            return reader;
        }

        public SqlDataReader ExecutrQueryReader(string query)
        {
            SqlDataReader reader=null;
            try
            {
                using (SqlCommand cmd = new SqlCommand(query, _conn))
                {

                    OpenConnection();
                    reader = cmd.ExecuteReader();
                }
            }
            catch (SqlException ex)
            {
                Console.Error.WriteLine(ex.Message);
                CloseConnection();
            }

            return reader;
        }

        public void OpenConnection()
        {
            _conn.Open();
        }

        public void CloseConnection()
        {
            _conn.Close();
        }
    }
}
