﻿namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record StartCartEvent(Guid CorrelationId, List<SagaCartItem> Items) : CorrelatedBy<Guid>;