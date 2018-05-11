///////////////////////////////////////////////////////////////////////
// CheckInControl.xaml.cs - Control GUI for Local Navigation        //
// ver 1.1                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based control GUI for Project4Demo.  It's 
 * responsibilities are to:
 * - Provide a display of directory contents of the local client.
 * - It provides a subdirectory list and a filelist for the selected directory.
 * - You can navigate into subdirectories by double-clicking on subdirectory
 *   or the parent directory, indicated by the name "..".
 *   
 * Required Files:
 * ---------------
 * CheckInControl.xaml, CheckInControl.xaml.cs
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
    /// <summary>
    /// Interaction logic for CheckInControl.xaml
    /// </summary>
    public partial class CheckInControl : UserControl
    {
        public CheckInControl()
        {
            InitializeComponent();
        }
        internal Stack<string> pathStack_ = new Stack<string>();
        internal string localStorageRoot_;
        internal CsEndPoint navEndPoint_;

        //Enable adding dependancy when dependancy checkbox is checked
        private void checkBoxDependancy_Checked(object sender, RoutedEventArgs e)
        {
            DependantfileNameValue.IsEnabled = true;
            DependantnameSpaceValue.IsEnabled = true;
            DependantfileNameValue.IsEnabled = true;
            dependantVersion.IsEnabled = true;
        }

        private void Refresh_Click(object sender, RoutedEventArgs e)
        {
            DirList.Items.Clear();
            string path = localStorageRoot_ + pathStack_.Peek();
            string[] dirs = System.IO.Directory.GetDirectories(path);
            foreach (string dir in dirs)
            {
                if (dir != "." && dir != "..")
                {
                    string itemDir = System.IO.Path.GetFileName(dir);
                    DirList.Items.Add(itemDir);
                }
            }
            DirList.Items.Insert(0, "..");

            FileList.Items.Clear();
            string[] files = System.IO.Directory.GetFiles(path);
            foreach (string file in files)
            {
                string itemFile = System.IO.Path.GetFileName(file);
                FileList.Items.Add(itemFile);
            }
        }

        internal void refreshDisplay()
        {
            Refresh_Click(this, null);
        }
        //----< strip off name of first part of path >---------------------

        public string removeFirstDir(string path)
        {
            string modifiedPath = path;
            int pos = path.IndexOf("/");
            modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
            return modifiedPath;
        }

        internal void clearDirs()
        {
            DirList.Items.Clear();
        }
        //----< function dispatched by child thread to main thread >-------

        internal void addDir(string dir)
        {
            DirList.Items.Add(dir);
        }
        //----< function dispatched by child thread to main thread >-------

        internal void insertParent()
        {
            DirList.Items.Insert(0, "..");
        }
        //----< function dispatched by child thread to main thread >-------

        internal void clearFiles()
        {
            FileList.Items.Clear();
        }
        //----< function dispatched by child thread to main thread >-------

        internal void addFile(string file)
        {
            FileList.Items.Add(file);
        }

        //----< respond to mouse double-click on dir name >----------------

        private void DirList_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            string selectedDir = (string)DirList.SelectedItem;
            string path;
            if (selectedDir == "..")
            {
                if (pathStack_.Count > 1)
                    pathStack_.Pop();
                else
                    return;
            }
            else
            {
                path = pathStack_.Peek() + "/" + selectedDir;
                pathStack_.Push(path);
            }
           
            PathTextBlock.Text = "LocalStorage" + pathStack_.Peek();
            refreshDisplay();
    }
        // Handler for checkin
        private void CheckIn_Click(object sender, RoutedEventArgs e)
        {
            MainWindow win = (MainWindow)Window.GetWindow(this);
            if (!validateCheckInParams())
                return;
            CsEndPoint serverEndPoint = new CsEndPoint();
            string fileName = (string)FileList.SelectedItem;
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            string checkInStatus = ((ComboBoxItem)checkInStatusSelected.SelectedItem).Content.ToString();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(navEndPoint_));
            msg.add("nameSpace", nameSpaceValue.Text);
            msg.add("category", CategoryValue.Text);
            msg.add("fileName", fileName);
            msg.add("sendingFile", fileName);
            if(!System.IO.File.Exists(win.sendFilesPath+"/"+fileName))
                System.IO.File.Copy(localStorageRoot_ + "/" + fileName, win.sendFilesPath + "/" + fileName);
            msg.add("status", checkInStatus);
            msg.add("description", descriptionValue.Text);
            if(checkBoxDependancy.IsChecked == true)
            {
                msg.add("depFlag", "yes");
                msg.add("dependantNameSpace", DependantnameSpaceValue.Text);
                msg.add("dependantFileName", DependantfileNameValue.Text);
                msg.add("dependantVersion", dependantVersion.Text);
            }
            msg.add("command", "checkin");
            win.translater.postMessage(msg);
        }

        //Validate parameters in UI
        private bool validateCheckInParams()
        {
            if(nameSpaceValue.Text == "")
            {
                validationMessage.Text = "Enter a value for NameSpace";
                return false;
            }
            if(descriptionValue.Text == "")
            {
                validationMessage.Text = "Enter description";
                return false;
            }
            if(CategoryValue.Text == "")
            {
                validationMessage.Text = "Enter category";
                return false;
            }
            if(DependantfileNameValue.IsEnabled && DependantfileNameValue.IsEnabled && dependantVersion.IsEnabled)
            {
                if(DependantnameSpaceValue.Text=="" || DependantfileNameValue.Text == "" || dependantVersion.Text == "")
                {
                    DependantfileNameValue.IsEnabled = false;
                    DependantnameSpaceValue.IsEnabled = false;
                    dependantVersion.IsEnabled = false;
                    checkBoxDependancy.IsChecked = false;
                    validationMessage.Text = "Enter correct dependancy values";
                    return false;
                }
            }
            if(FileList.SelectedItem == null)
            {
                validationMessage.Text = "Select a file from local storage to check in";
                return false;
            }
            return true;
        }
    }
}
