#pragma once
#include "ILyricsProvider.h"
#include <string>

namespace openreceiver {
namespace lyrics {

class LocalLrcProvider : public ILyricsProvider {
public:
    explicit LocalLrcProvider(const std::string& lyricsDirectory);
    ~LocalLrcProvider() override = default;

    LyricsResult findLyrics(const state::TrackInfo& track) override;

private:
    std::string lyricsDirectory_;
    std::string sanitizeFilename(const std::string& input) const;
};

} // namespace lyrics
} // namespace openreceiver
