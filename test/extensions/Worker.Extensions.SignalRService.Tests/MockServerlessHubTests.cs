using Microsoft.AspNetCore.SignalR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.SignalRService;
using Microsoft.Azure.SignalR.Management;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace Worker.Extensions.SignalRService.Tests;

/// <summary>
/// Tests how to mock the <see cref="ServerlessHub"/> from the user's perspective.
/// </summary>
public class MockServerlessHubTests
{
    public class ChatRoom : ServerlessHub
    {
        [ActivatorUtilitiesConstructor]
        public ChatRoom(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public ChatRoom(ServiceHubContext serviceHubContext) : base(serviceHubContext)
        {
        }

        public async Task Say([SignalRTrigger(nameof(ChatRoom), "messages", nameof(Say), "groupName", "words")] SignalRInvocationContext invocationContext, string groupName, string word)
        {
            await Clients.Group(groupName).SendAsync(nameof(Say), word);
        }
    }

    [Fact]
    public async Task TestChatRoom()
    {
        var mock = new Mock<IClientProxy>();
        var chatRoom = new ChatRoom(Mock.Of<ServiceHubContext>(context => context.Clients.Group("groupName") == mock.Object));
        var invocationContext = new SignalRInvocationContext
        {
            Arguments = new object[] { "roomName" },
            Category = SignalRInvocationCategory.Messages,
            Event = nameof(ChatRoom.Say),
            Hub = nameof(ChatRoom),
            ConnectionId = "connectionId",
            UserId = "userId"
        };
        await chatRoom.Say(invocationContext, "groupName", "word");
        mock.Verify(client => client.SendCoreAsync(nameof(ChatRoom.Say), new[] { "word" }, default), Times.Once);
    }

    public class StronglyTypedChatRoom : ServerlessHub<IChatRoom>
    {
        [ActivatorUtilitiesConstructor]
        public StronglyTypedChatRoom(IServiceProvider serviceProvider) : base(serviceProvider)
        {
        }

        public StronglyTypedChatRoom(ServiceHubContext<IChatRoom> serviceHubContext) : base(serviceHubContext)
        {
        }

        public async Task Say([SignalRTrigger(nameof(ChatRoom), "messages", nameof(Say), "groupName", "words")] SignalRInvocationContext invocationContext, string groupName, string word)
        {
            await Clients.Group(groupName).Say(word);
        }
    }

    public interface IChatRoom
    {
        Task Say(string word);
    }

    [Fact]
    public async Task TestStronglyTypedRoom()
    {
        var mock = new Mock<IChatRoom>();
        var chatRoom = new StronglyTypedChatRoom(Mock.Of<ServiceHubContext<IChatRoom>>(context => context.Clients.Group("groupName") == mock.Object));
        var invocationContext = new SignalRInvocationContext
        {
            Arguments = new object[] { "roomName" },
            Category = SignalRInvocationCategory.Messages,
            Event = nameof(ChatRoom.Say),
            Hub = nameof(ChatRoom),
            ConnectionId = "connectionId",
            UserId = "userId"
        };
        await chatRoom.Say(invocationContext, "groupName", "word");
        mock.Verify(client => client.Say("word"), Times.Once);
    }
}
