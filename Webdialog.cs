using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LianjiaWebWorm
{
    public partial class Webdialog : Form
    {
        public Webdialog()
        {
            InitializeComponent();
            webBrowser1.Url = new Uri("www.lianjia.com");
        }
    }
}
