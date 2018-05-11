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
#include "../CppNoSqlDb/DbCore/DbCore.h"
#include "../CppNoSqlDb/PayLoad/PayLoad.h"
#include <regex>
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"

class FileRepository {
public:
	static NoSqlDb::DbElement<NoSqlDb::PayLoad> generateMetadata(std::string&, std::string&, std::string&, std::string&, std::string&,NoSqlDb::CheckinStatus&, std::vector<std::string>&);
	static NoSqlDb::DbCore<NoSqlDb::PayLoad> database_;
private:
	static std::string removeVersionNumber(const std::string&);
	static int getCurrentVersion(std::string& package, std::string& fileName);
	static std::string getDestPath(std::string& package, std::string& fileName, int& version);
	static std::string generateCheckOutKey(std::string & package, std::string& fileName);
};

//Helper to generate metadata for database entry

NoSqlDb::DbElement<NoSqlDb::PayLoad> FileRepository::generateMetadata(std::string &package, std::string &fileName, std::string &descrip, std::string &author, std::string &category, NoSqlDb::CheckinStatus& status, std::vector<std::string>&)
{
	NoSqlDb::DbElement<NoSqlDb::PayLoad> element;
	element.name(fileName);
	element.descrip(descrip);
	element.author("User from port " + author);
	int version = getCurrentVersion(package, fileName);
	element.payLoad().value(getDestPath(package, fileName, version));
	element.payLoad().categories().push_back(category);
	element.payLoad().checkInStatus(status);
	return element;
}

//Helper function to get the current highest version of the file in namespace
int FileRepository::getCurrentVersion(std::string& package, std::string& fileName)
{
	std::regex mapper(package + "::" + fileName);
	int currentVersion = 0;
	for (auto iterator : database_)
	{
		if (std::regex_search(iterator.first, mapper))
			currentVersion++;
	}
	if (currentVersion == 0)
		currentVersion = 1;
	return currentVersion;
}

//Helper to get the destination path in the repository
std::string FileRepository::getDestPath(std::string& package, std::string& fileName, int& version)
{
	if (!FileSystem::Directory::exists("../Storage/" + package))
		FileSystem::Directory::create("../Storage/" + package);
	std::string dest = "../Storage/" + package + "/" + fileName + "." + std::to_string(version);
	return dest;
}
