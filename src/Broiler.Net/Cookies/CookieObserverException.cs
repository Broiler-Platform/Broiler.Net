namespace Broiler.Net.Cookies;

/// <summary>
/// One or more <see cref="CookieStore.Changed"/> observers failed after the mutation committed. Only
/// mutating operations throw it, after all of their work is done; the committed state is unaffected.
/// </summary>
public sealed class CookieObserverException : AggregateException
{
    public CookieObserverException(IEnumerable<Exception> errors)
        : base("Cookie change observer failed after commit.", errors) { }
}
