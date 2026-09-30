// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   8
// Annotated:        8/8
// Exempt:           3
// Human-reviewed:   0/8
// IP risk:          Low
// Security risk:    High
// Criteria:         7/5
// Resource impact:  2/10 max
// Unverified:       8
//
// GENERATED - DO NOT EDIT MANUALLY

using Broiler.Net.Sites;

namespace Broiler.Net.Http;

/// <summary>
/// Immutable identity of a document for request and cookie decisions. <see cref="DocumentUrl"/> is the URL
/// whose cookies the document uses (the creator's URL for about:blank and srcdoc documents), never the
/// HTML base URL. <see cref="Origin"/> may be opaque (sandboxed or data: documents). Frames are children
/// of the document that contains them, so the ancestor chain is <see cref="Parent"/> up to <see cref="TopLevel"/>.
/// </summary>
// Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=C641B7
// Broiler-Falsified-If: a context built by CreateChild resolves TopLevel to anything other than the context at the root of its Parent chain
// Broiler-Human:        PENDING
public sealed class DocumentRequestContext
{
    // Broiler-AI:           Origin=AI; IP=Low; Security=Medium; Resources=0; Fingerprint=CAC31F
    // Broiler-Falsified-If: a relative document URL is accepted, so IsCookieAverse later throws when it reads the Scheme
    // Broiler-Human:        PENDING
    private DocumentRequestContext(Uri documentUrl, Origin origin, DocumentRequestContext? parent)
    {
        ArgumentNullException.ThrowIfNull(documentUrl);
        if (!documentUrl.IsAbsoluteUri) throw new ArgumentException("The document URL must be absolute.", nameof(documentUrl));
        (DocumentUrl, Origin, Parent) = (documentUrl, origin, parent);
    }

    public Uri DocumentUrl { get; }
    public Origin Origin { get; }
    public DocumentRequestContext? Parent { get; }
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=0; Fingerprint=54F38D
    // Broiler-Falsified-If: a context created by CreateChild reports IsTopLevel true
    // Broiler-Human:        PENDING
    public bool IsTopLevel => Parent is null;

    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=2; Fingerprint=1A950A
    // Broiler-Falsified-If: for a frame nested two levels deep TopLevel returns its parent instead of the context that has no Parent
    // Broiler-Human:        PENDING
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
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=0; Fingerprint=A356BD
    // Broiler-Falsified-If: a document at a file: URL reports IsCookieAverse false, so document.cookie reaches the cookie store for it
    // Broiler-Human:        PENDING
    public bool IsCookieAverse => DocumentUrl.Scheme is not ("http" or "https");

    /// <summary>A document in a top-level traversable. The origin defaults to the URL's origin.</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=F012DD
    // Broiler-Falsified-If: an opaque origin passed for a sandboxed top-level document is replaced by the origin of its URL
    // Broiler-Human:        PENDING
    public static DocumentRequestContext CreateTopLevel(Uri documentUrl, Origin? origin = null) =>
        new(documentUrl, origin ?? Origin.FromUrl(documentUrl), null);

    /// <summary>A document nested in this one (iframe, frame, object, embed).</summary>
    // Broiler-AI:           Origin=AI; IP=Low; Security=High; Resources=1; Fingerprint=E290C4
    // Broiler-Falsified-If: the returned context has no Parent, so a cross-site frame is treated as a top-level document for cookies
    // Broiler-Human:        PENDING
    public DocumentRequestContext CreateChild(Uri documentUrl, Origin? origin = null) =>
        new(documentUrl, origin ?? Origin.FromUrl(documentUrl), this);

    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=19B360
    // Broiler-Human:        PENDING
    public override string ToString() => $"Document({Origin}, top-level={IsTopLevel})";
}
