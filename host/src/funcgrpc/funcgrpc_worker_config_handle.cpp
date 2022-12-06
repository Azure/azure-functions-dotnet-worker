
#include "funcgrpc_worker_config_handle.h"
#include "func_log.h"
#include "func_perf_marker.h"
#include "rapidjson/document.h"
#include "rapidjson/filereadstream.h"
std::string funcgrpc::WorkerConfigHandle::GetApplicationExePath(const std::string &dir)
{
    std::string fullPath;
    try
    {
        funcgrpc::FuncPerfMarker mark("WorkerConfigHandle->GetApplicationExePath");

        fullPath = dir + "/worker.config.json";

        fp = fopen(fullPath.c_str(), "r");
        char readBuffer[65536];
        rapidjson::FileReadStream is(fp, readBuffer, sizeof(readBuffer));

        rapidjson::Document doc;
        doc.ParseStream(is);
        closeFileHandle();

        if (doc.HasParseError())
        {
            FUNC_LOG_ERROR("Error parsing {} to rapidJson::Document.", fullPath);
        }
        std::string workerPath = doc["description"]["defaultWorkerPath"].GetString();
        std::string exePath = dir + "/" + workerPath;

        return exePath;
    }
    catch (std::exception &ex)
    {
        FUNC_LOG_ERROR("Error parsing {} to rapidJson::Document.", fullPath);
        throw;
    }
}
funcgrpc::WorkerConfigHandle::~WorkerConfigHandle()
{
    closeFileHandle();
}
void funcgrpc::WorkerConfigHandle::closeFileHandle()
{
    if (fp != nullptr)
    {
        fclose(fp);
        fp = nullptr;
    }
}
