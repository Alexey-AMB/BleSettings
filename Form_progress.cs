using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace BleSettings
{
    public partial class Form_progress : Form
    {
        public delegate void HaveStop();
        public static event HaveStop MyStop;
        public Form_progress(int iMaxProgress)
        {
            InitializeComponent();
            this.progressBar1.Maximum = iMaxProgress;
        }

        public void AddProgressValue()
        {
            this.progressBar1.Value++;
        }

        private void Button1_Click(object sender, EventArgs e)
        {
            MyStop();
        }
    }
}
