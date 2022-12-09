find_package(protobuf CONFIG REQUIRED)
find_package(gRPC CONFIG REQUIRED)
find_package(Threads)

# We cannot directly refer the proto files in the repo root as that dir is not a CMAKE Source dir.
# So we will copy protos files to a relative location.
get_filename_component(HOST_DIR ${CMAKE_SOURCE_DIR} DIRECTORY)
get_filename_component(REPO_ROOT ${HOST_DIR} DIRECTORY)
SET (PROTOS_TARGET_DIR ${CMAKE_SOURCE_DIR}/Protos)
SET (PROTOS_SRC_DIR ${REPO_ROOT}/protos/azure-functions-language-worker-protobuf/src/proto)

# Copy proto files to the "Protos" directory(where this CMakeList.txt file is present).
file(COPY ${PROTOS_SRC_DIR}/shared/NullableTypes.proto DESTINATION ${PROTOS_TARGET_DIR}/shared)
file(COPY ${PROTOS_SRC_DIR}/identity/ClaimsIdentityRpc.proto DESTINATION ${PROTOS_TARGET_DIR}/identity)
file(COPY ${PROTOS_SRC_DIR}/FunctionRpc.proto DESTINATION ${PROTOS_TARGET_DIR})

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