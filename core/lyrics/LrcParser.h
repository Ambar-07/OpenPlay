#pragma once
#include <string>
#include "LyricModels.h"

namespace openreceiver {
namespace lyrics {

class LrcParser {
public:
    static ParsedLyrics parse(const std::string& lrcContent);
};

} // namespace lyrics
} // namespace openreceiver
