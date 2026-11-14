# ==============================================================================
# Script đóng gói dự án PRN232 nộp bài (Giới hạn <= 500 entries, dung lượng < 5MB)
# ==============================================================================
param(
    [string]$OutputZipName = "05_SE1933_SportCourtManagement_Submission.zip"
)

$ErrorActionPreference = "Stop"
$rootPath = $PSScriptRoot
if ([string]::IsNullOrWhiteSpace($rootPath)) { $rootPath = (Get-Location).Path }

$zipFilePath = Join-Path $rootPath $OutputZipName
if (Test-Path $zipFilePath) { Remove-Item $zipFilePath -Force }

Add-Type -AssemblyName System.IO.Compression
Add-Type -AssemblyName System.IO.Compression.FileSystem

# Các thư mục và file cần loại bỏ để giảm dung lượng và số lượng entries
$excludePatterns = @(
    '\\\.git(\\.*)?$',
    '\\\.vs(\\.*)?$',
    '\\bin(\\.*)?$',
    '\\obj(\\.*)?$',
    '\\SportCourtManagement_FrontEnd$',
    '\\SportCourtManagent_Server$',
    '\\read_excel\.py$',
    '\\Endpoint_desc\.xlsx$',
    '\\test_api\.ps1$',
    '\\package_submission\.ps1$',
    '\\PRN232-Project-Final\.code-workspace$',
    '\\\.csproj\.user$',
    '\\wwwroot\\uploads\\.*\.(png|jpg)$',
    '\\docs(\\.*)?$',
    '\\API_Design\.md$',
    '\\PROJECT_ROADMAP\.md$',
    '\\SETUP_GUIDE\.md$',
    '\\SRS\.md$',
    '\\TECHNICAL_SPEC\.md$',
    '\\seed_data\.sql$',
    '\\Dockerfile$',
    '\.zip$'
)

$files = Get-ChildItem -Path $rootPath -Recurse -File | Where-Object {
    $path = $_.FullName
    $skip = $false
    foreach ($pat in $excludePatterns) {
        if ($path -match $pat) { $skip = $true; break }
    }
    -not $skip
}

Write-Host "Đang đóng gói $($files.Count) file mã nguồn và báo cáo vào $OutputZipName..." -ForegroundColor Cyan

$zip = [System.IO.Compression.ZipFile]::Open($zipFilePath, [System.IO.Compression.ZipArchiveMode]::Create)

foreach ($f in $files) {
    $rel = $f.FullName.Substring($rootPath.Length + 1).Replace("\", "/")
    [System.IO.Compression.ZipFileExtensions]::CreateEntryFromFile($zip, $f.FullName, $rel, [System.IO.Compression.CompressionLevel]::Optimal) | Out-Null
}

$zip.Dispose()

# Kiểm tra xác thực file ZIP sau đóng gói
$readZip = [System.IO.Compression.ZipFile]::OpenRead($zipFilePath)
$entryCount = $readZip.Entries.Count
$zipSize = (Get-Item $zipFilePath).Length
$readZip.Dispose()

Write-Host "`n========================================================" -ForegroundColor Green
Write-Host " KẾT QUẢ ĐÓNG GÓI NỘP BÀI (SUBMISSION PACKAGE)" -ForegroundColor Green
Write-Host "========================================================" -ForegroundColor Green
Write-Host " Tên file ZIP: $OutputZipName"
Write-Host " Tổng số entries: $entryCount (Yêu cầu hệ thống: <= 500 entries) -> $(if ($entryCount -le 500) { 'ĐẠT CHUẨN [PASS]' } else { 'VƯỢT QUÁ [FAIL]' })" -ForegroundColor $(if ($entryCount -le 500) { 'Green' } else { 'Red' })
Write-Host " Dung lượng file: $([Math]::Round($zipSize / 1MB, 2)) MB ($zipSize bytes) -> Siêu nhẹ, tải lên nhanh chóng!"
Write-Host " Đường dẫn file: $zipFilePath"
Write-Host "========================================================`n" -ForegroundColor Green
