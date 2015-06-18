# MongoDBCompare
Comparison library for comparing data in two MongoDB instances using .net

This is availabe on NuGet at https://www.nuget.org/packages/JimBobBennett.MongoDbCompare/

To use this, create a new instace of the `MongoDbComparer<T>` class using the object that is stored in your collection as the argument.  
Call `CompareAsync()` passing in a func that returns the unique key that can be used to marry up records from the two different collections to get a list of differences.
The differences are built up in memory, so don't use this for gigantic collections that would exceed your available memory space.
