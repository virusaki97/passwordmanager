using System;
using System.Linq;
using System.Windows.Forms;

namespace PasswordManager
{
    public partial class ChangePassword : Form
    {
        public ChangePassword()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox1.Text))
            {
                MessageBox.Show("Passowrd Field Cannot Be Empty", "Error", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            var mgrForm = Application.OpenForms.OfType<Manager>().Single();
            mgrForm.change_password(textBox1.Text.ToString());
            this.Hide();
        }

        private void ChangePassword_Load(object sender, EventArgs e)
        {

        }
    }
}
