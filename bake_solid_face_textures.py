from PIL import Image

SKIN_COLOR = (252, 229, 212, 255) # RGB (0.99, 0.90, 0.83)

faces = [
    "Assets/_Game/Textures/Characters/tex_mother_face.png",
    "Assets/_Game/Textures/Characters/tex_child_face.png",
    "Assets/_Game/Textures/Characters/tex_hunter_face.png"
]

for path in faces:
    im = Image.open(path).convert("RGBA")
    # Create background with skin tone
    bg = Image.new("RGBA", im.size, SKIN_COLOR)
    # Composite face features on top of skin background
    combined = Image.alpha_composite(bg, im)
    combined.save(path)
    print(f"Baked solid skin background into {path}")

