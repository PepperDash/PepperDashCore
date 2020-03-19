$destination = "$($Env:GITHUB_WORKSPACE)\output"
New-Item -ItemType Directory -Force -Path ($destination)
Get-ChildItem ($destination)
$exclusions = @(git submodule foreach --quiet 'echo $name')
Get-ChildItem -recurse -Path "$($Env:GITHUB_WORKSPACE)" -include @("*.clz", "*.cpz", "*.cplz", "*.xml", "$($Env:GITHUB_WORKSPACE).dll") | ForEach-Object {
  $allowed = $true;
  foreach ($exclude in $exclusions) {
    if ((Split-Path $_.FullName -Parent).contains("$($exclude)")) {
      $allowed = $false;
      break;
    }
  }
  if ($allowed) {
    Write-Host "allowing $($_)"
    $_;
  }
} | Copy-Item -Destination ($destination)
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\output" -include @("*.cpz", "*.clz", "*.cplz") | ForEach-Object {  
  $filenames = @($_ -replace "cpz|clz|cplz", "dll", $_ -replace "cpz|clz|cplz", "xml")
  Write-Host "Filenames:"
  Write-Host $filenames
  if ($filenames.length -gt 0) {
    Get-ChildItem -Recurse -Path "$($Env:GITHUB_WORKSPACE)" -include $filenames | Copy-Item -Destination ($destination)
  }
}
$version = $Env
Compress-Archive -Path "$($Env:GITHUB_WORKSPACE)\output\*" -DestinationPath "$($Env:GITHUB_WORKSPACE)\$($Env:SOLUTION_FILE)-$($Env:VERSION).zip"
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\"
