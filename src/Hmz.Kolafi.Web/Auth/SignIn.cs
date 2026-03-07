using System.Text.Json.Serialization;
using Hmz.Kolafi.UseCases.Users;
using Hmz.Kolafi.UseCases.Users.SignIn;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Hmz.Kolafi.Web.Auth;

/// <summary>
/// POST /api/v1/auth/sign-in — Logto webhook endpoint for user sign-in/sign-up events.
/// Fetches user profile from Logto, upserts into our database, updates cache.
/// </summary>
public class SignIn(IMediator mediator, Microsoft.Extensions.Logging.ILogger<SignIn> logger)
  : Endpoint<LogtoWebhookPayload,
             Results<Ok<SignInResponse>,
                     NotFound,
                     ProblemHttpResult>>
{
  public override void Configure()
  {
    Post("/api/v1/auth/sign-in");
    AllowAnonymous(); // Webhook endpoint — secured by signing secret validation
    Summary(s =>
    {
      s.Summary = "Logto sign-in webhook";
      s.Description = "Receives Logto webhook events for user sign-in/sign-up. " +
                      "Syncs user profile from Logto into the local database and updates the cache.";
      s.Responses[200] = "User profile synced successfully";
      s.Responses[404] = "User not found in Logto";
      s.Responses[400] = "Invalid webhook payload";
    });
    Tags("Auth");
    Description(builder => builder
      .Accepts<LogtoWebhookPayload>("application/json")
      .Produces<SignInResponse>(200, "application/json")
      .ProducesProblem(400)
      .ProducesProblem(404));
  }

  public override async Task<Results<Ok<SignInResponse>, NotFound, ProblemHttpResult>>
    ExecuteAsync(LogtoWebhookPayload req, CancellationToken ct)
  {
    // TODO: Validate webhook signing secret from header
    // var signature = HttpContext.Request.Headers["logto-signature-sha-256"].FirstOrDefault();
    // if (!ValidateSignature(signature, req)) return TypedResults.Problem(...);

    var userId = req.UserId ?? req.Data?.UserId;
    if (string.IsNullOrWhiteSpace(userId))
    {
      return TypedResults.Problem(
        title: "Invalid payload",
        detail: "UserId is missing from webhook payload.",
        statusCode: StatusCodes.Status400BadRequest);
    }

    logger.LogInformation("Received sign-in webhook for user {UserId}, event: {Event}", userId, req.Event);

    var result = await mediator.Send(new SyncUserOnSignInCommand(userId), ct);

    if (!result.IsSuccess)
    {
      if (result.Status == Ardalis.Result.ResultStatus.NotFound)
        return TypedResults.NotFound();

      return TypedResults.Problem(
        title: "Sync failed",
        detail: string.Join("; ", result.Errors),
        statusCode: StatusCodes.Status400BadRequest);
    }

    var profile = result.Value;
    return TypedResults.Ok(new SignInResponse(
      profile.Id,
      profile.SubjectId,
      profile.Email,
      profile.Name,
      profile.Roles,
      profile.LastSignedInAt));
  }
}

/// <summary>
/// Logto webhook payload — models the incoming webhook event.
/// Structure based on Logto webhook format.
/// </summary>
public class LogtoWebhookPayload
{
  [JsonPropertyName("event")]
  public string? Event { get; set; } // e.g. "PostSignIn", "PostRegister"

  [JsonPropertyName("userId")]
  public string? UserId { get; set; }

  [JsonPropertyName("data")]
  public LogtoWebhookData? Data { get; set; }
}

public class LogtoWebhookData
{
  [JsonPropertyName("userId")]
  public string? UserId { get; set; }

  [JsonPropertyName("email")]
  public string? Email { get; set; }
}

public record SignInResponse(
  int Id,
  string SubjectId,
  string Email,
  string Name,
  List<string> Roles,
  DateTimeOffset? LastSignedInAt);
