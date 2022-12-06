# CMakeList.txt : CMake project for FunctionsNetHost, include source and define
# project specific logic here.
#
add_executable (FunctionsNetHost "main.cpp"  "managedexports.cpp")
target_link_libraries(FunctionsNetHost PRIVATE funcgrpc)

set_target_properties(FunctionsNetHost PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS TRUE)

if (CMAKE_VERSION VERSION_GREATER 3.12)
  set_property(TARGET FunctionsNetHost PROPERTY CXX_STANDARD 20)
endif()

find_package(Boost REQUIRED COMPONENTS program_options)
target_link_libraries(FunctionsNetHost PRIVATE Boost::program_options)

if(NOT TARGET spdlog)
    # Stand-alone build
    find_package(spdlog REQUIRED)
endif()
target_link_libraries(FunctionsNetHost PRIVATE spdlog::spdlog)