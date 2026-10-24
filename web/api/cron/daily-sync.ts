// Vercel can only schedule a cron against a path in this deployment, not an arbitrary external URL,
// so this just relays the ping: it wakes the Render API by hitting its /health endpoint. Render then
// restarts DailyBankSyncService and DailySatelliteRefreshService, whose in-process loops catch up
// anything due. See api/AgroMesh.API/Program.cs for the endpoint and
// api/AgroMesh.Infrastructure/Services/Financial/DailyBankSyncService.cs for why this is needed
// (Render's free tier spins the instance down when idle).
export default async function handler(req: any, res: any) {
  const apiUrl = process.env.API_BASE_URL;
  if (!apiUrl) {
    res.status(500).json({ error: 'API_BASE_URL is not configured' });
    return;
  }

  try {
    const response = await fetch(`${apiUrl.replace(/\/$/, '')}/health`);
    res.status(response.ok ? 200 : 502).json({ woke: response.ok, status: response.status });
  } catch (err) {
    res.status(502).json({ woke: false, error: err instanceof Error ? err.message : String(err) });
  }
}
