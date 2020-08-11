# API Catalog

## UI

* Maintain selected framework when navigating to other APIs
* We need to handle multiple package versions
* Add search

## Indexing

* Handle display of constant enum and enum flags correctly
* Handle arrays in attributes (see: /catalog/0d9154acade21b26d7bd5d06341ecdb7)
* Attributes in parameter lists cause issues
* Split parameters on separate lines
* Split base types on separate lines
* `typeof()` should use open generic syntax
* Hide attributes which reference internal types in `typeof()` expressions
* Index extension methods

## DB

* Figure out how we an efficiently lookup qualified URLs

## Packages

* Review package indexing errors, it seems we're not resolving packages correctly
* Filter packages more aggressively -- there is a lot of garbage