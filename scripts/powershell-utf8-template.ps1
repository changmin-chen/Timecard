# UTF-8 safe read/write template for PowerShell scripts.
# Prefer this pattern for any content that may include CJK characters.

param(
    [Parameter(Mandatory = $true)]
    [string]$Path
)

$utf8NoBom = New-Object System.Text.UTF8Encoding($false)
$content = [System.IO.File]::ReadAllText($Path, [System.Text.Encoding]::UTF8)

# TODO: Apply your text updates to $content here.

[System.IO.File]::WriteAllText($Path, $content, $utf8NoBom)
