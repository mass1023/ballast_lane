namespace RecordLabel.Application.Users;

public record RegisterRequest(
    string Username,
    string Password);

public record AuthenticateRequest(
    string Username,
    string Password);

public record AuthenticateResponse(
    Guid UserId,
    string Username,
    string Token,
    DateTime ExpiresAt);

public record UserDto(
    Guid Id,
    string Username);
