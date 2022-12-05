#ifndef FUNCTIONSNETHOST_FUNC_PERF_MARKER_H
#define FUNCTIONSNETHOST_FUNC_PERF_MARKER_H

#include "func_log.h"
#include <chrono>
#include <iostream>
#include <string>
#include <utility>
namespace funcgrpc
{
class FuncPerfMarker
{
  public:
    explicit FuncPerfMarker(const std::string &name)
    {
        _name = name;
        _start = std::chrono::high_resolution_clock::now();
    }

    ~FuncPerfMarker()
    {
        auto stop = std::chrono::high_resolution_clock::now();
        auto durationMs = duration_cast<std::chrono::milliseconds>(stop - _start);
        FUNC_LOG_INFO("{} elapsed: {}ms", _name, durationMs.count());
    }

  private:
    std::chrono::time_point<std::chrono::high_resolution_clock> _start;
    std::string _name;
};
} // namespace funcgrpc
#endif
