using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Moq;

namespace Worker.Extensions.SignalRService.Tests;
public class ServerlessHubDependencyInjectionTests
{
    [Fact]
    public void AddServerlessHubTest()
    {
        var services = new ServiceCollection()
            .AddServerlessHub<ChatRoom>()
            .AddSingleton(Mock.Of<IConfiguration>());
        var sp = services.BuildServiceProvider();
        var chatRoom = ActivatorUtilities.CreateInstance<ChatRoom>(sp);
        Assert.NotNull(chatRoom);
    }

    [Fact]
    public void AddStronglyTypedServerlessHubTest()
    {
        var services = new ServiceCollection()
            .AddServerlessHub<StronglyTypedChatRoom>()
            .AddSingleton(Mock.Of<IConfiguration>());
        var sp = services.BuildServiceProvider();
        var chatRoom = ActivatorUtilities.CreateInstance<StronglyTypedChatRoom>(sp);
        Assert.NotNull(chatRoom);
    }

    [Fact]
    public void AddServerlessHubIdempotencyTest()
    {
        var services = new ServiceCollection()
            .AddServerlessHub<ChatRoom>()
            .AddServerlessHub<ChatRoom>()
            .AddSingleton(Mock.Of<IConfiguration>());
        var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService));
        Assert.Single(hostedServices);
        var sp = services.BuildServiceProvider();
        var chatRoom = ActivatorUtilities.CreateInstance<ChatRoom>(sp);
        Assert.NotNull(chatRoom);
    }

    [Fact]
    public void AddStronglyTypedServerlessHubIdempotencyTest()
    {
        var services = new ServiceCollection()
            .AddServerlessHub<StronglyTypedChatRoom>()
            .AddServerlessHub<StronglyTypedChatRoom>()
            .AddSingleton(Mock.Of<IConfiguration>());
        var hostedServices = services.Where(d => d.ServiceType == typeof(IHostedService));
        Assert.Single(hostedServices);
        var sp = services.BuildServiceProvider();
        var StronglyTypedChatRoom = ActivatorUtilities.CreateInstance<StronglyTypedChatRoom>(sp);
        Assert.NotNull(StronglyTypedChatRoom);
    }

    public class ChatRoom : ServerlessHub
    {
        public ChatRoom(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }
    }

    public class StronglyTypedChatRoom : ServerlessHub<IChatRoom>
    {
        public StronglyTypedChatRoom(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

    }

    public interface IChatRoom
    {
        Task Say(string word);
    }
}
