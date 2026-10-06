#include <iostream>
#include <cassert>
#include "../../core/lyrics/LyricsSynchronizer.h"

using namespace openreceiver::lyrics;

void testLyricsSynchronizer() {
    ParsedLyrics lyrics;
    lyrics.lines.push_back({10.0, "Line 1"});
    lyrics.lines.push_back({20.0, "Line 2"});
    lyrics.lines.push_back({30.0, "Line 3"});
    lyrics.lines.push_back({40.0, "Line 4"});

    LyricsSynchronizer sync(lyrics);

    // Test before first line
    assert(sync.getActiveLineIndex(5.0) == -1);
    assert(!sync.getActiveLine(5.0).has_value());

    // Test exactly on first line
    assert(sync.getActiveLineIndex(10.0) == 0);
    assert(sync.getActiveLine(10.0)->text == "Line 1");

    // Test between lines
    assert(sync.getActiveLineIndex(25.0) == 1);
    assert(sync.getActiveLine(25.0)->text == "Line 2");

    // Test exactly on last line
    assert(sync.getActiveLineIndex(40.0) == 3);
    assert(sync.getActiveLine(40.0)->text == "Line 4");

    // Test way past last line (should stick to last line)
    assert(sync.getActiveLineIndex(100.0) == 3);

    // Test surrounding lines
    auto context = sync.getSurroundingLines(1, 1); // target "Line 2", context 1 (should get lines 0, 1, 2)
    assert(context.size() == 3);
    assert(context[0].text == "Line 1");
    assert(context[1].text == "Line 2");
    assert(context[2].text == "Line 3");

    // Edge case: context hitting the end boundary
    auto contextEnd = sync.getSurroundingLines(3, 2);
    assert(contextEnd.size() == 3); // lines 1, 2, 3
    assert(contextEnd[2].text == "Line 4");

    std::cout << "testLyricsSynchronizer PASSED\n";
}

int main() {
    testLyricsSynchronizer();
    std::cout << "All LyricsSynchronizer tests passed successfully.\n";
    return 0;
}
