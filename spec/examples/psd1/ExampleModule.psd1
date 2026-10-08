# Sample data for module-manifest.skemata.yaml
@{
    RootModule        = 'ExampleModule.psm1'
    ModuleVersion     = '1.4.0'
    GUID              = '5f2a6f3e-8c1d-4c4e-9a7b-2d3e4f5a6b7c'
    Author            = 'Example Author'
    Description       = 'Example functions for the Skemata documentation.'
    PowerShellVersion = '5.1'
    FunctionsToExport = @('Get-Example', 'Set-Example')
    CmdletsToExport   = @()
    PrivateData       = @{
        PSData = @{
            Tags       = @('example', 'skemata')
            projecturi = 'https://example.org/example-module'   # keys are case-insensitive
        }
    }
}
