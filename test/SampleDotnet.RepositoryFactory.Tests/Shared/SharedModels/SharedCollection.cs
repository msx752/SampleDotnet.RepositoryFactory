﻿namespace SampleDotnet.RepositoryFactory.Tests.Shared.SharedModels;

[CollectionDefinition("Shared Collection")]
public class SharedCollection : ICollectionFixture<SharedContainerFixture>
{
    // This class has no code, and is never created. Its purpose is simply
    // to be the place to apply [CollectionDefinition] and all the
    // ICollectionFixture<> interfaces.
}