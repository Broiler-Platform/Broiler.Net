// SPDX-FileCopyrightText: 2026 Broiler Platform contributors
// SPDX-License-Identifier: Apache-2.0
//
// Broiler Code Assurance
// ----------------------
// Relevant units:   2
// Annotated:        2/2
// Exempt:           0
// Human-reviewed:   0/2
// IP risk:          None
// Security risk:    Low
// Criteria:         1/0
// Resource impact:  1/10 max
// Unverified:       2
//
// GENERATED - DO NOT EDIT MANUALLY

namespace Broiler.Net.Cookies;

/// <summary>
/// One or more <see cref="CookieStore.Changed"/> observers failed after the mutation committed. Only
/// mutating operations throw it, after all of their work is done; the committed state is unaffected.
/// </summary>
// Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=1321F7
// Broiler-Human:        PENDING
public sealed class CookieObserverException : AggregateException
{
    // Broiler-AI:           Origin=AI; IP=None; Security=Low; Resources=1; Fingerprint=516803
    // Broiler-Falsified-If: an exception in the errors sequence is missing from InnerExceptions of the constructed instance
    // Broiler-Human:        PENDING
    public CookieObserverException(IEnumerable<Exception> errors)
        : base("Cookie change observer failed after commit.", errors) { }
}
