"""Place actual Blender output on white and beside the attached reference."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE=Path(__file__).resolve().parent
font=ImageFont.truetype('C:/Windows/Fonts/segoeuib.ttf',24)
small=ImageFont.truetype('C:/Windows/Fonts/segoeui.ttf',17)
with Image.open(HERE/'ReferenceAdventurer.png') as source: sprite=source.convert('RGBA')
white=Image.new('RGBA',sprite.size,'white');white.alpha_composite(sprite)
white.convert('RGB').save(HERE/'ReferenceAdventurer-preview.png')
reference=Image.open(HERE/'reference.png').convert('RGBA').crop((211,0,521,710))
board=Image.new('RGBA',(1024,1090),'#F3F0E8');draw=ImageDraw.Draw(board)
draw.text((30,24),'REFERENCE',fill='#43372B',font=font)
draw.text((538,24),'BLENDER RECONSTRUCTION',fill='#43372B',font=font)
draw.text((30,61),'Central character from the supplied image',fill='#766B60',font=small)
draw.text((538,61),'Editable meshes / flat shaded / orthographic',fill='#766B60',font=small)
cropped=sprite.crop(sprite.getchannel('A').getbbox())
for x,im in ((20,reference),(530,cropped)):
    draw.rectangle((x,105,x+474,1070),fill='white')
    scale=min(454/im.width,935/im.height)
    im=im.resize((round(im.width*scale),round(im.height*scale)),Image.Resampling.LANCZOS)
    board.alpha_composite(im,(x+(474-im.width)//2,110+(950-im.height)//2))
board.convert('RGB').save(HERE/'reference-comparison.png')
print('PREVIEWS_WRITTEN')
