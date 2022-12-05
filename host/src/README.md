
This is the project root where we have our root level CMakeLists.txt.
### Load & Build. 

Open this directory in Visual studio(Open a local folder).
VS will read the CMakeLists.txt and start executing CMake.
We use [vcpkg](https://vcpkg.io) for dependency management. When CMake execution starts, it will start downloading the dependencies to the build output directory.This may take a while to finish the first time.

Dependencies are listed in the vcpkg.json file.

Once CMake generation is done, build the code by Build-> Build All (or F6 key). It will do compilation and linking.

### Testing with worker harness.

Update harness setting with sample files from tools\harness-testing. Update the EXE path as needed. 

Run harness(F5). It should:

1. Start the process(FunctionNetHost.exe).
2. Wait for Start stream call from client.
3. Validates start stream message.
4. Send worker init request.
5. Validates worker init response.
6. Send function metadata request (worker indexing request).
7. Validates the result (This is currently failing though we still receive the message from client. I think I set an incorrect prop in the response message. Let me investigate)


More scenario files are [here](https://github.com/Azure/azure-functions-host/tree/features/harness/tools/WorkerHarness/samples/scenarios)