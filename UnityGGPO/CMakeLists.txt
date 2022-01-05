cmake_minimum_required(VERSION 3.2)

include_directories("./include/")
include_directories("../ggpo/src/include/")
include_directories("../ggpo/src/lib/ggpo/")

file(GLOB_RECURSE SOURCE_FILES "./src/*.cpp" "./src/*.h")

add_library(UnityGGPO MODULE ${SOURCE_FILES})

#add_definitions(-D_UNICODE -DUNICODE)
target_link_libraries(UnityGGPO LINK_PUBLIC GGPO winmm.lib ws2_32.lib)

install(TARGETS UnityGGPO DESTINATION "../Unity/Packages/UnityGGPO/Plugins/Windows/x86_64")
