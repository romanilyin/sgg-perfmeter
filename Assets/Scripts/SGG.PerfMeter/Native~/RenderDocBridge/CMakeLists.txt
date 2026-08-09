cmake_minimum_required(VERSION 3.25)

project(SggRenderDocBridge VERSION 1.0 LANGUAGES C CXX)

if(NOT WIN32)
    message(FATAL_ERROR "PM-RDOC-002 requires a Windows x64 toolchain.")
endif()

if(NOT CMAKE_SIZEOF_VOID_P EQUAL 8)
    message(FATAL_ERROR "PM-RDOC-002 requires a Windows x64 target (8-byte pointers).")
endif()

set(RENDERDOC_HEADER_URL
    "https://raw.githubusercontent.com/baldurk/renderdoc/7db2264afa00a5313154022f8c4ae0628a641300/renderdoc/api/app/renderdoc_app.h")
set(RENDERDOC_HEADER_SHA256
    "b7005e7dc34c3635046868bbd76d81b9b055aede0f56daa0bd39fedee0639ffb")
set(RENDERDOC_HEADER_DIR "${CMAKE_BINARY_DIR}/_deps/renderdoc")
set(RENDERDOC_HEADER "${RENDERDOC_HEADER_DIR}/renderdoc_app.h")

file(MAKE_DIRECTORY "${RENDERDOC_HEADER_DIR}")
file(DOWNLOAD
    "${RENDERDOC_HEADER_URL}"
    "${RENDERDOC_HEADER}"
    EXPECTED_HASH "SHA256=${RENDERDOC_HEADER_SHA256}"
    TLS_VERIFY ON
    STATUS RENDERDOC_DOWNLOAD_STATUS)
list(GET RENDERDOC_DOWNLOAD_STATUS 0 RENDERDOC_DOWNLOAD_CODE)
if(NOT RENDERDOC_DOWNLOAD_CODE EQUAL 0)
    list(GET RENDERDOC_DOWNLOAD_STATUS 1 RENDERDOC_DOWNLOAD_MESSAGE)
    message(FATAL_ERROR
        "Pinned RenderDoc app header download or SHA-256 verification failed: ${RENDERDOC_DOWNLOAD_MESSAGE}")
endif()

set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)
set(CMAKE_CXX_EXTENSIONS OFF)
set(CMAKE_C_STANDARD 11)
set(CMAKE_C_STANDARD_REQUIRED ON)
set(CMAKE_C_EXTENSIONS OFF)

set(BRIDGE_INCLUDE_DIR "${CMAKE_CURRENT_SOURCE_DIR}/include")
set(BRIDGE_SOURCE "${CMAKE_CURRENT_SOURCE_DIR}/src/sgg_renderdoc_bridge.cpp")
set(BRIDGE_RENDERDOC_INCLUDE_DIR "${RENDERDOC_HEADER_DIR}")

add_library(sgg_renderdoc_bridge SHARED "${BRIDGE_SOURCE}")
target_include_directories(sgg_renderdoc_bridge
    PRIVATE
        "${BRIDGE_INCLUDE_DIR}"
        "${BRIDGE_RENDERDOC_INCLUDE_DIR}")
target_compile_definitions(sgg_renderdoc_bridge PRIVATE SGG_RD_BUILD_DLL=1)
target_compile_options(sgg_renderdoc_bridge PRIVATE /W4 /WX /permissive- /EHsc)
target_link_libraries(sgg_renderdoc_bridge PRIVATE kernel32)

include(CTest)

if(BUILD_TESTING)
    add_library(sgg_renderdoc_bridge_c_abi OBJECT
        "${CMAKE_CURRENT_SOURCE_DIR}/tests/renderdoc_bridge_c_abi.c")
    target_include_directories(sgg_renderdoc_bridge_c_abi PRIVATE "${BRIDGE_INCLUDE_DIR}")
    target_compile_options(sgg_renderdoc_bridge_c_abi PRIVATE /W4 /WX /permissive-)

    add_executable(sgg_renderdoc_bridge_tests
        "${CMAKE_CURRENT_SOURCE_DIR}/tests/renderdoc_bridge_tests.cpp"
        "${BRIDGE_SOURCE}")
    target_include_directories(sgg_renderdoc_bridge_tests
        PRIVATE
            "${BRIDGE_INCLUDE_DIR}"
            "${BRIDGE_RENDERDOC_INCLUDE_DIR}"
            "${CMAKE_CURRENT_SOURCE_DIR}/tests")
    target_compile_definitions(sgg_renderdoc_bridge_tests
        PRIVATE
            SGG_RD_BUILD_DLL=1
            SGG_RD_TESTING=1)
    target_compile_options(sgg_renderdoc_bridge_tests PRIVATE /W4 /WX /permissive- /EHsc)
    target_link_libraries(sgg_renderdoc_bridge_tests PRIVATE kernel32)
    add_test(NAME sgg_renderdoc_bridge_fake_table COMMAND sgg_renderdoc_bridge_tests)

    add_executable(sgg_renderdoc_bridge_live_probe
        "${CMAKE_CURRENT_SOURCE_DIR}/tests/renderdoc_bridge_live_probe.cpp")
    target_include_directories(sgg_renderdoc_bridge_live_probe PRIVATE "${BRIDGE_INCLUDE_DIR}")
    target_compile_options(sgg_renderdoc_bridge_live_probe PRIVATE /W4 /WX /permissive- /EHsc)
    target_link_libraries(sgg_renderdoc_bridge_live_probe PRIVATE sgg_renderdoc_bridge)
endif()
