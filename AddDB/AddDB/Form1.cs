using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.SqlServer.Management.Smo;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Diagnostics;

namespace AddDB
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            populateListViewPermissions();
            populateDatabases();
        }

        private void populateDatabases()
        {
            var serverName = ".";
            Server svr = new Server(serverName);
            foreach (Database db in svr.Databases) //delete all databases with entered name
            {
                if(db.Name.ToLower() != "master") //do not include master db
                {
                    comboBoxDatabases.Items.Add(db.Name);
                }
            }
        }

        private void populateListViewPermissions()
        {
            checkedListBoxPermissions.Items.Add("db_owner");
            checkedListBoxPermissions.Items.Add("db_accessadmin");
            checkedListBoxPermissions.Items.Add("db_backupoperator");
            checkedListBoxPermissions.Items.Add("db_datareader");
            checkedListBoxPermissions.Items.Add("db_datawriter");
            checkedListBoxPermissions.Items.Add("db_ddladmin");
            checkedListBoxPermissions.Items.Add("db_securityadmin");
        }

        private void button1_Click(object sender, EventArgs e) //add db
        {
            var dbName = textBoxDB.Text;
            string userName = textBoxUser.Text;
            var serverName = "."; // SQL Server Instance name

            Server svr = new Server(serverName);
            var db = new Database(svr, dbName);
            db.Create();

            if (db != null)
            {
                // In case I need to add a login to sql
                //Login login = new Login(svr, "beacon");
                //login.DefaultDatabase = "master"; // Logins typically have master as default database
                //login.LoginType = LoginType.SqlLogin;
                //login.Create("foobar", LoginCreateOptions.None); // Enter a suitable password
                //login.Enable();

                if(userName != "")
                {
                    User user = new User(db, userName);
                    user.UserType = UserType.SqlLogin;
                    user.Login = userName;
                    user.Create();

                    List<object> listOfPermissions = checkedListBoxPermissions.CheckedItems.OfType<object>().ToList();
                    foreach(var p in listOfPermissions)
                    {
                        user.AddToRole(p.ToString());
                    }
                }
              
            }
            comboBoxDatabases.Items.Clear();
            populateDatabases();
        }

        private void button2_Click(object sender, EventArgs e) //delete db
        {
            var serverName = "."; 
            Server svr = new Server(serverName);
            var dbToDrop = comboBoxDatabases.SelectedItem.ToString();//textBoxDBToDrop.Text;
            List<Database> databasesToDelete = new List<Database>();
            foreach (Database db in svr.Databases) //delete all databases with entered name
            {
                if (db.Name == dbToDrop)
                {
                    databasesToDelete.Add(db);
                }
            }
            databasesToDelete.ForEach(x =>
            {
                if (x.ActiveConnections > 0) //close all connections and drop db
                {
                    string connectionString = svr.ConnectionContext.ToString();

                    System.Data.SqlClient.SqlConnectionStringBuilder builder = new System.Data.SqlClient.SqlConnectionStringBuilder();
                    builder["Data Source"] = ".";
                    builder["integrated Security"] = true;
                    builder["Initial Catalog"] = dbToDrop;

                    SqlConnection sqlConn = new SqlConnection(builder.ToString());
                    Microsoft.SqlServer.Management.Common.ServerConnection serverConn = new Microsoft.SqlServer.Management.Common.ServerConnection(sqlConn);
                    Server svrSql = new Server(serverConn);

                    sqlConn.Open();
                    String sqlCOmmandText = @"
                             USE master
                             ALTER DATABASE " + dbToDrop + @" SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                             DROP DATABASE [" + dbToDrop + "]";

                    SqlCommand sqlCommand = new SqlCommand(sqlCOmmandText, sqlConn);
                    sqlCommand.ExecuteNonQuery();
                    sqlConn.Close();
                }
                else
                {
                    x.Drop();
                }                         
            });

            comboBoxDatabases.Items.Clear();
            populateDatabases();
        }

    }
}
