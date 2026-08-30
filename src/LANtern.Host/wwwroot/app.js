const $ = id => document.getElementById(id);
let gateway;
let latestApp;
let lang = localStorage.getItem('lantern-language') || (navigator.language?.toLowerCase().startsWith('en') ? 'en' : 'tr');

const text = {
  tr: { adminPanel:'Yönetim paneli',openViewer:'İzleme sayfasını aç',settings:'Ayarlar',save:'Kaydet',cancel:'İptal',startWithWindows:"Windows ile LANtern'ı başlat",autoConnectMonitor:'Sanal monitörü otomatik bağla',autoStartOnMonitorConnect:'Sanal monitör bağlanınca yayını başlat',autoStartStream:'LANtern açılınca yayını otomatik başlat',startInTray:"Tray'de başlat",settingsNote:'Otomatik yayın, ana panelde seçili olan ekranı ve bu yayın profilini kullanır.',settingsSaved:'Ayarlar kaydedildi.',address:'LANtern adresi',display:'Ekran',resolution:'Çözünürlük',scaling:'Görüntü yerleşimi',fps:'Kare hızı',bitrate:'Bit hızı',encoder:'Kodlayıcı',cursor:'Fare imlecini göster',monitorStart:'Sanal monitörü bağla',monitorStop:'Monitör bağlantısını kes',start:'Yayını Başlat',stop:'Yayını Durdur',connect:'Bağlan',fullscreen:'Tam Ekran',qr:'Telefon kamerasıyla okut: izleme sayfası',viewerEmpty:"Yayını görüntülemek için Bağlan'a basın",ready:'Hazır',on:'Yayın açık',off:'Yayın kapalı',unreachable:'Sunucuya ulaşılamıyor',noStatus:'Durum bilgisi alınamadı.',startFailed:'Yayın başlatılamadı.',monitorFailed:'Sanal monitör işlemi başarısız oldu.',localOnly:'Bu ayarlar yalnızca bu bilgisayardaki yönetim panelinden değiştirilebilir.',gatewayNotReady:'WebRTC geçidi henüz hazır değil. Önce yayını başlatın.',choose:'Ekranı seçip yayını başlatın.',gatewayReady:'WebRTC geçidi hazır',error:'Hata',primary:'Ana ekran',fill:'Ekranı doldur — kenarları kırp',fit:'Tam görüntü — siyah şerit olabilir',recommended:'15 Mbps — önerilen',auto:'Otomatik (GPU öncelikli)',software:'Yazılımsal H.264',notStarted:'Başlatılmadı' },
  en: { adminPanel:'Control panel',openViewer:'Open viewer',settings:'Settings',save:'Save',cancel:'Cancel',startWithWindows:'Start LANtern with Windows',autoConnectMonitor:'Automatically connect virtual monitor',autoStartOnMonitorConnect:'Start streaming when virtual monitor connects',autoStartStream:'Automatically start streaming when LANtern starts',startInTray:'Start in tray',settingsNote:'Automatic streaming uses the display selected in the main panel and this streaming profile.',settingsSaved:'Settings saved.',address:'LANtern address',display:'Display',resolution:'Resolution',scaling:'Image fit',fps:'Frame rate',bitrate:'Bitrate',encoder:'Encoder',cursor:'Show mouse cursor',monitorStart:'Connect virtual monitor',monitorStop:'Disconnect monitor',start:'Start Stream',stop:'Stop Stream',connect:'Connect',fullscreen:'Fullscreen',qr:'Scan with your phone camera: viewer page',viewerEmpty:'Press Connect to view the stream',ready:'Ready',on:'Stream on',off:'Stream off',unreachable:'Server unavailable',noStatus:'Could not retrieve status.',startFailed:'Could not start the stream.',monitorFailed:'Virtual monitor operation failed.',localOnly:'These settings can only be changed from the control panel on this computer.',gatewayNotReady:'WebRTC gateway is not ready. Start the stream first.',choose:'Select a display and start streaming.',gatewayReady:'WebRTC gateway ready',error:'Error',primary:'Primary display',fill:'Fill screen — crop edges',fit:'Fit image — may show black bars',recommended:'15 Mbps — recommended',auto:'Automatic (GPU preferred)',software:'Software H.264',notStarted:'Not started' }
};
const t = key => text[lang][key];

function applyLanguage() {
  document.documentElement.lang = lang;
  document.querySelectorAll('[data-i18n]').forEach(el => el.textContent = t(el.dataset.i18n));
  document.querySelectorAll('[data-lang]').forEach(el => el.classList.toggle('active', el.dataset.lang === lang));
  $('scalingMode').options[0].text = t('fill');
  $('scalingMode').options[1].text = t('fit');
  $('bitrate').querySelector('[value="15000"]').text = t('recommended');
  $('encoder').querySelector('[value="auto"]').text = t('auto');
  $('encoder').querySelector('[value="libx264"]').text = t('software');
  if (latestApp) renderStatus(latestApp);
  else $('state').textContent = t('ready');
}

function renderStatus(app) {
  $('state').textContent = app.running ? t('on') : t('off');
  const oldDisplay = $('display').value;
  $('display').innerHTML = app.displays.map(d => `<option value="${d.index}">${d.name} — ${d.width}×${d.height}${d.primary ? ` (${t('primary')})` : ''}</option>`).join('');
  $('display').value = oldDisplay || '0';
  $('monitorStart').disabled = app.virtualDisplayConnected;
  $('monitorStop').disabled = !app.virtualDisplayConnected;
  $('info').textContent = app.error ? `${t('error')}: ${app.error}` : (app.running ? `${t('encoder')}: ${encoderName(app.encoder)} · ${t('gatewayReady')}` : t('choose'));
}

async function status() {
  try {
    const [app, media] = await Promise.all([fetch('/api/status').then(r => r.json()), fetch('/api/obs/status').then(r => r.json())]);
    gateway = media; latestApp = app; $('url').value = app.url; renderStatus(app);
  } catch { $('state').textContent = t('unreachable'); $('info').textContent = t('noStatus'); }
}

async function start() {
  try {
    const [width, height] = $('resolution').value.split('x').map(Number);
    const body = { displayIndex:Number($('display').value),width,height,fps:Number($('fps').value),bitrateKbps:Number($('bitrate').value),encoder:$('encoder').value,scalingMode:$('scalingMode').value,captureCursor:$('captureCursor').checked };
    const response = await fetch('/api/stream/start',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(body)});
    const result = await response.json(); $('info').textContent = result.message; await status(); if (response.ok) await connect();
  } catch { $('info').textContent = t('startFailed'); }
}

async function stop() { disconnect(); await fetch('/api/stream/stop',{method:'POST'}); await status(); }
async function setVirtualDisplay(action) {
  try {
    const response = await fetch(`/api/virtual-display/${action}`,{method:'POST'});
    if (response.status === 403) { $('info').textContent=t('localOnly'); return; }
    const result = await response.json(); $('info').textContent=result.message || t('monitorFailed');
    await new Promise(resolve => setTimeout(resolve,500)); await status();
  } catch { $('info').textContent=t('monitorFailed'); }
}
async function loadSettings() {
  const response = await fetch('/api/settings');
  if (response.status === 403) return;
  const value = await response.json();
  $('resolution').value=`${value.width}x${value.height}`;
  $('fps').value=String(value.fps); $('bitrate').value=String(value.bitrateKbps);
  $('encoder').value=value.encoder; $('scalingMode').value=value.scalingMode;
  $('captureCursor').checked=value.captureCursor; $('startWithWindows').checked=value.startWithWindows;
  $('autoConnectMonitor').checked=value.autoConnectVirtualDisplay; $('autoStartOnMonitorConnect').checked=value.autoStartWhenMonitorConnect; $('autoStartStream').checked=value.autoStartStream;
  $('startInTray').checked=value.startInTray;
  if (value.preferredDisplayName && latestApp) {
    const display=latestApp.displays.find(d=>d.name===value.preferredDisplayName);
    if(display) $('display').value=String(display.index);
  }
}
async function saveSettings() {
  const [width,height]=$('resolution').value.split('x').map(Number);
  const selected=latestApp?.displays?.find(d=>d.index===Number($('display').value));
  const value={startWithWindows:$('startWithWindows').checked,autoConnectVirtualDisplay:$('autoConnectMonitor').checked,
    autoStartStream:$('autoStartStream').checked,autoStartWhenMonitorConnect:$('autoStartOnMonitorConnect').checked,startInTray:$('startInTray').checked,
    preferredDisplayName:selected?.name||'',width,height,fps:Number($('fps').value),bitrateKbps:Number($('bitrate').value),
    encoder:$('encoder').value,scalingMode:$('scalingMode').value,captureCursor:$('captureCursor').checked};
  const response=await fetch('/api/settings',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(value)});
  if(response.status===403){$('info').textContent=t('localOnly');return;}
  if(!response.ok){$('info').textContent=t('monitorFailed');return;}
  $('settingsDialog').close(); $('info').textContent=t('settingsSaved');
}
async function connect() { await status(); if (!gateway?.running) { $('info').textContent=t('gatewayNotReady'); return; } $('viewer').removeAttribute('srcdoc'); $('viewer').src=`${gateway.watchUrl}?controls=false&muted=true&autoplay=true`; $('empty').hidden=true; }
function disconnect() { const viewer=$('viewer'); viewer.removeAttribute('src'); viewer.srcdoc='<!doctype html><style>html,body{width:100%;height:100%;margin:0;background:#030508}</style>'; $('empty').hidden=false; }
function encoderName(value) { return ({h264_nvenc:'NVIDIA NVENC',h264_qsv:'Intel Quick Sync',h264_amf:'AMD AMF',libx264:t('software'),'Başlatılmadı':t('notStarted')})[value] || value; }

document.querySelectorAll('[data-lang]').forEach(button => button.onclick = () => { lang=button.dataset.lang; localStorage.setItem('lantern-language',lang); applyLanguage(); });
$('monitorStart').onclick=()=>setVirtualDisplay('start'); $('monitorStop').onclick=()=>setVirtualDisplay('stop');
$('openSettings').onclick=()=>$('settingsDialog').showModal(); $('saveSettings').onclick=saveSettings;
$('start').onclick=start; $('stop').onclick=stop; $('connect').onclick=connect;
async function enterFullscreen(){const shell=$('viewerShell');if(shell.requestFullscreen)await shell.requestFullscreen();else if(shell.webkitRequestFullscreen)shell.webkitRequestFullscreen();try{await screen.orientation?.lock?.('landscape')}catch{}}
function fullscreenChanged(){const active=Boolean(document.fullscreenElement||document.webkitFullscreenElement);$('viewerShell').classList.toggle('is-fullscreen',active)}
$('fullscreen').onclick=enterFullscreen; document.addEventListener('fullscreenchange',fullscreenChanged); document.addEventListener('webkitfullscreenchange',fullscreenChanged);
async function bootstrap(){applyLanguage();await status();try{await loadSettings()}catch{}setInterval(status,3000)}
bootstrap();
