Set-Location "E:\Study\C#\managerCMN"
$ErrorActionPreference = 'Stop'

$reportPath = "docs/action-thong-bao-tong-hop.md"
$workspaceRoot = (Get-Location).Path
$codeRoot = Join-Path $workspaceRoot "managerCMN/managerCMN"

function Find-Matches($pattern, $extensions) {
  $files = Get-ChildItem -Path $codeRoot -Recurse -File -ErrorAction SilentlyContinue |
    Where-Object { $extensions -contains $_.Extension.ToLowerInvariant() }

  if (-not $files) { return @() }

  $matches = foreach ($file in $files) {
    Select-String -Path $file.FullName -Pattern $pattern -AllMatches -CaseSensitive:$false -Encoding UTF8 -ErrorAction SilentlyContinue
  }

  if (-not $matches) { return @() }

  return $matches | ForEach-Object {
    $fullPath = $_.Path
    $relPath = $fullPath
    if ($fullPath.StartsWith($workspaceRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
      $relPath = $fullPath.Substring($workspaceRoot.Length).TrimStart([char]92, [char]47)
    }
    $relPath = $relPath.Replace('\\','/')
    "${relPath}:$($_.LineNumber):$($_.Line.Trim())"
  }
}

function Get-ModuleName($entry) {
  if (-not $entry) { return 'Unknown' }
  $firstColon = $entry.IndexOf(':')
  if ($firstColon -lt 0) { return 'Unknown' }
  $secondColon = $entry.IndexOf(':', $firstColon + 1)
  if ($secondColon -lt 0) { return 'Unknown' }
  $filePath = $entry.Substring(0, $firstColon)
  return [System.IO.Path]::GetFileNameWithoutExtension($filePath)
}

function Build-ModuleCountMap($entries) {
  $map = @{}
  foreach ($entry in $entries) {
    $module = Get-ModuleName $entry
    if (-not $map.ContainsKey($module)) { $map[$module] = 0 }
    $map[$module]++
  }
  return $map
}

$promptFiles = Get-ChildItem -Path $workspaceRoot -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -match 'prompt' } |
  ForEach-Object { $_.FullName.Replace($workspaceRoot + '\\','').Replace('\\','/') }

$skillFiles = Get-ChildItem -Path $workspaceRoot -Recurse -File -ErrorAction SilentlyContinue |
  Where-Object { $_.Name -match 'skill' -or $_.DirectoryName -match 'skills' } |
  ForEach-Object { $_.FullName.Replace($workspaceRoot + '\\','').Replace('\\','/') }

$tempData = Find-Matches 'TempData\["[^"]+"\]\s*=\s*.+;' @('.cs')
$modelState = Find-Matches 'ModelState\.AddModelError\(.+' @('.cs')
$jsonApi = Find-Matches 'return\s+(Json\(|Ok\(|BadRequest\(|Unauthorized\(|NotFound\(|StatusCode\(|Problem\(|ValidationProblem\(|Forbid\(|Conflict\().+' @('.cs')
$exceptions = Find-Matches 'throw\s+new\s+\w*Exception\(.+' @('.cs')
$frontendNotices = Find-Matches '(showToast\(|showPanelNotice\(|alert\(|toastr\.|swal\(|ViewBag\.Message\s*=|ViewData\["(Error|Success|Warning|Info|Message)"\]\s*=)' @('.js','.cshtml','.cs')

$moduleTempData = Build-ModuleCountMap $tempData
$moduleModelState = Build-ModuleCountMap $modelState
$moduleJsonApi = Build-ModuleCountMap $jsonApi
$moduleExceptions = Build-ModuleCountMap $exceptions
$moduleFrontend = Build-ModuleCountMap $frontendNotices

$allModules = @($moduleTempData.Keys + $moduleModelState.Keys + $moduleJsonApi.Keys + $moduleExceptions.Keys + $moduleFrontend.Keys) |
  Sort-Object -Unique

$allCounts = [ordered]@{
  TempData = $tempData.Count
  ModelStateAddModelError = $modelState.Count
  JsonAndApiReturn = $jsonApi.Count
  ThrowExceptions = $exceptions.Count
  FrontendAlertToast = $frontendNotices.Count
}

$semanticLines = @($tempData + $modelState + $jsonApi + $exceptions + $frontendNotices)
$semanticCounts = [ordered]@{
  AddOrCreate = (@($semanticLines | Where-Object { $_ -match '(?i)\b(thêm|tao|tạo|import|add|create)\b' })).Count
  UpdateOrEdit = (@($semanticLines | Where-Object { $_ -match '(?i)\b(cập nhật|cap nhat|sửa|sua|update|edit)\b' })).Count
  DeleteOrRemove = (@($semanticLines | Where-Object { $_ -match '(?i)\b(xóa|xoá|xoa|remove|delete|thu hoi|thu hồi|huy|hủy)\b' })).Count
  CompleteOrDone = (@($semanticLines | Where-Object { $_ -match '(?i)\b(hoàn thành|hoan thanh|duyet|duyệt|giải quyết|giai quyet|completed|done|resolve)\b' })).Count
  ValidationSyntax = (@($semanticLines | Where-Object { $_ -match '(?i)\b(không hợp lệ|khong hop le|invalid|validation|AddModelError|sai|phải|vui lòng|required|format)\b' })).Count
  ErrorOrFailure = (@($semanticLines | Where-Object { $_ -match '(?i)\b(lỗi|loi|error|exception|badrequest|notfound|không tìm thấy|khong tim thay|failed)\b' })).Count
  BlockOrForbidden = (@($semanticLines | Where-Object { $_ -match '(?i)\b(không có quyền|khong co quyen|forbidden|unauthorized|không thể|khong the|chỉ\s.+\sđược|chi\s.+\sduoc|blocked)\b' })).Count
}

$lines = New-Object System.Collections.Generic.List[string]
$lines.Add('# Tong hop action thong bao va chan loi')
$lines.Add('')
$lines.Add('- Ngay quet: 2026-04-20')
$lines.Add('- Workspace: E:/Study/C#/managerCMN')
$lines.Add('')
$lines.Add('## 1) Prompt va Skill tim thay trong workspace')
$lines.Add('')
$lines.Add('### Prompt files')
if ($promptFiles.Count -eq 0) { $lines.Add('- (khong tim thay)') } else { $promptFiles | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('### Skill files/folders')
if ($skillFiles.Count -eq 0) { $lines.Add('- (khong tim thay file skill trong workspace nay)') } else { $skillFiles | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 2) Thong ke nhanh theo loai action')
$lines.Add('')
$lines.Add('| Loai action | So luong dong match |')
$lines.Add('|---|---:|')
$allCounts.GetEnumerator() | ForEach-Object { $lines.Add("| $($_.Key) | $($_.Value) |") }
$lines.Add('')
$lines.Add('### 2.0) Tom tat theo y nghia action (loi/chan/sai cu phap/hoan thanh/them-sua-xoa)')
$lines.Add('')
$lines.Add('| Nhom action y nghia | So luong dong match |')
$lines.Add('|---|---:|')
$semanticCounts.GetEnumerator() | ForEach-Object { $lines.Add("| $($_.Key) | $($_.Value) |") }
$lines.Add('')
$lines.Add('### 2.1) Action do la cua gi (map theo module/file)')
$lines.Add('')
$lines.Add('| Module | TempData | Validation | API/Json | Block/Rule | Frontend notice |')
$lines.Add('|---|---:|---:|---:|---:|---:|')
foreach ($module in $allModules) {
  $c1 = if ($moduleTempData.ContainsKey($module)) { $moduleTempData[$module] } else { 0 }
  $c2 = if ($moduleModelState.ContainsKey($module)) { $moduleModelState[$module] } else { 0 }
  $c3 = if ($moduleJsonApi.ContainsKey($module)) { $moduleJsonApi[$module] } else { 0 }
  $c4 = if ($moduleExceptions.ContainsKey($module)) { $moduleExceptions[$module] } else { 0 }
  $c5 = if ($moduleFrontend.ContainsKey($module)) { $moduleFrontend[$module] } else { 0 }
  $lines.Add("| $module | $c1 | $c2 | $c3 | $c4 | $c5 |")
}
$lines.Add('')
$lines.Add('## 3) Action TempData (thanh cong, loi, warning, info, import, login...)')
$lines.Add('')
if ($tempData.Count -eq 0) { $lines.Add('- (khong co)') } else { $tempData | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 4) Action validation / sai cu phap du lieu (ModelState.AddModelError)')
$lines.Add('')
if ($modelState.Count -eq 0) { $lines.Add('- (khong co)') } else { $modelState | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 5) Action response API/Json (success, error, forbidden, notfound...)')
$lines.Add('')
if ($jsonApi.Count -eq 0) { $lines.Add('- (khong co)') } else { $jsonApi | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 6) Action chan thao tac / rule nghiep vu (throw exception)')
$lines.Add('')
if ($exceptions.Count -eq 0) { $lines.Add('- (khong co)') } else { $exceptions | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 7) Action thong bao phia frontend (toast, panel notice, alert)')
$lines.Add('')
if ($frontendNotices.Count -eq 0) { $lines.Add('- (khong co)') } else { $frontendNotices | Sort-Object -Unique | ForEach-Object { $lines.Add("- $_") } }
$lines.Add('')
$lines.Add('## 8) Ghi chu trien khai tiep theo')
$lines.Add('')
$lines.Add('- Moi dong da gom: file, line, action message hoac statement.')
$lines.Add('- Co the dung file nay de chuan hoa he thong notification theo ma mau: success, info, warning, error, blocked, validation.')
$lines.Add('- Co the tach theo module uu tien: ProjectTask, Request, Settings, Employee, Contract, Attendance, Ticket.')

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
[System.IO.File]::WriteAllLines((Join-Path $workspaceRoot $reportPath), $lines, $utf8NoBom)

Write-Output "REPORT_CREATED:$reportPath"
Write-Output "COUNTS: TempData=$($tempData.Count); ModelState=$($modelState.Count); JsonApi=$($jsonApi.Count); Exceptions=$($exceptions.Count); Frontend=$($frontendNotices.Count)"
