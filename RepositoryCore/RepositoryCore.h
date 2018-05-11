///////////////////////////////////////////////////////////////////////
// ServerPrototype.h - Console App that processes incoming messages  //
// ver 1.2                                                           //
// Jim Fawcett, CSE687 - Object Oriented Design, Spring 2018         //
///////////////////////////////////////////////////////////////////////
/*
*  Package Operations:
* ---------------------
*  Package that links the repository operations to NoSqlDb
*
*  Required Files:
* -----------------
*  DbCore.h,FileSystem.h
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
#pragma once
#include "../CppNoSqlDb/DbCore/DbCore.h"
#include "../FileSystem-Windows/FileSystemDemo/FileSystem.h"
#include <regex>
#include <algorithm>

using namespace NoSqlDb;
using namespace std;

template <typename T>
class RepositoryCore
{
	public:
		RepositoryCore();
		RepositoryCore(DbCore<T>& db);
		DbElement<T> generateMetadata(string&, string&, string&, string&, string&, CheckinStatus&, vector<string>&);
		bool performCheckIn(string&, string&, string&,string&,string&, CheckinStatus status = open, Keys dependants = {});
		bool performCheckOut(string& package, string& fileName, int& version);
	private:
		int getCurrentVersion(string& package, string& fileName);
		string removeVersionNumber(string&);
		Key generateDbKey(string&, string&, int& version);
		DbCore<T> database;
};

//Helper to remove version number
template <typename T>
string RepositoryCore<T>::removeVersionNumber(string& fileName)
{
	return (fileName.substr(0, fileName.find_last_of(".")));
}

//Function to validate checkout from database
template <typename T>
bool RepositoryCore<T>::performCheckOut(string& packageName, string& fileName, int& version)
{
	Key dbKey = generateDbKey(packageName, fileName, version);
	DbElement<T> elem = database[dbKey];
	if (elem.children().size() != 0)
	{
		for (auto child : elem.children())
		{
			DbElement<T> childElement = database[child];
			string childPath = "../SaveFile/" + FileSystem::Path::getName(removeVersionNumber(elem.payLoad().value()));
			FileSystem::File::copy(childElement.payLoad().value(), childPath);
		}
	}
	string destination = "../SaveFile/" + FileSystem::Path::getName(removeVersionNumber(elem.payLoad().value()));
	FileSystem::File::copy(elem.payLoad().value(), destination);
	return true;
}
//Helper to generate database key
template <typename T>
Key RepositoryCore<T>::generateDbKey(string& package, string& fileName, int& version)
{
	Key dbKey = package + "::" + fileName + "." + to_string(version);
	return dbKey;
}

//Function to validate checkIn request
template <typename T>
bool RepositoryCore<T>::performCheckIn(string& packageName, string& fileName,string& descrip,string& author,string& category,CheckinStatus status, Keys dependants)
{
	int fileVersion;
	if (status == closed)
	{
		for (auto dependant : dependants)
		{
			DbElement<T> childElement = database[dependant];
			if (childElement.payLoad().checkInStatus() == open)
			{
				std::cout << "\n\n Attempt to perform closed checkIn with open dependancy files. CheckIn failed\n\n";
				return false;
			}
			fileVersion = getCurrentVersion(packageName, fileName) + 1;
		}
	}
	else
		fileVersion = getCurrentVersion(packageName, fileName);
	Key dbKey = generateDbKey(packageName, fileName, fileVersion);
	DbElement<T> element = generateMetadata(packageName, fileName, descrip, author, category, status, dependants);
	database[dbKey] = element;
	string destinationPath = getDestPath(packageName, fileName, fileVersion);
	return FileSystem::File::copy("../SaveFile/" + fileName, destinationPath);
}



//Initialize repo database
template <typename T>
RepositoryCore<T>::RepositoryCore(DbCore<T>& db) : database(db) {}







