function Save-Configuration
{
	param
	(
		[Parameter(Mandatory=$true)]
		[string]$repositoryPath,
		[Parameter(ParameterSetName="export",Mandatory=$true)]
		[string]$route,
		[Parameter(ParameterSetName="export",Mandatory=$true)]
		[string]$outputPath
	)

	& $PSScriptRoot\Configuratively.exe export $repositoryPath $route $outputPath

}