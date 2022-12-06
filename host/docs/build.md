# Build

Follow below instructions to build and develop locally.

## Quick Start: Windows

### Prerequisites


* [CMake](https://cmake.org/download/) is our build system. Install version 3.23 or above.
    * _If you are new to CMake, [Modern CMake for C++](https://learning.oreilly.com/library/view/modern-cmake-for/9781801070058/) is great book to start with._
* Visual Studio 2022 or above with [Desktop development with C++ workload](https://learn.microsoft.com/en-us/cpp/build/vscpp-step-0-installation?view=msvc-170).
* [Git](https://git-scm.com/downloads)

## Build

1. Clone the [dotnet-worker](https://github.com/Azure/azure-functions-dotnet-worker) repo.
2. Open a new instance of Visual Studio. Select "Open a local folder".
3. Select the "host/src" directory.

Once VS opens this directory it will detect it as a CMake project and will execute the CMake command. You can see the output of this operation in the "Output" window. Once this step is done, you should see "CMake generation finished." log entry in the output window.

## Package management

We use [vcpkg](https://vcpkg.io/en/index.html) as our package manager. vcpkg downloads the source code of our dependencies, build the source locally and use the binaries. If you are running CMake for the first time in your machine, this process will take quite sometime to finish. vcpkg caches the output of this process locally so that subsequent CMake generation step will be using the built binaries from cache and thus build will be faster.

## Debugging

Visual studio supports F5 debugging for CMake/C++ projects. You should be able to put breakpoints in the code and inspect.

## Formatting

We have a .clang-format file in the root which uses the "Microsoft" based formatting styles. In visual studio, you can use the key combination of CTRL + K + D to format a document.

## Visual Studio Code

To build the binaries after code change, Run the below commands.

```
// Create a directory which we will use for building.
>mkdir -p build\win-x64

> cd build\win-x64

// Execute CMake command, specify source directory as 2 directories back of the current directory. build tree directory as current directory.
> CMAKE -S ../../ -B .

// Once this step is done, you should see "Generating done" message in the terminal.

// Run cmake --build command which builds the binaries. Specify "Release" configuration using the --config flag.

> CMAKE --build . --config Release

```