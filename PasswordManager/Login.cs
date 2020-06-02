using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Security.Cryptography;

namespace PasswordManager
{
    
    public partial class Login : Form
    {
        private DBTools db = DBTools.Instance();
        private CryptTools crypt_tools = CryptTools.Instance();
        public Login()
        {
            InitializeComponent();
        }

        private void Login_Load(object sender, EventArgs e)
        {
            label2.Text = (File.Exists(db.get_path()) && (new FileInfo(db.get_path()).Length > 0)) ? 
                         "Enter Password" : "Create Master Password";
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Passowrd Field Cannot Be Empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                textBox1.Text = "";
                return;
            }

            if(File.Exists(db.get_path()) && (new FileInfo(db.get_path()).Length > 0))
            {
                string connString = string.Format("Data Source={0}", db.get_path());

                using (SQLiteConnection conn = new SQLiteConnection(connString))
                {
                    StringBuilder query = new StringBuilder();
                    query.Append("SELECT * ");
                    query.Append("FROM PASSWORDS_TABLE ");

                    conn.Open();
                    if (!db.TableExists("PASSWORDS_TABLE", conn))
                    {
                        conn.Close();
                        GC.Collect();
                        MessageBox.Show("Cannot find passwords table from database. The database file may be empty or corrupt.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;             
                    }

                    using (SQLiteCommand cmd = new SQLiteCommand(query.ToString(), conn))
                    {
    
                        using (SQLiteDataReader dr = cmd.ExecuteReader())
                        {
                            dr.Read();
                            if (!crypt_tools.VerifyHash(textBox1.Text, dr.GetValue(2).ToString()))
                            {
                                conn.Close();
                                GC.Collect();
                                MessageBox.Show("Incorrect Password.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                                textBox1.Text = "";
                                return;                          
                            }
                        }
                    }
                    conn.Close();
                    GC.Collect();
                }

            }
            else
            {
                db.insert("PASS_HASH", "SHA256", crypt_tools.GetHash(textBox1.Text));
            }

            this.Hide();
            Manager mgr = new Manager();
            mgr.Pass = textBox1.Text;
            mgr.Hash = crypt_tools.GetHash(textBox1.Text);
            mgr.Show();
        }

        public void set_tray(bool set = true)
        {
            if(set)
            {
                this.WindowState = FormWindowState.Minimized;
                notifyIcon1.Icon = Properties.Resources.icon;           
                this.ShowInTaskbar = false;
                notifyIcon1.Visible = true;
              
            }
            else
            {
                this.WindowState = FormWindowState.Normal;
                this.ShowInTaskbar = true;
                notifyIcon1.Visible = false;
            }

        }

        private void Login_Resize(object sender, EventArgs e)
        {
            if(FormWindowState.Minimized == this.WindowState)
            {
                set_tray();
            }
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            set_tray(false);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            set_tray(false);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void Login_FormClosing(object sender, FormClosingEventArgs e)
        {
            Environment.Exit(0);
        }
    }
}
