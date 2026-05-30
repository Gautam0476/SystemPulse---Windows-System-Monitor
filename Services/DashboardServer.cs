using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CustomTaskManager.Services;

public sealed class DashboardServer : IAsyncDisposable
{
    private const string DashboardKeyHeader = "X-Dashboard-Key";
    private const string AccessKeyFileName = "dashboard.key";
    private const string AccessKeyAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    private readonly DashboardState _state;
    private readonly string _accessKey;
    private WebApplication? _app;

    public DashboardServer(DashboardState state, int port = 5055, string? accessKey = null)
    {
        _state = state;
        _accessKey = string.IsNullOrWhiteSpace(accessKey)
            ? LoadOrCreateAccessKey()
            : accessKey.Trim();
        Port = port;
    }

    public int Port { get; }

    public string AccessKey => _accessKey;

    public string LocalUrl => $"http://localhost:{Port}";

    public string LocalAccessUrl => BuildAccessUrl(LocalUrl);

    public string BuildAccessUrl(string baseUrl)
    {
        return $"{baseUrl}?key={Uri.EscapeDataString(_accessKey)}";
    }

    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        if (_app is not null)
        {
            return;
        }

        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls($"http://0.0.0.0:{Port}");
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = System.Text.Json.JsonNamingPolicy.CamelCase;
        });

        var app = builder.Build();
        app.MapGet("/", () => Results.Content(BuildDashboardHtml(), "text/html"));
        app.MapGet("/api/status", (HttpContext context) =>
        {
            if (!IsAuthorized(context))
            {
                return Results.Unauthorized();
            }

            return Results.Json(_state.GetSnapshot());
        });

        await app.StartAsync(cancellationToken);
        _app = app;
    }

    public IReadOnlyList<string> GetAccessUrls()
    {
        var urls = new List<string> { LocalUrl };

        try
        {
            foreach (var address in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
            {
                if (address.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(address))
                {
                    urls.Add($"http://{address}:{Port}");
                }
            }
        }
        catch (SocketException)
        {
            // Localhost still works even if host IP discovery fails.
        }

        return urls;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is null)
        {
            return;
        }

        using var stopTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        await _app.StopAsync(stopTimeout.Token);
        await _app.DisposeAsync();
        _app = null;
    }

    private bool IsAuthorized(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(DashboardKeyHeader, out var header) &&
            MatchesAccessKey(header.ToString(), _accessKey))
        {
            return true;
        }

        return context.Request.Query.TryGetValue("key", out var query) &&
            MatchesAccessKey(query.ToString(), _accessKey);
    }

    private static bool MatchesAccessKey(string? candidate, string actual)
    {
        if (string.IsNullOrWhiteSpace(candidate))
        {
            return false;
        }

        var candidateBytes = Encoding.UTF8.GetBytes(candidate.Trim());
        var actualBytes = Encoding.UTF8.GetBytes(actual);
        return candidateBytes.Length == actualBytes.Length &&
            CryptographicOperations.FixedTimeEquals(candidateBytes, actualBytes);
    }

    private static string LoadOrCreateAccessKey()
    {
        var folder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "CustomTaskManager");
        Directory.CreateDirectory(folder);

        var path = Path.Combine(folder, AccessKeyFileName);
        if (File.Exists(path))
        {
            var savedKey = File.ReadAllText(path, Encoding.UTF8).Trim();
            if (IsValidAccessKey(savedKey))
            {
                return savedKey;
            }
        }

        var accessKey = CreateAccessKey();
        File.WriteAllText(path, accessKey, Encoding.UTF8);
        return accessKey;
    }

    private static bool IsValidAccessKey(string value)
    {
        return value.Length >= 8 &&
            value.All(character => AccessKeyAlphabet.Contains(character, StringComparison.Ordinal));
    }

    private static string CreateAccessKey()
    {
        Span<char> key = stackalloc char[12];
        for (var index = 0; index < key.Length; index++)
        {
            key[index] = AccessKeyAlphabet[RandomNumberGenerator.GetInt32(AccessKeyAlphabet.Length)];
        }

        return new string(key);
    }

    private static string BuildDashboardHtml()
    {
        return """
<!doctype html>
<html lang="en">
<head>
  <meta charset="utf-8">
  <meta name="viewport" content="width=device-width, initial-scale=1">
  <title>Custom Task Manager Dashboard</title>
  <style>
    :root {
      color-scheme: dark;
      --bg:#111318;
      --surface:#181b22;
      --card:#20242d;
      --card-alt:#242934;
      --line:#2a9d8f;
      --warn:#e9c46a;
      --danger:#c64946;
      --text:#f4f6f8;
      --muted:#a8b0bd;
      --border:#343a46;
    }

    * { box-sizing:border-box; }

    body {
      margin:0;
      min-height:100vh;
      font-family:Segoe UI, Roboto, Arial, sans-serif;
      background:var(--bg);
      color:var(--text);
    }

    header {
      position:sticky;
      top:0;
      z-index:2;
      display:flex;
      align-items:center;
      justify-content:space-between;
      gap:16px;
      padding:16px;
      background:var(--surface);
      border-bottom:1px solid var(--border);
    }

    h1, h2, p { margin:0; }

    h1 { font-size:22px; font-weight:700; }

    h2 {
      margin:0 0 8px;
      font-size:15px;
      font-weight:700;
    }

    .sub, .updated, .hint {
      color:var(--muted);
      font-size:13px;
    }

    .header-status {
      display:flex;
      align-items:flex-end;
      flex-direction:column;
      gap:6px;
      min-width:210px;
    }

    .badge {
      display:inline-flex;
      align-items:center;
      min-height:24px;
      padding:3px 9px;
      border:1px solid var(--border);
      border-radius:999px;
      color:var(--text);
      background:var(--card);
      font-size:12px;
      font-weight:700;
    }

    .badge.ok { border-color:rgba(42,157,143,.7); color:#8ee5d8; }
    .badge.locked { border-color:rgba(198,73,70,.7); color:#ffaaa7; }

    main {
      width:min(1160px, 100%);
      margin:0 auto;
      padding:14px;
    }

    .login {
      width:min(430px, 100%);
      margin:8vh auto 0;
      padding:18px;
      background:var(--surface);
      border:1px solid var(--border);
      border-radius:8px;
    }

    .login label {
      display:block;
      margin:14px 0 7px;
      color:var(--muted);
      font-size:13px;
      font-weight:600;
    }

    .login-row {
      display:flex;
      gap:8px;
    }

    input {
      flex:1;
      min-width:0;
      height:38px;
      padding:8px 10px;
      border:1px solid var(--border);
      border-radius:6px;
      background:var(--card);
      color:var(--text);
      font:inherit;
    }

    button {
      height:38px;
      padding:0 14px;
      border:1px solid var(--line);
      border-radius:6px;
      background:var(--line);
      color:white;
      font:inherit;
      font-weight:700;
      cursor:pointer;
    }

    button:active { transform:translateY(1px); }

    .error {
      min-height:20px;
      margin-top:10px;
      color:#ffaaa7;
      font-size:13px;
    }

    .graphs {
      display:grid;
      grid-template-columns:repeat(2, minmax(0, 1fr));
      gap:12px;
    }

    canvas {
      width:100%;
      height:230px;
      display:block;
      background:var(--surface);
      border:1px solid var(--border);
      border-radius:8px;
    }

    .stats {
      display:grid;
      grid-template-columns:repeat(4, minmax(0, 1fr));
      gap:12px;
      margin:12px 0;
    }

    .stat {
      min-height:86px;
      padding:12px;
      background:var(--card);
      border:1px solid var(--border);
      border-radius:8px;
    }

    .label {
      margin-bottom:6px;
      color:var(--muted);
      font-size:12px;
      font-weight:700;
      text-transform:uppercase;
    }

    .value {
      min-height:29px;
      overflow:hidden;
      color:var(--text);
      font-size:22px;
      font-weight:800;
      text-overflow:ellipsis;
      white-space:nowrap;
    }

    .meter {
      height:5px;
      margin-top:9px;
      overflow:hidden;
      background:#151820;
      border-radius:999px;
    }

    .meter span {
      display:block;
      width:0;
      height:100%;
      background:var(--line);
      transition:width .25s ease;
    }

    .meter.warn span { background:var(--warn); }

    .tables {
      display:grid;
      grid-template-columns:repeat(2, minmax(0, 1fr));
      gap:12px;
    }

    .table-panel {
      min-width:0;
    }

    table {
      width:100%;
      overflow:hidden;
      border:1px solid var(--border);
      border-collapse:separate;
      border-spacing:0;
      border-radius:8px;
      background:var(--surface);
    }

    th, td {
      padding:10px 9px;
      border-bottom:1px solid var(--border);
      font-size:13px;
      text-align:left;
      vertical-align:middle;
    }

    th {
      color:var(--muted);
      background:var(--card-alt);
      font-weight:700;
    }

    tr:last-child td { border-bottom:0; }

    td:nth-child(2), td:nth-child(3) {
      width:92px;
      white-space:nowrap;
    }

    .process {
      display:block;
      max-width:100%;
      overflow:hidden;
      text-overflow:ellipsis;
      white-space:nowrap;
    }

    .pid {
      color:var(--muted);
      font-size:12px;
    }

    .empty {
      color:var(--muted);
      text-align:center;
    }

    [hidden] { display:none !important; }

    @media (max-width: 820px) {
      header {
        align-items:flex-start;
        flex-direction:column;
      }

      .header-status {
        align-items:flex-start;
        min-width:0;
      }

      .graphs,
      .tables,
      .stats {
        grid-template-columns:1fr;
      }

      canvas { height:190px; }
      .stats { gap:8px; }
    }
  </style>
</head>
<body>
  <header>
    <div>
      <h1>Custom Task Manager</h1>
      <div class="sub">Remote laptop dashboard</div>
    </div>
    <div class="header-status">
      <span class="badge locked" id="authState">Locked</span>
      <span class="updated" id="updated">Waiting for access key</span>
    </div>
  </header>

  <main>
    <section class="login" id="loginPanel">
      <h2>Dashboard access</h2>
      <p class="hint">Enter the access key shown in the Windows app.</p>
      <form id="unlockForm">
        <label for="accessKeyInput">Access key</label>
        <div class="login-row">
          <input id="accessKeyInput" autocomplete="current-password" spellcheck="false" type="password">
          <button type="submit">Unlock</button>
        </div>
      </form>
      <div class="error" id="loginError"></div>
    </section>

    <section id="dashboard" hidden>
      <section class="graphs">
        <canvas id="cpu"></canvas>
        <canvas id="memory"></canvas>
      </section>

      <section class="stats">
        <div class="stat">
          <div class="label">CPU</div>
          <div class="value" id="cpuValue">-</div>
          <div class="meter"><span id="cpuMeter"></span></div>
        </div>
        <div class="stat">
          <div class="label">Memory</div>
          <div class="value" id="memoryValue">-</div>
          <div class="meter warn"><span id="memoryMeter"></span></div>
        </div>
        <div class="stat">
          <div class="label">Battery</div>
          <div class="value" id="batteryValue">-</div>
          <div class="meter"><span id="batteryMeter"></span></div>
        </div>
        <div class="stat">
          <div class="label">Power</div>
          <div class="value" id="powerValue">-</div>
        </div>
        <div class="stat">
          <div class="label">Processes</div>
          <div class="value" id="processValue">-</div>
        </div>
        <div class="stat">
          <div class="label">Threads</div>
          <div class="value" id="threadValue">-</div>
        </div>
        <div class="stat">
          <div class="label">Handles</div>
          <div class="value" id="handleValue">-</div>
        </div>
        <div class="stat">
          <div class="label">Uptime</div>
          <div class="value" id="uptimeValue">-</div>
        </div>
      </section>

      <section class="tables">
        <div class="table-panel">
          <h2>Top CPU</h2>
          <table>
            <thead><tr><th>Process</th><th>CPU</th><th>RAM</th></tr></thead>
            <tbody id="topCpu"></tbody>
          </table>
        </div>
        <div class="table-panel">
          <h2>Top Memory</h2>
          <table>
            <thead><tr><th>Process</th><th>RAM</th><th>CPU</th></tr></thead>
            <tbody id="topMemory"></tbody>
          </table>
        </div>
      </section>
    </section>
  </main>

  <script>
    const keyStorageName = 'customTaskManagerDashboardKey';
    const cpuHistory = [];
    const memoryHistory = [];
    let accessKey = '';

    const authState = document.getElementById('authState');
    const updated = document.getElementById('updated');
    const loginPanel = document.getElementById('loginPanel');
    const dashboard = document.getElementById('dashboard');
    const loginError = document.getElementById('loginError');
    const accessKeyInput = document.getElementById('accessKeyInput');
    const unlockForm = document.getElementById('unlockForm');

    const query = new URLSearchParams(location.search);
    const queryKey = query.get('key');
    if (queryKey) {
      sessionStorage.setItem(keyStorageName, queryKey);
      accessKey = queryKey;
      history.replaceState({}, document.title, location.pathname);
    } else {
      accessKey = sessionStorage.getItem(keyStorageName) || '';
    }

    unlockForm.addEventListener('submit', event => {
      event.preventDefault();
      const value = accessKeyInput.value.trim();
      if (!value) {
        showLogin('Access key is required.');
        return;
      }

      accessKey = value;
      sessionStorage.setItem(keyStorageName, accessKey);
      refresh().catch(showFetchError);
    });

    function showLogin(message = '') {
      loginPanel.hidden = false;
      dashboard.hidden = true;
      authState.textContent = 'Locked';
      authState.className = 'badge locked';
      updated.textContent = 'Waiting for access key';
      loginError.textContent = message;
      accessKeyInput.focus();
    }

    function showDashboard() {
      loginPanel.hidden = true;
      dashboard.hidden = false;
      authState.textContent = 'Live';
      authState.className = 'badge ok';
      loginError.textContent = '';
    }

    function drawGraph(id, values, color, label) {
      const canvas = document.getElementById(id);
      const rect = canvas.getBoundingClientRect();
      canvas.width = Math.floor(rect.width * devicePixelRatio);
      canvas.height = Math.floor(rect.height * devicePixelRatio);
      const ctx = canvas.getContext('2d');
      ctx.scale(devicePixelRatio, devicePixelRatio);
      ctx.clearRect(0, 0, rect.width, rect.height);

      ctx.strokeStyle = '#343a46';
      ctx.lineWidth = 1;
      for (let i = 1; i < 4; i++) {
        const y = rect.height * i / 4;
        ctx.beginPath();
        ctx.moveTo(0, y);
        ctx.lineTo(rect.width, y);
        ctx.stroke();
      }

      ctx.fillStyle = '#f4f6f8';
      ctx.font = '700 14px Segoe UI';
      ctx.fillText(label, 12, 24);

      if (!values.length) {
        return;
      }

      ctx.beginPath();
      values.forEach((value, index) => {
        const x = values.length <= 1 ? 0 : index * rect.width / 59;
        const y = rect.height - (value / 100 * (rect.height - 38)) - 8;
        if (index === 0) ctx.moveTo(x, y); else ctx.lineTo(x, y);
      });
      ctx.strokeStyle = color;
      ctx.lineWidth = 2.5;
      ctx.stroke();
    }

    function setText(id, value) {
      document.getElementById(id).textContent = value;
    }

    function setMeter(id, value) {
      document.getElementById(id).style.width = `${clamp(value, 0, 100)}%`;
    }

    function push(history, value) {
      history.push(clamp(value || 0, 0, 100));
      while (history.length > 60) history.shift();
    }

    function clamp(value, min, max) {
      return Math.max(min, Math.min(max, Number(value) || 0));
    }

    function escapeHtml(value) {
      return String(value ?? '').replace(/[&<>"']/g, c => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[c]));
    }

    function formatPercent(value) {
      return `${(Number(value) || 0).toFixed(1)}%`;
    }

    function formatMemory(value) {
      return `${(Number(value) || 0).toFixed(1)} MB`;
    }

    function formatUptime(value) {
      const text = String(value || '');
      const match = text.match(/^(?:(\d+)\.)?(\d{1,2}):(\d{2}):/);
      if (!match) return text || '-';
      const days = Number(match[1] || 0);
      const hours = Number(match[2] || 0);
      const minutes = Number(match[3] || 0);
      return `${days}d ${hours}h ${minutes}m`;
    }

    function rows(processes, memoryFirst) {
      if (!processes.length) {
        return '<tr><td class="empty" colspan="3">No process data yet</td></tr>';
      }

      return processes.map(p => {
        const firstMetric = memoryFirst ? formatMemory(p.memoryMb) : formatPercent(p.cpuPercent);
        const secondMetric = memoryFirst ? formatPercent(p.cpuPercent) : formatMemory(p.memoryMb);
        return `<tr><td><span class="process">${escapeHtml(p.name)}</span><span class="pid">PID ${p.id}</span></td><td>${firstMetric}</td><td>${secondMetric}</td></tr>`;
      }).join('');
    }

    async function refresh() {
      if (!accessKey) {
        showLogin();
        return;
      }

      const response = await fetch('/api/status', {
        cache: 'no-store',
        headers: { 'X-Dashboard-Key': accessKey }
      });

      if (response.status === 401) {
        sessionStorage.removeItem(keyStorageName);
        accessKey = '';
        showLogin('Access key rejected.');
        return;
      }

      if (!response.ok) {
        throw new Error(`Dashboard API returned ${response.status}`);
      }

      const data = await response.json();
      updateDashboard(data);
    }

    function updateDashboard(data) {
      showDashboard();
      const p = data.performance || {};
      const battery = p.battery || {};
      const cpu = clamp(p.cpuPercent, 0, 100);
      const memory = clamp(p.memoryPercent, 0, 100);
      const batteryPercent = battery.isBatteryPresent ? clamp(battery.chargePercent, 0, 100) : 0;

      push(cpuHistory, cpu);
      push(memoryHistory, memory);
      drawGraph('cpu', cpuHistory, '#2a9d8f', `CPU ${cpu.toFixed(1)}%`);
      drawGraph('memory', memoryHistory, '#e9c46a', `Memory ${memory.toFixed(1)}%`);

      setText('cpuValue', formatPercent(cpu));
      setText('memoryValue', `${(p.memoryUsedGb || 0).toFixed(1)} / ${(p.memoryTotalGb || 0).toFixed(1)} GB`);
      setText('batteryValue', battery.isBatteryPresent ? `${batteryPercent.toFixed(0)}%` : 'No battery');
      setText('powerValue', battery.powerLineStatus || '-');
      setText('processValue', (p.processCount || 0).toLocaleString());
      setText('threadValue', (p.threadCount || 0).toLocaleString());
      setText('handleValue', (p.handleCount || 0).toLocaleString());
      setText('uptimeValue', formatUptime(p.uptime));
      setMeter('cpuMeter', cpu);
      setMeter('memoryMeter', memory);
      setMeter('batteryMeter', batteryPercent);
      setText('updated', `Updated ${new Date(data.timestamp).toLocaleTimeString()} - every 2 seconds`);
      document.getElementById('topCpu').innerHTML = rows(data.topCpu || [], false);
      document.getElementById('topMemory').innerHTML = rows(data.topMemory || [], true);
    }

    function showFetchError(error) {
      authState.textContent = 'Offline';
      authState.className = 'badge locked';
      updated.textContent = error.message || 'Dashboard unavailable';
    }

    refresh().catch(showFetchError);
    setInterval(() => refresh().catch(showFetchError), 2000);
  </script>
</body>
</html>
""";
    }
}
