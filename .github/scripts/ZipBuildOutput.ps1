$destination = "$($Env:GITHUB_WORKSPACE)\output"
New-Item -ItemType Directory -Force -Path ($destination)
Get-ChildItem ($destination)
$exclusions = @(git submodule foreach --quiet 'echo $name')
Write-Output "Exclusions $($exclusions)"
Get-ChildItem -recurse -Path "$($Env:GITHUB_WORKSPACE)" -include @("*.clz", "*.cpz", "*.cplz") | ForEach-Object{
  Write-Host "checking $($_)"
  $allowed = $true;
  foreach($exclude in $exclusions) {
    if((Split-Path $_.FullName -Parent).contains("$($exclude)")) {
      Write-Host "excluding $($_)"
      $allowed = $false;
      break;
      }
    }
  if($allowed) {
    Write-Host "allowing $($_)"
    $_;
  }
} | Copy-Item -Destination ($destination)
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\output"
Compress-Archive -Path "$($Env:GITHUB_WORKSPACE)\output\*" -DestinationPath "$($Env:GITHUB_WORKSPACE)\$($Env:SOLUTION) $($Env.VERSION).zip"
Get-ChildItem "$($Env:GITHUB_WORKSPACE)\"
