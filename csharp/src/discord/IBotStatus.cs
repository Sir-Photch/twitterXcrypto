using Discord;

namespace twitterXcrypto.discord;

internal interface IBotStatus : IActivity
{
    public UserStatus UserStatus { get; }
}

internal readonly struct TwitterStatus : IBotStatus
{
    public string Name { get; init; };

    public ActivityType Type => ActivityType.Watching;

    public ActivityProperties Flags => ActivityProperties.None;

    public string Details { get; init; }; // unused??

    public UserStatus UserStatus { get; init; }
}
