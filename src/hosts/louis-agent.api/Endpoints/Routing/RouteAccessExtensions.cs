namespace LouisAgent.Api.Endpoints.Routing;

/// <summary>Marks a route with who may call it, and reads the mark back; the mark is endpoint metadata, so the route carries it.</summary>
internal static class RouteAccessExtensions
{
    /// <summary>Marks the route, or every route of the group, as <paramref name="access"/>.</summary>
    public static TBuilder WithAccess<TBuilder>(this TBuilder builder, RouteAccess access) where TBuilder : IEndpointConventionBuilder =>
        builder.WithMetadata(access);

    /// <summary>The route's mark (the last one set, so a route can narrow its group's); <see cref="RouteAccess.Key"/>, the enum's default, when it has none.</summary>
    public static RouteAccess AccessOf(Endpoint? endpoint) =>
        endpoint?.Metadata.OfType<RouteAccess>().LastOrDefault() ?? default;
}
