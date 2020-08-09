# API Catalog

## UI

* Maintain selected framework when navigating to other APIs
* We need to handle multiple package versions
* We should change the order of namespaces: System, Microsoft, Windows, the rest
* Add symbol-specific colorization
* Add tooltips for symbols

## Indexing

* Handle display of constant enum and enum flags correctly
* Handle operator methods
* Handle arrays in attributes (see: /catalog/0d9154acade21b26d7bd5d06341ecdb7)
* Attributes in parameter lists cause issues
* Split parameters on separate lines
* Split base types on separate lines
* `typeof()` should use open generic syntax
* Hide internal attributes
* Hide attributes which reference internal types in `typeof()` expressions
* Index extension methods
* Exclude boilerplate attributes
	- CompilerGenerated
	- TargedPatchingOptOut
	- Nullable
	- NullableContext
* It seems symbols with empty namespace don't end up in the root namespace

## DB

* Figure out how we an efficiently lookup qualified URLs