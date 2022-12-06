// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

#include "../funcgrpc/func_bidi_reactor.h"
#include "../funcgrpc/func_perf_marker.h"
#include "../funcgrpc/funcgrpc.h"
#include "appconfig.h"
#include <boost/program_options.hpp>
#include <exception>
#include <iostream>
#include <limits.h>
#include <string>

using namespace std;
using namespace funcgrpc;
namespace po = boost::program_options;

unique_ptr<funcgrpc::GrpcWorkerStartupOptions> getWorkerStartupOptions(int argc, char *const *argv);

int main(int argc, char *argv[])
{
    funcgrpc::Log::Init();
    FUNC_LOG_INFO("Starting FunctionsNetHost main.Build:{}", FunctionsNetHost_VERSION);

    try
    {
        auto pOptions = getWorkerStartupOptions(argc, argv);
        auto pApplication = std::make_unique<NativeHostApplication>();
        auto worker = std::make_unique<FunctionBidiReactor>(pOptions.get(), pApplication.get());
        Status status = worker->Await();

        if (!status.ok())
        {
            FUNC_LOG_ERROR("Rpc failed. error_message:{}", status.error_message());
        }
    }
    catch (const std::exception &ex)
    {
        FUNC_LOG_ERROR("Caught unknown exception.{}", ex.what());
    }
    catch (...)
    {
        FUNC_LOG_ERROR("Caught unknown exception.");
    }

    return 0;
}

unique_ptr<GrpcWorkerStartupOptions> getWorkerStartupOptions(int argc, char *const *argv)
{
    FuncPerfMarker marker("BuildWorkerStartupOptions");

    po::options_description desc("Allowed options");
    desc.add_options()("help", "sample usage: FunctionsNetHost --host <endpoint> --port <port> --workerid <workerid> "
                               "--requestid <requestid> --grpcmaxrequestlength <maxrequestlength>")(
        "host", boost::program_options::value<string>(),
        "Address of grpc server")("port", po::value<int>(), "Port number of grpc server connection")(
        "workerId", boost::program_options::value<string>(),
        "Worker id")("requestId", boost::program_options::value<string>(),
                     "Request id")("grpcMaxMessageLength", po::value<int>()->default_value(INT_MAX),
                                   "Max length for grpc messages. Default is INT_MAX");

    po::variables_map vm;
    po::store(po::parse_command_line(argc, argv, desc), vm);
    po::notify(vm);

    auto options = make_unique<GrpcWorkerStartupOptions>();

    if (vm.count("host"))
    {
        options->host = vm["host"].as<string>();
    }
    if (vm.count("port"))
    {
        options->port = vm["port"].as<int>();
    }
    if (vm.count("workerId"))
    {
        options->workerId = vm["workerId"].as<string>();
    }
    if (vm.count("requestId"))
    {
        options->requestId = vm["requestId"].as<string>();
    }
    if (vm.count("grpcMaxMessageLength"))
    {
        options->grpcMaxMessageLength = vm["grpcMaxMessageLength"].as<int>();
    }

    return options;
}
