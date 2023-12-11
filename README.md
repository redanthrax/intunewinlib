# IntuneWinLib

## Introduction

This library is to create .intunewin files from powershell.

## Running locally

Download the DLL from the releases.
Utilize the Release.ps1 file to create the package for intune.

## SWA Deploy

```powershell
swa deploy --env production ./frontend --verbose silly
```

## Func Deploy
```powershell
func azure functionapp publish --nozip --no-build <funcappname>
```

Make sure function app is 64-bit