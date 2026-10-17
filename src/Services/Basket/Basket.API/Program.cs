using Common.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using EventBus.Messages.Common;
using EventBus.Messages.Events;
using JasperFx;
using JasperFx.Resources;
using Wolverine;
using Wolverine.RabbitMQ;
using Wolverine.SqlServer;

namespace Basket.API
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
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Executes the CreateHostBuilder operation.
        /// </summary>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog(SeriLogger.Configure)
                .UseWolverine(options =>
                {
                    var rabbitAddress = System.Environment.GetEnvironmentVariable("EventBusSettings:HostAddress")
                        ?? System.Environment.GetEnvironmentVariable("EventBusSettings__HostAddress")
                        ?? throw new System.InvalidOperationException("EventBusSettings:HostAddress must be configured.");
                    var sqlConnection = System.Environment.GetEnvironmentVariable("ConnectionStrings__BasketMessagingConnectionString")
                        ?? throw new System.InvalidOperationException("ConnectionStrings:BasketMessagingConnectionString must be configured.");
                    options.UseRabbitMq(new System.Uri(rabbitAddress))
                        .AutoProvision();
                    options.PersistMessagesWithSqlServer(
                        sqlConnection, "wolverine");
                    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                    options.Services.AddResourceSetupOnStartup();
                    options.Discovery.IncludeAssembly(typeof(Program).Assembly);
                    options.ListenToRabbitQueue(EventBusConstants.BasketWorkflowQueue).UseDurableInbox();
                    options.PublishMessage<BasketCheckoutEvent>()
                        .ToRabbitQueue(EventBusConstants.BasketCheckoutQueue);
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
