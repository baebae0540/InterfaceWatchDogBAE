using Microsoft.Data.SqlClient;

namespace InterfaceWatchDog.Core.Actions;

public interface IAlarmWriter
{
    Task<bool> WriteAlarmAsync(string connectionString, string plantCode,
                                string alarmContent, string processName, string errorDate);
}

public class AlarmWriter : IAlarmWriter
{
    public async Task<bool> WriteAlarmAsync(string connectionString, string plantCode,
                                            string alarmContent, string processName, string errorDate)
    {
        const string sql = """
            INSERT INTO SYS_ALARM
                (PLANT_CD, ALARM_CONTENT, READ_YN, FORM_NAME,
                 REF_KEY1, REF_KEY2, INSERT_USER_ID, INSERT_TIME, REMARK)
            VALUES
                (@PlantCd, @Content, 'N', 'InterfaceWatchDog',
                 @ErrorDate, @ProcessName, 'SYSTEM', @InsertTime, 'I/F감시프로그램')
            """;

        await using var conn = new SqlConnection(connectionString);
        await conn.OpenAsync();

        await using var cmd = new SqlCommand(sql, conn);
        cmd.Parameters.AddWithValue("@PlantCd", plantCode);
        cmd.Parameters.AddWithValue("@Content", alarmContent);
        cmd.Parameters.AddWithValue("@ErrorDate", errorDate);
        cmd.Parameters.AddWithValue("@ProcessName", processName);
        cmd.Parameters.AddWithValue("@InsertTime", DateTime.Now);

        await cmd.ExecuteNonQueryAsync();
        return true;
    }
}
