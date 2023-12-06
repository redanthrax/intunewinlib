# IntuneWinLib

## Introduction

This library is to create .intunewin files from powershell.

## SWA Deploy

```powershell
swa deploy --env production ./frontend --verbose silly
```

## Func Deploy
```powershell
func azure functionapp publish --nozip --no-build <funcappname>
```

Make sure function app is 64-bit