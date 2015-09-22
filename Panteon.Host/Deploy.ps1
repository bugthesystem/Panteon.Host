function getSourcePath
{
	if($OctopusEnvironmentName -ceq 'Development'){$BuildPath = "Debug"}
	else{$BuildPath = "Release"}

	return Join-Path (Get-Location).Path $BuildPath;
}

function copyRecursive([string]$sourceFolder, [string]$targetFolder, [string]$regExclude)
{
	if(! $regExclude)
	{
		$regExclude = 'EMPTY_NOTMATCH'
	}

    $sourceRegEx = "^"+[regex]::escape($sourceFolder);
    $sources = gci -path $sourceFolder -r | ? {($_.fullname -replace $sourceRegEx, "") -notmatch $regExclude}

    if($sources)
    {
        foreach($source in $sources)
        {
            $targetFullName = ($source.fullname -replace $sourceRegEx, $targetFolder)
            copy-Item $source.fullname -destination $targetFullName -force
        }
    }
}

# $sp1 = 'D:\Projects\TY.Web\Services\MobileService_Rest\WindowsService\bin\Release'
# $tp1 = 'C:\@Sites'
# $ex1 = '\\App_Data|.config$'
# copyRecursive $sp1 $tp1 $ex1

#$OctopusPackagePath = "C:\Octopus\Tentacle\Applications\TY.MobileService.0.0.12072.10"
#$OctopusEnvironmentName = "Production"
#$OctopusPackageName = "TY.MobileService"

if(! $ServiceName)
{
	$ServiceName = $OctopusPackageName
}

if(! $ServiceRoot)
{
	$ServiceRoot = "C:\@Services"
}

$SourcePath = getSourcePath
$TargetPath = Join-Path $ServiceRoot $ServiceName

if ((Test-Path -path $TargetPath) -ne $True)
{
	New-Item $TargetPath -type directory
}

if(! $ServiceExecutable)
{
	$ServiceExecutable = (Get-ChildItem $SourcePath\*.exe -Name | Select-Object -First 1)
}

$FullPath = (Join-Path $TargetPath $ServiceExecutable)

$Service = Get-WmiObject -Class Win32_Service -Filter ("Name = '" + $ServiceName + "'")
if ($Service)
{
	Write-Host "Stopping service"
	Stop-Service $ServiceName -Force | Write-Host

	Write-Host "Updating the service files"
	copyRecursive $SourcePath $TargetPath $exclude
    & "sc.exe" config "$ServiceName" binPath= $FullPath  start= auto | Write-Host

	Write-Host "Starting Service"
	Start-Service $ServiceName	
}
else
{
	Write-Host "The service will be installed"
	
	& "xcopy.exe" "$SourcePath" "$TargetPath" /D /E /Y | Write-Host		
	
	New-Service -Name $ServiceName -BinaryPathName $FullPath -StartupType Automatic
}
