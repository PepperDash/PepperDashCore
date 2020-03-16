$tagCount = $(git rev-list --tags='*.*.*' --count)
if ($tagCount -eq 0) {
  $latestVersion = "0.0.0"
}
else {
  $latestVersions = $(git describe --tags $(git rev-list --tags='*.*.*' --max-count=10) --abbrev=0) 
  $latestVersion = ""
  Foreach ($version in $latestVersions) {
    Write-Output $version
    if ($version -match '^[1-9]+.\d+.\d+$') {
      $latestVersion = $version
      Write-Output "Setting latest version to: $latestVersion"
      break
    }
  }
}
$newVersion = [version]$latestVersion
$phase = ""
$newVersionString = ""
switch -regex ($Env:GITHUB_REF) {
  '^refs\/heads\/master\/*.' {
    $newVersionString = "{0}.{1}.{2}" -f $newVersion.Major, $newVersion.Minor, ($newVersion.Build + 1)
  }
  '^refs\/heads\/feature\/*.' {
    $phase = 'alpha'
  }
  '^refs\/heads\/release\/*.' {
    $phase = 'rc'
  }
  '^refs\/heads\/development\/*.' {
    $phase = 'beta'
  }
  '^refs\/heads\/hotfix\/*.' {
    $phase = 'hotfix'
  }
}
$newVersionString = "{0}.{1}.{2}-{3}-{4}" -f $newVersion.Major, $newVersion.Minor, ($newVersion.Build + 1), $phase, $Env:GITHUB_RUN_NUMBER

Write-Output $newVersionString
