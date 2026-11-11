from PIL import Image, ImageDraw
import math

S = 1024
img = Image.new("RGBA", (S, S), (0, 0, 0, 0))
d = ImageDraw.Draw(img)

# soft ocean disc
for i in range(40, 0, -1):
    a = int(28 * (i / 40) ** 2)
    r = int(S * 0.47 * (i / 40))
    cx, cy = S // 2, S // 2
    d.ellipse((cx - r, cy - r + 20, cx + r, cy + r + 20), fill=(70, 180, 255, a))

# tail
d.polygon([(S*0.72, S*0.66), (S*0.91, S*0.52), (S*0.87, S*0.68), (S*0.94, S*0.76),
           (S*0.82, S*0.78), (S*0.70, S*0.76)], fill=(36, 92, 210, 255))
d.polygon([(S*0.72, S*0.68), (S*0.88, S*0.57), (S*0.85, S*0.68), (S*0.90, S*0.73),
           (S*0.80, S*0.74), (S*0.71, S*0.73)], fill=(93, 176, 255, 255))

# body blob
d.ellipse((S*0.16, S*0.33, S*0.83, S*0.92), fill=(44, 112, 235, 255))
# belly
d.ellipse((S*0.29, S*0.48, S*0.72, S*0.91), fill=(224, 242, 255, 255))
# apron
d.rounded_rectangle((S*0.36, S*0.54, S*0.66, S*0.86), radius=int(S*0.05), fill=(250, 253, 255, 255))

# fin ear left / right
d.polygon([(S*0.17, S*0.43), (S*0.02, S*0.36), (S*0.08, S*0.55), (S*0.20, S*0.57)], fill=(78, 162, 250, 255))
d.polygon([(S*0.82, S*0.43), (S*0.98, S*0.36), (S*0.92, S*0.55), (S*0.80, S*0.57)], fill=(78, 162, 250, 255))

# hair cap
d.pieslice((S*0.18, S*0.10, S*0.82, S*0.74), 180, 360, fill=(47, 106, 225, 255))
d.ellipse((S*0.18, S*0.34, S*0.34, S*0.83), fill=(39, 93, 210, 255))
d.ellipse((S*0.66, S*0.34, S*0.82, S*0.83), fill=(39, 93, 210, 255))

# headband frill
for i in range(9):
    x = S*0.25 + i*S*0.0625
    d.ellipse((x-S*0.038, S*0.245, x+S*0.038, S*0.36), fill=(249, 251, 255, 255))
d.rounded_rectangle((S*0.24, S*0.30, S*0.76, S*0.36), radius=int(S*0.03), fill=(240, 247, 255, 255))

# face
d.ellipse((S*0.27, S*0.28, S*0.73, S*0.72), fill=(255, 229, 216, 255))
# bangs
d.pieslice((S*0.27, S*0.18, S*0.73, S*0.62), 180, 360, fill=(58, 124, 235, 255))
d.polygon([(S*0.27, S*0.43), (S*0.40, S*0.34), (S*0.36, S*0.56)], fill=(58, 124, 235, 255))
d.polygon([(S*0.73, S*0.43), (S*0.60, S*0.34), (S*0.64, S*0.56)], fill=(58, 124, 235, 255))
d.polygon([(S*0.45, S*0.36), (S*0.52, S*0.30), (S*0.55, S*0.58), (S*0.48, S*0.50)], fill=(70, 139, 246, 255))

# eyes
for ex in (0.405, 0.595):
    d.ellipse((S*(ex-0.052), S*0.455, S*(ex+0.052), S*0.585), fill=(250, 253, 255, 255))
    d.ellipse((S*(ex-0.043), S*0.468, S*(ex+0.043), S*0.578), fill=(31, 88, 202, 255))
    d.ellipse((S*(ex-0.030), S*0.490, S*(ex+0.030), S*0.560), fill=(12, 35, 92, 255))
    d.ellipse((S*(ex-0.032), S*0.478, S*(ex-0.008), S*0.506), fill=(255, 255, 255, 255))
    d.ellipse((S*(ex+0.008), S*0.535, S*(ex+0.026), S*0.552), fill=(176, 234, 255, 255))

# blush
d.ellipse((S*0.30, S*0.565, S*0.365, S*0.605), fill=(255, 148, 170, 150))
d.ellipse((S*0.635, S*0.565, S*0.70, S*0.605), fill=(255, 148, 170, 150))
# mouth
d.arc((S*0.465, S*0.565, S*0.535, S*0.635), 10, 170, fill=(120, 54, 66, 255), width=8)

# whale mark on apron
d.ellipse((S*0.44, S*0.70, S*0.56, S*0.77), fill=(45, 112, 228, 255))
d.polygon([(S*0.44, S*0.735), (S*0.40, S*0.715), (S*0.42, S*0.755)], fill=(45, 112, 228, 255))
d.ellipse((S*0.47, S*0.722, S*0.485, S*0.736), fill=(255, 255, 255, 255))
d.arc((S*0.46, S*0.748, S*0.54, S*0.785), 10, 150, fill=(255, 255, 255, 255), width=5)

# ahoge
d.arc((S*0.49, S*0.045, S*0.62, S*0.20), 120, 350, fill=(58, 124, 235, 255), width=18)

# bubble highlights
for (x, y, r) in [(0.12, 0.18, 0.025), (0.88, 0.22, 0.018), (0.10, 0.72, 0.014), (0.90, 0.62, 0.022)]:
    d.ellipse((S*(x-r), S*(y-r), S*(x+r), S*(y+r)), outline=(173, 233, 255, 140), width=5)

img.save("assets/logo.png")
sizes = [(16,16),(24,24),(32,32),(48,48),(64,64),(128,128),(256,256)]
img.save("assets/fish.ico", sizes=sizes)
print("saved")
