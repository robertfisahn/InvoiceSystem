$body = '{"contextIdentifier":{"type":"onip","identifier":"1111111111"}}'
try {
    $r1 = Invoke-WebRequest -Uri "https://api-test.ksef.mf.gov.pl/api/online/Session/AuthorisationChallenge" -Method POST -Body $body -ContentType "application/json"
    Write-Host "V1 URL:" $r1.StatusCode
} catch {
    Write-Host "V1 URL:" $_.Exception.Response.StatusCode.value__
}

try {
    $r2 = Invoke-WebRequest -Uri "https://api-test.ksef.mf.gov.pl/api/v2/online/Session/AuthorisationChallenge" -Method POST -Body $body -ContentType "application/json"
    Write-Host "V2 URL:" $r2.StatusCode
} catch {
    Write-Host "V2 URL:" $_.Exception.Response.StatusCode.value__
}

try {
    $r3 = Invoke-WebRequest -Uri "https://ksef-test.mf.gov.pl/api/online/Session/AuthorisationChallenge" -Method POST -Body $body -ContentType "application/json"
    Write-Host "V3 URL:" $r3.StatusCode
} catch {
    Write-Host "V3 URL:" $_.Exception.Response.StatusCode.value__
}
