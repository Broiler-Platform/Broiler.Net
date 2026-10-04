# web-platform-tests data

Files copied unchanged from [web-platform-tests](https://github.com/web-platform-tests/wpt) at
commit `13748c36fdf9dd77cd0b34dd6b244256b365ea60`, under their own license (the 3-Clause BSD
License, in `LICENSE.md` here). They are test fixtures and are not part of the package.

| File | Used by |
| --- | --- |
| `fetch/data-urls/resources/base64.json` | `ForgivingBase64Tests`, and `DataUrlTests` as WPT's `base64.any.js` applies them (`data:;base64,` + input) |
| `fetch/data-urls/resources/data-urls.json` | `DataUrlTests`: the `data:` URL processor's serialized MIME types and bodies |
| `mimesniff/mime-types/resources/mime-types.json` | `MimeTypeTests`: parsing and serializing MIME types (the `navigable`, `encoding` and `minimizedMIMEType` fields are not used) |
| `mimesniff/mime-types/resources/generated-mime-types.json` | `MimeTypeTests`: the same, over the generated vectors |

Broiler.HTML, Broiler.Layout and Broiler.HtmlBridge carry the same two `fetch/data-urls` files for
their own `data:` URL tests.

To update them, copy the files from a newer commit and change the commit above.
