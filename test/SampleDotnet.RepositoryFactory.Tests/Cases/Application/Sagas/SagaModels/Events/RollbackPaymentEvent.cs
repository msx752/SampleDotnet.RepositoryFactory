using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Consumers;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.DbContexts;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Entities;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Enums;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.EventMessages;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Interfaces;
using SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Services;
namespace SampleDotnet.RepositoryFactory.Tests.Cases.Application.Sagas.SagaModels.Events;

public record RollbackPaymentEvent(Guid CorrelationId) : CorrelatedBy<Guid>;