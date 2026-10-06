#pragma once
#include <string>
#include <optional>
#include "LyricModels.h"
#include "../state/Models.h"

namespace openreceiver {
namespace lyrics {

enum class LyricsStatus {
    FOUND,
    NOT_FOUND,
    PROVIDER_ERROR
};

struct LyricsResult {
    LyricsStatus status = LyricsStatus::NOT_FOUND;
    std::string source;
    std::optional<ParsedLyrics> syncedLyrics;
    std::string errorMessage;
};

class ILyricsProvider {
public:
    virtual ~ILyricsProvider() = default;
    virtual LyricsResult findLyrics(const state::TrackInfo& track) = 0;
};

} // namespace lyrics
} // namespace openreceiver
