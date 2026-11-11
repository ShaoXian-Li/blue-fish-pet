from PIL import Image, ImageDraw
import os

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
SRC = os.path.join(ROOT, "pic")
OUT = os.path.join(ROOT, "assets", "expressions")
os.makedirs(OUT, exist_ok=True)

# Chinese filename -> stable resource key
FILES = [
    ("日常", "jpg", "daily"),
    ("高兴", "jpg", "happy"),
    ("疑惑", "jpg", "confused"),
    ("害羞", "png", "shy"),
    ("吃白饭", "png", "eating"),
    ("委屈", "png", "sad"),
    ("收到", "png", "ok"),
    ("生气", "png", "angry"),
    ("睡觉", "jpg", "sleep"),
    ("大哭", "jpg", "cry"),
    ("躺下", "png", "lie"),
    ("震惊", "png", "shock"),
]


def cutout(path):
    im = Image.open(path).convert("RGBA")
    w, h = im.size
    probe = im.copy()
    step = max(4, w // 180)
    for x in range(0, w, step):
        for p in ((x, 0), (x, h - 1)):
            r, g, b, a = probe.getpixel(p)
            if r > 225 and g > 225 and b > 225:
                ImageDraw.floodfill(probe, p, (255, 0, 255, 255), thresh=26)
    for y in range(0, h, step):
        for p in ((0, y), (w - 1, y)):
            r, g, b, a = probe.getpixel(p)
            if r > 225 and g > 225 and b > 225:
                ImageDraw.floodfill(probe, p, (255, 0, 255, 255), thresh=26)
    px = probe.load(); outp = im.load()
    for y in range(h):
        for x in range(w):
            if px[x, y][:3] == (255, 0, 255):
                outp[x, y] = (255, 255, 255, 0)
    # Remove the grey "豆包 AI 生成" watermark in the lower-right corner.
    for y in range(int(h * 0.86), h):
        for x in range(int(w * 0.66), w):
            r, g, b, a = outp[x, y]
            if a < 8:
                continue
            mx, mn = max(r, g, b), min(r, g, b)
            avg = (r + g + b) / 3
            if mx - mn < 20 and 85 < avg < 238:
                outp[x, y] = (255, 255, 255, 0)
    bbox = im.getbbox()
    if bbox:
        im = im.crop(bbox)
    return im


def normalize(im, key, target_h=600):
    scale = target_h / im.height
    if im.width * scale > 610:
        scale = 610 / im.width
    im = im.resize((max(1, int(im.width * scale)), max(1, int(im.height * scale))), Image.LANCZOS)
    canvas = Image.new("RGBA", (640, 640), (0, 0, 0, 0))
    canvas.alpha_composite(im, ((640 - im.width) // 2, 620 - im.height))
    canvas.save(os.path.join(OUT, key + ".png"))
    return canvas


for cn, ext, key in FILES:
    im = cutout(os.path.join(SRC, cn + "." + ext))
    cv = normalize(im, key)
    print(key, im.size, "->", cv.size)
