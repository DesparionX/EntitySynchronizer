# EntitySynchronizer
Simple helper to manipulate collections with EF Core made by me as an exercise after finishing C# Masterclass course.
Its using custom interfaces IEntity and IDTO that ensures providen entities has ID and its a value type, and DTOs bound to the given entity.
It also uses IMapper for easily converting DTOs to entities and ILogger for customized logging system.
