param(
    [Parameter(Mandatory = $true)]
    [string]$ArtifactDir,

    [string]$PfxPath = $env:CODESIGN_PFX_PATH,
    [string]$PfxBase64 = $env:CODESIGN_PFX_BASE64,
    [string]$PfxPassword = $env:CODESIGN_PFX_PASSWORD,
    [string]$TimestampUrl = $(if ($env:CODESIGN_TIMESTAMP_URL) { $env:CODESIGN_TIMESTAMP_URL } else { "http://timestamp.digicert.com" }),
    [switch]$SkipTimestamp
)

$ErrorActionPreference = "Stop"

function Resolve-SignTool {
    $fromPath = Get-Command signtool.exe -ErrorAction SilentlyContinue
    if ($fromPath) {
        return $fromPath.Source
    }

    $windowsKits = Join-Path ${env:ProgramFiles(x86)} "Windows Kits\10\bin"
    if (Test-Path -LiteralPath $windowsKits) {
        $candidate = Get-ChildItem -LiteralPath $windowsKits -Recurse -Filter signtool.exe |
            Where-Object { $_.FullName -match "\\x64\\signtool\.exe$" } |
            Sort-Object FullName -Descending |
            Select-Object -First 1

        if ($candidate) {
            return $candidate.FullName
        }
    }

    throw "signtool.exe nao foi encontrado. Instale o Windows SDK com o componente de signing tools."
}

function Get-RequiredFile {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path
    )

    $resolved = Resolve-Path -LiteralPath $Path -ErrorAction SilentlyContinue
    if (-not $resolved) {
        throw "Arquivo obrigatorio nao encontrado: $Path"
    }

    return $resolved.Path
}

function Write-TempPfx {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Base64
    )

    $cleanBase64 = (($Base64 -split "\r?\n") |
        Where-Object { $_ -and $_ -notmatch "^-+BEGIN" -and $_ -notmatch "^-+END" }) -join ""

    $tempPfx = Join-Path ([System.IO.Path]::GetTempPath()) ("ghub-freestyle-codesign-{0}.pfx" -f ([Guid]::NewGuid()))
    [System.IO.File]::WriteAllBytes($tempPfx, [Convert]::FromBase64String($cleanBase64))
    return $tempPfx
}

function Test-EmbeddedSignature {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Target,

        [Parameter(Mandatory = $true)]
        [string]$ExpectedThumbprint,

        [Parameter(Mandatory = $true)]
        [bool]$RequireTimestamp
    )

    $signature = Get-AuthenticodeSignature -LiteralPath $Target
    if (-not $signature.SignerCertificate) {
        throw "Assinatura ausente: $Target"
    }

    if ($signature.SignerCertificate.Thumbprint -ne $ExpectedThumbprint) {
        throw "Assinatura usa certificado inesperado: $Target"
    }

    if ($signature.Status -in @("NotSigned", "HashMismatch")) {
        throw "Assinatura invalida em $Target. Status: $($signature.Status)"
    }

    if ($RequireTimestamp -and -not $signature.TimeStamperCertificate) {
        throw "Assinatura sem timestamp: $Target"
    }

    Write-Host "Embedded signature matches expected signer for $Target. Status: $($signature.Status)"
}

$artifactPath = Resolve-Path -LiteralPath $ArtifactDir -ErrorAction SilentlyContinue
if (-not $artifactPath) {
    throw "Diretorio do artefato nao encontrado: $ArtifactDir"
}

$temporaryPfx = $null
try {
    if (-not $PfxPath) {
        if (-not $PfxBase64) {
            throw "Informe CODESIGN_PFX_PATH ou CODESIGN_PFX_BASE64 para assinar o artefato."
        }

        $temporaryPfx = Write-TempPfx -Base64 $PfxBase64
        $PfxPath = $temporaryPfx
    }

    if (-not $PfxPassword) {
        throw "Informe CODESIGN_PFX_PASSWORD para assinar o artefato."
    }

    $pfxFile = Get-RequiredFile -Path $PfxPath
    $signTool = Resolve-SignTool
    $expectedSigner = [System.Security.Cryptography.X509Certificates.X509Certificate2]::new(
        $pfxFile,
        $PfxPassword,
        [System.Security.Cryptography.X509Certificates.X509KeyStorageFlags]::EphemeralKeySet
    )
    $targets = @(
        (Join-Path $artifactPath.Path "GHubFreestyleInjector.WinUI.exe"),
        (Join-Path $artifactPath.Path "ghub-freestyle.exe")
    )

    foreach ($target in $targets) {
        $null = Get-RequiredFile -Path $target
    }

    Write-Host "Signing Windows executables in $($artifactPath.Path)"
    foreach ($target in $targets) {
        $signArgs = @(
            "sign",
            "/f", $pfxFile,
            "/p", $PfxPassword,
            "/fd", "SHA256"
        )

        if (-not $SkipTimestamp) {
            $signArgs += @("/td", "SHA256", "/tr", $TimestampUrl)
        }

        $signArgs += $target
        & $signTool @signArgs
        if ($LASTEXITCODE -ne 0) {
            throw "Falha ao assinar: $target"
        }
    }

    foreach ($target in $targets) {
        & $signTool verify /pa /v $target
        if ($LASTEXITCODE -ne 0) {
            Write-Warning "signtool verify nao confiou na cadeia do certificado. Validando assinatura embutida."
        }

        Test-EmbeddedSignature -Target $target -ExpectedThumbprint $expectedSigner.Thumbprint -RequireTimestamp (-not $SkipTimestamp)
    }

    Write-Host "Windows executables signed and verified as Authenticode binaries."
    $global:LASTEXITCODE = 0
}
finally {
    if ($temporaryPfx -and (Test-Path -LiteralPath $temporaryPfx)) {
        Remove-Item -LiteralPath $temporaryPfx -Force
    }
}
