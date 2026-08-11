using NSubstitute;
using Shouldly;
using Soraeru.Application.Abstractions.Persistence;
using Soraeru.Application.Auth;
using Soraeru.Application.Quota;

namespace Soraeru.Application.Tests.Auth;

public sealed class MeServiceDeleteAccountTests
{
    private readonly IUserRepository _users = Substitute.For<IUserRepository>();
    private readonly IWordCardRepository _cards = Substitute.For<IWordCardRepository>();
    private readonly IQuotaService _quota = Substitute.For<IQuotaService>();
    private readonly MeService _sut;

    public MeServiceDeleteAccountTests()
    {
        _sut = new MeService(_users, _quota, _cards);
    }

    [Fact]
    public async Task DeleteAccountAsync_deletes_cloud_notebook_then_user()
    {
        var userId = Guid.Parse("55555555-5555-5555-5555-555555555555");
        var user = new UserRecord(
            userId,
            "learner@example.com",
            "hash",
            null,
            "Learner",
            "Free",
            20,
            "Zhuyin",
            false,
            true,
            DateTimeOffset.Parse("2026-01-01T00:00:00Z"));

        _users.FindByIdAsync(userId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.DeleteAccountAsync(userId);

        result.IsSuccess.ShouldBeTrue();
        result.Value.ShouldBeTrue();
        await _cards.Received(1).DeleteAllByUserAsync(userId, Arg.Any<CancellationToken>());
        await _users.Received(1).DeleteAsync(userId, Arg.Any<CancellationToken>());
    }
}
