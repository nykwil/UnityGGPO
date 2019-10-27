# Specify the minimum version for CMake
cmake_minimum_required(VERSION 3.2)
# Plugins's name
project(GGPOPlugin)

#Build type
#set(CMAKE_BUILD_TYPE Release)

#Include Unity plugin API
include_directories("/Program Files/Unity/Hub/Editor/2019.3.0b3/Editor/Data/PluginAPI/")
include_directories("${CMAKE_CURRENT_SOURCE_DIR}/include/")

# Find all .c, .cc and .cpp files in the source folder
file(GLOB_RECURSE SOURCE_FILES "${CMAKE_CURRENT_SOURCE_DIR}/src/*.cpp" "${CMAKE_CURRENT_SOURCE_DIR}/include/*.h")

target_link_libraries(GGPOPlugin LINK_PUBLIC ggpo)

add_library(GGPOPlugin MODULE ${SOURCE_FILES})
install(TARGETS GGPOPlugin DESTINATION "${CMAKE_CURRENT_SOURCE_DIR}/../Unity/Assets/GGPOPlugin/Plugins/Windows/x86_64")
