using InterfaceWatchDog.Core.Models;

namespace InterfaceWatchDog.Core.Monitors;

public class FileActivityMonitor
{
    public FileActivityStatus Check(PdfFolderConfig config)
    {
        var status = new FileActivityStatus();

        if (!config.IsConfigured)
        {
            status.StatusMessage = "PDF 폴더가 설정되지 않았습니다.";
            return status;
        }

        status.IsFolderConfigured = true;

        try
        {
            var pdfFiles = Directory.GetFiles(config.Path, "*.pdf");
            status.FileCount = pdfFiles.Length;

            if (pdfFiles.Length > 0)
            {
                status.LastFileCreated = pdfFiles
                    .Select(f => File.GetCreationTime(f))
                    .Max();
            }

            // 신규 파일 생성 대기 시간 초과 여부
            if (status.LastFileCreated.HasValue)
            {
                var idleMinutes = (DateTime.Now - status.LastFileCreated.Value).TotalMinutes;
                status.IsIdleWarning = idleMinutes >= config.MaxIdleMinutes;
            }
            else
            {
                // 파일이 아예 없으면 폴더 생성 시간 기준으로 판단
                var folderAge = (DateTime.Now - Directory.GetCreationTime(config.Path)).TotalMinutes;
                status.IsIdleWarning = folderAge >= config.MaxIdleMinutes;
            }

            // 미처리 파일 누적 여부
            status.IsBacklogWarning = status.FileCount >= config.MaxBacklogCount;

            status.StatusMessage = BuildStatusMessage(status, config);
        }
        catch (Exception ex)
        {
            status.StatusMessage = $"폴더 접근 실패: {ex.Message}";
        }

        return status;
    }

    private static string BuildStatusMessage(FileActivityStatus status, PdfFolderConfig config)
    {
        if (status.IsBacklogWarning)
            return $"미처리 PDF {status.FileCount}개 누적 (임계값: {config.MaxBacklogCount}개)";

        if (status.IsIdleWarning)
        {
            var lastTime = status.LastFileCreated.HasValue
                ? status.LastFileCreated.Value.ToString("HH:mm:ss")
                : "없음";
            return $"PDF 신규 생성 없음 {config.MaxIdleMinutes}분 초과 (마지막: {lastTime})";
        }

        return status.LastFileCreated.HasValue
            ? $"정상 - PDF {status.FileCount}개 (최신: {status.LastFileCreated.Value:HH:mm:ss})"
            : $"정상 - PDF 파일 없음";
    }
}
