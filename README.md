# API Catalog

## UI

* The table still looks a bit ugly
* Grey out APIs that aren't available for the selected framework
* Add syntax highlighting for numbers and strings
* We need to handle multiple package versions

## Indexing

* Handle display of constant enum and enum flags correctly
* Split parameters on separate lines
* Split base types on separate lines
* `typeof()` should use open generic syntax
* Hide internal attributes
* Hide attributes which reference internal types in `typeof()` expressions
* Sort namespaces, types, and members
* Index extension methods
* Exclude boilerplate attributes
	- CompilerGenerated
	- TargedPatchingOptOut
	- Nullable
	- NullableContext

## DB

* Externalize syntax strings
* Consider externalizing API strings
* Figure out how we an efficiently lookup qualified URLs