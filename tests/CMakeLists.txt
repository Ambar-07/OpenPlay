enable_testing()
# Test harness configuration
add_executable(AirPlaySessionTest core/AirPlaySessionTest.cpp)
target_link_libraries(AirPlaySessionTest PRIVATE core_lib)
add_test(NAME AirPlaySessionTest COMMAND AirPlaySessionTest)

add_executable(ModelsTest state/ModelsTest.cpp)
target_link_libraries(ModelsTest PRIVATE core_lib)
add_test(NAME ModelsTest COMMAND ModelsTest)

add_executable(MetadataTest metadata/MetadataTest.cpp)
target_link_libraries(MetadataTest PRIVATE core_lib)
add_test(NAME MetadataTest COMMAND MetadataTest)

add_executable(StateManagerTest state/StateManagerTest.cpp)
target_link_libraries(StateManagerTest PRIVATE core_lib)
add_test(NAME StateManagerTest COMMAND StateManagerTest)

add_executable(LrcParserTest lyrics/LrcParserTest.cpp)
target_link_libraries(LrcParserTest PRIVATE core_lib)
add_test(NAME LrcParserTest COMMAND LrcParserTest)

add_executable(LocalLrcProviderTest lyrics/LocalLrcProviderTest.cpp)
target_link_libraries(LocalLrcProviderTest PRIVATE core_lib)
add_test(NAME LocalLrcProviderTest COMMAND LocalLrcProviderTest)
