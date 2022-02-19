// This file is used by Code Analysis to maintain SuppressMessage
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given
// a specific target and scoped to a namespace, type, member, etc.

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Usage", "VSTHRD101:Avoid unsupported async delegates", Justification = "Avoid async lambdas that might throw")]
[assembly: SuppressMessage("Usage", "VSTHRD110:Observe result of async calls", Justification = "Not necessary", Scope = "member", Target = "~M:twitterXcrypto.discord.DiscordClient.WriteAsync(System.String)~System.Threading.Tasks.Task")]
