using Broiler.Net.Sites;

namespace Broiler.Net.Http;

/// <summary>
/// Immutable identity of a document for request and cookie decisions. <see cref="DocumentUrl"/> is the URL
/// whose cookies the document uses (the creator's URL for about:blank and srcdoc documents), never the
/// HTML base URL. <see cref="Origin"/> may be opaque (sandboxed or data: documents). Frames are children
/// of the document that contains them, so the ancestor chain is <see cref="Parent"/> up to <see cref="TopLevel"/>.
/// </summary>
public sealed class DocumentRequestContext
{
    private DocumentRequestContext(Uri documentUrl, Origin origin, DocumentRequestContext? parent)
    {
        ArgumentNullException.ThrowIfNull(documentUrl);
        if (!documentUrl.IsAbsoluteUri) throw new ArgumentException("The document URL must be absolute.", nameof(documentUrl));
        (DocumentUrl, Origin, Parent) = (documentUrl, origin, parent);
    }

    public Uri DocumentUrl { get; }
    public Origin Origin { get; }
    public DocumentRequestContext? Parent { get; }
    public bool IsTopLevel => Parent is null;

    public DocumentRequestContext TopLevel
    {
        get
        {
            var context = this;
            while (context.Parent is not null) context = context.Parent;
            return context;
        }
    }

    /// <summary>HTML "cookie-averse": documents whose URL is not HTTP(S) neither read nor write cookies.</summary>
    public bool IsCookieAverse => DocumentUrl.Scheme is not ("http" or "https");

    /// <summary>A document in a top-level traversable. The origin defaults to the URL's origin.</summary>
    public static DocumentRequestContext CreateTopLevel(Uri documentUrl, Origin? origin = null) =>
        new(documentUrl, origin ?? Origin.FromUrl(documentUrl), null);

    /// <summary>A document nested in this one (iframe, frame, object, embed).</summary>
    public DocumentRequestContext CreateChild(Uri documentUrl, Origin? origin = null) =>
        new(documentUrl, origin ?? Origin.FromUrl(documentUrl), this);

    public override string ToString() => $"Document({Origin}, top-level={IsTopLevel})";
}
