using InterfaceWatchDog.Core.Actions;

namespace InterfaceWatchDog.Tests.Core;

public class RestartResultTests
{
    [Fact]
    public void Ok_ShouldSetSuccessTrue()
    {
        var result = RestartResult.Ok(1234);
        result.Success.Should().BeTrue();
    }

    [Fact]
    public void Ok_ShouldSetPid()
    {
        var result = RestartResult.Ok(5678);
        result.Pid.Should().Be(5678);
    }

    [Fact]
    public void Fail_ShouldSetSuccessFalse()
    {
        var result = RestartResult.Fail("오류 발생");
        result.Success.Should().BeFalse();
    }

    [Fact]
    public void Fail_ShouldSetMessage()
    {
        var result = RestartResult.Fail("실행 파일 없음");
        result.Message.Should().Be("실행 파일 없음");
    }

    [Fact]
    public void Fail_ShouldHaveNullPid()
    {
        var result = RestartResult.Fail("이유");
        result.Pid.Should().BeNull();
    }
}
