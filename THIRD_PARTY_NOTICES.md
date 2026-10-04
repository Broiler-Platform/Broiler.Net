# Third-party notices

Broiler.Net's own code is licensed under the Apache License 2.0 (see `LICENSE`).
It also contains the third-party data below, which keeps its own license.

## Public Suffix List

`src/Broiler.Net/Data/public_suffix_list.dat` is the Public Suffix List from
[publicsuffix/list](https://github.com/publicsuffix/list), at the revision recorded
in `public-suffix-list.json`. Its original license header is preserved. The list is
provided under the [Mozilla Public License 2.0](https://mozilla.org/MPL/2.0/).
The embedded resource and the packaged `data/` file contain the same unmodified bytes.

`tests/Broiler.Net.Tests/Fixtures/test_psl.txt` comes from the same upstream revision.
Its original [CC0 public-domain dedication](https://creativecommons.org/publicdomain/zero/1.0/)
is preserved. Tests canonicalize expected IDN results to ASCII for this API. The fixture
is not part of the package.

## web-platform-tests vectors

`tests/Broiler.Net.Tests/Fixtures/wpt/` holds four JSON vector files from
[web-platform-tests](https://github.com/web-platform-tests/wpt), at the commit recorded in its
`README.md`: the `data:` URL and forgiving-base64 vectors from `fetch/data-urls/resources` and the
MIME type vectors from `mimesniff/mime-types/resources`. They are unmodified and stay under their
[3-Clause BSD License](https://github.com/web-platform-tests/wpt/blob/master/LICENSE.md), a copy of
which is beside them. They are test fixtures and are not part of the package.

No upstream cookie implementation or Web Platform Test code is copied into this package.
