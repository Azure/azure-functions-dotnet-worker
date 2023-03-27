#include <Windows.h>
#include <filesystem>
#include <iostream>
#include <wchar.h>

// TO DO: Need to revisit this to make it work cross platform.
std::string getCurrentDirectory()
{
    char buffer[MAX_PATH];
    GetModuleFileNameA(NULL, buffer, MAX_PATH);
    std::string::size_type pos = std::string(buffer).find_last_of("\\/");

    return std::string(buffer).substr(0, pos);
}