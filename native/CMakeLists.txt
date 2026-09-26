# The game's native (C++) GDExtensions: code the engine calls in its inner loops, where
# a call into C# costs too much (see CLAUDE.local.md, "No C# on engine-called hot
# paths"). Built by scripts/native-build.ps1.
cmake_minimum_required(VERSION 3.17)
project(mmo_native LANGUAGES CXX)

include(FetchContent)

# Pinned. godot-cpp has no 4.7 branch; an extension built against 4.5 loads in later 4.x
# engines, and nothing here needs an API newer than 4.5.
FetchContent_Declare(
    godot-cpp
    GIT_REPOSITORY https://github.com/godotengine/godot-cpp.git
    GIT_TAG godot-4.5-stable
    GIT_SHALLOW TRUE
)
FetchContent_MakeAvailable(godot-cpp)

get_target_property(GODOTCPP_SUFFIX godot-cpp GODOTCPP_SUFFIX)
set(OUTPUT_DIR "${CMAKE_CURRENT_SOURCE_DIR}/bin")

add_library(packet_log SHARED src/packet_log_peer.cpp src/register_types.cpp)
target_link_libraries(packet_log PRIVATE godot-cpp)

# The $<1:...> wrapper keeps multi-config generators from adding a config folder.
set_target_properties(
    packet_log
    PROPERTIES
        CXX_STANDARD 17
        CXX_EXTENSIONS OFF
        PREFIX ""
        OUTPUT_NAME "packet_log${GODOTCPP_SUFFIX}"
        RUNTIME_OUTPUT_DIRECTORY "$<1:${OUTPUT_DIR}>"
        LIBRARY_OUTPUT_DIRECTORY "$<1:${OUTPUT_DIR}>"
        PDB_OUTPUT_DIRECTORY "$<1:${OUTPUT_DIR}>"
)
