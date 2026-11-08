function Invoke-UnityRpc {
 param([string]$Method,[hashtable]$Params)
 if(!(Test-Path -LiteralPath 'Docs/Phase26/MCP_SESSION.txt')) {
  $init=@{jsonrpc='2.0';id=1;method='initialize';params=@{protocolVersion='2024-11-05';capabilities=@{};clientInfo=@{name='phase26-recovery';version='1'}}}|ConvertTo-Json -Depth 8 -Compress
  $session=Invoke-WebRequest -Uri 'http://127.0.0.1:8080/mcp' -Method Post -ContentType 'application/json' -Headers @{Accept='application/json, text/event-stream'} -Body $init -UseBasicParsing -TimeoutSec 20
  $session.Headers['Mcp-Session-Id']|Set-Content Docs/Phase26/MCP_SESSION.txt
 }
 $headers=@{Accept='application/json, text/event-stream';'Mcp-Session-Id'=(Get-Content Docs/Phase26/MCP_SESSION.txt -Raw).Trim()}
 $body=@{jsonrpc='2.0';id=[DateTime]::UtcNow.Ticks;method=$Method;params=$Params}|ConvertTo-Json -Depth 40 -Compress
 $response=Invoke-WebRequest -Uri 'http://127.0.0.1:8080/mcp' -Method Post -ContentType 'application/json; charset=utf-8' -Headers $headers -Body ([Text.Encoding]::UTF8.GetBytes($body)) -UseBasicParsing -TimeoutSec 55
 foreach($line in ($response.Content -split "`n")){if($line.StartsWith('data: ')){$payload=$line.Substring(6)|ConvertFrom-Json;if($null -ne $payload.error){$payload.error|ConvertTo-Json -Depth 10 -Compress}else{$payload.result}}}
}
