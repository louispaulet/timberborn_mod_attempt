"""Run in Blender with the V2 .blend open; creates a separate V3 and matched views.

Uses only procedural materials and native geometry; V2 is never overwritten.
Invoke through Blender MCP execute_blender_code_for_cli with exec(open(...).read()).
"""
import bpy
import math
import os
from mathutils import Vector

REPO = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
ASSETS = os.path.join(REPO, 'src/RcCar.Mod/Assets')
OUT = os.path.join(REPO, 'verification/rc_car_v3')
os.makedirs(OUT, exist_ok=True)
scene = bpy.context.scene
if os.path.basename(bpy.data.filepath) != 'IronTeethRcCar_V2.blend':
    raise RuntimeError('Open the original IronTeethRcCar_V2.blend before generating V3.')
scene.render.engine = 'CYCLES'
scene.cycles.samples = 32
scene.cycles.use_denoising = True
scene.render.resolution_x = 1100
scene.render.resolution_y = 1000
scene.render.resolution_percentage = 100
scene.render.image_settings.file_format = 'PNG'
camera = scene.camera
camera.data.type = 'ORTHO'
camera.data.ortho_scale = 1.93
camera.data.clip_start = .01
camera.data.clip_end = 1000

def aim(obj, point):
    obj.rotation_euler = (Vector(point) - obj.location).to_track_quat('-Z', 'Y').to_euler()

def render(version):
    paths = []
    for view, loc in [('front', (2.5, -3.4, 2.15)), ('rear', (2.5, 3.4, 2.15))]:
        camera.location = loc
        aim(camera, (0, -.015, .17))
        scene.render.filepath = os.path.join(OUT, f'{version}_{view}.png')
        bpy.ops.render.render(write_still=True)
        paths.append(scene.render.filepath)
    return paths

baseline = render('v2')
root = bpy.data.objects['Iron Teeth RC Car']
root.name = 'Iron Teeth RC Car V3'
details = bpy.data.collections.new('V3 Iron Teeth engineering')
scene.collection.children.link(details)

def material(name, color, metal=0, rough=.5, emission=0, texture=False, wood=False):
    m = bpy.data.materials.new(name)
    m.diffuse_color = (*color, 1)
    m.use_nodes = True
    n = m.node_tree.nodes
    links = m.node_tree.links
    p = n.get('Principled BSDF')
    p.inputs['Base Color'].default_value = (*color, 1)
    p.inputs['Metallic'].default_value = metal
    p.inputs['Roughness'].default_value = rough
    if emission:
        p.inputs['Emission Color'].default_value = (*color, 1)
        p.inputs['Emission Strength'].default_value = emission
    if texture or wood:
        coord = n.new('ShaderNodeTexCoord')
        mapping = n.new('ShaderNodeVectorMath')
        mapping.operation = 'MULTIPLY'
        mapping.inputs[1].default_value = (35, 2, 16) if wood else (1, 1, 1)
        links.new(coord.outputs['Generated'], mapping.inputs[0])
        noise = n.new('ShaderNodeTexNoise')
        noise.inputs['Scale'].default_value = 5 if wood else 38
        noise.inputs['Detail'].default_value = 3
        links.new(mapping.outputs[0], noise.inputs['Vector'])
        ramp = n.new('ShaderNodeValToRGB')
        ramp.color_ramp.elements[0].position = .22
        ramp.color_ramp.elements[0].color = (*[v * .34 for v in color], 1)
        ramp.color_ramp.elements[1].position = .8
        ramp.color_ramp.elements[1].color = (*color, 1)
        links.new(noise.outputs['Fac'], ramp.inputs[0])
        links.new(ramp.outputs[0], p.inputs['Base Color'])
        bump = n.new('ShaderNodeBump')
        bump.inputs['Strength'].default_value = .24
        bump.inputs['Distance'].default_value = .002 if wood else .00025
        links.new(noise.outputs['Fac'], bump.inputs['Height'])
        links.new(bump.outputs[0], p.inputs['Normal'])
    return m

steel = material('V3 worn blue gunmetal', (.115, .145, .16), .78, .38, texture=True)
dark = material('V3 charcoal cast iron', (.035, .043, .047), .65, .52, texture=True)
brass = material('V3 aged brass', (.49, .29, .09), .8, .37, texture=True)
rust = material('V3 oxidised iron', (.25, .071, .025), .48, .65, texture=True)
wood = material('V3 sawn warm oak', (.32, .145, .049), 0, .67, wood=True)
wood_light = material('V3 honey timber', (.44, .245, .095), 0, .61, wood=True)
green = material('V3 green extract', (.13, .61, .022), .12, .22, .5)
red = material('V3 red badwater', (.58, .018, .012), .12, .23, .4)
amber = material('V3 warm lamp glass', (1, .49, .10), .15, .19, 2)
ivory = material('V3 enamel lettering', (.78, .67, .44), .1, .5)
glass = bpy.data.materials.new('V3 clear chemical vessel')
glass.use_nodes = True
nodes = glass.node_tree.nodes
nodes.clear()
out = nodes.new('ShaderNodeOutputMaterial')
mix = nodes.new('ShaderNodeMixShader')
mix.inputs[0].default_value = .12
trans = nodes.new('ShaderNodeBsdfTransparent')
gloss = nodes.new('ShaderNodeBsdfPrincipled')
gloss.inputs['Base Color'].default_value = (.7, .9, .85, 1)
gloss.inputs['Metallic'].default_value = .25
gloss.inputs['Roughness'].default_value = .16
glass.node_tree.links.new(trans.outputs[0], mix.inputs[1])
glass.node_tree.links.new(gloss.outputs[0], mix.inputs[2])
glass.node_tree.links.new(mix.outputs[0], out.inputs[0])

for obj in list(scene.objects):
    if obj.type != 'MESH' or obj.name == 'Studio ground':
        continue
    for slot in obj.material_slots:
        if not slot.material:
            continue
        name = slot.material.name
        if 'WoodLight' in name:
            slot.material = wood_light
        elif 'DarkWood' in name:
            slot.material = wood
        elif 'RustOrange' in name:
            slot.material = steel
        elif 'Gunmetal' in name or 'WeatheredSteel' in name:
            slot.material = steel
        elif 'Brass' in name:
            slot.material = brass
        elif 'Charcoal' in name or 'Exhaust' in name:
            slot.material = dark
        elif 'TealGlass' in name:
            slot.material = amber
    if obj.name.startswith('V2 chemical cell') or obj.name.startswith('Bonnet pinstripe') or obj.name == 'Sloped bonnet' or obj.name.startswith('Gear emblem'):
        obj.hide_render = True
        obj.hide_set(True)

def finish(obj, name, mat, bevel=0):
    obj.name = 'V3 ' + name
    for col in list(obj.users_collection):
        col.objects.unlink(obj)
    details.objects.link(obj)
    obj.parent = root
    obj.data.materials.append(mat)
    if bevel:
        mod = obj.modifiers.new('Soft forged edges', 'BEVEL')
        mod.width = bevel
        mod.segments = 3
        obj.modifiers.new('Weighted normals', 'WEIGHTED_NORMAL')
    return obj

def box(name, loc, size, mat, bevel=.003):
    bpy.ops.mesh.primitive_cube_add(size=1, location=loc)
    obj = bpy.context.object
    obj.scale = size
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    return finish(obj, name, mat, bevel)

def cyl(name, loc, radius, depth, mat, axis=(0, 0, 1), vertices=32):
    bpy.ops.mesh.primitive_cylinder_add(vertices=vertices, radius=radius, depth=depth, location=loc)
    obj = bpy.context.object
    obj.rotation_euler = Vector(axis).to_track_quat('Z', 'Y').to_euler()
    finish(obj, name, mat, .001)
    for p in obj.data.polygons:
        p.use_smooth = len(p.vertices) == 4
    return obj

def pipe(name, points, radius, mat):
    curve = bpy.data.curves.new('V3 ' + name, 'CURVE')
    curve.dimensions = '3D'
    curve.bevel_depth = radius
    curve.bevel_resolution = 3
    poly = curve.splines.new('POLY')
    poly.points.add(len(points) - 1)
    for v, point in zip(poly.points, points):
        v.co = (*point, 1)
    obj = bpy.data.objects.new('V3 ' + name, curve)
    details.objects.link(obj)
    obj.parent = root
    curve.materials.append(mat)
    return obj

def ring(name, loc, major, minor, mat, axis=(0, 0, 1)):
    bpy.ops.mesh.primitive_torus_add(major_radius=major, minor_radius=minor, major_segments=40, minor_segments=8, location=loc)
    obj = bpy.context.object
    obj.rotation_euler = Vector(axis).to_track_quat('Z', 'Y').to_euler()
    return finish(obj, name, mat)

def text(name, body, loc, size, mat, rear=False):
    data = bpy.data.curves.new(name, 'FONT')
    data.body = body
    data.size = size
    data.align_x = 'CENTER'
    data.align_y = 'CENTER'
    data.extrude = .0002
    obj = bpy.data.objects.new('V3 ' + name, data)
    details.objects.link(obj)
    obj.location = loc
    obj.rotation_euler = (math.pi / 2, 0, math.pi if rear else 0)
    obj.parent = root
    data.materials.append(mat)
    return obj

# Planked service bonnet, metal spine, hinges and fasteners.
for i in range(5):
    x = (i - 2) * .081
    box('bonnet oak plank', (x, -.31, .242), (.077, .397, .028), wood_light if i % 2 else wood)
for y in (-.46, -.16):
    box('bonnet steel strap', (0, y, .263), (.423, .022, .009), steel)
    for x in (-.185, -.09, .09, .185):
        cyl('bonnet strap rivet', (x, y, .270), .005, .005, brass, vertices=12)
box('bonnet central spine', (0, -.31, .269), (.018, .39, .008), brass)
ring('hood gear medallion', (0, -.33, .283), .039, .005, brass)
for i in range(12):
    a = i * math.tau / 12
    ob = box('hood cog tooth', (.047 * math.sin(a), -.33 + .047 * math.cos(a), .283), (.012, .015, .01), brass, .001)
    ob.rotation_euler.z = -a
cyl('hood dark enamel', (0, -.33, .281), .032, .007, dark)

# Taller sealed chemical cells, visible liquid headspace and safety cages.
for x, fluid, label in [(-.112, green, 'EXTRACT'), (.112, red, 'BADWATER')]:
    y = .31
    cyl(label + ' cast mount', (x, y, .337), .077, .027, dark)
    cyl(label + ' lower cap', (x, y, .362), .068, .022, brass)
    cyl(label + ' liquid', (x, y, .442), .053, .144 if x < 0 else .12, fluid)
    cyl(label + ' glass', (x, y, .461), .061, .190, glass)
    for z in (.376, .53):
        ring(label + ' retaining hoop', (x, y, z), .065, .004, brass)
    cyl(label + ' sealed lid', (x, y, .565), .069, .024, steel)
    cyl(label + ' terminal insulator', (x, y, .587), .024, .023, dark)
    cyl(label + ' brass terminal', (x, y, .604), .011, .015, brass)
    for a in [math.pi / 4 + n * math.pi / 2 for n in range(4)]:
        px, py = x + .066 * math.cos(a), y + .066 * math.sin(a)
        cyl(label + ' cage tie rod', (px, py, .461), .0035, .208, steel, vertices=12)
        cyl(label + ' cap nut', (px, py, .584), .006, .01, brass, vertices=6)
    box(label + ' rear plate', (x, .381, .356), (.105, .009, .026), dark)
    text(label + ' label', label, (x, .387, .356), .012, ivory, rear=True)
    pipe(label + ' insulated feed', [(x, y, .613), (x, .22, .634), (x * 1.85, .155, .54), (x * 1.85, .155, .285)], .007, dark)
    for z in (.39, .45, .51):
        box(label + ' level mark', (x + .04, .251, z), (.013, .003, .003), ivory, .0005)
    pipe(label + ' rear outlet', [(x, .35, .365), (x, .446, .36), (x, .466, .29), (x, .466, .25)], .009, brass)

# Rear manifold, two analog gauges and small valve wheels.
pipe('rear fluid manifold', [(-.15, .472, .265), (.15, .472, .265)], .014, steel)
for x in (-.112, .112):
    cyl('pressure gauge casing', (x, .49, .302), .029, .018, brass, (0, 1, 0))
    cyl('pressure gauge enamel', (x, .501, .302), .023, .003, ivory, (0, 1, 0))
    pipe('gauge needle', [(x, .505, .302), (x + .012, .505, .313)], .0016, dark)
    for i in range(7):
        a = i * math.pi / 4
        pipe('gauge scale', [(x + .018 * math.cos(a), .505, .302 + .018 * math.sin(a)), (x + .021 * math.cos(a), .505, .302 + .021 * math.sin(a))], .0008, dark)
    ring('manifold valve', (x, .482, .231), .023, .003, rust, (0, 1, 0))
    for a in (0, math.pi / 2):
        pipe('valve spoke', [(x - .02 * math.cos(a), .482, .231 - .02 * math.sin(a)), (x + .02 * math.cos(a), .482, .231 + .02 * math.sin(a))], .002, rust)

# Front recovery winch with wound cable and a raised protective bumper rail.
box('winch bed', (0, -.67, -.015), (.29, .13, .025), steel)
cyl('winch drum', (0, -.68, .025), .033, .17, dark, (1, 0, 0))
for i in range(17):
    ring('winch cable winding', (-.075 + i * .009, -.68, .025), .033, .0035, steel, (1, 0, 0))
for x in (-.104, .104):
    cyl('winch end flange', (x, -.68, .025), .046, .014, brass, (1, 0, 0))
cyl('winch motor', (.15, -.68, .025), .03, .07, steel, (1, 0, 0))
pipe('front bull bar', [(-.32, -.72, -.06), (-.30, -.75, .09), (-.25, -.76, .115), (.25, -.76, .115), (.30, -.75, .09), (.32, -.72, -.06)], .012, steel)
pipe('winch hanging cable', [(0, -.715, .022), (0, -.78, -.005), (0, -.78, -.065)], .003, steel)
ring('recovery hook', (0, -.78, -.087), .019, .005, brass, (0, 1, 0))

# Lamp guards and industrial running boards; retain the open gear pairs.
for side in (-1, 1):
    x = side * .19
    for dx in (-.019, 0, .019):
        pipe('lamp vertical guard', [(x + dx, -.619, .045), (x + dx, -.621, .105)], .0022, dark)
    pipe('lamp horizontal guard', [(x - .03, -.622, .075), (x + .03, -.622, .075)], .0022, dark)
    box('timber running board', (side * .332, 0, -.075), (.075, .35, .021), wood_light)
    for y in (-.14, .14):
        box('running board steel clamp', (side * .332, y, -.06), (.08, .017, .009), steel)
    pipe('gear protection rail', [(side * .334, -.19, .035), (side * .35, -.15, .17), (side * .35, .18, .17), (side * .334, .22, .035)], .007, brass)
    for y in (-.425, .425):
        # Broad steel fender crowns over the original segmented wheel guards.
        verts, faces = [], []
        for j in range(17):
            a = -.16 + j * (math.pi + .32) / 16
            for xx in (side * .25, side * .405):
                verts.append((xx, y + .219 * math.cos(a), -.075 + .219 * math.sin(a)))
        for j in range(16):
            faces.append((2*j, 2*j+1, 2*j+3, 2*j+2))
        mesh = bpy.data.meshes.new('V3 rolled fender')
        mesh.from_pydata(verts, [], faces)
        for polygon in mesh.polygons:
            polygon.use_smooth = True
        obj = bpy.data.objects.new('V3 rolled fender', mesh)
        details.objects.link(obj)
        obj.parent = root
        mesh.materials.append(steel)
        mod = obj.modifiers.new('Rolled steel thickness', 'SOLIDIFY')
        mod.thickness = .006
        mod = obj.modifiers.new('Rounded fender edge', 'BEVEL')
        mod.width = .003
        mod.segments = 2

# Rear tail lamps, hinge hardware, tow eye and receiver coil on the original antenna.
for x in (-.235, .235):
    box('tail lamp bracket', (x, .605, .145), (.047, .04, .07), dark)
    cyl('red rear lamp', (x, .632, .158), .016, .012, red, (0, 1, 0))
    box('tailgate hinge leaf', (x * .58, .667, .065), (.054, .008, .024), steel)
    cyl('tailgate hinge pin', (x * .58, .675, .06), .007, .064, brass, (1, 0, 0))
ring('rear recovery eye', (0, .718, -.075), .022, .006, brass, (0, 1, 0))
# Offset receiver so the mast stays legible from both comparison angles.
for obj in list(scene.objects):
    if obj.name.startswith('V2 RC'):
        obj.location.x += .17
        if 'radio antenna mast' in obj.name:
            obj.scale.z *= 1.28
            obj.location.z += .04
        elif 'ferrule' in obj.name or 'antenna brass tip' in obj.name:
            obj.location.z = .415 + (obj.location.z - .415) * 1.28
coil = [( .17 + .015 * math.cos(i * math.tau / 20), .505 + .015 * math.sin(i * math.tau / 20), .418 + i * .00065) for i in range(101)]
pipe('antenna spring base', coil, .002, brass)
box('rear serial plaque', (0, .669, .19), (.12, .007, .022), dark)
text('serial lettering', 'ITM / RC-03', (0, .674, .19), .012, ivory, rear=True)

scene['design_version'] = 'V3 — Iron Teeth chemical recovery scout'
scene['source_model'] = 'IronTeethRcCar_V2.blend; original file preserved'
scene['design_notes'] = 'Sawn oak, worn steel, extract/badwater cells, gauges, antenna, exposed gears, winch and lamp guards.'
camera.location = (2.5, -3.4, 2.15)
aim(camera, (0, -.015, .17))
bpy.ops.object.select_all(action='DESELECT')
root.select_set(True)
bpy.context.view_layer.objects.active = root
bpy.context.view_layer.update()
for area in bpy.context.screen.areas:
    if area.type == 'VIEW_3D':
        area.spaces.active.region_3d.view_perspective = 'CAMERA'
        area.spaces.active.shading.color_type = 'MATERIAL'
blend_path = os.path.join(ASSETS, 'IronTeethRcCar_V3.blend')
bpy.ops.wm.save_as_mainfile(filepath=blend_path)
updated = render('v3')
result = {'blend': blend_path, 'renders': baseline + updated, 'v3_objects': len(details.objects), 'scene_objects': len(scene.objects)}
