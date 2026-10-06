#include <iostream>
#include <cassert>
#include <vector>
#include <cstring>
#include "../../core/metadata/MetadataParser.h"
#include "../../core/metadata/ArtworkCache.h"
#include "../../core/metadata/BackgroundGenerator.h"

using namespace openreceiver::metadata;

// Helper to construct mock DMAP payload
void appendTag(std::vector<uint8_t>& buf, const std::string& tag, const std::string& val) {
    for (char c : tag) buf.push_back(c);
    uint32_t len = val.length();
    buf.push_back((len >> 24) & 0xFF);
    buf.push_back((len >> 16) & 0xFF);
    buf.push_back((len >> 8) & 0xFF);
    buf.push_back(len & 0xFF);
    for (char c : val) buf.push_back(c);
}

void appendIntTag(std::vector<uint8_t>& buf, const std::string& tag, uint32_t val) {
    for (char c : tag) buf.push_back(c);
    uint32_t len = 4;
    buf.push_back(0); buf.push_back(0); buf.push_back(0); buf.push_back(4);
    buf.push_back((val >> 24) & 0xFF);
    buf.push_back((val >> 16) & 0xFF);
    buf.push_back((val >> 8) & 0xFF);
    buf.push_back(val & 0xFF);
}

void testMetadataParser() {
    std::vector<uint8_t> payload;
    appendTag(payload, "minm", "Test Track");
    appendTag(payload, "asar", "Test Artist");
    appendTag(payload, "asal", "Test Album");
    appendIntTag(payload, "astm", 215000); // 215 seconds

    auto track = MetadataParser::parse(payload);
    
    assert(track.title == "Test Track");
    assert(track.artist == "Test Artist");
    assert(track.album == "Test Album");
    assert(track.duration == 215.0);

    std::cout << "testMetadataParser PASSED\n";
}

void testArtworkCache() {
    ArtworkCache cache;
    std::string trackId = "track_123";
    std::vector<uint8_t> artData = {0xFF, 0xD8, 0xFF, 0xE0}; // Fake JPEG header
    
    assert(!cache.get(trackId).has_value());
    
    cache.put(trackId, artData);
    auto retrieved = cache.get(trackId);
    assert(retrieved.has_value());
    assert(retrieved.value() == artData);
    
    cache.clear();
    assert(!cache.get(trackId).has_value());

    std::cout << "testArtworkCache PASSED\n";
}

void testBackgroundGenerator() {
    auto& generator = BackgroundGenerator::getInstance();
    std::string trackId = "track_bg_123";
    std::vector<uint8_t> dummyImage = {0xFF, 0xD8};

    // First call should run asynchronously and cache
    auto futureColors = generator.generateAsync(trackId, dummyImage);
    BackgroundColors colors = futureColors.get();
    
    assert(colors.primary.r == 43);
    assert(colors.secondary.r == 17);

    // Second call should hit the cache
    auto cachedFuture = generator.generateAsync(trackId, dummyImage);
    BackgroundColors cachedColors = cachedFuture.get();
    
    assert(cachedColors.primary.r == 43);
    
    generator.clearCache();
    std::cout << "testBackgroundGenerator PASSED\n";
}

int main() {
    testMetadataParser();
    testArtworkCache();
    testBackgroundGenerator();
    std::cout << "All metadata tests passed successfully.\n";
    return 0;
}
