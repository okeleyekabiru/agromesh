// Renders Ilevora brand rasters from assets/brand/ilevora_mark.svg.
// Run: node mobile/tool/gen_brand.js (uses playwright from web/node_modules)
const path = require('path'), fs = require('fs');
const { chromium } = require(path.resolve(__dirname, '../../web/node_modules/playwright'));
const root = path.resolve(__dirname, '..');
const svg = fs.readFileSync(path.join(root, 'assets/brand/ilevora_mark.svg'), 'utf8');
const inner = svg.replace(/<svg[^>]*>/, '').replace('</svg>', '');
const tile = (size) => svg.replace(/width="96" height="96"/, `width="${size}" height="${size}"`);
// Mark without the green tile, for the adaptive icon foreground.
const bare = inner.replace(/<rect width="96"[^>]*\/>/, '');
const css = `@font-face{font-family:PJS;src:url(data:font/ttf;base64,${fs.readFileSync(path.join(root, 'assets/fonts/PlusJakartaSans-VariableFont_wght.ttf')).toString('base64')});font-weight:200 800}
html,body{margin:0;background:transparent}`;
(async () => {
  const b = await chromium.launch(); const p = await b.newPage();
  async function shot(html, w, h, out, omit = true) {
    await p.setViewportSize({ width: w, height: h });
    await p.setContent(`<style>${css}</style>${html}`);
    await p.evaluate(() => document.fonts.ready);
    fs.mkdirSync(path.dirname(out), { recursive: true });
    await p.screenshot({ path: out, omitBackground: omit });
  }
  const t = path.join(root, 'tool/icon');
  await shot(`<div style="width:1024px;height:1024px;background:#0F7A3A">${tile(1024).replace('rx="22"','rx="0"')}</div>`, 1024, 1024, t + '/icon.png', false);
  // Foreground: content bbox ~ x18..78, y14..86 -> centre (48,50), fit in ~58% of canvas.
  await shot(`<svg xmlns="http://www.w3.org/2000/svg" width="1024" height="1024" viewBox="${48-52} ${50-52} 104 104">${bare}</svg>`, 1024, 1024, t + '/icon_fg.png');
  // Android 12 splash icon: tile with generous padding (circle mask safe).
  await shot(`<div style="width:1152px;height:1152px;display:flex;align-items:center;justify-content:center">${tile(480)}</div>`, 1152, 1152, path.join(root, 'android/app/src/main/res/drawable-nodpi/splash_icon.png'));
  // Lockup: mark above wordmark. Base 1x = 240x240 canvas.
  for (const [s, dir] of [[1, 'LaunchImage.png'], [2, 'LaunchImage@2x.png'], [3, 'LaunchImage@3x.png']]) {
    const html = `<div style="width:${240*s}px;height:${240*s}px;display:flex;flex-direction:column;align-items:center;justify-content:center;gap:${16*s}px">${tile(120*s)}<div style="font:800 ${44*s}px PJS;color:#0D2B1F;letter-spacing:${-1*s}px">Ilevora</div></div>`;
    await shot(html, 240*s, 240*s, path.join(root, 'ios/Runner/Assets.xcassets/LaunchImage.imageset', dir));
  }
  fs.copyFileSync(path.join(root, 'ios/Runner/Assets.xcassets/LaunchImage.imageset/LaunchImage@3x.png'), path.join(root, 'android/app/src/main/res/drawable-nodpi/launch_lockup.png'));
  await b.close();
})();
