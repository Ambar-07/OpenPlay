#include <iostream>
#include <cassert>
#include <fstream>
#include "../../core/lyrics/LocalLrcProvider.h"
#include "../../core/state/Models.h"

using namespace openreceiver::lyrics;
using namespace openreceiver::state;

void testLocalLrcProvider() {
    // Create a mock LRC file in the current directory
    std::string testDir = "./";
    std::string filename = "Rick Astley - Never Gonna Give You Up.lrc";
    std::ofstream testFile(testDir + filename);
    testFile << "[ti:Never Gonna Give You Up]\n[00:18.78] We're no strangers to love\n";
    testFile.close();

    LocalLrcProvider provider(testDir);
    
    TrackInfo track;
    track.title = "Never Gonna Give You Up";
    track.artist = "Rick Astley";
    
    LyricsResult result = provider.findLyrics(track);
    
    assert(result.status == LyricsStatus::FOUND);
    assert(result.syncedLyrics.has_value());
    assert(result.syncedLyrics->title == "Never Gonna Give You Up");
    assert(result.syncedLyrics->lines.size() == 1);
    assert(result.syncedLyrics->lines[0].text == "We're no strangers to love");
    
    // Test missing file gracefully returns NOT_FOUND
    TrackInfo missingTrack;
    missingTrack.title = "Missing Song";
    missingTrack.artist = "Unknown Artist";
    LyricsResult missingResult = provider.findLyrics(missingTrack);
    assert(missingResult.status == LyricsStatus::NOT_FOUND);

    // Cleanup
    std::remove((testDir + filename).c_str());

    std::cout << "testLocalLrcProvider PASSED\n";
}

int main() {
    testLocalLrcProvider();
    std::cout << "All Local LRC Provider tests passed successfully.\n";
    return 0;
}
