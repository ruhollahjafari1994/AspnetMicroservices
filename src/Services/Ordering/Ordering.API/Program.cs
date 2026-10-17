using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using Ordering.API.Extensions;
using Ordering.Infrastructure.Persistence;
using Serilog;
using Common.Logging;
using EventBus.Messages.Common;
using EventBus.Messages.Events;
using JasperFx;
using JasperFx.Resources;
using Ordering.API.Workflows;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;
using Wolverine.EntityFrameworkCore;
using Wolverine.RDBMS;
using Wolverine.Configuration;

namespace Ordering.API
{
    /// <summary>
    /// Defines the Program type.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// Describes the Main operation.
        /// </summary>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args)
                .Build()
                .MigrateDatabase<OrderContext>((context, services) =>
                    {
                        var logger = services.GetService<ILogger<OrderContextSeed>>();
                        OrderContextSeed
                            .SeedAsync(context, logger)
                            .Wait();
                    })
                .Run();
        }

        /// <summary>
        /// Creates the host builder for the ordering service.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException"></exception>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog(SeriLogger.Configure)
                .UseWolverine(options =>
                {
                    var rabbitAddress = System.Environment.GetEnvironmentVariable("EventBusSettings:HostAddress")
                        ?? System.Environment.GetEnvironmentVariable("EventBusSettings__HostAddress")
                        ?? throw new System.InvalidOperationException("EventBusSettings:HostAddress must be configured.");
                    var sqlConnection = System.Environment.GetEnvironmentVariable("ConnectionStrings:OrderingConnectionString")
                        ?? System.Environment.GetEnvironmentVariable("ConnectionStrings__OrderingConnectionString")
                        ?? throw new System.InvalidOperationException("ConnectionStrings:OrderingConnectionString must be configured.");

                    options.UseRabbitMq(new System.Uri(rabbitAddress)).AutoProvision();
                    options.PersistMessagesWithSqlServer(sqlConnection, "wolverine");
                    options.UseEntityFrameworkCoreTransactions();
                    options.Policies.AutoApplyTransactions();
                    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                    options.MessagePartitioning
                        .UseInferredMessageGrouping()
                        .ByMessage<BasketCheckoutEvent>(message => message.Id.ToString());
                    options.Discovery.IncludeAssembly(typeof(CheckoutSaga).Assembly);
                    options.AddSagaType<CheckoutSaga>();
                    options.Services.AddResourceSetupOnStartup();

                    options.ListenToRabbitQueue(EventBusConstants.BasketCheckoutQueue)
                        .UseDurableInbox()
                        .PartitionProcessingByGroupId(PartitionSlots.Five);
                    options.ListenToRabbitQueue(EventBusConstants.OrderSagaQueue)
                        .UseDurableInbox()
                        .PartitionProcessingByGroupId(PartitionSlots.Five);
                    options.ListenToRabbitQueue(EventBusConstants.OrderSagaEventsQueue)
                        .UseDurableInbox()
                        .PartitionProcessingByGroupId(PartitionSlots.Five);

                    options.PublishMessage<CancelOrderSaga>()
                        .ToRabbitQueue(EventBusConstants.OrderSagaQueue);
                    options.PublishMessage<BeginCheckoutSaga>()
                        .ToRabbitQueue(EventBusConstants.OrderSagaQueue);
                    options.PublishMessage<ClearBasketForOrder>()
                        .ToRabbitQueue(EventBusConstants.BasketWorkflowQueue);
                    options.PublishMessage<RestoreBasketForOrder>()
                        .ToRabbitQueue(EventBusConstants.BasketWorkflowQueue);
                    options.PublishMessage<BasketClearedForOrder>()
                        .ToRabbitQueue(EventBusConstants.OrderSagaEventsQueue);
                    options.PublishMessage<BasketRestoredForOrder>()
                        .ToRabbitQueue(EventBusConstants.OrderSagaEventsQueue);
                })
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                });
    }
}
