///////////////////////////////////////////////////////////////////////
// MainWindow.xaml.cs - GUI for Project3HelpWPF                      //
// ver 2.0                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
 * Package Operations:
 * -------------------
 * This package provides a WPF-based GUI for Project3HelpWPF demo.  It's 
 * responsibilities are to:
 * - Provide a display of directory contents of a remote ServerPrototype.
 * - It provides a subdirectory list and a filelist for the selected directory.
 * - You can navigate into subdirectories by double-clicking on subdirectory
 *   or the parent directory, indicated by the name "..".
 *   
 * Required Files:
 * ---------------
 * Mainwindow.xaml, MainWindow.xaml.cs
 * Translater.dll
 * 
 * Maintenance History:
 * --------------------
 * ver 2.0 : 22 Apr 2018
 * - added tabbed display
 * - moved remote file view to RemoteNavControl
 * - migrated some methods from MainWindow to RemoteNavControl
 * - added local file view
 * - added NoSqlDb with very small demo as server starts up
 * ver 1.0 : 30 Mar 2018
 * - first release
 * - Several early prototypes were discussed in class. Those are all superceded
 *   by this package.
 *  ver 1.1 : 1 May 2018
 *  Added dispatchers to handle display reults of checkin, checkout, viewMetadata
 */

// Translater has to be statically linked with CommLibWrapper
// - loader can't find Translater.dll dependent CommLibWrapper.dll
// - that can be fixed with a load failure event handler
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
using System.Threading;
using System.IO;
using MsgPassingCommunication;
using System.Collections.ObjectModel;

namespace WpfApp1
{
  public partial class MainWindow : Window
  {
   
    public MainWindow()
    {
      InitializeComponent();
      Console.Title = "Project4Demo GUI Console";
      
    }
    
    private Stack<string> pathStack_ = new Stack<string>();
    private Stack<string> checkOutPathStack_ = new Stack<string>();
    
    internal Translater translater;
    internal CsEndPoint endPoint_;
    private Thread rcvThrd = null;
    private Dictionary<string, Action<CsMessage>> dispatcher_ 
      = new Dictionary<string, Action<CsMessage>>();
    internal string saveFilesPath;
    internal string sendFilesPath;

    //----< process incoming messages on child thread >----------------

    private void processMessages()
    {
      ThreadStart thrdProc = () => {
        while (true)
        {
          CsMessage msg = translater.getMessage();
          try
          {
            string msgId = msg.value("command");
            //Console.Write("\n  client getting message \"{0}\"", msgId);
            if (dispatcher_.ContainsKey(msgId))
              dispatcher_[msgId].Invoke(msg);
          }
          catch(Exception ex)
          {
            Console.Write("\n  {0}", ex.Message);
            msg.show();
          }
        }
      };
      rcvThrd = new Thread(thrdProc);
      rcvThrd.IsBackground = true;
      rcvThrd.Start();
    }
    //----< add client processing for message with key >---------------

    private void addClientProc(string key, Action<CsMessage> clientProc)
    {
      dispatcher_[key] = clientProc;
    }

    //----< Proc to load the checkout tab >--------
    private void DispatcherLoadCheckOutDirs()
    {
        Action<CsMessage> getDirs = (CsMessage rcvMsg) =>
        {
            Action clrDirs = () =>
            {
                //NavLocal.clearDirs();
                CheckOutControl.clearDirs();
            };
            Dispatcher.Invoke(clrDirs, new Object[] { });
            var enumer = rcvMsg.attributes.GetEnumerator();
            while (enumer.MoveNext())
            {
                string key = enumer.Current.Key;
                if (key.Contains("dir"))
                {
                    Action<string> doDir = (string dir) =>
                    {
                        CheckOutControl.addDir(dir);
                    };
                    Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                }
            }
            Action insertUp = () =>
            {
                CheckOutControl.insertParent();
            };
            Dispatcher.Invoke(insertUp, new Object[] { });
        };
        addClientProc("getCheckOutDirs", getDirs);
     }
   //-------< Proc to load checkout files >-------------------------
   private void DispatcherLoadCheckOutFiles()
    {
        Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
        {
            Action clrFiles = () =>
            {
                CheckOutControl.clearFiles();
            };
            Dispatcher.Invoke(clrFiles, new Object[] { });
            var enumer = rcvMsg.attributes.GetEnumerator();
            while (enumer.MoveNext())
            {
                string key = enumer.Current.Key;
                if (key.Contains("file"))
                {
                    Action<string> doFile = (string file) =>
                    {
                        CheckOutControl.addFile(file);
                    };
                    Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
                }
            }
        };
        addClientProc("getCheckOutFiles", getFiles);
    }
    ////----< load getDirs processing into dispatcher dictionary >-------

    private void DispatcherLoadGetDirs()
    {
      Action<CsMessage> getDirs = (CsMessage rcvMsg) =>
      {
        Action clrDirs = () =>
        {
          //NavLocal.clearDirs();
          NavRemote.clearDirs();
        };
        Dispatcher.Invoke(clrDirs, new Object[] { });
        var enumer = rcvMsg.attributes.GetEnumerator();
        while (enumer.MoveNext())
        {
          string key = enumer.Current.Key;
          if (key.Contains("dir"))
          {
            Action<string> doDir = (string dir) =>
            {
              NavRemote.addDir(dir);
            };
            Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
          }
        }
        Action insertUp = () =>
        {
          NavRemote.insertParent();
        };
        Dispatcher.Invoke(insertUp, new Object[] { });
      };
      addClientProc("getDirs", getDirs);
    }
    //----< load getFiles processing into dispatcher dictionary >------

    private void DispatcherLoadGetFiles()
    {
      Action<CsMessage> getFiles = (CsMessage rcvMsg) =>
      {
        Action clrFiles = () =>
        {
          NavRemote.clearFiles();
        };
        Dispatcher.Invoke(clrFiles, new Object[] { });
        var enumer = rcvMsg.attributes.GetEnumerator();
        while (enumer.MoveNext())
        {
          string key = enumer.Current.Key;
          if (key.Contains("file"))
          {
            Action<string> doFile = (string file) =>
            {
              NavRemote.addFile(file);
            };
            Dispatcher.Invoke(doFile, new Object[] { enumer.Current.Value });
          }
        }
      };
      addClientProc("getFiles", getFiles);
    }

    //---<Dispatcher to load query results>-----
    private void DispatcherLoadQueryResults()
    {
            Action<CsMessage> getResults = (CsMessage rcvMsg) =>
            {
                Action clrResults = () =>
                {
                    queryControl.fileList.Items.Clear();
                };
                Dispatcher.Invoke(clrResults, new Object[] { });
                var enumer = rcvMsg.attributes.GetEnumerator();
                Console.WriteLine("\nFile(s) obtianed from server:\n");
                Console.WriteLine("---------------------------------------------------");
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("key"))
                    {
                        Action<string> doDir = (string dir) =>
                        {
                            
                            Console.WriteLine(dir);
                            queryControl.fileList.Items.Add(dir);
                        };
                        Dispatcher.Invoke(doDir, new Object[] { enumer.Current.Value });
                    }
                }
                Action insertUp = () =>
                {
                    NavRemote.insertParent();
                };
                Dispatcher.Invoke(insertUp, new Object[] { });
            };
            addClientProc("queryResults", getResults);
        }
    //----< load getFiles processing into dispatcher dictionary >------

    private void DispatcherLoadSendFile()
    {
      Action<CsMessage> sendFile = (CsMessage rcvMsg) =>
      {
        string fileName = "";
        var enumer = rcvMsg.attributes.GetEnumerator();
        while (enumer.MoveNext())
        {
          string key = enumer.Current.Key;
          if (key.Contains("sendingFile"))
          {
            fileName = enumer.Current.Value;
            break;
          }
        }
        if (fileName.Length > 0)
        {
          Action<string> act = (string fileNm) => { showFile(fileNm); };
          Dispatcher.Invoke(act, new object[] { fileName });
        }
      };
      addClientProc("sendFile", sendFile);
    }
    //----< load all dispatcher processing >---------------------------

    private void loadDispatcher()
    {
      DispatcherConnect();
      DispatcherMetadata();
      DispatcherLoadQueryResults();
      DispatcherProcessCheckIn();
      DispatcherLoadGetDirs();
      DispatcherLoadGetFiles();
      loadCheckOutDispatcher();
      DispatcherLoadSendFile();
    }


// Wrapper around checkout dispatcher
    private void loadCheckOutDispatcher()
    {
      DispatcherProcessCheckOut();
      DispatcherLoadCheckOutDirs();
      DispatcherLoadCheckOutFiles();
    }

    //proc to display metadata from server
    private void DispatcherMetadata()
    {
        Action<CsMessage> connect = (CsMessage rcvMsg) =>
        {
            var enumer = rcvMsg.attributes.GetEnumerator();
            Action clrResults = () =>
            {
                MetadataControl.metadataResult.Items.Clear();
                Console.WriteLine("\n======== Displaying metadata =============\n");
            };
            Dispatcher.Invoke(clrResults, new Object[] { });
            while (enumer.MoveNext())
            {
                string key = enumer.Current.Key;
                if (key.Contains("message"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.validationMessage.Text = enumer.Current.Value;
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("message"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.validationMessage.Text = enumer.Current.Value;
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("name"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("status"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("description"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("author"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("filePath"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("category"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        MetadataControl.metadataResult.Items.Add(enumer.Current.Value);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
            }
        };
        addClientProc("viewMetadata", connect);
    }


    //Connect proc to fetch message for successful connection
    private void DispatcherConnect()
    {
        Action<CsMessage> connect = (CsMessage rcvMsg) =>
        {
            var enumer = rcvMsg.attributes.GetEnumerator();
            while (enumer.MoveNext())
            {
                string key = enumer.Current.Key;
                if (key.Contains("Message"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine(enumer.Current.Value);
                        connectionControl.connectMessage.Text = enumer.Current.Value;
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
            }
        };
        addClientProc("connected", connect);
    }

        //Checkout proc to fetch message for successful
        private void DispatcherProcessCheckOut()
    {
        Action<CsMessage> completeCheckOut = (CsMessage rcvMsg) =>
        {
            var enumer = rcvMsg.attributes.GetEnumerator();
            while (enumer.MoveNext())
            {
                string key = enumer.Current.Key;
                if(key.Contains("fileName"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine("Checked out file copied to local storage");
        
                        System.IO.File.Copy(saveFilesPath + "/" + enumer.Current.Value, "../../../../LocalStorage/"+enumer.Current.Value,true);
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
                if (key.Contains("Message"))
                {
                    Action<string> mess = (string value) =>
                    {
                        Console.WriteLine("\n\n Message from server regarding checkout -> " + enumer.Current.Value);
                        CheckOutControl.CheckOutStatusBar.Text = enumer.Current.Value;
                    };
                    Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                }
            }
        };
        addClientProc("performCheckOut", completeCheckOut);
    }

        //Checkin proc to fetch message for successful
        private void DispatcherProcessCheckIn()
        {
            Action<CsMessage> completeCheckIn = (CsMessage rcvMsg) =>
            {
                var enumer = rcvMsg.attributes.GetEnumerator();
                while (enumer.MoveNext())
                {
                    string key = enumer.Current.Key;
                    if (key.Contains("message"))
                    {
                        Action<string> mess = (string value) =>
                        {
                            Console.WriteLine("\n\nMessage from server for checkin -> " + enumer.Current.Value);
                            checkInControl.ServerMessage.Text = enumer.Current.Value;
                        };
                        Dispatcher.Invoke(mess, new Object[] { enumer.Current.Value });
                    }
                }
            };
            addClientProc("completeCheckIn", completeCheckIn);
        }

        //----< start Comm, fill window display with dirs and files >------

        private void Window_Loaded(object sender, RoutedEventArgs e)
    {
      // start Comm
      endPoint_ = new CsEndPoint();
      endPoint_.machineAddress = "localhost";
      endPoint_.port = 8082;
      NavRemote.navEndPoint_ = endPoint_;
      CheckOutControl.navEndPoint_ = endPoint_;
      connectionControl.navEndPoint_ = endPoint_;
      checkInControl.navEndPoint_ = endPoint_;
      queryControl.navEndPoint_ = endPoint_;
      MetadataControl.navEndPoint_ = endPoint_;
      translater = new Translater();
      translater.listen(endPoint_);

      // start processing messages
      processMessages();

      // load dispatcher
      loadDispatcher();

      CsEndPoint serverEndPoint = new CsEndPoint();
      serverEndPoint.machineAddress = "localhost";
      serverEndPoint.port = 8080;
      pathStack_.Push("../Storage");
      NavRemote.PathTextBlock.Text = "Storage";
      CheckOutControl.PathTextBlock.Text = "Storage";
      NavRemote.pathStack_.Push("../Storage");
      CheckOutControl.pathStack_.Push("../Storage");
      NavLocal.PathTextBlock.Text = "LocalStorage";
      checkInControl.PathTextBlock.Text = "LocalStorage";
      NavLocal.pathStack_.Push("");
      checkInControl.pathStack_.Push("");
      NavLocal.localStorageRoot_ = "../../../../LocalStorage";
      checkInControl.localStorageRoot_= "../../../../LocalStorage";
      saveFilesPath = translater.setSaveFilePath("../../../SaveFiles");
      sendFilesPath = translater.setSendFilePath("../../../SendFiles");
      NavLocal.refreshDisplay();
      NavRemote.refreshDisplay();
      CheckOutControl.refreshDisplay();
      checkInControl.refreshDisplay();
      connectionControl.setupConnect();
      mainTestStub();
      //test1();
    }
    //----< strip off name of first part of path >---------------------

    public string removeFirstDir(string path)
    {
      string modifiedPath = path;
      int pos = path.IndexOf("/");
      modifiedPath = path.Substring(pos + 1, path.Length - pos - 1);
      return modifiedPath;
    }
    //----< show file text >-------------------------------------------

    private void showFile(string fileName)
    {
      Console.WriteLine("\n----------------Showing file text in pop up window-------------");
      Paragraph paragraph = new Paragraph();
      string fileSpec = saveFilesPath + "\\" + fileName;
      string fileText = File.ReadAllText(fileSpec);
      paragraph.Inlines.Add(new Run(fileText));
      CodePopupWindow popUp = new CodePopupWindow();
      popUp.codeView.Blocks.Clear();
      popUp.codeView.Blocks.Add(paragraph);
      popUp.Show();
    }

    void mainTestStub()
    {
        connectTestStub();
        checkInTestStub();
        queryTestStub();
        getMetatdataTestStub();
        checkOutTestStub();
        browseTestStub();
        fullFileTextTestStub();
    }

    // Test stub for connect
    void connectTestStub()
    {
        CsEndPoint serverEndPoint = new CsEndPoint();
        CsMessage msg = new CsMessage();
        serverEndPoint.machineAddress = "localhost";
        serverEndPoint.port = 8080;
        Console.WriteLine("\n\n --------------Demonstrating test stub for making connection------------\n");
        Console.WriteLine("\n\n Sending message 'Hello' to the server \n\n");
        connectionControl.connectMessage.Text = "Attempting to establish connection";
        serverEndPoint.port = 8080;
        serverEndPoint.machineAddress = "localhost";
        msg.add("to", CsEndPoint.toString(serverEndPoint));
        msg.add("from", CsEndPoint.toString(endPoint_));
        msg.add("message", "Hello");
        msg.add("command", "connect");
        translater.postMessage(msg);
     }
   
        //test stub for checkin
        void checkInTestStub()
        {
            simpleCheckIn();
            checkInWithOpenDependant();
            closedCheckIn();
        }

        //test stub for simple checkin
        void simpleCheckIn()
        {
            
            Console.WriteLine("Demonstrating checkin with open status\n\n");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("nameSpace", "Test5");
            msg.add("category", "Header");
            msg.add("fileName", "DbCore.h");
            msg.add("sendingFile", "DbCore.h");
            System.IO.File.Copy(NavLocal.localStorageRoot_ + "/" + "DbCore.h", sendFilesPath + "/" + "DbCore.h",true);
            msg.add("status", "Open");
            msg.add("description", "First checkin");
            msg.add("command", "checkin");
            translater.postMessage(msg);
        }

        void checkInWithOpenDependant()
        {
            
            Console.WriteLine("Demonstrating close checkin with open dependants\n\n");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("nameSpace", "Test5");
            msg.add("category", "Header");
            msg.add("fileName", "DbCore.cpp");
            msg.add("sendingFile", "DbCore.cpp");
            if (!System.IO.File.Exists(sendFilesPath + "/" + "DbCore.cpp"))
                System.IO.File.Copy(NavLocal.localStorageRoot_ + "/" + "DbCore.cpp", sendFilesPath + "/" + "DbCore.cpp");
            msg.add("depFlag", "yes");
            msg.add("dependantNameSpace", "Test5");
            msg.add("dependantFileName", "DbCore.h");
            msg.add("dependantVersion", "1");
            msg.add("status", "Closed");
            msg.add("description", "Second checkin");
            msg.add("command", "checkin");
            translater.postMessage(msg);
        }

        //test stub for closed checkin
        void closedCheckIn()
        {
            
            Console.WriteLine("Demonstrating checkin with closed status\n\n");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("nameSpace", "Test1");
            msg.add("category", "Header");
            msg.add("fileName", "Message.h");
            msg.add("sendingFile", "Message.h");
            System.IO.File.Copy(NavLocal.localStorageRoot_ + "/" + "Message.h", sendFilesPath + "/" + "Message.h",true);
            msg.add("status", "Closed");
            msg.add("description", "close checkin");
            msg.add("command", "checkin");
            translater.postMessage(msg);
        }

        // test stub for checkout
        void checkOutTestStub()
        {
            Console.WriteLine("\n\n---------Demonstrating checking out file---------\n\n");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            Console.WriteLine("Requesting checkout for file Message.cpp.1 in package Test1 of the repository\n\n");
            msg.add("command", "performCheckOut");
            msg.add("package", "Test1");
            msg.add("fileName", "Message.cpp.1");
            translater.postMessage(msg);
        }

        //query test stub
        void queryTestStub()
        {
            queryByNameTestStub();
            queryByCategory();
            queryByVersion();
            queryNoDependants();
        }

        // test stub to query file by name
        void queryByNameTestStub()
        {
            Console.WriteLine("======= Query for files by name =========");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            Console.WriteLine("Search for file with name DbCore\n");
            msg.add("command", "queryByName");
            msg.add("queryParam","DbCore");
            translater.postMessage(msg);
        }

        //test stub to query by version
        void queryByVersion()
        {
            Console.WriteLine("======= Query for files by version number =========");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            Console.WriteLine("Search for file with version 2\n");
            msg.add("command", "queryByVersion");
            msg.add("queryParam", "2");
            translater.postMessage(msg);
        }

        //test stub to query by category
        void queryByCategory()
        {
            Console.WriteLine("======= Query for files by category =========");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            Console.WriteLine("Search for file with category Header \n");
            msg.add("command", "queryByCategory");
            msg.add("queryParam", "Header");
            translater.postMessage(msg);
        }

        void queryNoDependants()
        {
            Console.WriteLine("======= Query for files by category =========");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            Console.WriteLine("Search for file without dependants \n");
            msg.add("command", "queryByDependants");
            msg.add("queryParam", "Header");
            translater.postMessage(msg);
        }

        //test stub to view metadata
        void getMetatdataTestStub()
        {
            Console.WriteLine("\n---------- Viewing metadata on UI -----------\n");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            Console.WriteLine("\nRequesting metadata details for DbCore.h of version 2 in namespace Test5\n");
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_)); msg.add("package", "Test5");
            msg.add("fileName","DbCore.h");
            msg.add("version", "1");
            msg.add("command", "viewMetadata");
            translater.postMessage(msg);
        }

        //Test stub for browse
        void browseTestStub()
        {
            Console.WriteLine("\n---------- Browsing in repository -----------\n");
            CheckOutControl.refreshDisplay();
        }

        //Test stub to view file text
        void fullFileTextTestStub()
        {
            Console.WriteLine("--------- Viewing full file text------------");
            CsEndPoint serverEndPoint = new CsEndPoint();
            serverEndPoint.machineAddress = "localhost";
            serverEndPoint.port = 8080;
            CsMessage msg = new CsMessage();
            msg.add("to", CsEndPoint.toString(serverEndPoint));
            msg.add("from", CsEndPoint.toString(endPoint_));
            msg.add("command", "sendFile");
            msg.add("path", NavRemote.pathStack_.Peek());
            msg.add("fileName", "Process.h");
            translater.postMessage(msg);
        }
        //----< first test not completed >---------------------------------

        //void test1()
        //{
        //  MouseButtonEventArgs e = new MouseButtonEventArgs(null, 0, 0);
        //  DirList.SelectedIndex = 1;
        //  DirList_MouseDoubleClick(this, e);
        //}
    }
}
