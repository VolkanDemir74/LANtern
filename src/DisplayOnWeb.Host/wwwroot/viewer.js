const viewer = document.getElementById('viewer');
const message = document.getElementById('messageText') || document.getElementById('message');
const watch = document.getElementById('watch');
const fullscreenButton = document.getElementById('fullscreen');
let connectedUrl = '';

async function connect(force = false) {
  try {
    const [stream, gateway] = await Promise.all([
      fetch('/api/status').then(r => r.json()),
      fetch('/api/obs/status').then(r => r.json())
    ]);
    if (!stream.running || !gateway.running) {
      message.hidden = false;
      message.textContent = 'Yayın henüz açık değil. Otomatik olarak bekleniyor…';
      return;
    }
    const url = `${gateway.watchUrl}?controls=false&muted=true&autoplay=true`;
    if (force || connectedUrl !== url) {
      connectedUrl = url;
      viewer.src = url;
    }
    message.hidden = true;
  } catch {
    message.hidden = false;
    message.textContent = 'LANtern sunucusuna ulaşılamıyor.';
  }
}

async function toggleFullscreen() {
  const active = document.fullscreenElement || document.webkitFullscreenElement;
  if (active) {
    if (document.exitFullscreen) await document.exitFullscreen();
    else if (document.webkitExitFullscreen) document.webkitExitFullscreen();
    try { screen.orientation?.unlock?.(); } catch { }
    return;
  }

  if (watch.requestFullscreen) await watch.requestFullscreen();
  else if (watch.webkitRequestFullscreen) watch.webkitRequestFullscreen();
  try { await screen.orientation?.lock?.('landscape'); } catch { }
}

function updateFullscreenButton() {
  const active = Boolean(document.fullscreenElement || document.webkitFullscreenElement);
  fullscreenButton.textContent = active ? '×' : '⛶';
  fullscreenButton.title = active ? 'Tam ekrandan çık' : 'Tam ekran';
  fullscreenButton.setAttribute('aria-label', fullscreenButton.title);
}

document.getElementById('reconnect').onclick = () => connect(true);
fullscreenButton.onclick = toggleFullscreen;
document.addEventListener('fullscreenchange', updateFullscreenButton);
document.addEventListener('webkitfullscreenchange', updateFullscreenButton);
connect();
setInterval(connect, 2500);
