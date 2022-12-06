# Add source to this project's executable.
add_library(funcgrpc
        "funcgrpc_handlers.h"
        "funcgrpc.h"
        "func_bidi_reactor.h"
        "func_bidi_reactor.cpp"
        "handlers/funcgrpc_native_handler.h"
        "handlers/funcgrpc_native_handler.cpp"
        "messaging_channel.h"
        "messaging_channel.cpp"
        "nativehostapplication.cpp"
        "nativehostapplication.h"
        func_perf_marker.h 
        func_log.cpp 
        func_log.h
        funcgrpc_worker_config_handle.cpp
        funcgrpc_worker_config_handle.h
        byte_buffer_helper.cpp 
        byte_buffer_helper.h)

# set_target_properties(funcgrpc PROPERTIES ENABLE_EXPORTS TRUE)
# set_target_properties(funcgrpc PROPERTIES WINDOWS_EXPORT_ALL_SYMBOLS TRUE)

target_link_libraries(funcgrpc PUBLIC func_protos)

find_package(Boost REQUIRED COMPONENTS fiber)
target_link_libraries(funcgrpc PUBLIC Boost::fiber)

#find_package(Boost REQUIRED COMPONENTS json)
#target_link_libraries(funcgrpc PUBLIC Boost::json)

#find_package(nlohmann_json 3.2.0 REQUIRED)
#target_link_libraries(funcgrpc PRIVATE nlohmann_json::nlohmann_json)

find_package(unofficial-nethost CONFIG REQUIRED)
target_link_libraries(funcgrpc PRIVATE unofficial::nethost::nethost)

if (NOT TARGET spdlog)
    find_package(spdlog REQUIRED)
endif ()
target_link_libraries(funcgrpc PRIVATE spdlog::spdlog)