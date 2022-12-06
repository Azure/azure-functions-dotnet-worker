#pragma once

#include "spdlog/sinks/stdout_color_sinks.h"
#include "spdlog/spdlog.h"
#include <iostream>
#include <memory>

namespace funcgrpc
{
class Log
{
  public:
    static void Init();

  private:
    static std::shared_ptr<spdlog::logger> funcLogger;
};
} // namespace funcgrpc

#if defined(_DEBUG) || defined(DEBUG)
#define FUNC_LOG_TRACE(...) spdlog::get("FunctionsNetHost")->trace(__VA_ARGS__)
#define FUNC_LOG_DEBUG(...) spdlog::get("FunctionsNetHost")->debug(__VA_ARGS__)
#define FUNC_LOG_INFO(...) spdlog::get("FunctionsNetHost")->info(__VA_ARGS__)
#define FUNC_LOG_WARN(...) spdlog::get("FunctionsNetHost")->warn(__VA_ARGS__)
#define FUNC_LOG_ERROR(...) spdlog::get("FunctionsNetHost")->error(__VA_ARGS__)
#elif defined(NDEBUG)
#define FUNC_LOG_TRACE(...) (void)0
#define FUNC_LOG_DEBUG(...) (void)0
#define FUNC_LOG_INFO(...) spdlog::get("FunctionsNetHost")->info(__VA_ARGS__)
#define FUNC_LOG_WARN(...) spdlog::get("FunctionsNetHost")->warn(__VA_ARGS__)
#define FUNC_LOG_ERROR(...) spdlog::get("FunctionsNetHost")->error(__VA_ARGS__)
#endif