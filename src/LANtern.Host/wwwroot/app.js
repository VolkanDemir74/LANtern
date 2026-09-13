const $ = id => document.getElementById(id);
let gateway;
let latestApp;
let loadedSettings;
let availableUpdate;
let lang = localStorage.getItem('lantern-language') || (navigator.language?.toLowerCase().startsWith('en') ? 'en' : 'tr');

const text = {
  tr: { adminPanel:'Yönetim paneli',openViewer:'İzleme sayfasını aç',settings:'Ayarlar',save:'Kaydet',cancel:'İptal',startWithWindows:"Windows ile LANtern'ı başlat",autoConnectMonitor:'Sanal monitörü otomatik bağla',autoStartOnMonitorConnect:'Sanal monitör bağlanınca yayını başlat',autoStartStream:'LANtern açılınca yayını otomatik başlat',startInTray:"Tray'de başlat",settingsNote:'Otomatik yayın, ana panelde seçili olan ekranı ve bu yayın profilini kullanır.',settingsSaved:'Ayarlar kaydedildi.',address:'LANtern adresi',display:'Ekran',resolution:'Çözünürlük',scaling:'Görüntü yerleşimi',fps:'Kare hızı',bitrate:'Bit hızı',encoder:'Kodlayıcı',cursor:'Fare imlecini göster',monitorStart:'Sanal monitörü bağla',monitorStop:'Monitör bağlantısını kes',start:'Yayını Başlat',stop:'Yayını Durdur',connect:'Bağlan',fullscreen:'Tam Ekran',qr:'Telefon kamerasıyla okut: izleme sayfası',viewerEmpty:"Yayını görüntülemek için Bağlan'a basın",ready:'Hazır',on:'Yayın açık',off:'Yayın kapalı',unreachable:'Sunucuya ulaşılamıyor',noStatus:'Durum bilgisi alınamadı.',startFailed:'Yayın başlatılamadı.',monitorFailed:'Sanal monitör işlemi başarısız oldu.',localOnly:'Bu ayarlar yalnızca bu bilgisayardaki yönetim panelinden değiştirilebilir.',gatewayNotReady:'WebRTC geçidi henüz hazır değil. Önce yayını başlatın.',choose:'Ekranı seçip yayını başlatın.',gatewayReady:'WebRTC geçidi hazır',error:'Hata',primary:'Ana ekran',fill:'Ekranı doldur — kenarları kırp',fit:'Tam görüntü — siyah şerit olabilir',recommended:'15 Mbps — önerilen',auto:'Otomatik (GPU öncelikli)',software:'Yazılımsal H.264',notStarted:'Başlatılmadı' },
  en: { adminPanel:'Control panel',openViewer:'Open viewer',settings:'Settings',save:'Save',cancel:'Cancel',startWithWindows:'Start LANtern with Windows',autoConnectMonitor:'Automatically connect virtual monitor',autoStartOnMonitorConnect:'Start streaming when virtual monitor connects',autoStartStream:'Automatically start streaming when LANtern starts',startInTray:'Start in tray',settingsNote:'Automatic streaming uses the display selected in the main panel and this streaming profile.',settingsSaved:'Settings saved.',address:'LANtern address',display:'Display',resolution:'Resolution',scaling:'Image fit',fps:'Frame rate',bitrate:'Bitrate',encoder:'Encoder',cursor:'Show mouse cursor',monitorStart:'Connect virtual monitor',monitorStop:'Disconnect monitor',start:'Start Stream',stop:'Stop Stream',connect:'Connect',fullscreen:'Fullscreen',qr:'Scan with your phone camera: viewer page',viewerEmpty:'Press Connect to view the stream',ready:'Ready',on:'Stream on',off:'Stream off',unreachable:'Server unavailable',noStatus:'Could not retrieve status.',startFailed:'Could not start the stream.',monitorFailed:'Virtual monitor operation failed.',localOnly:'These settings can only be changed from the control panel on this computer.',gatewayNotReady:'WebRTC gateway is not ready. Start the stream first.',choose:'Select a display and start streaming.',gatewayReady:'WebRTC gateway ready',error:'Error',primary:'Primary display',fill:'Fill screen — crop edges',fit:'Fit image — may show black bars',recommended:'15 Mbps — recommended',auto:'Automatic (GPU preferred)',software:'Software H.264',notStarted:'Not started' }
};
Object.assign(text.tr, {checkForUpdates:'Açılışta güncellemeleri denetle',checkUpdate:'Güncellemeleri denetle',updateAvailable:'Yeni LANtern sürümü bulundu',installNow:'Şimdi yükle',later:'Daha sonra',skipVersion:'Bu sürümü atla',never:'Asla denetleme',updateCurrent:'LANtern güncel.',updateChecking:'Denetleniyor…',updateFailed:'Güncelleme denetlenemedi.',updateInstalling:'Güncelleme hazırlanıyor…',updateDownloading:'İndiriliyor',updateVerifying:'Doğrulanıyor',updateLaunching:'Kurulum başlatılıyor'});
Object.assign(text.en, {checkForUpdates:'Check for updates at startup',checkUpdate:'Check for updates',updateAvailable:'A new LANtern version is available',installNow:'Install now',later:'Later',skipVersion:'Skip this version',never:'Never check',updateCurrent:'LANtern is up to date.',updateChecking:'Checking…',updateFailed:'Could not check for updates.',updateInstalling:'Preparing the update…',updateDownloading:'Downloading',updateVerifying:'Verifying',updateLaunching:'Starting installer'});
Object.assign(text.tr, {controlPanelClient:'Yönetim paneli istemcisi',clientNative:'Native (önerilen)',viewerEmpty:'Önizleme için yayını başlatın'});
Object.assign(text.en, {controlPanelClient:'Control panel client',clientNative:'Native (recommended)',viewerEmpty:'Start streaming to preview'});
Object.assign(text.tr, {generalSettings:'Genel',startupAutomation:'Başlangıç ve otomasyon',startWithWindows:"Windows açıldığında LANtern'ı çalıştır",startInTray:"LANtern'ı panel açmadan tray'de başlat",autoConnectMonitor:'LANtern açıldığında sanal monitörü bağla',autoStartStream:'LANtern açıldığında seçili ekranın yayınını başlat',autoStartOnMonitorConnect:'Panelden sanal monitör bağlanınca yayınını da başlat',settingsNote:'Başlangıç yayını, ana panelde son seçilen ekranı ve yukarıdaki yayın profilini kullanır.'});
Object.assign(text.en, {generalSettings:'General',startupAutomation:'Startup and automation',startWithWindows:'Run LANtern when Windows starts',startInTray:'Start LANtern in the tray without opening the panel',autoConnectMonitor:'Connect the virtual monitor when LANtern starts',autoStartStream:'Stream the selected display when LANtern starts',autoStartOnMonitorConnect:'Also start streaming when the virtual monitor is connected from the panel',settingsNote:'Startup streaming uses the last display selected in the main panel and the profile above.'});
Object.assign(text.tr, {autoRecoverStream:'Beklenmedik durumda yayını yeniden başlat'});
Object.assign(text.en, {autoRecoverStream:'Restart streaming after an unexpected stop'});
const t = key => text[lang][key];

function applyLanguage() {
  document.documentElement.lang = lang;
  document.querySelectorAll('[data-i18n]').forEach(el => el.textContent = t(el.dataset.i18n));
  document.querySelectorAll('[data-lang]').forEach(el => el.classList.toggle('active', el.dataset.lang === lang));
  $('scalingMode').options[0].text = t('fill');
  $('scalingMode').options[1].text = t('fit');
  $('bitrate').querySelector('[value="10000"]').text = lang === 'tr'
    ? '10 Mbps — düşük gecikme'
    : '10 Mbps — low latency';
  $('encoder').querySelector('[value="auto"]').text = t('auto');
  $('encoder').querySelector('[value="libx264"]').text = t('software');
  if (latestApp) renderStatus(latestApp);
  else $('state').textContent = t('ready');
}

function renderStatus(app) {
  $('appVersion').textContent=app.version?`v${app.version}`:'';
  $('state').textContent = app.running ? t('on') : t('off');
  const oldDisplay = $('display').value;
  $('display').innerHTML = app.displays.map(d => `<option value="${d.index}">${d.name} — ${d.width}×${d.height}${d.primary ? ` (${t('primary')})` : ''}</option>`).join('');
  $('display').value = oldDisplay || '0';
  $('monitorToggle').textContent = app.virtualDisplayConnected ? t('monitorStop') : t('monitorStart');
  $('monitorToggle').classList.toggle('is-active', app.virtualDisplayConnected);
  $('streamToggle').textContent = app.running ? t('stop') : t('start');
  $('streamToggle').classList.toggle('is-active', app.running);
  const performance = app.encodeFps > 0 ? ` · ${app.encodeFps.toFixed(1)} FPS · ${app.encodeSpeed.toFixed(2)}× · Drop ${app.droppedFrames} · Dup ${app.duplicatedFrames}` : '';
  $('info').textContent = app.error ? `${t('error')}: ${app.error}` : (app.running ? `${t('encoder')}: ${encoderName(app.encoder)} · ${t('gatewayReady')}${performance}` : t('choose'));
}

async function status() {
  try {
    const [app, media] = await Promise.all([fetch('/api/status').then(r => r.json()), fetch('/api/obs/status').then(r => r.json())]);
    gateway = media; latestApp = app; $('url').textContent = app.url; renderStatus(app);
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
  loadedSettings=value;
  $('resolution').value=`${value.width}x${value.height}`;
  $('fps').value=String(value.fps); $('bitrate').value=String(value.bitrateKbps);
  $('encoder').value=value.encoder; $('scalingMode').value=value.scalingMode;
  $('captureCursor').checked=value.captureCursor; $('startWithWindows').checked=value.startWithWindows;
  $('autoRecoverStream').checked=value.autoRecoverStream;
  $('autoConnectMonitor').checked=value.autoConnectVirtualDisplay; $('autoStartOnMonitorConnect').checked=value.autoStartWhenMonitorConnect; $('autoStartStream').checked=value.autoStartStream;
  $('startInTray').checked=value.startInTray;
  $('checkForUpdates').checked=value.checkForUpdates;
  $('controlPanelClient').value=value.controlPanelClient||'native';
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
    checkForUpdates:$('checkForUpdates').checked,skippedUpdateVersion:loadedSettings?.skippedUpdateVersion||'',controlPanelClient:$('controlPanelClient').value,
    preferredDisplayName:selected?.name||'',width,height,fps:Number($('fps').value),bitrateKbps:Number($('bitrate').value),
    encoder:$('encoder').value,scalingMode:$('scalingMode').value,captureCursor:$('captureCursor').checked,autoRecoverStream:$('autoRecoverStream').checked};
  const response=await fetch('/api/settings',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(value)});
  if(response.status===403){$('info').textContent=t('localOnly');return;}
  if(!response.ok){$('info').textContent=t('monitorFailed');return;}
  $('settingsDialog').close(); $('info').textContent=t('settingsSaved');
}
async function checkUpdate(manual=false) {
  $('updateStatus').textContent=t('updateChecking');
  try {
    const response=await fetch(`/api/update/check?manual=${manual}`);
    if(!response.ok) throw new Error();
    const result=await response.json();
    if(result.available){availableUpdate=result;$('updateMessage').textContent=`${result.currentVersion} → ${result.latestVersion}`;$('updateDialog').showModal();$('updateStatus').textContent=result.latestVersion;}
    else $('updateStatus').textContent=t('updateCurrent');
  } catch {$('updateStatus').textContent=t('updateFailed');}
}
async function setUpdatePreference(value){await fetch('/api/update/preference',{method:'POST',headers:{'content-type':'application/json'},body:JSON.stringify(value)});$('updateDialog').close();await loadSettings();}
async function installUpdate(){
  $('installUpdate').disabled=true;$('updateMessage').textContent=t('updateInstalling');
  $('updateProgress').hidden=false;$('updateProgressText').hidden=false;
  const renderProgress=progress=>{const percent=Math.max(0,Math.min(100,progress.percent||0));$('updateProgressBar').style.width=`${percent}%`;const key=progress.stage==='verifying'?'updateVerifying':progress.stage==='launching'?'updateLaunching':'updateDownloading';$('updateProgressText').textContent=`${t(key)} · ${percent}%`;};
  const timer=setInterval(async()=>{try{const response=await fetch('/api/update/progress');if(response.ok)renderProgress(await response.json())}catch{}},250);
  try {
    const response=await fetch('/api/update/install',{method:'POST'});
    if(!response.ok)throw new Error();
    renderProgress({percent:100,stage:'launching'});
  } catch {
    $('installUpdate').disabled=false;$('updateMessage').textContent=t('updateFailed');
  } finally {clearInterval(timer);}
}
async function connect() { await status(); if (!gateway?.running) { $('info').textContent=t('gatewayNotReady'); return; } $('viewer').removeAttribute('srcdoc'); $('viewer').src=`${gateway.watchUrl}?controls=false&muted=true&autoplay=true`; $('empty').hidden=true; }
function disconnect() { const viewer=$('viewer'); viewer.removeAttribute('src'); viewer.srcdoc='<!doctype html><style>html,body{width:100%;height:100%;margin:0;background:#030508}</style>'; $('empty').hidden=false; }
function encoderName(value) { return ({h264_nvenc:'NVIDIA NVENC',h264_qsv:'Intel Quick Sync',h264_amf:'AMD AMF',libx264:t('software'),'Başlatılmadı':t('notStarted')})[value] || value; }

document.querySelectorAll('[data-lang]').forEach(button => button.onclick = () => { lang=button.dataset.lang; localStorage.setItem('lantern-language',lang); applyLanguage(); });
$('monitorToggle').onclick=()=>setVirtualDisplay(latestApp?.virtualDisplayConnected?'stop':'start');
$('streamToggle').onclick=()=>latestApp?.running?stop():start();
$('openSettings').onclick=()=>$('settingsDialog').showModal(); $('saveSettings').onclick=saveSettings;
$('checkUpdate').onclick=()=>checkUpdate(true); $('installUpdate').onclick=installUpdate;
$('qrToggle').onclick=()=>{const details=$('qrDetails');details.hidden=!details.hidden;$('qrToggle').setAttribute('aria-expanded',String(!details.hidden));};
$('skipUpdate').onclick=()=>setUpdatePreference({disable:false,skipVersion:availableUpdate?.latestVersion});
$('disableUpdates').onclick=()=>setUpdatePreference({disable:true,skipVersion:null});
async function enterFullscreen(){const shell=$('viewerShell');if(shell.requestFullscreen)await shell.requestFullscreen();else if(shell.webkitRequestFullscreen)shell.webkitRequestFullscreen();try{await screen.orientation?.lock?.('landscape')}catch{}}
function fullscreenChanged(){const active=Boolean(document.fullscreenElement||document.webkitFullscreenElement);$('viewerShell').classList.toggle('is-fullscreen',active)}
$('fullscreen').onclick=enterFullscreen; document.addEventListener('fullscreenchange',fullscreenChanged); document.addEventListener('webkitfullscreenchange',fullscreenChanged);
async function bootstrap(){applyLanguage();await status();try{await loadSettings();if(loadedSettings?.checkForUpdates)await checkUpdate(false)}catch{}setInterval(status,3000)}
bootstrap();
