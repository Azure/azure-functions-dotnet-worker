find_package(protobuf CONFIG REQUIRED)
find_package(gRPC CONFIG REQUIRED)
find_package(Threads)

#
# Protobuf/Grpc source files for function RPC
#
set(PROTO_FILES  
    FunctionRpc.proto
    shared/NullableTypes.proto
    identity/ClaimsIdentityRpc.proto
)


#
# Add Library target with protobuf sources
#
add_library(func_protos ${PROTO_FILES})
target_link_libraries(func_protos
    PUBLIC
        protobuf::libprotobuf
        gRPC::grpc
        gRPC::grpc++
)

target_include_directories(func_protos PUBLIC ${CMAKE_CURRENT_BINARY_DIR})

#
# Compile protobuf and grpc files
#
get_target_property(grpc_cpp_plugin_location gRPC::grpc_cpp_plugin LOCATION)
protobuf_generate(TARGET func_protos LANGUAGE cpp)
protobuf_generate(TARGET func_protos LANGUAGE grpc GENERATE_EXTENSIONS .grpc.pb.h .grpc.pb.cc PLUGIN "protoc-gen-grpc=${grpc_cpp_plugin_location}")