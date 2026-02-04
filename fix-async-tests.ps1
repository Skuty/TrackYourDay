# Script to fix async test methods
$ErrorActionPreference = "Stop"

$testFiles = @(
    'Tests\TrackYourDay.Tests\ApplicationTrackers\MsTeamsMeetings\MsTeamsMeetingsTrackerTests.cs',
    'Tests\TrackYourDay.Tests\ApplicationTrackers\MsTeamsMeetings\MsTeamsMeetingTrackerPendingEndTests.cs',
    'Tests\TrackYourDay.Tests\ApplicationTrackers\MsTeamsMeetings\MsTeamsMeetingTrackerManualEndTests.cs',
    'Tests\TrackYourDay.Tests\ApplicationTrackers\MsTeamsMeetings\MsTeamsMeetingTrackerValidationTests.cs',
    'Tests\TrackYourDay.Tests\ApplicationTrackers\MsTeamsMeetings\MsTeamsMeetingServiceTests.cs'
)

foreach ($file in $testFiles) {
    Write-Host "Processing $file..."
    
    $lines = Get-Content $file
    $output = @()
    $inMethod = $false
    $methodNeedsAsync = $false
    
    for ($i = 0; $i < $lines.Count; $i++) {
        $line = $lines[$i]
        
        # Check if method signature needs to be made async
        if ($line -match '^\s*public void (Given.+|When.+)\(') {
            # Look ahead to see if this method has RecognizeActivityAsync
            $needsAsync = $false
            for ($j = $i + 1; $j < [Math]::Min($i + 100, $lines.Count); $j++) {
                if ($lines[$j] -match 'RecognizeActivityAsync\(\)') {
                    $needsAsync = $true
                    break
                }
                if ($lines[$j] -match '^\s*\[Fact\]|^\s*public ') {
                    break
                }
            }
            
            if ($needsAsync) {
                $line = $line -replace 'public void ', 'public async Task '
                Write-Host "  Made async: line $($i + 1)"
            }
        }
        
        # Replace synchronous calls with await
        if ($line -match '^\s+_tracker\.RecognizeActivity\(\);') {
            $line = $line -replace '_tracker\.RecognizeActivity\(\);', 'await _tracker.RecognizeActivityAsync();'
            Write-Host "  Added await: line $($i + 1)"
        }
        if ($line -match '^\s+_msTeamsMeetingsTracker\.RecognizeActivity\(\);') {
            $line = $line -replace '_msTeamsMeetingsTracker\.RecognizeActivity\(\);', 'await _msTeamsMeetingsTracker.RecognizeActivityAsync();'
            Write-Host "  Added await: line $($i + 1)"
        }
        
        $output += $line
    }
    
    $output | Set-Content $file -Encoding UTF8
    Write-Host "  Done`n"
}

Write-Host "All files processed successfully"
