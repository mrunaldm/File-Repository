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
    /// <summary>
    /// Interaction logic for ConnectionControl.xaml
    /// </summary>
    public partial class ConnectionControl : UserControl
    {
        internal CsEndPoint navEndPoint_;
        public ConnectionControl()
        {
            InitializeComponent();
        }

        internal void setupConnect()
        {
            portName.Text = "8080";
            ipAddress.Text = "localhost";
        }

        // function for connection handling in UI
        private void connection_click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            CsEndPoint serverEndPoint = new CsEndPoint();
            CsMessage msg = new CsMessage();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            connectMessage.Text = "Attempting to establish connection";
            serverEndPoint.port = Convert.ToInt32((string)portName.Text);
            serverEndPoint.machineAddress = ipAddress.Text;
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("command", "connect");
            win.translater.postMessage(msg);
        }
    }
}
