bl_info = {
    "name": "Celeste 3D Engine Exports",
    "author": "Eylstar",
    "version": (1, 2),
    "blender": (5, 0, 0),
    "location": "View3D > Sidebar > Celeste3DEngine",
    "description": "Exports object models, placements or animations as JSON for Celeste3DEngine",
    "category": "Import-Export",
}

import os
import bpy
import json
import mathutils


# MATRIX UTILS

BLENDER_TO_ENGINE = mathutils.Matrix((
    (1, 0, 0, 0),
    (0, 0, 1, 0),
    (0, -1, 0, 0),
    (0, 0, 0, 1),
))

def convert_matrix(mat):
    return BLENDER_TO_ENGINE @ mat @ BLENDER_TO_ENGINE.inverted()

def get_all_mesh_objects_recursive(collection):
    result = []
    for obj in collection.objects:
        if obj.type == 'MESH':
            result.append(obj)
    for child_collection in collection.children:
        result.extend(get_all_mesh_objects_recursive(child_collection))
    return result


# ANIMATION ACTION GETTER (FIRST NLA TRACK FALLBACK)

def get_action_and_range(obj):
    if obj.animation_data is None:
        return None, None

    if obj.animation_data.action is not None:
        action = obj.animation_data.action
        return action, action.frame_range

    for track in obj.animation_data.nla_tracks:
        for strip in track.strips:
            if strip.action is not None:
                return strip.action, (strip.frame_start, strip.frame_end)

    return None, None


# ANIMATION DATA EXPORT

def export_animation(obj, fps, loop, output_path):
    action, frame_range = get_action_and_range(obj)

    if action is None:
        return None, "Selected object has no animation data (no active action or NLA strip found)"

    scene = bpy.context.scene
    frame_start, frame_end = int(frame_range[0]), int(frame_range[1])

    original_frame = scene.frame_current

    pos_keys, rot_keys, scale_keys = [], [], []

    for frame in range(frame_start, frame_end + 1):
        scene.frame_set(frame)

        mat = convert_matrix(obj.matrix_basis)
        loc, rot, scl = mat.decompose()
        time = (frame - frame_start) / scene.render.fps

        pos_keys.append({"time": time, "value": [loc.x, loc.y, loc.z]})
        rot_keys.append({"time": time, "value": [rot.x, rot.y, rot.z, rot.w]})
        scale_keys.append({"time": time, "value": [scl.x, scl.y, scl.z]})

    scene.frame_set(original_frame)

    data = {
        "name": action.name,
        "duration": (frame_end - frame_start) / scene.render.fps,
        "loop": loop,
        "positionKeyframes": pos_keys,
        "rotationKeyframes": rot_keys,
        "scaleKeyframes" : scale_keys
    }

    with open(output_path, "w") as f:
        json.dump(data, f, indent=2)

    return len(pos_keys), None


# MODEL EXPORT (GLB), only Base Color and Normal are ever kept, everything else on the
# Principled BSDF (Metallic, Roughness, AO, etc.) is stripped automatically before export

def strip_unwanted_channels(obj):
    keep_inputs = {"Base Color", "Normal"}
    removed_links = []

    for slot in obj.material_slots:
        mat = slot.material
        if mat is None or not mat.use_nodes:
            continue

        principled = None
        for node in mat.node_tree.nodes:
            if node.type == 'BSDF_PRINCIPLED':
                principled = node
                break
        if principled is None:
            continue

        for input_socket in principled.inputs:
            if input_socket.name in keep_inputs:
                continue
            if not input_socket.is_linked:
                continue

            link = input_socket.links[0]
            removed_links.append((mat, link.from_socket, input_socket))
            mat.node_tree.links.remove(link)

    return removed_links


def restore_channels(removed_links):
    for mat, from_socket, to_socket in removed_links:
        mat.node_tree.links.new(from_socket, to_socket)


def export_model_glb(obj, output_path, apply_transform, compress_jpeg):
    original_selection = list(bpy.context.selected_objects)
    original_active = bpy.context.view_layer.objects.active

    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj

    if apply_transform:
        original_location = obj.location.copy()
        original_scale = obj.scale.copy()
        original_rot_mode = obj.rotation_mode
        original_rotation = (
            obj.rotation_quaternion.copy() if original_rot_mode == 'QUATERNION'
            else obj.rotation_euler.copy()
        )

        obj.location = (0, 0, 0)
        obj.scale = (1, 1, 1)
        if original_rot_mode == 'QUATERNION':
            obj.rotation_quaternion = (1, 0, 0, 0)
        else:
            obj.rotation_euler = (0, 0, 0)

    removed_links = strip_unwanted_channels(obj)

    try:
        bpy.ops.export_scene.gltf(
            filepath=output_path,
            export_format='GLB',
            use_selection=True,
            export_apply=True,
            export_image_format='JPEG' if compress_jpeg else 'AUTO',
            export_vertex_color='NONE',
            export_extras=False,
        )
    finally:
        restore_channels(removed_links)

    if apply_transform:
        obj.location = original_location
        obj.scale = original_scale
        if original_rot_mode == 'QUATERNION':
            obj.rotation_quaternion = original_rotation
        else:
            obj.rotation_euler = original_rotation

    bpy.ops.object.select_all(action='DESELECT')
    for o in original_selection:
        o.select_set(True)
    bpy.context.view_layer.objects.active = original_active


# ANIMAITON PANEL

class C3D_OT_ExportAnimation(bpy.types.Operator):
    bl_idname = "c3d.export_animation"
    bl_label = "Export Animation"
    bl_description = "Export the selected object's animation as Transform keyframes"

    def execute(self, context):
        props = context.scene.c3d_anim_export_props

        if props.target_object is None:
            self.report({'ERROR'}, "No object selected")
            return {'CANCELLED'}

        if not props.output_path:
            self.report({'ERROR'}, "No output path set")
            return {'CANCELLED'}

        file_name = props.output_file_name.strip() if props.output_file_name else "animation"
        if not file_name.endswith(".json"):
            file_name += ".json"

        folder = bpy.path.abspath(props.output_path)
        os.makedirs(folder, exist_ok=True)
        output_path = os.path.join(folder, file_name)

        count, error = export_animation(props.target_object, context.scene.render.fps, props.loop, output_path)

        if error:
            self.report({'ERROR'}, error)
            return {'CANCELLED'}

        self.report({'INFO'}, f"Exported {count} keyframes to {output_path}")
        return {'FINISHED'}



class C3D_OT_ExportPlacements(bpy.types.Operator):
    bl_idname = "c3d.export_placements"
    bl_label = "Export Placements"
    bl_description = "Export the selected collection's transforms as JSON"

    def execute(self, context):
        props = context.scene.c3d_export_props

        if props.collection is None:
            self.report({'ERROR'}, "No collection selected")
            return {'CANCELLED'}

        if not props.output_path:
            self.report({'ERROR'}, "No output path set")
            return {'CANCELLED'}

        file_name = props.output_file_name.strip() if props.output_file_name else "placements"

        if not file_name.endswith(".json"):
            file_name += ".json"

        folder = bpy.path.abspath(props.output_path)
        os.makedirs(folder, exist_ok=True)

        output_path = os.path.join(folder, file_name)

        placements = []
        for obj in get_all_mesh_objects_recursive(props.collection):
            #if obj.type != 'MESH':
                #continue

            converted = convert_matrix(obj.matrix_world)
            loc, rot, scale = converted.decompose()

            placements.append({
                "model": obj.name.split(".")[0],
                "position": [loc.x, loc.y, loc.z],
                "rotation": [rot.x, rot.y, rot.z, rot.w],
                "scale": [scale.x, scale.y, scale.z]
            })

        with open(output_path, "w") as f:
            json.dump(placements, f, indent=2)

        self.report({'INFO'}, f"Exported {len(placements)} placements")
        return {'FINISHED'}



class C3D_OT_ExportModel(bpy.types.Operator):
    bl_idname = "c3d.export_model"
    bl_label = "Export Model"
    bl_description = "Export the selected object as a GLB"

    def execute(self, context):
        props = context.scene.c3d_model_export_props

        if props.target_object is None:
            self.report({'ERROR'}, "No object selected")
            return {'CANCELLED'}

        if not props.output_path:
            self.report({'ERROR'}, "No output path set")
            return {'CANCELLED'}

        file_name = props.output_file_name.strip() if props.output_file_name else props.target_object.name.split(".")[0]
        if not file_name.endswith(".glb"):
            file_name += ".glb"

        folder = bpy.path.abspath(props.output_path)
        os.makedirs(folder, exist_ok=True)
        output_path = os.path.join(folder, file_name)

        export_model_glb(props.target_object, output_path, props.apply_transform, props.compress_jpeg)

        self.report({'INFO'}, f"Exported model to {output_path}")
        return {'FINISHED'}


# EXPORT PANELS

class C3D_PT_ExportPanel(bpy.types.Panel):
    bl_label = "C3DEngine Placements Export"
    bl_idname = "C3D_PT_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "C3DEngine"
    bl_order = 1

    def draw(self, context):
        layout = self.layout
        props = context.scene.c3d_export_props

        layout.prop(props, "collection")
        layout.prop(props, "output_path")
        layout.prop(props, "output_file_name")
        layout.operator("c3d.export_placements")


class C3D_PT_ModelExportPanel(bpy.types.Panel):
    bl_label = "C3DEngine Model Export"
    bl_idname = "C3D_PT_model_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "C3DEngine"
    bl_order = 0

    def draw(self, context):
        layout = self.layout
        props = context.scene.c3d_model_export_props

        layout.prop(props, "target_object")
        layout.prop(props, "apply_transform")
        layout.prop(props, "compress_jpeg")
        layout.prop(props, "output_path")
        layout.prop(props, "output_file_name")
        layout.operator("c3d.export_model")


class C3D_PT_AnimationExportPanel(bpy.types.Panel):
    bl_label = "C3DEngine Animation Export"
    bl_idname = "C3D_PT_animation_export_panel"
    bl_space_type = 'VIEW_3D'
    bl_region_type = 'UI'
    bl_category = "C3DEngine"
    bl_order = 2

    def draw(self, context):
        layout = self.layout
        props = context.scene.c3d_anim_export_props

        layout.prop(props, "target_object")
        layout.prop(props, "loop")
        layout.prop(props, "output_path")
        layout.prop(props, "output_file_name")
        layout.operator("c3d.export_animation")




# EXPORT PROPERTIES

class C3D_ExportProperties(bpy.types.PropertyGroup):
    collection: bpy.props.PointerProperty(
        name="Collection",
        type=bpy.types.Collection
    )
    output_path: bpy.props.StringProperty(
        name="Output Folder",
        subtype='DIR_PATH'
    )
    output_file_name: bpy.props.StringProperty(
        name="File Name"
    )


class C3D_AnimExportProperties(bpy.types.PropertyGroup):
    target_object: bpy.props.PointerProperty(
        name="Object",
        type=bpy.types.Object
    )
    loop: bpy.props.BoolProperty(
        name="Loop",
        default=True
    )
    output_path: bpy.props.StringProperty(
        name="Output Folder",
        subtype='DIR_PATH'
    )
    output_file_name: bpy.props.StringProperty(
        name="File Name"
    )


class C3D_ModelExportProperties(bpy.types.PropertyGroup):
    target_object: bpy.props.PointerProperty(
        name="Object",
        type=bpy.types.Object
    )
    apply_transform: bpy.props.BoolProperty(
        name="Origin Placement",
        description="Apply Transform to origin before exporting, so the model can be freely repositioned via code. Uncheck to export with its current transform",
        default=True
    )
    compress_jpeg: bpy.props.BoolProperty(
        name="Compress Textures (JPEG)",
        description="Export textures as JPEG instead of PNG, much smaller file size for photographic textures like wood or stone. Textures using transparency are still exported as PNG regardless of this setting",
        default=True
    )
    output_path: bpy.props.StringProperty(
        name="Output Folder",
        subtype='DIR_PATH'
    )
    output_file_name: bpy.props.StringProperty(
        name="File Name"
    )




classes = (
    C3D_OT_ExportModel, C3D_PT_ModelExportPanel, C3D_ModelExportProperties,
    C3D_OT_ExportPlacements, C3D_PT_ExportPanel, C3D_ExportProperties,
    # C3D_OT_ExportAnimation, C3D_PT_AnimationExportPanel, C3D_AnimExportProperties,
)

def register():
    for cls in classes:
        bpy.utils.register_class(cls)
    bpy.types.Scene.c3d_export_props = bpy.props.PointerProperty(type=C3D_ExportProperties)
    # bpy.types.Scene.c3d_anim_export_props = bpy.props.PointerProperty(type=C3D_AnimExportProperties)
    bpy.types.Scene.c3d_model_export_props = bpy.props.PointerProperty(type=C3D_ModelExportProperties)

def unregister():
    for cls in classes:
        bpy.utils.unregister_class(cls)
    del bpy.types.Scene.c3d_export_props
    # del bpy.types.Scene.c3d_anim_export_props
    del bpy.types.Scene.c3d_model_export_props

if __name__ == "__main__":
    register()