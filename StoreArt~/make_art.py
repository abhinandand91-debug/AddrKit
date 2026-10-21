#!/usr/bin/env python3
"""
Generates the Asset Store art for AddrKit: HTML pages at exact pixel sizes, rendered to PNG with headless Chrome.

  python3 make_art.py          # writes html/*.html and out/*.png

Sources: src/*.png are real Editor captures. Everything else is drawn here.
Asset Store rules honoured: icon has no text; card has only title and publisher; no Unity logos;
key images are designed graphics, not bare Editor screenshots.
"""
import os
import subprocess
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
CHROME = "/Applications/Google Chrome.app/Contents/MacOS/Google Chrome"
PUBLISHER = "Abhinandan Dharmadhikari"

GLYPH = """<svg viewBox="0 0 100 100" xmlns="http://www.w3.org/2000/svg">
<defs><linearGradient id="g" x1="0" y1="0" x2="1" y2="1"><stop offset="0" stop-color="#6db4ff"/><stop offset="1" stop-color="#2b63f0"/></linearGradient></defs>
<rect width="100" height="100" rx="22" fill="url(#g)"/>
<path d="M28 76 L50 26 L72 76" fill="none" stroke="#fff" stroke-width="9" stroke-linecap="round" stroke-linejoin="round"/>
<path d="M37 60 H63" stroke="#fff" stroke-width="9" stroke-linecap="round"/>
<circle cx="50" cy="25" r="6.5" fill="#ffb454"/></svg>"""

BASE_CSS = """
*{box-sizing:border-box;margin:0;padding:0}
html,body{width:%(w)dpx;height:%(h)dpx;overflow:hidden}
body{font-family:-apple-system,'SF Pro Display','Helvetica Neue',Helvetica,Arial,sans-serif;color:#eef2f8;
 background:radial-gradient(1200px 800px at 85%% -10%%,#1b2a4a 0%%,#0d1118 60%%) #0d1118}
.code{font-family:'SF Mono',Menlo,Consolas,monospace;background:#0a0d13;border:1px solid #232b3b;border-radius:14px;color:#cfd8e6}
.kw{color:#7db4ff}.str{color:#9be29b}.cm{color:#6c7a90}.hl{color:#ffb454}
.pill{display:inline-block;padding:8px 16px;border-radius:999px;background:#16203a;border:1px solid #2a3a63;color:#bcd2ff;font-weight:600}
.shot{border-radius:16px;box-shadow:0 30px 80px rgba(0,0,0,.55),0 0 0 1px rgba(255,255,255,.08);background-repeat:no-repeat;background-size:cover}
"""


def page(w, h, body, css=""):
    return f"<!doctype html><meta charset='utf-8'><style>{BASE_CSS % {'w': w, 'h': h}}{css}</style><body>{body}</body>"


def write(name, w, h, html):
    path = os.path.join(HERE, "html", name + ".html")
    os.makedirs(os.path.dirname(path), exist_ok=True)
    open(path, "w").write(html)
    out = os.path.join(HERE, "out", name + ".png")
    os.makedirs(os.path.dirname(out), exist_ok=True)
    subprocess.run([CHROME, "--headless=new", "--disable-gpu", "--hide-scrollbars", "--force-device-scale-factor=1",
                    f"--window-size={w},{h}", f"--screenshot={out}", "file://" + path],
                   check=True, stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    print("wrote", out, w, h)


def img(name):
    return "file://" + os.path.join(HERE, "src", name)


# ------------------------------------------------------------------ key images

def icon():
    # 160x160, no text.
    write("icon_160x160", 160, 160, page(160, 160, f"<div style='width:160px;height:160px;display:grid;place-items:center;background:#0d1118'><div style='width:136px;height:136px'>{GLYPH}</div></div>"))


def card():
    # 420x280: title and publisher only.
    body = f"""
    <div style="width:420px;height:280px;padding:34px;display:flex;flex-direction:column;justify-content:space-between">
      <div style="width:84px;height:84px">{GLYPH}</div>
      <div><div style="font-size:54px;font-weight:800;letter-spacing:-1px;line-height:1">AddrKit</div>
      <div style="margin-top:8px;font-size:19px;color:#9fb0cc">{PUBLISHER}</div></div>
    </div>"""
    write("card_420x280", 420, 280, page(420, 280, body))


def cover():
    w, h = 1950, 1300
    body = f"""
    <div style="position:relative;width:{w}px;height:{h}px">
      <div style="position:absolute;left:110px;top:150px;width:880px">
        <div style="display:flex;align-items:center;gap:26px"><div style="width:110px;height:110px">{GLYPH}</div>
          <div style="font-size:92px;font-weight:800;letter-spacing:-2px">AddrKit</div></div>
        <div style="margin-top:56px;font-size:84px;font-weight:800;line-height:1.04;letter-spacing:-2px">
          Addressables,<br>without the <span style="color:#ffb454">leaks</span>.</div>
        <div style="margin-top:30px;font-size:34px;color:#a7b6cf;line-height:1.35">Load in one line. Release automatically.<br>See everything in memory.</div>
        <div style="margin-top:50px;display:flex;flex-wrap:wrap;gap:14px;font-size:25px">
          <span class="pill">Owner-based auto release</span><span class="pill">Typed keys</span><span class="pill">Leak debugger</span>
          <span class="pill">Folder rules</span><span class="pill">Preload + cancel</span></div>
        <div class="code" style="margin-top:60px;padding:34px 40px;font-size:28px;line-height:1.6;white-space:nowrap">
<span class="kw">var</span> goblin = <span class="kw">await</span> Addr.<span class="hl">Spawn</span>(<br>
&nbsp;&nbsp;&nbsp;&nbsp;Keys.Enemies.Goblin, owner: <span class="kw">this</span>);<br>
<span class="cm">// 'this' destroyed =&gt; goblin released.</span></div>
      </div>
      <div class="shot" style="position:absolute;left:1040px;top:130px;width:800px;height:760px;background-image:url('{img('play_debug.png')}');background-size:800px auto;background-position:top left"></div>
      <div class="shot" style="position:absolute;left:1180px;top:640px;width:690px;height:470px;background-image:url('{img('play_game.png')}');background-size:690px auto;background-position:center"></div>
    </div>"""
    write("cover_1950x1300", w, h, page(w, h, body))


def social():
    w, h = 1200, 630
    body = f"""
    <div style="padding:64px 72px;width:{w}px;height:{h}px;position:relative">
      <div style="display:flex;align-items:center;gap:20px"><div style="width:76px;height:76px">{GLYPH}</div>
        <div style="font-size:60px;font-weight:800;letter-spacing:-1px">AddrKit</div></div>
      <div style="margin-top:44px;font-size:68px;font-weight:800;line-height:1.05;letter-spacing:-1.5px;width:640px">
        Addressables,<br>without the <span style="color:#ffb454">leaks</span>.</div>
      <div style="margin-top:22px;font-size:27px;color:#a7b6cf;width:600px">Auto release by owner, typed keys, leak debugger. For Unity 6.</div>
      <div class="shot" style="position:absolute;right:-70px;top:90px;width:520px;height:480px;background-image:url('{img('play_debug.png')}');background-size:520px auto;background-position:top left"></div>
    </div>"""
    write("social_1200x630", w, h, page(w, h, body))


# ------------------------------------------------------------------ screenshots (1920x1080)

def pic_size(name):
    return {"play_game.png": (2000, 1280), "play_debug.png": (1560, 1520), "tab1.png": (1560, 1360), "tab2.png": (1560, 1360),
            "tab3.png": (1560, 1360), "tab4.png": (1560, 1360), "tab0.png": (1560, 1360), "drawer_typo.png": (1040, 720)}[name]


def screenshot(name, title, sub, image, crop_h_ratio=None, snippet=None, bullets=None, second=None):
    w, h = 1920, 1080
    box_w = 1040
    # Real captures keep their aspect; optionally cropped to the top part.
    pic = {"play_game.png": (2000, 1280), "play_debug.png": (1560, 1520), "tab1.png": (1560, 1360), "tab2.png": (1560, 1360),
           "tab3.png": (1560, 1360), "tab4.png": (1560, 1360), "tab0.png": (1560, 1360), "drawer_typo.png": (1040, 720)}[image]
    ratio = pic[1] / pic[0]
    shown_h = box_w * ratio
    if crop_h_ratio:
        shown_h = shown_h * crop_h_ratio
    shown_h = min(shown_h, 920)
    extra = ""
    if snippet:
        extra += f'<div class="code" style="margin-top:36px;padding:24px 28px;font-size:22px;line-height:1.55;white-space:nowrap">{snippet}</div>'
    if bullets:
        extra += "<ul style='margin-top:34px;list-style:none;font-size:27px;color:#b4c3dc;line-height:1.9'>" + "".join(
            f"<li><span style='color:#35c46f;font-weight:800'>&#10003;</span>&nbsp; {b}</li>" for b in bullets) + "</ul>"
    second_html = ""
    total_h = shown_h
    if second:
        s_img, s_crop = second
        sw, sh = pic_size(s_img)
        s_h = box_w * sh / sw * s_crop
        gap = 28
        total_h = shown_h + gap + s_h
        top0 = (h - total_h) / 2
        second_html = f"""<div class="shot" style="position:absolute;right:80px;top:{top0 + shown_h + gap:.0f}px;width:{box_w}px;height:{s_h:.0f}px;
        background-image:url('{img(s_img)}');background-size:{box_w}px auto;background-position:top center"></div>"""
    top = (h - total_h) / 2
    body = f"""
    <div style="position:relative;width:{w}px;height:{h}px">
      <div style="position:absolute;left:90px;top:110px;width:640px">
        <div style="display:flex;align-items:center;gap:16px;margin-bottom:44px"><div style="width:52px;height:52px">{GLYPH}</div>
          <div style="font-size:30px;font-weight:700;color:#9fb0cc">AddrKit</div></div>
        <div style="font-size:70px;font-weight:800;line-height:1.05;letter-spacing:-1.5px">{title}</div>
        <div style="margin-top:26px;font-size:31px;color:#a7b6cf;line-height:1.4">{sub}</div>
        {extra}
      </div>
      <div class="shot" style="position:absolute;right:80px;top:{top:.0f}px;width:{box_w}px;height:{shown_h:.0f}px;
        background-image:url('{img(image)}');background-size:{box_w}px auto;background-position:top center"></div>
      {second_html}
    </div>"""
    write(name, w, h, page(w, h, body))


def screenshots():
    screenshot("shot1_spawn_forget", "Spawn it.<br>Forget it.",
               "Owner-based release means loaded assets go away with the thing that asked for them.",
               "play_game.png",
               snippet='Addr.<span class="hl">Spawn</span>(Keys.Enemies.Goblin, owner: <span class="kw">this</span>);')
    screenshot("shot2_leak_debugger", "See every load.<br>Find every leak.",
               "Live list of everything in memory: owner, age, and the line of code that loaded it.",
               "play_debug.png", crop_h_ratio=0.62,
               bullets=["Global loads highlighted", "Search and filter", "One-click copy report"])
    screenshot("shot3_rules", "Point at a folder.<br>Done.",
               "A rule turns a folder into addresses, a group and labels, with a live preview.",
               "tab1.png", crop_h_ratio=0.80,
               bullets=["Auto-applies on change", "Preview before you commit", "Local or remote groups"])
    screenshot("shot4_typed_keys", "Typos fail<br>at compile time.",
               "Addresses become generated constants. Rename one and every stale use is a build error.",
               "tab2.png", crop_h_ratio=0.95,
               snippet='Keys.Enemies.<span class="hl">Goblin</span>   <span class="cm">// not "Enemies/Gobln"</span>')
    screenshot("shot5_analyzer", "Catch duplicates.<br>Fix in one click.",
               "The analyzer finds assets copied into several bundles, duplicate addresses and stale keys.",
               "tab3.png", crop_h_ratio=0.32, second=("tab0.png", 0.435))
    screenshot("shot6_inspector", "Mistakes show up<br>in the Inspector.",
               "Pick addresses from a dropdown. A wrong one turns red and suggests the right one.",
               "drawer_typo.png")
    screenshot("shot7_remote", "Test downloadable<br>content locally.",
               "Build, serve and load remote bundles from your own machine. No hosting needed.",
               "tab4.png", crop_h_ratio=0.72,
               bullets=["Play mode script switcher", "Built-in local server", "Preload with progress"])


if __name__ == "__main__":
    which = sys.argv[1:] or ["icon", "card", "cover", "social", "screenshots"]
    for name in which:
        globals()[name]()
