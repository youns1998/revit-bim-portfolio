param(
    [string]$DestinationDirectory = (Join-Path $env:APPDATA 'Autodesk/Revit/Addins/2026')
)
$ErrorActionPreference = 'Stop'
$root = (Resolve-Path (Join-Path $PSScriptRoot '..')).Path
$build = Join-Path $root 'src/BimInspector.Addin/bin/Release/net8.0-windows'
$assembly = Join-Path $build 'BimInspector.Addin.dll'
if (-not (Test-Path -LiteralPath $assembly)) { throw 'Build Release before installing the add-in.' }
$target = Join-Path $DestinationDirectory 'Youns.BimInspector'
$null = New-Item -ItemType Directory -Path $target -Force
foreach ($filename in @('BimInspector.Addin.dll', 'BimInspector.Core.dll', 'BimInspector.UI.dll')) {
    Copy-Item -LiteralPath (Join-Path $build $filename) -Destination (Join-Path $target $filename) -Force
}
$manifest = Join-Path $DestinationDirectory 'Youns.BimInspector.addin'
$settings = [Xml.XmlWriterSettings]::new()
$settings.Indent = $true
$settings.Encoding = [Text.UTF8Encoding]::new($false)
$writer = [Xml.XmlWriter]::Create($manifest, $settings)
try {
    $writer.WriteStartDocument()
    $writer.WriteStartElement('RevitAddIns')
    $writer.WriteStartElement('AddIn')
    $writer.WriteAttributeString('Type', 'Command')
    $writer.WriteElementString('Name', 'Revit BIM Inspector')
    $writer.WriteElementString('Assembly', (Join-Path ([IO.Path]::GetFullPath($target)) 'BimInspector.Addin.dll'))
    $writer.WriteElementString('AddInId', '8A896C23-8B5B-42CB-A7A6-919A71C13785')
    $writer.WriteElementString('FullClassName', 'BimInspector.Addin.InspectCommand')
    $writer.WriteElementString('Text', 'BIM Inspector')
    $writer.WriteElementString('VendorId', 'YOUN')
    $writer.WriteElementString('VendorDescription', 'Youns Revit BIM Portfolio')
    $writer.WriteEndElement()
    $writer.WriteEndElement()
    $writer.WriteEndDocument()
} finally { $writer.Dispose() }
Write-Host ('Installed manifest: ' + $manifest)
