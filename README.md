# API Catalog

## Tasks

* Build binary output for DB
    - Instead of building binary DB while indexing, we should model it as a
      conversion from SQLite to binary. This allows us to read the data in any
      order that's convenient for creating the binary representation.
    - Experimental testing showed huge wins when we change the way we store
      syntax. Instead of the XML markup, we can use special characters < 32 to
      indicate specific tokens. Also, instead of emitting the API GUID, we can
      emit the API ID, as a four character encoding of the Int32 value. And in
      addition, we can write all tokens as an interned string, when it's more
      five characters (we need 1 character to indicate an interned ID follows
      and four more characters to encode the ID). This mean instead of 215 MB
      we only need 80 MB for the string table. That dropped from 250 MB to
      126 MB total.
* Build an object-model over the binary data so we don't need to use SQLite
  when serving requests
* Combine Suffix Tree with binary data so we can use a combined table for 
  all interned strings
* Add NuGet, API Port, and Compat Lab usage
* Expose what's necessary to reference a given assembly (framework reference,
  SDK, and property)

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