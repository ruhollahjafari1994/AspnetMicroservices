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
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .UseSerilog(SeriLogger.Configure)
                .UseWolverine(options =>
                {
                    var rabbitAddress = System.Environment.GetEnvironmentVariable("EventBusSettings:HostAddress")
                        ?? System.Environment.GetEnvironmentVariable("EventBusSettings__HostAddress")
                        ?? "amqp://guest:guest@localhost:5672";
                    var sqlConnection = System.Environment.GetEnvironmentVariable("ConnectionStrings:OrderingConnectionString")
                        ?? System.Environment.GetEnvironmentVariable("ConnectionStrings__OrderingConnectionString")
                        ?? "Server=orderdb;Database=OrderDb;User Id=sa;Password=SwN12345678;TrustServerCertificate=True";
                    options.UseRabbitMq(new System.Uri(rabbitAddress))
                        .AutoProvision();
                    options.PersistMessagesWithSqlServer(
                        sqlConnection, "basket_wolverine");
                    options.Policies.UseDurableOutboxOnAllSendingEndpoints();
                    options.Services.AddResourceSetupOnStartup();
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
