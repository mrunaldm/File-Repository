#pragma once
///////////////////////////////////////////////////////////////////////
// ServerPrototype.h - Console App that processes incoming messages  //
// ver 1.2                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
*  Package Operations:
* ---------------------
*  Package contains one class, Server, that contains a Message-Passing Communication
*  facility. It processes each message by invoking an installed callable object
*  defined by the message's command key.
*  - This is implemented with a message dispatcher (unodered_map<Msg.Id,ServerProc>
*    where ServerProcs are defined for each type of processing required by the server.
*
*  Message handling runs on a child thread, so the Server main thread is free to do
*  any necessary background processing (none, so far).
*
*  Required Files:
* -----------------
*  ServerPrototype.h, ServerPrototype.cpp
*  Comm.h, Comm.cpp, IComm.h
*  Message.h, Message.cpp (static library)
*  Process.h, Process.cpp (static library)
*  FileSystem.h, FileSystem.cpp
*  Utilities.h
*
*  Maintenance History:
* ----------------------
*  ver 1.2 : 22 Apr 2018
*  - added NoSqlDb to server members
*  - added simple demo of db in Server startup
*  ver 1.1 : 09 Apr 2018
*  - added ServerProcs for
*    - sending files for popup display
*    - executing remote analysis
*  ver 1.0 : 03/27/2018
*  - first release
*/
#include <vector>
#include <string>
#include <unordered_map>
#include <functional>
#include <thread>
#include "../CppCommWithFileXfer/Message/Message.h"
#include "../CppCommWithFileXfer/MsgPassingComm/Comm.h"
#include "../CppNoSqlDb/DbCore/DbCore.h"
#include "../CppNoSqlDb/PayLoad/PayLoad.h"
#include <windows.h>
#include <tchar.h>


namespace Repository
{
  using File = std::string;
  using Files = std::vector<File>;
  using Dir = std::string;
  using Dirs = std::vector<Dir>;
  using SearchPath = std::string;
  using Key = std::string;
  using Msg = MsgPassingCommunication::Message;
  using ServerProc = std::function<Msg(Msg)>;
  using MsgDispatcher = std::unordered_map<Key,ServerProc>;
  const SearchPath storageRoot = "../Storage";  // root for all server file storage
  const MsgPassingCommunication::EndPoint serverEndPoint("localhost", 8080);  // listening endpoint
  static NoSqlDb::DbCore<NoSqlDb::PayLoad> database_;
  class Server
  {
  public:
    Server(MsgPassingCommunication::EndPoint ep, const std::string& name);
    void start();
    void stop();
    void addMsgProc(Key key, ServerProc proc);
    bool hasMessageKey(const Key& key);
    void processMessages();
	NoSqlDb::DbCore<NoSqlDb::PayLoad> database() { return db_; }
    void postMessage(MsgPassingCommunication::Message msg);
    MsgPassingCommunication::Message getMessage();
    static Dirs getDirs(const SearchPath& path = storageRoot);
    static Files getFiles(const SearchPath& path = storageRoot);
    MsgPassingCommunication::Context* getContext();
	static std::string checkOutFile(std::string& package, std::string& fileName);
    std::string setSendFilePath(const std::string& relPath);
    std::string setSaveFilePath(const std::string& relPath);
    std::string getSendFilePath();
    std::string getSaveFilePath();
    void initializeDb();
	void populateDb();
  private:
    MsgPassingCommunication::Comm comm_;
    MsgDispatcher dispatcher_;
    std::thread msgProcThrd_;
	NoSqlDb::DbCore<NoSqlDb::PayLoad> db_;
  };

  //----< return reference to MsgPassingCommunication context >--------

  inline MsgPassingCommunication::Context* Server::getContext()
  {
    return comm_.getContext();
  }
  //----< initialize server endpoint and give server a name >----------

 Server::Server(MsgPassingCommunication::EndPoint ep, const std::string& name)
    : comm_(ep, name) {
    initializeDb();
  }

  inline std::string Server::setSendFilePath(const std::string& relPath)
  {
    comm_.setSendFilePath(relPath);
  }
  inline std::string Server::setSaveFilePath(const std::string& relPath)
  {
    comm_.setSaveFilePath(relPath);
  }
  inline std::string Server::getSendFilePath()
  {
    comm_.getSendFilePath();
  }
  inline std::string Server::getSaveFilePath()
  {
    comm_.getSaveFilePath();
  }
  //----< start server's instance of Comm >----------------------------

  inline void Server::start()
  {
    comm_.start();
  }
  //----< stop Comm instance >-----------------------------------------

  inline void Server::stop()
  {
    if(msgProcThrd_.joinable())
      msgProcThrd_.join();
    comm_.stop();
  }
  //----< pass message to Comm for sending >---------------------------

  inline void Server::postMessage(MsgPassingCommunication::Message msg)
  {
    comm_.postMessage(msg);
  }
  //----< get message from Comm >--------------------------------------

  inline MsgPassingCommunication::Message Server::getMessage()
  {
    Msg msg = comm_.getMessage();
    return msg;
  }
  //----< add ServerProc callable object to server's dispatcher >------

  void Server::addMsgProc(Key key, ServerProc proc)
  {
    dispatcher_[key] = proc;
  }
  //----< does server have specified key? >----------------------------

  bool Server::hasMessageKey(const Key& key)
  {
    for (auto item : dispatcher_)
    {
      if (item.first == key)
        return true;
    }
    return false;
  }
  //----< start processing messages on child thread >------------------

  void Server::processMessages()
  {
    auto proc = [&]()
    {
      if (dispatcher_.size() == 0)
      {
        std::cout << "\n  no server procs to call";
        return;
      }
      while (true)
      {
        Msg msg = getMessage();
        std::cout << "\n  received message: " << msg.command() << " from " << msg.from().toString();
        if (msg.containsKey("verbose"))
        {
          std::cout << "\n";
          msg.show();
        }
        if (msg.command() == "serverQuit")
          break;
        Msg reply;
        reply.to(msg.from());
        reply.from(msg.to());
        if (hasMessageKey(msg.command()))
        {
          reply = dispatcher_[msg.command()](msg);
        }
        else
        {
          reply.command("error - unknown command");
        }
        if (msg.to().port != msg.from().port)  // avoid infinite message loop
        {
          postMessage(reply);
          msg.show();
          reply.show();
        }
        else
          std::cout << "\n  server attempting to post to self";
      }
      std::cout << "\n  server message processing thread is shutting down";
    };
    std::thread t(proc);
    //SetThreadPriority(t.native_handle(), THREAD_PRIORITY_HIGHEST);
    std::cout << "\n  starting server thread to process messages";
    msgProcThrd_ = std::move(t);
  }

 void Server::initializeDb()
  {
    NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
	elem.name("Comm.h");
	elem.descrip("First file");
	elem.payLoad().value("../Storage/Test4/Comm.h.2");
	Key key = "Test4::Comm.h.2";
	elem.payLoad().categories().push_back("Header");
	db_[key] = elem;
	populateDb();
	database_ = db_;
  }

 void Server::populateDb()
 {
	 NoSqlDb::DbElement<NoSqlDb::PayLoad> elem,elem2;
	 elem.name("Message.h");
	 elem.descrip("Existing file");
	 elem.payLoad().value("../Storage/Test1/Message.h.1");
	 elem.payLoad().categories().push_back("Header");
	 elem.payLoad().checkInStatus(NoSqlDb::CheckinStatus::closed);
	 Key key = "Test1::Message.h.1";
	 db_[key] = elem;
	 elem2.name("Message.cpp");
	 elem2.descrip("Second exiting file");
	 elem2.payLoad().value("../Storage/Test1/Message.cpp.1");
	 elem2.payLoad().categories().push_back("Source");
	 elem2.payLoad().checkInStatus(NoSqlDb::CheckinStatus::closed);
	 Key key2 = "Test1::Message.cpp.1";
	 db_[key2] = elem2;
 }
}