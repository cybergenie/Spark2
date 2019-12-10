using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Controls;

namespace Spark2
{
    public class BasePage : Page
    {
        private MainWindow _parentWin;
        public MainWindow ParentWindow
        {
            get { return _parentWin; }
            set { _parentWin = value; }
        }
    }
}
