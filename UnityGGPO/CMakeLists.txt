cmake_minimum_required(VERSION 3.2)

include_directories("./include/")

file(GLOB_RECURSE SOURCE_FILES "./src/*.cpp" "./src/*.h")

add_library(UnityGGPO MODULE ${SOURCE_FILES})

#add_definitions(-D_UNICODE -DUNICODE)
target_link_libraries(UnityGGPO LINK_PUBLIC ggpo winmm.lib ws2_32.lib)

install(TARGETS UnityGGPO DESTINATION "c:/Code/Projects/UnityGGPO/Unity/Assets/UnityGGPO/Plugins/Windows/x86_64")
