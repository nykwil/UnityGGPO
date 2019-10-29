cmake_minimum_required(VERSION 3.2)

# TODO: SDK Versioning.
project(ggpo VERSION 1.0.0)

# What do we want to build?
option(BUILD_GGPO "Enable the build of the GGPO SDK" ON)
option(BUILD_VECTORWAR "Enable the build of the Vector War example app" ON)
option(BUILD_SHARED_LIBS "Enable the build of shared libraries (.dll/.so) instead of static ones (.lib/.a)" OFF)
option(BUILD_UNITY_DLL "Unity DLL" ON)

set(GGPO_SDK
	ggpo/src/include/ggponet.h
	ggpo/src/lib/ggpo/main.cpp
	ggpo/src/lib/ggpo/bitvector.h
	ggpo/src/lib/ggpo/bitvector.cpp
	ggpo/src/lib/ggpo/game_input.h
	ggpo/src/lib/ggpo/game_input.cpp
	ggpo/src/lib/ggpo/input_queue.h
	ggpo/src/lib/ggpo/input_queue.cpp
	ggpo/src/lib/ggpo/log.h
	ggpo/src/lib/ggpo/log.cpp
	ggpo/src/lib/ggpo/poll.h
	ggpo/src/lib/ggpo/poll.cpp
	ggpo/src/lib/ggpo/ring_buffer.h
	ggpo/src/lib/ggpo/static_buffer.h
	ggpo/src/lib/ggpo/sync.h
	ggpo/src/lib/ggpo/sync.cpp
	ggpo/src/lib/ggpo/timesync.h
	ggpo/src/lib/ggpo/timesync.cpp
	ggpo/src/lib/ggpo/types.h
	ggpo/src/lib/ggpo/zconf.h
	ggpo/src/lib/ggpo/zlib.h
	ggpo/src/lib/ggpo/backends/backend.h
	ggpo/src/lib/ggpo/backends/p2p.h
	ggpo/src/lib/ggpo/backends/p2p.cpp
	ggpo/src/lib/ggpo/backends/spectator.h
	ggpo/src/lib/ggpo/backends/spectator.cpp
	ggpo/src/lib/ggpo/backends/synctest.h
	ggpo/src/lib/ggpo/backends/synctest.cpp
	ggpo/src/lib/ggpo/network/udp.h
	ggpo/src/lib/ggpo/network/udp.cpp
	ggpo/src/lib/ggpo/network/udp_msg.h
	ggpo/src/lib/ggpo/network/udp_proto.h
	ggpo/src/lib/ggpo/network/udp_proto.cpp
)

if(BUILD_GGPO)
	add_library(ggpo ${GGPO_SDK})
	
	target_include_directories(ggpo PUBLIC ggpo/src/include)
	target_include_directories(ggpo PUBLIC ggpo/src/lib/ggpo)

	if(WIN32 AND BUILD_SHARED_LIBS)
		# Link to Multimedia API and Winsocks during a static build.
		target_link_libraries(ggpo LINK_PUBLIC winmm.lib ws2_32.lib)
	endif()
	
	set_target_properties(ggpo PROPERTIES VERSION ${PROJECT_VERSION})
endif()

if(BUILD_VECTORWAR)
	# Vector War is windows only.
	if(WIN32)
		add_subdirectory(ggpo/src/apps/vectorwar)
	else()
		message(WARNING "The Vector War app only supports windows, skipping...")
	endif()
endif()

if(BUILD_UNITY_DLL)
	add_subdirectory(UnityGGPO)
endif()