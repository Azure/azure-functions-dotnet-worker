#ifndef FUNC_WORKER
#define FUNC_WORKER

#include "funcgrpc_handlers.h"
#include <future>
#include <iostream>

using namespace AzureFunctionsRpc;

namespace funcgrpc
{

class GrpcWorkerStartupOptions
{
  public:
    GrpcWorkerStartupOptions() = default;
    ;
    std::string host;
    int port;
    std::string workerId;
    std::string requestId;
    int grpcMaxMessageLength;
};
} // namespace funcgrpc

#endif