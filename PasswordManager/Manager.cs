using System;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Data.SQLite;
using System.Diagnostics;

namespace PasswordManager
{
    public partial class Manager : Form
    {
        private DBTools db = DBTools.Instance();
        private CryptTools crypt_tools = CryptTools.Instance();

        public Manager()
        {
            InitializeComponent();
        }

        public string Pass { get; set; }
        public string Hash { get; set; }
        private bool changes = false;

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ChangePassword passch = new ChangePassword();
            passch.Show();
        }
        public void add_row(string website, string login, string pass)
        {
            this.changes = true;
            Grid.Rows.Add(website, login, new String('*', pass.Length), pass, "Show");
        }

        private bool to_tray(bool end = false)
        {
            if(changes)
            {
                DialogResult result = MessageBox.Show("Do you want to save changes before exiting?", "Exit", MessageBoxButtons.YesNoCancel);
                if (result == DialogResult.Yes)
                {
                    save_data();                
                }
                else
                {
                    if (result == DialogResult.Cancel)
                    return true;                                     
                }
            }
            if(end)
            {
                Environment.Exit(0);
            }
            else
            {
                this.Dispose();
                this.Close();
                Login loginform = new Login();
                loginform.Show();
                loginform.set_tray();
            }
            return false;           
        }

        public void change_password(string pass)
        {
            this.Hash = crypt_tools.GetHash(pass);
            this.Pass = pass;
            changes = true;
        }

        private void save_data()
        {
            this.changes = false;
            Directory.CreateDirectory(db.get_folder_path());
            SQLiteConnection.CreateFile(db.get_path());
           
            string connString = string.Format("Data Source={0}", db.get_path());
            SQLiteConnection m_dbConnection = new SQLiteConnection(connString);
            m_dbConnection.Open();

        
            string t_query = "create table PASSWORDS_TABLE (site varchar(64), login varchar(64),pass varchar(64))";

            SQLiteCommand command = new SQLiteCommand(t_query, m_dbConnection);
            command.ExecuteNonQuery();

            string adr, login, pass;

            adr = "PASS_HASH";
            login = "SHA256";
            pass = this.Hash;

            t_query = string.Format("insert into PASSWORDS_TABLE (site, login,pass) values ('{0}', '{1}','{2}')", adr, login, pass);
            command = new SQLiteCommand(t_query, m_dbConnection);
            command.ExecuteNonQuery();

            foreach (DataGridViewRow row in Grid.Rows)
            {
                adr = crypt_tools.EncryptText(row.Cells[0].Value.ToString(), this.Pass);
                login = crypt_tools.EncryptText(row.Cells[1].Value.ToString(), this.Pass);
                pass = crypt_tools.EncryptText(row.Cells[3].Value.ToString(), this.Pass);

                t_query = string.Format("insert into PASSWORDS_TABLE (site, login,pass) values ('{0}', '{1}','{2}')", adr, login, pass);
                command = new SQLiteCommand(t_query, m_dbConnection);
                command.ExecuteNonQuery();
            }
            m_dbConnection.Close();
            GC.Collect();
        }

        private void Manager_Load(object sender, EventArgs e)
        {

            if (File.Exists(db.get_path()) && (new FileInfo(db.get_path()).Length > 0))
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
                            dr.Read(); //skipping password hash row

                            while(dr.Read())
                            {
                                String szPass = crypt_tools.DecryptText(dr.GetValue(2).ToString(), this.Pass);
                                Grid.Rows.Add(crypt_tools.DecryptText(dr.GetValue(0).ToString(), this.Pass), crypt_tools.DecryptText(dr.GetValue(1).ToString(), this.Pass), new String('*', szPass.Length),szPass, "Show");               
                            }  
                        }
                    }
                    conn.Close();
                    GC.Collect();
                }

            }
        }

        private void Manager_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (to_tray())
            {
                e.Cancel = true;
                return;
            }

        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("I don't feel so good Mr.Stark...", "About", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            to_tray(true);
        }

        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Addpass passform = new Addpass();
            passform.Show();
        }

        private void Grid_CellPainting(object sender, DataGridViewCellPaintingEventArgs e)
        {
            if (e.RowIndex < 0)
            return;  
            
            if (e.ColumnIndex == 5)
            {
                e.Paint(e.CellBounds, DataGridViewPaintParts.All);

                var w = 16;
                var h = 16;
                var x = e.CellBounds.Left + (e.CellBounds.Width - w) / 2;
                var y = e.CellBounds.Top + (e.CellBounds.Height - h) / 2;

                e.Graphics.DrawImage(Properties.Resources.remove, new Rectangle(x, y, w, h));
                e.Handled = true;             
            }
           
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you really want to save changes?", "Save", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                save_data();
                MessageBox.Show("Data Has Been Saved Successfully.", "Password Manager", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void Grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.ColumnIndex == 0)
            {
                string sUrl = Grid.Rows[e.RowIndex].Cells[0].Value.ToString();
                string prefix = "http://";
                string prefixS = "https://";
                if ((prefix.Length > sUrl.Length || (sUrl.Substring(0, prefix.Length) != prefix)) || (prefixS.Length > sUrl.Length || sUrl.Substring(0, prefixS.Length) != prefixS))
                {
                    sUrl = prefix + sUrl;
                }
                Process.Start(sUrl);
            }

            if (e.ColumnIndex == 4)
            {
                if (Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString() == "Show")
                {
                    Grid.Rows[e.RowIndex].Cells[2].Value = Grid.Rows[e.RowIndex].Cells[3].Value.ToString();
                    Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "Hide";
                }
                else
                {
                    Grid.Rows[e.RowIndex].Cells[2].Value = new String('*', Grid.Rows[e.RowIndex].Cells[3].Value.ToString().Length);
                    Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value = "Show";
                }

            }
            if (e.ColumnIndex == 5)
            {
                DialogResult result = MessageBox.Show("Do you want to delete current record?", "Delete Record", MessageBoxButtons.YesNo);
                if (result == DialogResult.Yes)
                {        
                    Grid.Rows.RemoveAt(e.RowIndex);
                    this.changes = true;
                }
            }
        }
        private void Grid_EditingControlShowing(object sender, DataGridViewEditingControlShowingEventArgs e)
        {
            if (Grid.CurrentCell.ColumnIndex == 2)
            {
                TextBox textBox = e.Control as TextBox;
                if (textBox != null)
                {
                    textBox.UseSystemPasswordChar = true;
                }
            }
        }

        private void Grid_CellDoubleClick(object sender, DataGridViewCellEventArgs e)
        {
            if ((e.ColumnIndex >= 0) && (e.ColumnIndex <= 2))
            {
                Clipboard.SetText(Grid.Rows[e.RowIndex].Cells[e.ColumnIndex].Value.ToString());
            }
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DialogResult result = MessageBox.Show("Do you want to delete all data?", "Delete Data", MessageBoxButtons.YesNo);
            if (result == DialogResult.Yes)
            {
                Grid.Rows.Clear();
                this.changes = true;
            }
        }

        private void helpToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MessageBox.Show("[File->New] - Create a new record" + Environment.NewLine +
                            "[File->Save] - Save current changes" + Environment.NewLine +
                            "[File->Clear] - Clear all data" + Environment.NewLine +
                            "[File->Change Password] - Change current password to new one" + Environment.NewLine +
                            "[File->Exit] - Exit application" + Environment.NewLine +
                            "[About] - Software Credentials" + Environment.NewLine +
                            "[Help] - Help Information Box" + Environment.NewLine +
                            "[Help] - Help Information Box" + Environment.NewLine+ Environment.NewLine +
                            "Grid Table Controls:" + Environment.NewLine +
                            "Click on button 'X' in the last column to delete the row" + Environment.NewLine +
                            "Double-click on cell to copy value to clipboard" + Environment.NewLine +
                            "Click on button 'Show' to display password", "Help", MessageBoxButtons.OK);
        }

        
    }
}
