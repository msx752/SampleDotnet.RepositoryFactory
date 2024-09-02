using Docker.DotNet.Models;
using MassTransit;
using Microsoft.AspNetCore.Cors.Infrastructure;
using SampleDotnet.RepositoryFactory.Entities.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleDotnet.RepositoryFactory.Tests.TestModels.Sagas
{
    public record StartCart(Guid CorrelationId, List<SagaCartItem> Items) : CorrelatedBy<Guid>;

}
