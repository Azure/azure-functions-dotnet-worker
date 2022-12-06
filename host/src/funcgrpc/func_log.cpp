#include "func_log.h"

namespace funcgrpc
{
std::shared_ptr<spdlog::logger> Log::funcLogger;

void Log::Init()
{
    funcLogger = spdlog::stdout_color_mt("FunctionsNetHost");
    spdlog::set_pattern("LanguageWorkerConsoleLog%^[%H:%M:%S.%e] [%l] %n: %v%$");
    funcLogger->set_level(spdlog::level::info);

#if defined(_DEBUG) || defined(DEBUG)
    funcLogger->flush_on(spdlog::level::trace);
#elif defined(NDEBUG)
    funcLogger->flush_on(spdlog::level::warn);
#endif
}
} // namespace funcgrpc