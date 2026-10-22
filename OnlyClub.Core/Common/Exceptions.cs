namespace OnlyClub.Core.Common;

public class DomainException(string m) : Exception(m);
public class NotFoundException(string m) : Exception(m);
public class ForbiddenException(string m) : Exception(m);