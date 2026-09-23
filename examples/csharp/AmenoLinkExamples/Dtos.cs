namespace AmenoLinkExamples;

public record User(string Name, string BirthDate);

public record Talk(string Author, string Text, bool Reply = false);

public record UserAstrology(string Name, string BirthDate, string WeekDay, string Sign);
