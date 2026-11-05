$env:VITE_API_URL="http://localhost:5000"
Set-Location (Join-Path $PSScriptRoot '..\frontend')
npm run dev
