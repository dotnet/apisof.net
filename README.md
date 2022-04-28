# API Catalog

The site <apisof.net> is exposing which .NET platform APIs exist, which packages
are needed to use them, whether they are obsolete, and which operating system
they are supported for.

## Missing

* Expose what's necessary to reference a given assembly (framework reference,
  SDK, and property)
* Maintain selected framework when navigating to other APIs
* We need to handle multiple package versions
* `typeof()` should use open generic syntax
* Index extension methods
* Figure out how we an efficiently lookup qualified URLs
* Review package indexing errors, it seems we're not resolving packages correctly
