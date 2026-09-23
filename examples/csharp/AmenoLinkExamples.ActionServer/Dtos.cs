namespace AmenoLinkExamples.ActionServer;

public record User(string Name, string BirthDate);

public record UserAstrology(string Name, string BirthDate, string WeekDay, string Sign);
