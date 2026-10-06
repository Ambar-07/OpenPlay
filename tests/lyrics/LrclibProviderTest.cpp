#include <iostream>
#include <cassert>
#include "../../core/lyrics/LrclibProvider.h"
#include "../../core/state/Models.h"

using namespace openreceiver::lyrics;
using namespace openreceiver::state;

void testLrclibProvider() {
    LrclibProvider provider;
    
    // 1. Test Mocked Successful API Response
    TrackInfo validTrack;
    validTrack.title = "Never Gonna Give You Up";
    validTrack.artist = "Rick Astley";
    validTrack.album = "Whenever You Need Somebody";
    validTrack.duration = 212.0;
    
    LyricsResult result = provider.findLyrics(validTrack);
    
    assert(result.status == LyricsStatus::FOUND);
    assert(result.syncedLyrics.has_value());
    assert(result.syncedLyrics->lines.size() == 2);
    assert(result.syncedLyrics->lines[0].text == "We're no strangers to love");
    assert(result.syncedLyrics->lines[1].text == "You know the rules and so do I");
    
    // 2. Test Caching (second request should hit memory Cache immediately)
    LyricsResult cachedResult = provider.findLyrics(validTrack);
    assert(cachedResult.status == LyricsStatus::FOUND);
    assert(cachedResult.syncedLyrics->lines.size() == 2);

    // 3. Test Missing Metadata
    TrackInfo missingTrack;
    missingTrack.title = "";
    LyricsResult errorResult = provider.findLyrics(missingTrack);
    assert(errorResult.status == LyricsStatus::NOT_FOUND);

    // 4. Test API Not Found (Mock returns empty for unknown artists)
    TrackInfo unknownTrack;
    unknownTrack.title = "Obscure Indie Song";
    unknownTrack.artist = "Unknown Artist";
    LyricsResult notFoundResult = provider.findLyrics(unknownTrack);
    assert(notFoundResult.status == LyricsStatus::NOT_FOUND);

    std::cout << "testLrclibProvider PASSED\n";
}

int main() {
    testLrclibProvider();
    std::cout << "All LRCLIB Provider tests passed successfully.\n";
    return 0;
}
