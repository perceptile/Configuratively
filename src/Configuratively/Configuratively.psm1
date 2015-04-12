function Save-Configuration
{
	param
	(
		[Parameter(Mandatory=$true)]
		[string]$repositoryPath,
		[Parameter(ParameterSetName="export",Mandatory=$true)]
		[string]$routes
	)

	& $PSScriptRoot\Configuratively.exe export $repositoryPath $routes
}