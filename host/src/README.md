
This is the project root where we have our root level CMakeLists.txt.
### Load & Build. 

Open this directory in Visual studio(Open a local folder).
VS will read the CMakeLists.txt and start executing CMake.
We use [vcpkg](https://vcpkg.io) for dependency management. When CMake execution starts, it will start downloading the dependencies to the build output directory.This may take a while to finish the first time.

Dependencies are listed in the vcpkg.json file.

Once CMake generation is done, build the code by Build-> Build All (or F6 key). It will do compilation and linking.
