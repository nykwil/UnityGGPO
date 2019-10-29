cmake_minimum_required(VERSION 3.2)

# TODO: SDK Versioning.
project(GGPO VERSION 1.0.0)

# Remove RelWithDebInfo and MinSizeRelease build types to match the original Visual Studio Project. 
set(CMAKE_CONFIGURATION_TYPES "Debug;Release" CACHE STRING "" FORCE)

# target_include_directories(GGPO PUBLIC ggpo/src/include)
# target_include_directories(GGPO PUBLIC ggpo/src/lib/ggpo)

include(ggpo/src/cmake/properties.cmake)
include(ggpo/src/cmake/helper_methods.cmake)

if(WIN32)
	## Prevent the CMake trying to install GGPO in Program Files on Windows
	if (CMAKE_INSTALL_PREFIX_INITIALIZED_TO_DEFAULT)
		set (CMAKE_INSTALL_PREFIX "${CMAKE_BINARY_DIR}/installed" CACHE PATH "Default install path" FORCE)
	endif()
endif()

add_subdirectory(ggpo/src)
add_subdirectory(ggpo/src/apps/vectorwar)
add_subdirectory(UnityGGPO)
