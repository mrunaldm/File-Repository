///////////////////////////////////////////////////////////////////////
// Metadata.xaml.cs - Control GUI for Local Navigation        //
// ver 1.1                                                           //
// Mrunal, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based control GUI for Project4Demo.  It's 
 * responsibilities are to:
 * - Send request to server to obtain metadat of queried file
 *   
 * Required Files:
 * ---------------
 * Metadata.xaml, Metatdata.xaml.cs
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
    public partial class Metadata : UserControl
    {
        internal CsEndPoint navEndPoint_;
        public Metadata()
        {
            InitializeComponent();
        }

        private bool valiadteInputs()
        {
            if (metadata_namespace.Text == "")
            {
                validationMessage.Text = " Enter value for namespace ";
                return false;
            }
            if(metadata_file.Text == "")
            {
                validationMessage.Text = " Enter value for file name ";
                return false;
            }
            if(versionNumber.Text == "")
            {
                validationMessage.Text = "Enter value for version ";
                return false;
            }
            return true;
        }

        private void MetadataButton_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            if (!valiadteInputs())
                return;
            else
                validationMessage.Text = "";
            CsEndPoint serverEndPoint = new CsEndPoint();
            CsMessage msg = new CsMessage();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("package", metadata_namespace.Text);
            msg.add("fileName", metadata_file.Text);
            msg.add("version", versionNumber.Text);
            msg.add("command", "viewMetadata");
            win.translater.postMessage(msg);
        }
    }
}
