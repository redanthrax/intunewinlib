# IntuneWinLib

## Introduction

This library is to create .intunewin files from powershell.

## Running locally

Download the DLL from the [releases](https://github.com/redanthrax/intunewinlib/releases/).

Utilize the Release.ps1 file to create the package for intune.

## SWA Deploy

```powershell
swa deploy --env production ./frontend --verbose silly
```

## Func Deploy
```powershell
func azure functionapp publish <funcappname> --nozip --no-build 
```

Make sure function app is 64-bit



New Method

Generate SAS Token for upload
Upload from client directly to storage
return file guid to client

trigger azure function app to convert file when appears in uploads
poll function to see if file is done based on guid
trigger delete once download completes