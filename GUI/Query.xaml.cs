///////////////////////////////////////////////////////////////////////
// Query.xaml.cs - Control GUI for Local Navigation        //
// ver 1.1                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based control GUI for Project4Demo.  It's 
 * responsibilities are to:
 * - Send queries to the repository based on user input
 *   
 * Required Files:
 * ---------------
 * Query.xaml, Query.xaml.cs
 * 
 * Maintenance History:
 * --------------------
 * ver 1.0 : 01 May 2018
 * - first release
 * 
 */
using MsgPassingCommunication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp1
{
    public partial class Query : UserControl
    {
        internal CsEndPoint navEndPoint_;
        public Query()
        {
            InitializeComponent();
        }

        private void SendQuery_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            CsEndPoint serverEndPoint = new CsEndPoint();
            CsMessage msg = new CsMessage();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            if (querySelector.SelectedIndex == 0)
                msg.add("command", "queryByName");
            else if (querySelector.SelectedIndex == 1)
                msg.add("command", "queryByCategory");
            else if (querySelector.SelectedIndex == 2)
                msg.add("command", "queryByDependants");
            else
                msg.add("command", "queryByVersion");
            msg.add("queryParam", (string)inputParam.Text);
            win.translater.postMessage(msg);
        }
    }
}
