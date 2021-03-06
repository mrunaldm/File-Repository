/////////////////////////////////////////////////////////////////////
// ServerPrototype.cpp - Handles all commands from server          //
// ver 1.2                                                         //
// Mrunal, CSE687 - Object Oriented Design, Spring 2018            //
/////////////////////////////////////////////////////////////////////
/*
* Package Operations:
* -------------------
* This package provides two classes:
* - Checkin,CheckOut, sending metadata, version management

* Required Files:
* ---------------
* DbCore.h, DbCore.cpp
* DateTime.h, DateTime.cpp
* Utilities.h, Utilities.cpp
*
* Maintenance History:
* --------------------
*
* ver 1.0 : 10 Jan 2018
* - first release
1 May 2018 : ver 1.1
Checkin, checkout, viewMetadata endpoints added
*/


#include "ServerPrototype.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include "../Process/Process/Process.h"
#include "../Query/Query.h"
#include <chrono>
#include <regex>

namespace MsgPassComm = MsgPassingCommunication;

using namespace Repository;
using namespace FileSystem;
using Msg = MsgPassingCommunication::Message;

//----< return name of every file on path >----------------------------

Files Server::getFiles(const Repository::SearchPath& path)
{
  return Directory::getFiles(path);
}
//----< return name of every subdirectory on path >--------------------

Dirs Server::getDirs(const Repository::SearchPath& path)
{
  return Directory::getDirectories(path);
}

namespace MsgPassingCommunication
{
  // These paths, global to MsgPassingCommunication, are needed by 
  // several of the ServerProcs, below.
  // - should make them const and make copies for ServerProc usage

  std::string sendFilePath;
  std::string saveFilePath;

  //----< show message contents >--------------------------------------

  template<typename T>
  void show(const T& t, const std::string& msg)
  {
    std::cout << "\n  " << msg.c_str();
    for (auto item : t)
    {
      std::cout << "\n    " << item.c_str();
    }
  }
  //----< test ServerProc simply echos message back to sender >--------

  std::function<Msg(Msg)> echo = [](Msg msg) {
    Msg reply = msg;
    reply.to(msg.from());
    reply.from(msg.to());
    return reply;
  };
  //----< getFiles ServerProc returns list of files on path >----------

  std::function<Msg(Msg)> getFiles = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command(msg.command());
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != ".")
        searchPath = searchPath + "\\" + path;
      Files files = Server::getFiles(searchPath);
      size_t count = 0;
      for (auto item : files)
      {
        std::string countStr = Utilities::Converter<size_t>::toString(++count);
        reply.attribute("file" + countStr, item);
      }
    }
    else
    {
      std::cout << "\n  getFiles message did not define a path attribute";
    }
    return reply;
  };


  //Helper to remove version number
  std::string removeVersionNumber(const std::string& fileName)
  {
	  return (fileName.substr(0, fileName.find_last_of(".")));
  }

  //Helper to get checkinStatus
  NoSqlDb::CheckinStatus getStatus(const std::string& status)
  {
	  if (status == "Open")
		  return NoSqlDb::CheckinStatus::open;
	  else
		  return NoSqlDb::CheckinStatus::closed;
  }

  //Helper to generate dependancy key
  std::string generateDependancyKey(const std::string& package, const std::string& fileName, const std::string& version)
  {
	  return (package + "::" + fileName + "." + version);
  }

  //helper to get current version number
  int getVersionNumber(std::string& package, std::string& fileName)
  {
	  int currentVersion = 0;
	  std::string keyMapper = package + "::" + fileName;
	  std::regex fileMatch(keyMapper);
	  for (auto iterator : Repository::database_)
	  {
		  if (std::regex_search(iterator.first, fileMatch))
			  currentVersion++;
	  }
	  if (currentVersion == 0)
		  currentVersion = 1;
	  return currentVersion;
  }
 
  //Function to create metadata
  NoSqlDb::DbElement<NoSqlDb::PayLoad> createMetadata(std::string& fileName, std::string& descrip,NoSqlDb::CheckinStatus status, std::string& destPath, std::string& dependancy, std::string& categ)
  {
	  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
	  elem.name(fileName);
	  elem.author("User");
	  elem.descrip(descrip);
	  elem.payLoad().value(destPath);
	  elem.payLoad().checkInStatus(status);
	  elem.payLoad().categories().push_back(categ);
	  if(dependancy != "")
		elem.addChildKey(dependancy);
	  return elem;
  }

  //Perform checkIn
  std::string doCheckIn(std::string& package, std::string& fileName,std::string& descrip, std::string& dependants, NoSqlDb::CheckinStatus status, std::string& category)
  {
	  int version = getVersionNumber(package, fileName);
	  NoSqlDb::DbElement<NoSqlDb::PayLoad> depElem;
	  std::string message;
	  if (!FileSystem::Directory::exists("../Storage/" + package))
		  FileSystem::Directory::create("../Storage/" + package);
	  NoSqlDb::Key currentKey = generateDependancyKey(package, fileName, std::to_string(version));
	  if (Repository::database_.contains(currentKey))
	  {
		  NoSqlDb::DbElement<NoSqlDb::PayLoad> currentElem = database_[currentKey];
		  if (currentElem.payLoad().checkInStatus() == NoSqlDb::CheckinStatus::closed)
		  {
			  version++;
			  std::cout << "\n\n New version created for file " + fileName + " in package " + package + "\n\n";
			  message = "New Version for file created on closed checkin";
		  }
	  }
	  else
	  {
		  std::cout << "\n\n New File " + fileName + " checked into the package " + package + " of the repository\n\n";
		  message = "New file successfully checked in";
	  }
	  std::string src = "SaveFiles/" + fileName;
	  std::string destPath = "../Storage/" + package + "/" + fileName + "." + std::to_string(version);
	  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem = createMetadata(fileName, descrip, status,destPath, dependants, category);
	  NoSqlDb::Key key = package + "::" + fileName + "." + std::to_string(version);
	  if (dependants != "" && !Repository::database_.contains(dependants))
		  return "Dependant file not found in repository";
	  else
	  {
		  if(dependants != "")
			depElem = database_[dependants];
		  if (status == NoSqlDb::CheckinStatus::closed && depElem.payLoad().checkInStatus() == NoSqlDb::CheckinStatus::open)
			  return "Closed checkin with open dependant not possible";
	  }
	  FileSystem::File::copy(src, destPath);
	  if (!Repository::database_.contains(key))
		  Repository::database_.addRecord(key, elem);
	  else
		  Repository::database_[key] = elem;
	  return message;
  }

  //Handler for checkIn
  std::function<Msg(Msg)> checkin = [](Msg msg) {
	  std::cout << "\n\n============== CheckIn handler on server ============\n\n";
	  Msg reply;
	  std::string dependancy="";
	  std::string package = msg.value("nameSpace");
	  std::string fileName = msg.value("fileName");
	  std::string category = msg.value("category");
	  std::string descrip = msg.value("description");
	  reply.to(msg.from());
	  reply.from(msg.to());
	  NoSqlDb::CheckinStatus cstatus = getStatus(msg.value("status"));
	  if (msg.value("depFlag") == "yes")
		  dependancy = generateDependancyKey(msg.value("dependantNameSpace"), msg.value("dependantFileName"), msg.value("dependantVersion"));
	  std::string message = doCheckIn(package, fileName, descrip, dependancy, cstatus, category);
	  reply.attribute("command", "completeCheckIn");
	  reply.attribute("message", message);
	  return reply;
  };

  //Handler for checkOut
  std::string checkOutFile(std::string& package, std::string& fileName)
  {
	  std::string failMessage = "CheckOut failed";
	  std::string dbKey = package + "::" + fileName;
	  NoSqlDb::DbElement<NoSqlDb::PayLoad> checkOutElement = Repository::database_[dbKey];
	  std::string sourcePath = checkOutElement.payLoad().value();
	  std::string file = removeVersionNumber(sourcePath);
	  std::string dir =  FileSystem::Directory::getCurrentDirectory();
	  std::string destPath = "SendFiles/" + FileSystem::Path::getName(file);
	  if (FileSystem::File::copy(sourcePath, destPath))
	  {
		  std::cout << "File " + fileName + " of package " + package + " successfully checked out of repository via socket\n\n";
		  return destPath;
	  }
	  else
		  return failMessage;
  }

  // function to handle checkout
  std::function<Msg(Msg)> performCheckOut = [](Msg msg) {
	  std::cout << "\n\n =============== CheckOut handler on server ========== \n\n";
	  Msg response;
	  response.to(msg.from());
	  response.from(msg.to());
	  std::string package = msg.value("package");
	  std::string fileName = msg.value("fileName");
	  std::string destPath = checkOutFile(package, fileName);
	  response.attribute("fileName", FileSystem::Path::getName(destPath));
	  response.attribute("sendingFile", FileSystem::Path::getName(destPath));
	  response.attribute("Message", "Successful CheckOut");
	  response.command(msg.command());
	  return response;
  };

  //Function to handle query by name
  NoSqlDb::Keys getKeysByName(const std::string& inputParam)
  {
	  NoSqlDb::Conditions<NoSqlDb::PayLoad> condition;
	  condition.name(inputParam);
	  NoSqlDb::Query<NoSqlDb::PayLoad> query(Repository::database_);
	  return query.select(condition).keys();
  }
  // function to handle querying by fileName
  std::function<Msg(Msg)> queryByName = [](Msg msg) {
	  Msg response;
	  std::cout << "Request obtianed to query files by fileName having name " + msg.value("queryParam") + "\n\n";
	  NoSqlDb::Keys results = getKeysByName(msg.value("queryParam"));
	  response.to(msg.from());
	  response.from(msg.to());
	  size_t count = 0;
	  NoSqlDb::showDb(Repository::database_);
	  std::cout << "\nSending fileName(s) :\n";
	  for (auto key : results)
	  {
		  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
		  elem = Repository::database_[key];
		  std::string countStr = Utilities::Converter<size_t>::toString(++count);
		  response.attribute("key" + countStr, FileSystem::Path::getName(elem.payLoad().value()));
		  std::cout << FileSystem::Path::getName(elem.payLoad().value()) << "\n";
	  }
	  response.command("queryResults");
	  return response;
  };

  //Helper to get files by version number
  NoSqlDb::Keys getFilesByVersion(const std::string& param)
  {
	  NoSqlDb::Query<NoSqlDb::PayLoad> query(Repository::database_);
	  std::string value = param;
	  auto hasValue = [&value](NoSqlDb::DbElement<NoSqlDb::PayLoad>& elem) {
		  std::regex val(value);
		  return std::regex_search(elem.payLoad().value(), val);
	  };
	  return query.select(hasValue).keys();
  }
  //Function to handle querying by version
  std::function<Msg(Msg)> queryByVersion = [](Msg msg) {
	  Msg response;
	  NoSqlDb::Keys results = getFilesByVersion(msg.value("queryParam"));
	  std::cout << "\nRequest received to obtain files having version " + msg.value("queryParam") << "\n";
	  response.to(msg.from());
	  response.from(msg.to());
	  size_t count = 0;
	  NoSqlDb::showDb(Repository::database_);
	  std::cout << "\nSending fileName(s) :\n";
	  for (auto key : results)
	  {
		  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
		  elem = Repository::database_[key];
		  std::string countStr = Utilities::Converter<size_t>::toString(++count);
		  response.attribute("key" + countStr, FileSystem::Path::getName(elem.payLoad().value()));
		  std::cout << FileSystem::Path::getName(elem.payLoad().value()) << "\n";
	  }
	  response.command("queryResults");
	  return response;
  };

  //Helper to get keys by category
  NoSqlDb::Keys getFilesByCategory(const std::string& cat)
  {
	  NoSqlDb::Query<NoSqlDb::PayLoad> query(Repository::database_);
	  std::string category = cat;
	  auto hasCategory = [&category](NoSqlDb::DbElement<NoSqlDb::PayLoad>& elem) {
		  return (elem.payLoad()).hasCategory(category);
	  };
	  return query.select(hasCategory).keys();
  }
  //Handler for query by category
  std::function<Msg(Msg)> queryByCategory = [](Msg msg) {
	  Msg response;
	  std::cout << "\nRequest obtained to get files in the category " + msg.value("queryParam") + "\n\n";
	  NoSqlDb::Keys results = getFilesByCategory(msg.value("queryParam"));
	  response.to(msg.from());
	  response.from(msg.to());
	  size_t count = 0;
	  NoSqlDb::showDb(Repository::database_);
	  std::cout << "\nSending fileName(s) :\n";
	  for (auto key : results)
	  {
		  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
		  elem = Repository::database_[key];
		  std::string countStr = Utilities::Converter<size_t>::toString(++count);
		  response.attribute("key" + countStr, FileSystem::Path::getName(elem.payLoad().value()));
		  std::cout << FileSystem::Path::getName(elem.payLoad().value()) << "\n";
	  }
	  response.command("queryResults");
	  return response;
  };

  //function to get files without dependants
  NoSqlDb::Keys getFilesWithoutDependats()
  {
	  NoSqlDb::Keys result;
	  result.clear();
	  for (auto iterator : Repository::database_)
	  {
		  if (iterator.second.children().size() == 0)
			  result.push_back(iterator.first);
	  }
	  return result;
  }

  //Helper to get status as string
  std::string getCheckStatus(NoSqlDb::CheckinStatus status)
  {
	  if (status == NoSqlDb::CheckinStatus::open)
		  return "Open";
	  else
		  return "Closed";

  }

  //Handler to send files without dependants
  std::function<Msg(Msg)> queryByDependants = [](Msg msg) {
	  Msg response;
	  NoSqlDb::Keys results = getFilesWithoutDependats();
	  std::cout << "\n Request received to send files without dependancy\n";
	  response.to(msg.from());
	  response.from(msg.to());
	  size_t count = 0;
	  NoSqlDb::showDb(Repository::database_);
	  std::cout << "Sending fileName(s) :\n";
	  for (auto key : results)
	  {
		  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem;
		  elem = Repository::database_[key];
		  std::string countStr = Utilities::Converter<size_t>::toString(++count);
		  response.attribute("key" + countStr, FileSystem::Path::getName(elem.payLoad().value()));
		  std::cout << FileSystem::Path::getName(elem.payLoad().value()) << "\n";
	  }
	  response.command("queryResults");
	  return response;
  };

  //Handler for viewing file metadata
  std::function<Msg(Msg)> viewMetadata = [](Msg msg) {
	  Msg response;
	  response.to(msg.from());
	  response.from(msg.to());
	  NoSqlDb::Key queryKey = generateDependancyKey(msg.value("package"), msg.value("fileName"), msg.value("version"));
	  if (!Repository::database_.contains(queryKey))
	  {
		  response.attribute("message", "Requested file combination not present in repository");
		  return response;
	  }
	  NoSqlDb::DbElement<NoSqlDb::PayLoad> elem = Repository::database_[queryKey];
	  response.attribute("name", "Name : " + elem.name());
	  response.attribute("description", "Description : " + elem.descrip());
	  response.attribute("author", "Author : " + elem.author());
	  response.attribute("filePath", "Path : " + elem.payLoad().value());
	  response.attribute("status", "Check-In status : " + getCheckStatus(elem.payLoad().checkInStatus()));
	  response.attribute("command", "viewMetadata");
	  response.attribute("category", "Categories : " + elem.payLoad().categories()[0]);
	  return response;
  };

 
  //Handler for connection request on server side
  std::function<Msg(Msg)> connection = [](Msg msg) {
	  std::cout << "\n================ Connection handler on server =========== \n\n";
	  Msg response;
	  std::cout << "\n\n Connection request recieved\n\n";
	  std::cout << "Recieved message from client -> " << msg.value("message");
	  response.to(msg.from());
	  response.from(msg.to());
	  response.attribute("Message", "Connection Established");
	  response.command("connected");
	  std::cout << "\n\n Connection established between client and server \n\n";
	  return response;
  };
  //----< getDirs ServerProc returns list of directories on path >-----

  std::function<Msg(Msg)> getDirs = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command(msg.command());
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != ".")
        searchPath = searchPath + "\\" + path;
      Files dirs = Server::getDirs(searchPath);
      size_t count = 0;
      for (auto item : dirs)
      {
        if (item != ".." && item != ".")
        {
          std::string countStr = Utilities::Converter<size_t>::toString(++count);
          reply.attribute("dir" + countStr, item);
        }
      }
    }
    else
    {
      std::cout << "\n  getDirs message did not define a path attribute";
    }
    return reply;
  };

  //----< sendFile ServerProc sends file to requester >----------------
  /*
  *  - Comm sends bodies of messages with sendingFile attribute >------
  */
  std::function<Msg(Msg)> sendFile = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command("sendFile");
    reply.attribute("sendingFile", msg.value("fileName"));
    reply.attribute("fileName", msg.value("fileName"));
    reply.attribute("verbose", "blah blah");
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != "." && path != searchPath)
        searchPath = searchPath + "\\" + path;
      if (!FileSystem::Directory::exists(searchPath))
      {
        std::cout << "\n  file source path does not exist";
        return reply;
      }
      std::string filePath = searchPath + "/" + msg.value("fileName");
      std::string fullSrcPath = FileSystem::Path::getFullFileSpec(filePath);
      std::string fullDstPath = sendFilePath;
      if (!FileSystem::Directory::exists(fullDstPath))
      {
        std::cout << "\n  file destination path does not exist";
        return reply;
      }
      fullDstPath += "/" + msg.value("fileName");
      FileSystem::File::copy(fullSrcPath, fullDstPath);
    }
    else
    {
      std::cout << "\n  getDirs message did not define a path attribute";
    }
    return reply;
  };

  //----< analyze code on current server path >--------------------------
  /*
  *  - Creates process to run CodeAnalyzer on specified path
  *  - Won't return until analysis is done and logfile.txt
  *    is copied to sendFiles directory
  */
  std::function<Msg(Msg)> codeAnalyze = [](Msg msg) {
    Msg reply;
    reply.to(msg.from());
    reply.from(msg.to());
    reply.command("sendFile");
    reply.attribute("sendingFile", "logfile.txt");
    reply.attribute("fileName", "logfile.txt");
    reply.attribute("verbose", "blah blah");
    std::string path = msg.value("path");
    if (path != "")
    {
      std::string searchPath = storageRoot;
      if (path != "." && path != searchPath)
        searchPath = searchPath + "\\" + path;
      if (!FileSystem::Directory::exists(searchPath))
      {
        std::cout << "\n  file source path does not exist";
        return reply;
      }
      Process p;
      p.title("test application");
      std::string appPath = "CodeAnalyzer.exe";
      p.application(appPath);
      std::string cmdLine = "CodeAnalyzer.exe ";
      cmdLine += searchPath + " ";
      cmdLine += "*.h *.cpp /m /r /f";
      p.commandLine(cmdLine);

      std::cout << "\n  starting process: \"" << appPath << "\"";
      std::cout << "\n  with this cmdlne: \"" << cmdLine << "\"";

      CBP callback = []() { std::cout << "\n  --- child process exited ---"; };
      p.setCallBackProcessing(callback);

      if (!p.create())
      {
        std::cout << "\n  can't start process";
      }
      p.registerCallback();

      std::string filePath = searchPath + "\\" + /*msg.value("codeAnalysis")*/ "logfile.txt";
      std::string fullSrcPath = FileSystem::Path::getFullFileSpec(filePath);
      std::string fullDstPath = sendFilePath;
      if (!FileSystem::Directory::exists(fullDstPath))
      {
        std::cout << "\n  file destination path does not exist";
        return reply;
      }
      fullDstPath += std::string("\\") + /*msg.value("codeAnalysis")*/ "logfile.txt";
      FileSystem::File::copy(fullSrcPath, fullDstPath);
    }
    else
    {
      std::cout << "\n  getDirs message did not define a path attribute";
    }
    return reply;
  };
}


using namespace MsgPassingCommunication;

int main()
{
  SetConsoleTitleA("Project4Sample Server Console");

  std::cout << "\n  Testing Server Prototype";
  std::cout << "\n ==========================";

  sendFilePath = FileSystem::Directory::createOnPath("./SendFiles");
  saveFilePath = FileSystem::Directory::createOnPath("./SaveFiles");

  Server server(serverEndPoint, "ServerPrototype");

  // may decide to remove Context
  MsgPassingCommunication::Context* pCtx = server.getContext();
  pCtx->saveFilePath = saveFilePath;
  pCtx->sendFilePath = sendFilePath;

  server.start();

  server.addMsgProc("echo", echo);
  server.addMsgProc("getFiles", getFiles);
  server.addMsgProc("getDirs", getDirs);
  server.addMsgProc("sendFile", sendFile);
  server.addMsgProc("checkin", checkin);
  server.addMsgProc("codeAnalyze", codeAnalyze);
  server.addMsgProc("serverQuit", echo);
  server.addMsgProc("getCheckOutDirs", getDirs);
  server.addMsgProc("getCheckOutFiles", getFiles);
  server.addMsgProc("performCheckOut", performCheckOut);
  server.addMsgProc("connect", connection);
  server.addMsgProc("queryByName", queryByName);
  server.addMsgProc("queryByCategory", queryByCategory);
  server.addMsgProc("queryByVersion", queryByVersion);
  server.addMsgProc("queryByDependants", queryByDependants);
  server.addMsgProc("viewMetadata", viewMetadata);
  server.processMessages();
  Msg msg(serverEndPoint, serverEndPoint);  // send to self
  msg.name("msgToSelf");

  /////////////////////////////////////////////////////////////////////
  // Additional tests here, used during development
  //
  //msg.command("echo");
  //msg.attribute("verbose", "show me");
  //server.postMessage(msg);
  //std::this_thread::sleep_for(std::chrono::milliseconds(1000));

  //msg.command("getFiles");
  //msg.remove("verbose");
  //msg.attributes()["path"] = storageRoot;
  //server.postMessage(msg);
  //std::this_thread::sleep_for(std::chrono::milliseconds(1000));

  //msg.command("getDirs");
  //msg.attributes()["path"] = storageRoot;
  //server.postMessage(msg);
  //std::this_thread::sleep_for(std::chrono::milliseconds(1000));

  std::cout << "\n  press enter to exit\n";
  std::cin.get();
  std::cout << "\n";

  msg.command("serverQuit");
  server.postMessage(msg);
  server.stop();
  return 0;
}

