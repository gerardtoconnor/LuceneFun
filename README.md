# LuceneFun
F# Wrapper around Lucene &amp; AzureDirectory to persist data

**Experimental & Testing only, not to be used in production**

The library is a small basic wrapper around Lucene & AzureDiretory that allows use of local Lucene index as NoSql database with data synced & persisted to azure storage. 

On wiping of machine (webapp), the index is automatically pulled back in again from (cheap) azure storage and continues where left off.

### Helper Functions use:

[AzureDirectory](https://github.com/azure-contrib/AzureDirectory)

[Lucence.net](https://lucenenet.apache.org/)


