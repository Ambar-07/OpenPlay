#include <iostream>
#include <cassert>
#include "../../core/lyrics/LrcParser.h"

using namespace openreceiver::lyrics;

void testLrcParser() {
    std::string mockLrc = 
        "[ti:Never Gonna Give You Up]\n"
        "[ar:Rick Astley]\n"
        "[00:00.00] \n"
        "[00:18.78] We're no strangers to love\n"
        "[00:22.83][00:25.00] You know the rules and so do I\n";

    auto parsed = LrcParser::parse(mockLrc);
    
    assert(parsed.isValid);
    assert(parsed.title == "Never Gonna Give You Up");
    assert(parsed.artist == "Rick Astley");
    assert(parsed.lines.size() == 4);
    
    // Check timestamps and sorting
    assert(parsed.lines[0].timestamp == 0.0);
    assert(parsed.lines[1].timestamp == 18.78);
    assert(parsed.lines[1].text == "We're no strangers to love");
    assert(parsed.lines[2].timestamp == 22.83); // First part of repeated tag
    assert(parsed.lines[2].text == "You know the rules and so do I");
    assert(parsed.lines[3].timestamp == 25.0);  // Second part of repeated tag
    assert(parsed.lines[3].text == "You know the rules and so do I");

    std::cout << "testLrcParser PASSED\n";
}

int main() {
    testLrcParser();
    std::cout << "All lyrics tests passed successfully.\n";
    return 0;
}
