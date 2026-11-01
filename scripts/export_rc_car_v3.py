"""Run through Blender MCP with IronTeethRcCar_V3.blend open.

Export visible model geometry in Blender coordinates for the runtime OBJ parser,
which swaps Y/Z and reverses triangle winding for Unity. The source stays intact.
"""
import bpy
import os

if os.path.basename(bpy.data.filepath) != 'IronTeethRcCar_V3.blend':
    raise RuntimeError('Open IronTeethRcCar_V3.blend before exporting the runtime mesh.')

root = bpy.data.objects['Iron Teeth RC Car V3']
def belongs_to_car(obj):
    # V2 upgrade objects were stored in their collection without a root parent.
    if any(col.name == 'V2 Upgrade Details' for col in obj.users_collection):
        return True
    parent = obj.parent
    while parent:
        if parent == root:
            return True
        parent = parent.parent
    return False

objects = [obj for obj in bpy.context.scene.objects
           if obj.type in {'MESH', 'CURVE', 'FONT'}
           and not obj.hide_render and belongs_to_car(obj)]
if not objects:
    raise RuntimeError('V3 has no visible model geometry.')
bpy.ops.object.select_all(action='DESELECT')
for obj in objects:
    obj.hide_set(False)
    obj.select_set(True)
bpy.context.view_layer.objects.active = objects[0]
# Convert pipework and labels, and evaluate bevel/solidify modifiers in memory.
bpy.ops.object.convert(target='MESH')
path = os.path.join(os.path.dirname(bpy.data.filepath), 'IronTeethRcCar_V3.obj')
bpy.ops.wm.obj_export(
    filepath=path, check_existing=False, export_selected_objects=True,
    forward_axis='Y', up_axis='Z', apply_modifiers=True,
    export_eval_mode='DAG_EVAL_RENDER', export_normals=True, export_uv=False,
    export_materials=True, export_pbr_extensions=True,
    export_triangulated_mesh=True, export_curves_as_nurbs=False,
    export_object_groups=True,
)
result = {'path': path, 'objects': len(objects), 'bytes': os.path.getsize(path)}
