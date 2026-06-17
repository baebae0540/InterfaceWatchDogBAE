using InterfaceWatchDog.Core;
using InterfaceWatchDog.Core.Actions;
using InterfaceWatchDog.Core.Models;
using Microsoft.Data.SqlClient;

namespace InterfaceWatchDog.Tests.Integration;

[Trait("Category", "Integration")]
public class AlarmWriterIntegrationTests : IDisposable
{
    private const string TestProcessName = "wd_test_alarm";

    private readonly AlarmDbConfig _dbConfig;
    private readonly AlarmWriter _writer = new();
    private readonly List<(string PlantCd, long AlarmId)> _insertedKeys = new();

    public AlarmWriterIntegrationTests()
    {
        _dbConfig = ConfigManager.LoadAlarmDb();
    }

    // ── 1. INSERT 성공 및 컬럼 값 검증 ──────────────────────────────────────

    [Fact]
    public async Task WriteAlarmAsync_ShouldInsertCorrectValues()
    {
        if (!_dbConfig.IsConfigured) return;

        var errorDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        var content = "통합 테스트 알람";

        var result = await _writer.WriteAlarmAsync(
            _dbConfig.ConnectionString, _dbConfig.PlantCode,
            content, TestProcessName, errorDate);

        result.Should().BeTrue();

        await using var conn = new SqlConnection(_dbConfig.ConnectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand("""
            SELECT TOP 1 ALARM_ID, PLANT_CD, ALARM_CONTENT, READ_YN, FORM_NAME,
                         REF_KEY1, REF_KEY2, INSERT_USER_ID, INSERT_TIME, REMARK, AUDITTRAIL_ID
            FROM SYS_ALARM
            WHERE PLANT_CD = @PlantCd AND REF_KEY2 = @ProcessName AND REF_KEY1 = @ErrorDate
            ORDER BY ALARM_ID DESC
            """, conn);
        cmd.Parameters.AddWithValue("@PlantCd", _dbConfig.PlantCode);
        cmd.Parameters.AddWithValue("@ProcessName", TestProcessName);
        cmd.Parameters.AddWithValue("@ErrorDate", errorDate);

        await using var reader = await cmd.ExecuteReaderAsync();
        reader.Read().Should().BeTrue("INSERT한 레코드를 조회할 수 있어야 함");

        var alarmId = reader.GetInt64(reader.GetOrdinal("ALARM_ID"));
        _insertedKeys.Add((_dbConfig.PlantCode, alarmId));

        reader["PLANT_CD"].Should().Be(_dbConfig.PlantCode);
        reader["ALARM_CONTENT"].Should().Be(content);
        reader["READ_YN"].Should().Be("N");
        reader["FORM_NAME"].Should().Be("InterfaceWatchDog");
        reader["REF_KEY1"].Should().Be(errorDate);
        reader["REF_KEY2"].Should().Be(TestProcessName);
        reader["INSERT_USER_ID"].Should().Be("SYSTEM");
        reader["INSERT_TIME"].Should().NotBe(DBNull.Value);
        reader["REMARK"].Should().Be("I/F감시프로그램");
        alarmId.Should().BeGreaterThan(0);
        reader.GetInt32(reader.GetOrdinal("AUDITTRAIL_ID")).Should().BeGreaterThan(0);
    }

    // ── 2. 반환값 true 확인 ─────────────────────────────────────────────────

    [Fact]
    public async Task WriteAlarmAsync_ShouldReturnTrue()
    {
        if (!_dbConfig.IsConfigured) return;

        var errorDate = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

        var result = await _writer.WriteAlarmAsync(
            _dbConfig.ConnectionString, _dbConfig.PlantCode,
            "반환값 테스트", TestProcessName, errorDate);

        result.Should().BeTrue();

        await CleanupByRef(errorDate);
    }

    // ── 3. 잘못된 연결 문자열 → 예외 전파 ───────────────────────────────────

    [Fact]
    public async Task WriteAlarmAsync_WithInvalidConnection_ShouldThrow()
    {
        var badConnStr = "Server=invalid_host_99999;Database=nodb;User Id=x;Password=x;Connect Timeout=3;TrustServerCertificate=True";

        var act = () => _writer.WriteAlarmAsync(badConnStr, "P1", "test", "proc", "2025-01-01 00:00:00");

        await act.Should().ThrowAsync<SqlException>();
    }

    // ── cleanup ─────────────────────────────────────────────────────────────

    private async Task CleanupByRef(string errorDate)
    {
        if (!_dbConfig.IsConfigured) return;

        await using var conn = new SqlConnection(_dbConfig.ConnectionString);
        await conn.OpenAsync();
        await using var cmd = new SqlCommand(
            "DELETE FROM SYS_ALARM WHERE PLANT_CD = @PlantCd AND REF_KEY2 = @ProcessName AND REF_KEY1 = @ErrorDate", conn);
        cmd.Parameters.AddWithValue("@PlantCd", _dbConfig.PlantCode);
        cmd.Parameters.AddWithValue("@ProcessName", TestProcessName);
        cmd.Parameters.AddWithValue("@ErrorDate", errorDate);
        await cmd.ExecuteNonQueryAsync();
    }

    public void Dispose()
    {
        if (!_dbConfig.IsConfigured) return;

        using var conn = new SqlConnection(_dbConfig.ConnectionString);
        conn.Open();

        foreach (var (plantCd, alarmId) in _insertedKeys)
        {
            using var cmd = new SqlCommand(
                "DELETE FROM SYS_ALARM WHERE PLANT_CD = @PlantCd AND ALARM_ID = @AlarmId", conn);
            cmd.Parameters.AddWithValue("@PlantCd", plantCd);
            cmd.Parameters.AddWithValue("@AlarmId", alarmId);
            cmd.ExecuteNonQuery();
        }

        using var cleanAll = new SqlCommand(
            "DELETE FROM SYS_ALARM WHERE REF_KEY2 = @ProcessName AND FORM_NAME = 'InterfaceWatchDog'", conn);
        cleanAll.Parameters.AddWithValue("@ProcessName", TestProcessName);
        cleanAll.ExecuteNonQuery();
    }
}
