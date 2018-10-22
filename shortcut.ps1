
Param(
	[string]$solutionDir,
	[string]$outDir
)

$Shell = New-Object -ComObject ("WScript.Shell")

$ShortCut = $Shell.CreateShortcut($solutionDir + "DialogGenerator.lnk")
$ShortCut.TargetPath=$outDir + "DialogGenerator.exe"
$ShortCut.WindowStyle = 1;
$ShortCut.Description = "Dialog generator";

$ShortCut.Save()

Write-Output "Shortcut path:"
Write-Output $solutionDir

Write-Output "Path to exe file:"
Write-Output $outDir