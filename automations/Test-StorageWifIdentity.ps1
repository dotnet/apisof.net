<#
.SYNOPSIS
  Validates the currently-authenticated identity can access an Azure Storage Account via Entra/RBAC.

.DESCRIPTION
  - Prints authenticated context (who am I)
  - Requests an access token for https://storage.azure.com/
  - Creates an AzStorageContext using -UseConnectedAccount (Entra auth, no keys)
  - Attempts container/blob operations to confirm data-plane permissions

.NOTES
  Requires Az PowerShell modules (Az.Accounts, Az.Storage).
  Works best when executed in the same environment where the identity is used (e.g., pipeline agent task shell).
#>

[CmdletBinding()]
param(
  # Optional: do a write/read roundtrip to prove Contributor-like permissions
  [switch] $DoWriteReadTest
)

$ErrorActionPreference = "Stop"

$resultLines = New-Object System.Collections.Generic.List[string]

function Add-ResultLine([string]$Line) {
  $null = $resultLines.Add($Line)
}

function Write-Section([string]$Title) {
  Write-Host ""
  Write-Host ("=" * 80)
  Write-Host $Title
  Write-Host ("=" * 80)
}

function Resolve-ResultOutputPath {
  if ($env:BUILD_ARTIFACTSTAGINGDIRECTORY) {
    $outputDirectory = Join-Path $env:BUILD_ARTIFACTSTAGINGDIRECTORY "GenDesignNotesOutput"
  }
  else {
    $outputDirectory = Join-Path (Get-Location).Path "GenDesignNotesOutput"
  }

  New-Item -ItemType Directory -Path $outputDirectory -Force | Out-Null
  return (Join-Path $outputDirectory "storage-wif-identity-result.txt")
}

function Write-ResultFile {
  param(
    [Parameter(Mandatory = $true)]
    [string] $Status,

    [Parameter(Mandatory = $true)]
    [string] $Message
  )

  $path = Resolve-ResultOutputPath
  Add-ResultLine "status=$Status"
  Add-ResultLine "message=$Message"
  Add-ResultLine "timestampUtc=$((Get-Date).ToUniversalTime().ToString('o'))"

  $resultLines | Set-Content -Path $path -Encoding UTF8
  Write-Host ("Result file: {0}" -f $path)
}

try {
  Write-Section "1) Azure context (who am I?)"

  # If you're running inside a pipeline where az login already happened, this should succeed.
  # Otherwise, you can authenticate manually before running (Connect-AzAccount).
  $ctx = Get-AzContext
  if (-not $ctx) {
    throw "No Az context found. Run Connect-AzAccount (or ensure your pipeline/task performed WIF login) before running this script."
  }

  $ctx = Get-AzContext

  Write-Host ("Account        : {0}" -f $ctx.Account)
  Write-Host ("Tenant         : {0}" -f $ctx.Tenant.Id)
  Write-Host ("Subscription   : {0} ({1})" -f $ctx.Subscription.Name, $ctx.Subscription.Id)
  Write-Host ("Environment    : {0}" -f $ctx.Environment.Name)
  Add-ResultLine ("account={0}" -f $ctx.Account)
  Add-ResultLine ("tenantId={0}" -f $ctx.Tenant.Id)
  Add-ResultLine ("subscriptionName={0}" -f $ctx.Subscription.Name)
  Add-ResultLine ("subscriptionId={0}" -f $ctx.Subscription.Id)
  Add-ResultLine ("environment={0}" -f $ctx.Environment.Name)
  Add-ResultLine ("doWriteReadTest={0}" -f [bool]$DoWriteReadTest)

  Write-Section "2) Acquire Storage audience token (https://storage.azure.com/)"
  $token = Get-AzAccessToken -ResourceUrl "https://storage.azure.com/"
  Write-Host ("Token tenant   : {0}" -f $token.TenantId)
  Write-Host ("Token expires  : {0}" -f $token.ExpiresOn)
  Write-Host ("Token length   : {0}" -f $token.Token.Length)
  Add-ResultLine ("tokenTenantId={0}" -f $token.TenantId)
  Add-ResultLine ("tokenExpiresOn={0}" -f $token.ExpiresOn)

  Write-Section "3) Discover storage accounts in current subscription"
  $storageAccounts = Get-AzStorageAccount
  Add-ResultLine ("storageAccountsDiscovered={0}" -f @($storageAccounts).Count)
  Write-Host ("Storage accounts found: {0}" -f @($storageAccounts).Count)

  $roundtripCompleted = $false

  foreach ($sa in $storageAccounts) {
    Add-ResultLine ("storageAccount={0};resourceGroup={1};location={2};sku={3}" -f $sa.StorageAccountName, $sa.ResourceGroupName, $sa.Location, $sa.Sku.Name)

    try {
      $storageCtx = New-AzStorageContext -StorageAccountName $sa.StorageAccountName -UseConnectedAccount
      Add-ResultLine ("storageContextCreated={0}" -f $sa.StorageAccountName)

      $containers = Get-AzStorageContainer -Context $storageCtx -ErrorAction Stop
      Add-ResultLine ("containersFound[{0}]={1}" -f $sa.StorageAccountName, @($containers).Count)

      $sampleContainers = @($containers | Select-Object -First 10)
      foreach ($container in $sampleContainers) {
        Add-ResultLine ("container[{0}]={1}" -f $sa.StorageAccountName, $container.Name)
      }

      if ($DoWriteReadTest -and -not $roundtripCompleted -and @($containers).Count -gt 0) {
        Write-Section ("4) Write/Read roundtrip test on {0}" -f $sa.StorageAccountName)

        $targetContainer = @($containers | Select-Object -First 1)[0].Name
        $tempDir = Join-Path $env:TEMP ("StorageWifTest_" + [Guid]::NewGuid().ToString("N"))
        New-Item -ItemType Directory -Path $tempDir | Out-Null

        $localFile = Join-Path $tempDir "wif-test.txt"
        "WIF storage test $(Get-Date -Format o)" | Set-Content -Path $localFile -Encoding UTF8

        $blobName = "wif-test-" + [Guid]::NewGuid().ToString("N") + ".txt"
        Set-AzStorageBlobContent -File $localFile -Container $targetContainer -Blob $blobName -Context $storageCtx -Force | Out-Null
        Add-ResultLine ("writeReadTestUploadedBlob={0}/{1}/{2}" -f $sa.StorageAccountName, $targetContainer, $blobName)

        $downloadFile = Join-Path $tempDir "wif-test-downloaded.txt"
        Get-AzStorageBlobContent -Container $targetContainer -Blob $blobName -Destination $downloadFile -Context $storageCtx -Force | Out-Null
        Add-ResultLine ("writeReadTestDownloadedPath={0}" -f $downloadFile)

        Remove-AzStorageBlob -Container $targetContainer -Blob $blobName -Context $storageCtx -Force | Out-Null
        Add-ResultLine "writeReadTestCleanup=true"

        Remove-Item -Path $tempDir -Recurse -Force
        $roundtripCompleted = $true
      }
    }
    catch {
      Add-ResultLine ("storageAccountError[{0}]={1}" -f $sa.StorageAccountName, $_.Exception.Message)
    }
  }

  if ($DoWriteReadTest -and -not $roundtripCompleted) {
    Add-ResultLine "writeReadTestSkipped=true"
    Add-ResultLine "writeReadTestReason=No accessible storage account/container found for roundtrip"
  }

  Write-Section "RESULT"
  Write-Host "✅ SUCCESS: Current identity is authenticated and discovery completed."
  Write-ResultFile -Status "success" -Message "Current identity is authenticated and discovery completed."
}
catch {
  $errorMessage = $_.Exception.Message
  Write-ResultFile -Status "failed" -Message $errorMessage

  Write-Section "RESULT"
  Write-Host "❌ FAILED"
  Write-Host $errorMessage
  Write-Host ""
  Write-Host "Troubleshooting hints:"
  Write-Host " - If token acquisition fails: the federated identity / service connection wiring may be wrong for this principal."
  Write-Host " - If token succeeds but container/blob ops fail: RBAC assignment may be missing or assigned to the wrong principal (UAMI vs app registration/SP)."
  Write-Host " - If errors mention network/firewall: check storage account networking and whether the agent can reach the data plane."
  throw
}